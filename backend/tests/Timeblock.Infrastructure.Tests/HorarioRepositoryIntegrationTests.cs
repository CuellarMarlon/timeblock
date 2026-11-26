using Microsoft.EntityFrameworkCore;
using Npgsql;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Horarios;
using Timeblock.Application.Personas;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Common;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Personas;
using Timeblock.Infrastructure.Persistence;

namespace Timeblock.Infrastructure.Tests;

[CollectionDefinition("Horarios PostgreSQL", DisableParallelization = true)]
public sealed class HorariosPostgreSqlCollection;

[Collection("Horarios PostgreSQL")]
public sealed class HorarioRepositoryIntegrationTests
{
    [PostgreSqlFact]
    public async Task EliminarAutorizacion_SinUsoElimina_ConHorarioProtegeReferencia()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        Escenario e = await CrearEscenarioAsync(db);
        var personas = new PersonasService(new PersonaRepository(db));
        var fechaSinUso = e.InicioAutorizacion.AddDays(-1);
        await personas.CrearAutorizacionAsync(e.Coordinador, e.Programa.Id, fechaSinUso, null, true, false, CancellationToken.None);
        await personas.EliminarAutorizacionAsync(e.Coordinador, e.Programa.Id, fechaSinUso, CancellationToken.None);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            personas.EliminarAutorizacionAsync(e.Coordinador, e.Programa.Id, fechaSinUso, CancellationToken.None));

        var horario = await Servicio(db).CrearAsync(Comando(e, "PRUEBA", new BloqueEnHorarioCommand(e.Teoria.Id, Fecha1)), CancellationToken.None);
        await transaction.CreateSavepointAsync("antes_eliminar_autorizacion");
        await Assert.ThrowsAsync<ResourceConflictException>(() =>
            personas.EliminarAutorizacionAsync(e.Coordinador, e.Programa.Id, e.InicioAutorizacion, CancellationToken.None));
        await transaction.RollbackToSavepointAsync("antes_eliminar_autorizacion");
        Assert.True(await db.Autorizaciones.AsNoTracking().AnyAsync(x => x.CoordinadorId == e.Coordinador && x.ProgramaId == e.Programa.Id && x.FechaInicio == e.InicioAutorizacion));
        Assert.True(await db.Horarios.AsNoTracking().AnyAsync(x => x.Id == horario.Horario.Id));
        await transaction.RollbackAsync();
    }

    [PostgreSqlTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DesvincularYAgregarBloqueConcurrentemente_NoDejaHorarioIncompatible(bool borrarPrimero)
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();
        Escenario e = await CrearEscenarioAsync(db);
        HorarioDetalle horario = await Servicio(db).CrearAsync(
            Comando(e, "CONCURRENCIA", new BloqueEnHorarioCommand(e.Teoria.Id, Fecha1)), CancellationToken.None);
        // La fecha existe antes de iniciar las transacciones concurrentes.
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO \"FECHA\" (\"fecha\") VALUES ({Fecha2}) ON CONFLICT DO NOTHING");
        try
        {
            await using var c1 = new NpgsqlConnection(Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION"));
            await using var c2 = new NpgsqlConnection(Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION"));
            await c1.OpenAsync();
            await c2.OpenAsync();
            await using var t1 = await c1.BeginTransactionAsync();
            await using var t2 = await c2.BeginTransactionAsync();
            string borrar = "DELETE FROM \"BLOQUE_PROGRAMA\" WHERE \"idBloque\" = @bloque AND \"idPrograma\" = @programa";
            string insertar = "INSERT INTO \"BLOQUE_EN_HORARIO\" (\"idHorario\", \"idBloque\", \"fecha\") VALUES (@horario, @bloque, @fecha)";

            NpgsqlCommand ComandoSql(NpgsqlConnection c, NpgsqlTransaction t, string sql)
            {
                var cmd = new NpgsqlCommand(sql, c, t);
                cmd.Parameters.AddWithValue("bloque", e.Practica.Id);
                cmd.Parameters.AddWithValue("programa", e.Programa.Id);
                cmd.Parameters.AddWithValue("horario", horario.Horario.Id);
                cmd.Parameters.AddWithValue("fecha", Fecha2);
                return cmd;
            }

            await using var primero = ComandoSql(c1, t1, borrarPrimero ? borrar : insertar);
            await using var segundo = ComandoSql(c2, t2, borrarPrimero ? insertar : borrar);
            await primero.ExecuteNonQueryAsync();
            Task<int> segunda = segundo.ExecuteNonQueryAsync();
            // Esperar al bloqueo real evita depender de una pausa fija.
            await using var monitor = new NpgsqlConnection(Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION"));
            await monitor.OpenAsync();
            var limite = DateTime.UtcNow.AddSeconds(10);
            bool bloqueada = false;
            while (!segunda.IsCompleted && DateTime.UtcNow < limite)
            {
                await using var consulta = new NpgsqlCommand(
                    "SELECT EXISTS (SELECT 1 FROM pg_stat_activity WHERE pid = @pid AND wait_event_type = 'Lock')", monitor);
                consulta.Parameters.AddWithValue("pid", c2.ProcessID);
                if ((bool)(await consulta.ExecuteScalarAsync())!) { bloqueada = true; break; }
                await Task.Delay(20);
            }
            Assert.True(bloqueada, "La segunda operación no quedó esperando el bloqueo.");
            await t1.CommitAsync();
            var error = await Assert.ThrowsAsync<PostgresException>(async () => await segunda.WaitAsync(TimeSpan.FromSeconds(10)));
            Assert.Contains(error.SqlState, new[] { "23503", "23001" });
            await t2.RollbackAsync();

            bool existeRelacion = await db.BloquesProgramas.AsNoTracking().AnyAsync(x => x.BloqueId == e.Practica.Id && x.ProgramaId == e.Programa.Id);
            bool existeBloqueHorario = await db.BloquesEnHorario.AsNoTracking().AnyAsync(x => x.BloqueId == e.Practica.Id && x.HorarioId == horario.Horario.Id);
            Assert.False(existeBloqueHorario && !existeRelacion);
            Assert.Equal(!borrarPrimero, existeBloqueHorario);
        }
        finally
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"BLOQUE_EN_HORARIO\" WHERE \"idHorario\" = {horario.Horario.Id}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"HORARIO\" WHERE \"idHorario\" = {horario.Horario.Id}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"COORDINADOR_EN_PROGRAMA\" WHERE \"idCoordinador\" = {e.Coordinador} AND \"idPrograma\" = {e.Programa.Id}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"BLOQUE_PROGRAMA\" WHERE \"idBloque\" IN ({e.Teoria.Id}, {e.Practica.Id})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"BLOQUE_CONTENIDO\" WHERE \"idBloque\" IN ({e.Teoria.Id}, {e.Practica.Id})");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"PROGRAMA_ASIGNATURA\" WHERE \"idPrograma\" = {e.Programa.Id}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"PROGRAMA_ACADEMICO\" WHERE \"idPrograma\" = {e.Programa.Id}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"ASIGNATURA\" WHERE \"idAsignatura\" = {e.Asignatura}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"COORDINADOR\" WHERE \"idCoordinador\" = {e.Coordinador}");
            await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM \"PERSONA\" WHERE \"idPersona\" = {e.Coordinador}");
        }
    }

    [PostgreSqlFact]
    public async Task DesvincularBloque_ConHorario_RechazaYConservaLaAsociacion()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        Escenario e = await CrearEscenarioAsync(db);
        var bloques = new BloqueRepository(db);
        Assert.True(await bloques.DesasociarProgramaAsync(e.Practica.Id, e.Programa.Id, CancellationToken.None));
        Assert.False(await bloques.DesasociarProgramaAsync(e.Practica.Id, e.Programa.Id, CancellationToken.None));

        await Servicio(db).CrearAsync(Comando(e, "PRUEBA", new BloqueEnHorarioCommand(e.Teoria.Id, Fecha1)), CancellationToken.None);
        await transaction.CreateSavepointAsync("antes_baja");
        await Assert.ThrowsAsync<ResourceConflictException>(() =>
            bloques.DesasociarProgramaAsync(e.Teoria.Id, e.Programa.Id, CancellationToken.None));
        await transaction.RollbackToSavepointAsync("antes_baja");
        Assert.True(await db.BloquesProgramas.AsNoTracking().AnyAsync(x => x.BloqueId == e.Teoria.Id && x.ProgramaId == e.Programa.Id));
        await transaction.RollbackAsync();
    }

    private static readonly DateOnly Fecha1 = new(2031, 3, 10);
    private static readonly DateOnly Fecha2 = new(2031, 3, 12);

    private static DbContextOptions<TimeblockDbContext> Opciones() =>
        new DbContextOptionsBuilder<TimeblockDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION")!)
            .Options;

    private sealed record Escenario(
        ProgramaAcademico Programa,
        Guid Asignatura,
        Guid Coordinador,
        DateOnly InicioAutorizacion,
        BloqueContenido Teoria,
        BloqueContenido Practica);

    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.UtcNow);

    private static async Task<Guid> CrearCoordinadorAsync(PersonasService personas, string nombre)
    {
        Persona coordinador = await personas.CrearAsync(
            new CrearPersonaCommand(nombre, new PerfilCoordinadorCrearCommand("Bases de datos")),
            CancellationToken.None);
        return coordinador.Id;
    }

    /// <summary>
    /// Crea un programa con una asignatura, dos bloques utilizables en él y un
    /// coordinador con autorización vigente para generar horarios.
    /// </summary>
    private static async Task<Escenario> CrearEscenarioAsync(TimeblockDbContext db)
    {
        var personas = new PersonasService(new PersonaRepository(db));
        Guid coordinador = await CrearCoordinadorAsync(personas, "Luis García");

        Asignatura asignatura = Asignatura.Crear(Guid.NewGuid(), "Bases de datos");
        ProgramaAcademico programa = ProgramaAcademico.Crear(Guid.NewGuid(), "Informática", "Plan 2026");
        programa.AsociarAsignatura(asignatura.Id);
        db.Asignaturas.Add(asignatura);
        db.Programas.Add(programa);

        BloqueContenido teoria = BloqueContenido.Crear(
            "Introducción a las bases de datos", TipoActividad.Teoria, NivelComplejidad.Medio,
            asignatura.Id, coordinador);
        teoria.AsociarPrograma(programa);
        BloqueContenido practica = BloqueContenido.Crear(
            "Consultas y operaciones SQL", TipoActividad.Practica, NivelComplejidad.Alto,
            asignatura.Id, coordinador);
        practica.AsociarPrograma(programa);
        db.Bloques.Add(teoria);
        db.Bloques.Add(practica);
        await db.SaveChangesAsync();

        DateOnly inicio = Hoy.AddDays(-30);
        await personas.CrearAutorizacionAsync(
            coordinador, programa.Id, inicio, null, true, false, CancellationToken.None);

        return new Escenario(programa, asignatura.Id, coordinador, inicio, teoria, practica);
    }

    private static HorariosService Servicio(TimeblockDbContext db) =>
        new(new HorarioRepository(db), TimeProvider.System);

    private static CrearHorarioCommand Comando(
        Escenario e,
        string periodo,
        params BloqueEnHorarioCommand[] bloques) =>
        new(e.Programa.Id, periodo, e.Coordinador, bloques);

    [PostgreSqlFact]
    public async Task Horario_SeCreaConsultaYListaConSusBloquesYFechas()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        Escenario e = await CrearEscenarioAsync(db);
        // Reloj con fracciones de 100 ns, como el del sistema en Linux y
        // Windows; PostgreSQL solo conserva microsegundos.
        var servicio = new HorariosService(
            new HorarioRepository(db), new RelojManual(ConFraccionesDe100Ns(DateTimeOffset.UtcNow)));
        string periodo = $"TEST-{Guid.NewGuid():N}";

        // El mismo bloque en dos fechas y otro bloque en una de ellas.
        HorarioDetalle creado = await servicio.CrearAsync(
            Comando(
                e,
                periodo,
                new BloqueEnHorarioCommand(e.Teoria.Id, Fecha1),
                new BloqueEnHorarioCommand(e.Teoria.Id, Fecha2),
                new BloqueEnHorarioCommand(e.Practica.Id, Fecha1)),
            CancellationToken.None);

        Assert.Equal(2, creado.CantTotalBloques);
        Assert.Equal(3, creado.Bloques.Count);

        // Lo guardado se recupera igual, con la autorización utilizada.
        db.ChangeTracker.Clear();
        HorarioDetalle leido = await servicio.ObtenerAsync(creado.Horario.Id, CancellationToken.None);
        Assert.Equal(EstadoHorario.Generado, leido.Horario.Estado);
        Assert.Equal(periodo, leido.Horario.PeriodoAcademico);
        Assert.Equal(e.Coordinador, leido.Horario.CoordinadorId);
        Assert.Equal(e.InicioAutorizacion, leido.Horario.FechaInicioAutorizacion);
        Assert.Equal(creado.Horario.FechaCreacion, leido.Horario.FechaCreacion);
        Assert.Equal(2, leido.CantTotalBloques);
        Assert.Equal(
            new[] { Fecha1, Fecha1, Fecha2 },
            leido.Bloques.Select(item => item.Asociacion.Fecha));
        Assert.All(leido.Bloques, item =>
        {
            Assert.Null(item.Asociacion.HoraInicio);
            Assert.Null(item.Asociacion.HoraFin);
            Assert.Null(item.Asociacion.PeriodoImp);
            Assert.Null(item.Asociacion.AulaId);
        });

        // El contexto agregó en la misma operación las fechas que faltaban.
        Assert.True(await db.Fechas.AnyAsync(fecha => fecha.Fecha == Fecha1));
        Assert.True(await db.Fechas.AnyAsync(fecha => fecha.Fecha == Fecha2));

        // Listado con filtros traducidos a SQL.
        var porPeriodo = await servicio.ListarAsync(
            new HorarioFilter(PeriodoAcademico: periodo), null, null, CancellationToken.None);
        Assert.Equal(creado.Horario.Id, Assert.Single(porPeriodo.Items).Horario.Id);
        Assert.Equal(3, porPeriodo.Items[0].Bloques.Count);

        var porProgramaYEstado = await servicio.ListarAsync(
            new HorarioFilter(e.Programa.Id, periodo, EstadoHorario.Generado), 1, 1, CancellationToken.None);
        Assert.Equal(1, porProgramaYEstado.TotalItems);

        var otroPrograma = await servicio.ListarAsync(
            new HorarioFilter(Guid.NewGuid(), periodo), null, null, CancellationToken.None);
        Assert.Empty(otroPrograma.Items);
    }

    [PostgreSqlFact]
    public async Task Listar_OrdenaDelMasRecienteAlMasAntiguoYPagina()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        Escenario e = await CrearEscenarioAsync(db);
        string periodo = $"TEST-{Guid.NewGuid():N}";
        var repositorio = new HorarioRepository(db);

        var antiguo = new HorariosService(repositorio, new RelojManual(DateTimeOffset.UtcNow.AddMinutes(-10)));
        var reciente = new HorariosService(repositorio, new RelojManual(DateTimeOffset.UtcNow));
        HorarioDetalle primero = await antiguo.CrearAsync(
            Comando(e, periodo, new BloqueEnHorarioCommand(e.Teoria.Id, Fecha1)), CancellationToken.None);
        HorarioDetalle segundo = await reciente.CrearAsync(
            Comando(e, periodo, new BloqueEnHorarioCommand(e.Practica.Id, Fecha2)), CancellationToken.None);

        db.ChangeTracker.Clear();
        var pagina1 = await reciente.ListarAsync(
            new HorarioFilter(PeriodoAcademico: periodo), 1, 1, CancellationToken.None);
        var pagina2 = await reciente.ListarAsync(
            new HorarioFilter(PeriodoAcademico: periodo), 2, 1, CancellationToken.None);

        Assert.Equal(2, pagina1.TotalItems);
        Assert.Equal(segundo.Horario.Id, Assert.Single(pagina1.Items).Horario.Id);
        Assert.Equal(primero.Horario.Id, Assert.Single(pagina2.Items).Horario.Id);
    }

    [PostgreSqlFact]
    public async Task Crear_ConFallosDeNegocio_NoDejaUnHorarioParcial()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        Escenario e = await CrearEscenarioAsync(db);
        HorariosService servicio = Servicio(db);

        var personas = new PersonasService(new PersonaRepository(db));
        Guid sinAutorizacion = await CrearCoordinadorAsync(personas, "Marta Díaz");
        BloqueContenido ajeno = BloqueContenido.Crear(
            "Bloque sin programa", TipoActividad.Laboratorio, NivelComplejidad.Bajo,
            e.Asignatura, e.Coordinador);
        db.Bloques.Add(ajeno);
        await db.SaveChangesAsync();

        int horarios = await db.Horarios.CountAsync();
        int asociaciones = await db.BloquesEnHorario.CountAsync();

        // 404: un bloque inexistente entre varios válidos.
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => servicio.CrearAsync(
            Comando(e, "P", new BloqueEnHorarioCommand(e.Teoria.Id, Fecha1), new BloqueEnHorarioCommand(Guid.NewGuid(), Fecha2)),
            CancellationToken.None));

        // 409: un bloque no utilizable en el programa, entre varios válidos.
        var noUtilizable = await Assert.ThrowsAsync<DomainException>(() => servicio.CrearAsync(
            Comando(e, "P", new BloqueEnHorarioCommand(e.Teoria.Id, Fecha1), new BloqueEnHorarioCommand(ajeno.Id, Fecha2)),
            CancellationToken.None));
        Assert.Equal(TipoErrorDominio.Conflicto, noUtilizable.Tipo);

        // 409: el mismo bloque repetido en la misma fecha.
        var repetido = await Assert.ThrowsAsync<DomainException>(() => servicio.CrearAsync(
            Comando(e, "P", new BloqueEnHorarioCommand(e.Teoria.Id, Fecha1), new BloqueEnHorarioCommand(e.Practica.Id, Fecha1), new BloqueEnHorarioCommand(e.Teoria.Id, Fecha1)),
            CancellationToken.None));
        Assert.Equal(TipoErrorDominio.Conflicto, repetido.Tipo);

        // 403: coordinador sin autorización para el programa.
        var sinPermiso = await Assert.ThrowsAsync<DomainException>(() => servicio.CrearAsync(
            new CrearHorarioCommand(
                e.Programa.Id, "P", sinAutorizacion, [new BloqueEnHorarioCommand(e.Teoria.Id, Fecha1)]),
            CancellationToken.None));
        Assert.Equal(TipoErrorDominio.AutorizacionNegocio, sinPermiso.Tipo);

        Assert.Equal(horarios, await db.Horarios.CountAsync());
        Assert.Equal(asociaciones, await db.BloquesEnHorario.CountAsync());
    }

    [PostgreSqlFact]
    public async Task Crear_ConAutorizacionVencidaOSinPermisoDeGenerar_EsAutorizacionDeNegocio()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        Escenario e = await CrearEscenarioAsync(db);
        HorariosService servicio = Servicio(db);
        var personas = new PersonasService(new PersonaRepository(db));

        Guid vencida = await CrearCoordinadorAsync(personas, "Vencida");
        await personas.CrearAutorizacionAsync(
            vencida, e.Programa.Id, Hoy.AddDays(-60), Hoy.AddDays(-30), true, true, CancellationToken.None);
        Guid sinGenerar = await CrearCoordinadorAsync(personas, "Sin permiso");
        await personas.CrearAutorizacionAsync(
            sinGenerar, e.Programa.Id, Hoy.AddDays(-60), null, false, true, CancellationToken.None);

        foreach (Guid coordinador in new[] { vencida, sinGenerar })
        {
            var excepcion = await Assert.ThrowsAsync<DomainException>(() => servicio.CrearAsync(
                new CrearHorarioCommand(
                    e.Programa.Id, "P", coordinador, [new BloqueEnHorarioCommand(e.Teoria.Id, Fecha1)]),
                CancellationToken.None));

            Assert.Equal(TipoErrorDominio.AutorizacionNegocio, excepcion.Tipo);
        }
    }

    [PostgreSqlFact]
    public async Task Persistencia_RechazaUnHorarioQueReferenciaUnaAutorizacionInexistente()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        Escenario e = await CrearEscenarioAsync(db);

        // La autorización del horario no existe en COORDINADOR_EN_PROGRAMA, de
        // modo que la clave foránea compuesta debe rechazarlo.
        var inexistente = new AutorizacionCoordinador(
            e.Coordinador, e.Programa.Id, Hoy.AddDays(-5), null, true, false);
        Horario horario = Horario.Crear(
            Guid.NewGuid(), DateTimeOffset.UtcNow, "P", e.Programa.Id, e.Coordinador, inexistente);

        // Va al final porque PostgreSQL aborta la transacción tras el error.
        await Assert.ThrowsAsync<ResourceConflictException>(
            () => new HorarioRepository(db).AgregarAsync(horario, CancellationToken.None));
    }

    private static DateTimeOffset ConFraccionesDe100Ns(DateTimeOffset valor) =>
        new(valor.Ticks - (valor.Ticks % TimeSpan.TicksPerMicrosecond) + 7, TimeSpan.Zero);

    private sealed class RelojManual(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
