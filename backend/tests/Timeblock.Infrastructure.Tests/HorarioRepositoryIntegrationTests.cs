using Microsoft.EntityFrameworkCore;
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

public sealed class HorarioRepositoryIntegrationTests
{
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
        HorariosService servicio = Servicio(db);
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

    private sealed class RelojManual(DateTimeOffset ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => ahora;
    }
}
