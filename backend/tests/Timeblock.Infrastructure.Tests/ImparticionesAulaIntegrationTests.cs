using Microsoft.EntityFrameworkCore;
using Npgsql;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Personas;
using Timeblock.Infrastructure.Persistence;
using Timeblock.Infrastructure.Persistence.Configurations;

namespace Timeblock.Infrastructure.Tests;

/// <summary>
/// Imparticiones completas en PostgreSQL: RII04 (sin solapes en la misma aula
/// y fecha, contiguos permitidos) y clases sin solapar dentro de un horario,
/// también ante dos transacciones simultáneas. Los datos se confirman para que
/// las dos conexiones los vean, así que llevan nombres y fechas únicos y se
/// borran al terminar.
/// </summary>
public sealed class ImparticionesAulaIntegrationTests
{
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(10);

    private static string Conexion =>
        Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION")!;

    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.UtcNow);

    private sealed record Escenario(
        Guid Coordinador,
        Guid Asignatura,
        Guid Programa,
        Guid Teoria,
        Guid Practica,
        Guid AulaA,
        Guid AulaB,
        DateOnly Fecha);

    private static ProgramacionImparticion De(int inicio, int fin, Guid aula) =>
        new(new TimeOnly(inicio, 0), new TimeOnly(fin, 0), aula);

    [PostgreSqlFact]
    public async Task ImparticionCompleta_SeGuardaYSeConsulta()
    {
        Escenario e = await CrearEscenarioAsync();
        try
        {
            Guid id;
            await using (var db = new TimeblockDbContext(Opciones()))
            {
                Horario horario = await PrepararHorarioAsync(db, e, (e.Teoria, De(8, 10, e.AulaA)));
                await new HorarioRepository(db).AgregarAsync(horario, CancellationToken.None);
                id = horario.Id;
            }

            await using var lectura = new TimeblockDbContext(Opciones());
            Horario guardado = (await new HorarioRepository(lectura).ObtenerAsync(id, CancellationToken.None))!;
            BloqueEnHorario imparticion = Assert.Single(guardado.Bloques);
            Assert.Equal(new TimeOnly(8, 0), imparticion.HoraInicio);
            Assert.Equal(new TimeOnly(10, 0), imparticion.HoraFin);
            Assert.Equal(e.AulaA, imparticion.AulaId);
            Assert.Equal("2026-2027", imparticion.PeriodoImp);
        }
        finally
        {
            await LimpiarAsync(e);
        }
    }

    [PostgreSqlTheory]
    [InlineData(9, 11)]
    [InlineData(7, 9)]
    [InlineData(8, 10)]
    [InlineData(8, 9)]
    [InlineData(7, 12)]
    public async Task OtroHorarioSolapadoEnLaMismaAula_EsConflicto(int inicio, int fin)
    {
        Escenario e = await CrearEscenarioAsync();
        try
        {
            await GuardarHorarioAsync(e, (e.Teoria, De(8, 10, e.AulaA)));

            var error = await Assert.ThrowsAsync<ResourceConflictException>(
                () => GuardarHorarioAsync(e, (e.Teoria, De(inicio, fin, e.AulaA))));

            Assert.Contains("aula", error.Message);
            Assert.Equal(1, await ContarHorariosAsync(e));
        }
        finally
        {
            await LimpiarAsync(e);
        }
    }

    [PostgreSqlFact]
    public async Task IntervalosContiguosUOtraAula_SePermiten()
    {
        Escenario e = await CrearEscenarioAsync();
        try
        {
            await GuardarHorarioAsync(e, (e.Teoria, De(8, 10, e.AulaA)));
            await GuardarHorarioAsync(e, (e.Teoria, De(10, 12, e.AulaA)));
            await GuardarHorarioAsync(e, (e.Teoria, De(6, 8, e.AulaA)));
            await GuardarHorarioAsync(e, (e.Teoria, De(8, 10, e.AulaB)));

            Assert.Equal(4, await ContarHorariosAsync(e));
        }
        finally
        {
            await LimpiarAsync(e);
        }
    }

    /// <summary>
    /// El dominio ya impide clases solapadas en un horario; la base de datos
    /// lo garantiza también para escrituras que no pasen por él.
    /// </summary>
    [PostgreSqlFact]
    public async Task SolapeDentroDelMismoHorario_LoRechazaTambienLaBaseDeDatos()
    {
        Escenario e = await CrearEscenarioAsync();
        try
        {
            Guid horario = await GuardarHorarioAsync(e, (e.Teoria, De(8, 10, e.AulaA)));

            await using var conexion = new NpgsqlConnection(Conexion);
            await conexion.OpenAsync();
            var error = await Assert.ThrowsAsync<PostgresException>(() => EjecutarAsync(conexion, null,
                $"""
                INSERT INTO "BLOQUE_EN_HORARIO" ("idHorario", "idBloque", "fecha", "horaInicio", "horaFin", "periodoImp", "idAula")
                VALUES ('{horario}', '{e.Practica}', '{e.Fecha:yyyy-MM-dd}', '09:00', '11:00', '2026-2027', '{e.AulaB}')
                """));

            Assert.Equal(PostgresErrorCodes.ExclusionViolation, error.SqlState);
            Assert.Equal(RestriccionesImparticion.HorarioSinSolapes, error.ConstraintName);
        }
        finally
        {
            await LimpiarAsync(e);
        }
    }

    /// <summary>
    /// Dos horarios piden a la vez la misma aula en intervalos que se cortan.
    /// El segundo espera al primero y, cuando este confirma, se rechaza.
    /// </summary>
    [PostgreSqlFact]
    public async Task AltasSimultaneasSolapadasEnLaMismaAula_SoloSeConfirmaUna()
    {
        Escenario e = await CrearEscenarioAsync();
        try
        {
            Exception? errorSegunda = await CarreraAsync(e, De(8, 10, e.AulaA), De(9, 11, e.AulaA), confirmarPrimera: true);

            var conflicto = Assert.IsType<ResourceConflictException>(errorSegunda);
            Assert.Contains("aula", conflicto.Message);
            Assert.Equal(1, await ContarHorariosAsync(e));
        }
        finally
        {
            await LimpiarAsync(e);
        }
    }

    [PostgreSqlFact]
    public async Task AltasSimultaneasConLaPrimeraDeshecha_LaSegundaSeGuarda()
    {
        Escenario e = await CrearEscenarioAsync();
        try
        {
            Exception? errorSegunda = await CarreraAsync(e, De(8, 10, e.AulaA), De(9, 11, e.AulaA), confirmarPrimera: false);

            Assert.Null(errorSegunda);
            Assert.Equal(1, await ContarHorariosAsync(e));
        }
        finally
        {
            await LimpiarAsync(e);
        }
    }

    [PostgreSqlFact]
    public async Task AltasSimultaneasContiguas_SeGuardanLasDos()
    {
        Escenario e = await CrearEscenarioAsync();
        try
        {
            Exception? errorSegunda = await CarreraAsync(e, De(8, 10, e.AulaA), De(10, 12, e.AulaA), confirmarPrimera: true);

            Assert.Null(errorSegunda);
            Assert.Equal(2, await ContarHorariosAsync(e));
        }
        finally
        {
            await LimpiarAsync(e);
        }
    }

    /// <summary>
    /// La primera alta queda sin confirmar mientras se lanza la segunda; se
    /// espera a que la segunda termine o quede bloqueada antes de confirmar o
    /// deshacer la primera. Devuelve el error de la segunda, si lo hubo.
    /// </summary>
    private static async Task<Exception?> CarreraAsync(
        Escenario e,
        ProgramacionImparticion primera,
        ProgramacionImparticion segunda,
        bool confirmarPrimera)
    {
        await using var db1 = new TimeblockDbContext(Opciones());
        await using var db2 = new TimeblockDbContext(Opciones());
        Horario horario1 = await PrepararHorarioAsync(db1, e, (e.Teoria, primera));
        Horario horario2 = await PrepararHorarioAsync(db2, e, (e.Teoria, segunda));

        await using var transaccion1 = await db1.Database.BeginTransactionAsync();
        await new HorarioRepository(db1).AgregarAsync(horario1, CancellationToken.None);

        await db2.Database.OpenConnectionAsync();
        int pid = ((NpgsqlConnection)db2.Database.GetDbConnection()).ProcessID;
        Task<Exception?> alta2 = IntentarAsync(
            () => new HorarioRepository(db2).AgregarAsync(horario2, CancellationToken.None));
        await EsperarBloqueoOFinAsync(pid, alta2);

        if (confirmarPrimera)
        {
            await transaccion1.CommitAsync();
        }
        else
        {
            await transaccion1.RollbackAsync();
        }

        return await alta2;
    }

    private static async Task<Guid> GuardarHorarioAsync(
        Escenario e,
        params (Guid Bloque, ProgramacionImparticion Programacion)[] imparticiones)
    {
        await using var db = new TimeblockDbContext(Opciones());
        Horario horario = await PrepararHorarioAsync(db, e, imparticiones);
        await new HorarioRepository(db).AgregarAsync(horario, CancellationToken.None);
        return horario.Id;
    }

    /// <summary>
    /// Lee la autorización y los bloques como lo hace la aplicación y arma un
    /// horario con las imparticiones indicadas, sin guardarlo.
    /// </summary>
    private static async Task<Horario> PrepararHorarioAsync(
        TimeblockDbContext db,
        Escenario e,
        params (Guid Bloque, ProgramacionImparticion Programacion)[] imparticiones)
    {
        var repositorio = new HorarioRepository(db);
        AutorizacionCoordinador autorizacion = Assert.Single(
            await repositorio.ObtenerAutorizacionesAsync(e.Coordinador, e.Programa, CancellationToken.None));
        Dictionary<Guid, BloqueContenido> bloques = (await repositorio.ObtenerBloquesAsync(
                imparticiones.Select(i => i.Bloque).Distinct().ToArray(), CancellationToken.None))
            .ToDictionary(bloque => bloque.Id);

        Horario horario = Horario.Crear(
            Guid.NewGuid(), DateTimeOffset.UtcNow, "2026-2027", e.Programa, e.Coordinador, autorizacion);
        foreach ((Guid bloque, ProgramacionImparticion programacion) in imparticiones)
        {
            horario.AgregarBloque(bloques[bloque], e.Fecha, programacion);
        }

        return horario;
    }

    private static async Task<Exception?> IntentarAsync(Func<Task> accion)
    {
        try
        {
            await accion();
            return null;
        }
        catch (Exception error)
        {
            return error;
        }
    }

    private static async Task EjecutarAsync(
        NpgsqlConnection conexion,
        NpgsqlTransaction? transaccion,
        string sql)
    {
        await using var comando = new NpgsqlCommand(sql, conexion, transaccion);
        await comando.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Espera a que la operación termine o quede bloqueada esperando un
    /// bloqueo de la otra transacción, sin depender de pausas fijas.
    /// </summary>
    private static async Task EsperarBloqueoOFinAsync(int pid, Task operacion)
    {
        await using var monitor = new NpgsqlConnection(Conexion);
        await monitor.OpenAsync();
        DateTime limite = DateTime.UtcNow + EsperaMaxima;

        while (!operacion.IsCompleted)
        {
            long esperando = await ContarAsync(monitor,
                $"SELECT count(*) FROM pg_stat_activity WHERE pid = {pid} AND wait_event_type = 'Lock'");
            if (esperando > 0)
            {
                return;
            }

            if (DateTime.UtcNow > limite)
            {
                throw new TimeoutException("La operación no terminó ni quedó bloqueada.");
            }

            await Task.Delay(20);
        }
    }

    private static async Task<long> ContarHorariosAsync(Escenario e)
    {
        await using var conexion = new NpgsqlConnection(Conexion);
        await conexion.OpenAsync();
        return await ContarAsync(conexion,
            $"""SELECT count(*) FROM "HORARIO" WHERE "idCoordinadorGen" = '{e.Coordinador}'""");
    }

    private static async Task<long> ContarAsync(NpgsqlConnection conexion, string sql)
    {
        await using var comando = new NpgsqlCommand(sql, conexion);
        return Convert.ToInt64(await comando.ExecuteScalarAsync());
    }

    private static DbContextOptions<TimeblockDbContext> Opciones() =>
        new DbContextOptionsBuilder<TimeblockDbContext>()
            .UseNpgsql(Conexion)
            .Options;

    /// <summary>
    /// Programa con dos bloques, un coordinador autorizado a generar, dos aulas
    /// propias y una fecha lejana y aleatoria que ya existe en FECHA, para que
    /// las aulas no coincidan con las de otras pruebas en paralelo.
    /// </summary>
    private static async Task<Escenario> CrearEscenarioAsync()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();

        string sufijo = Guid.NewGuid().ToString("N");
        Guid coordinador = Guid.NewGuid();
        PersonaPersistenceRows filas = PersonaPersistenceMapper.ToRows(
            Persona.Crear(coordinador, $"Coordinador aulas {sufijo}", new PerfilCoordinador("Aulas")));
        db.Personas.Add(filas.Persona);
        db.Coordinadores.Add(filas.Coordinador!);

        Asignatura asignatura = Asignatura.Crear(Guid.NewGuid(), $"Aulas A {sufijo}");
        ProgramaAcademico programa = ProgramaAcademico.Crear(Guid.NewGuid(), $"Aulas P {sufijo}", "Plan 2026");
        programa.AsociarAsignatura(asignatura.Id);
        db.Asignaturas.Add(asignatura);
        db.Programas.Add(programa);
        db.Autorizaciones.Add(new AutorizacionCoordinador(
            coordinador, programa.Id, Hoy.AddDays(-30), null, puedeGenerar: true, puedeValidar: false));

        var aulaA = new AulaRecord { IdAula = Guid.NewGuid(), Edificio = $"Edificio {sufijo}", Capacidad = 30 };
        var aulaB = new AulaRecord { IdAula = Guid.NewGuid(), Edificio = $"Edificio {sufijo}", Capacidad = 40 };
        db.Aulas.AddRange(aulaA, aulaB);

        DateOnly fecha = new DateOnly(3000, 1, 1).AddDays(Random.Shared.Next(0, 1_000_000));
        db.Fechas.Add(new FechaRecord { Fecha = fecha });

        // RII03 debe ver la pertenencia antes de insertar BLOQUE_PROGRAMA.
        await db.SaveChangesAsync();

        BloqueContenido teoria = BloqueContenido.Crear(
            Guid.NewGuid(), $"Aulas teoría {sufijo}", TipoActividad.Teoria,
            NivelComplejidad.Medio, asignatura.Id, coordinador);
        BloqueContenido practica = BloqueContenido.Crear(
            Guid.NewGuid(), $"Aulas práctica {sufijo}", TipoActividad.Practica,
            NivelComplejidad.Alto, asignatura.Id, coordinador);
        teoria.AsociarPrograma(programa);
        practica.AsociarPrograma(programa);
        db.Bloques.AddRange(teoria, practica);
        await db.SaveChangesAsync();

        return new Escenario(
            coordinador, asignatura.Id, programa.Id, teoria.Id, practica.Id,
            aulaA.IdAula, aulaB.IdAula, fecha);
    }

    private static async Task LimpiarAsync(Escenario e)
    {
        await using var db = new TimeblockDbContext(Opciones());
        Guid[] bloques = [e.Teoria, e.Practica];
        Guid[] aulas = [e.AulaA, e.AulaB];

        Guid[] horarios = await db.Horarios
            .Where(horario => horario.CoordinadorId == e.Coordinador)
            .Select(horario => horario.Id)
            .ToArrayAsync();
        await db.BloquesEnHorario.Where(b => horarios.Contains(b.HorarioId)).ExecuteDeleteAsync();
        await db.Horarios.Where(horario => horarios.Contains(horario.Id)).ExecuteDeleteAsync();
        await db.BloquesProgramas.Where(relacion => bloques.Contains(relacion.BloqueId)).ExecuteDeleteAsync();
        await db.Bloques.Where(bloque => bloques.Contains(bloque.Id)).ExecuteDeleteAsync();
        await db.Autorizaciones.Where(a => a.CoordinadorId == e.Coordinador).ExecuteDeleteAsync();
        await db.ProgramasAsignaturas.Where(relacion => relacion.ProgramaId == e.Programa).ExecuteDeleteAsync();
        await db.Programas.Where(programa => programa.Id == e.Programa).ExecuteDeleteAsync();
        await db.Asignaturas.Where(asignatura => asignatura.Id == e.Asignatura).ExecuteDeleteAsync();
        await db.Coordinadores.Where(c => c.IdCoordinador == e.Coordinador).ExecuteDeleteAsync();
        await db.Personas.Where(persona => persona.IdPersona == e.Coordinador).ExecuteDeleteAsync();
        await db.Aulas.Where(aula => aulas.Contains(aula.IdAula)).ExecuteDeleteAsync();
        await db.Fechas.Where(f => f.Fecha == e.Fecha).ExecuteDeleteAsync();
    }
}
