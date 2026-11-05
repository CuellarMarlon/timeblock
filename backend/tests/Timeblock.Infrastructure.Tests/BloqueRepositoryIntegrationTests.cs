using Microsoft.EntityFrameworkCore;
using Timeblock.Application.Bloques;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Personas;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Personas;
using Timeblock.Infrastructure.Persistence;

// Application.Personas define su propio PagedResult<T>; este alias evita la ambigüedad.
using PaginaDeBloques = Timeblock.Application.Common.Pagination.PagedResult<Timeblock.Domain.Bloques.BloqueContenido>;

namespace Timeblock.Infrastructure.Tests;

public sealed class BloqueRepositoryIntegrationTests
{
    private static DbContextOptions<TimeblockDbContext> Opciones() =>
        new DbContextOptionsBuilder<TimeblockDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION")!)
            .Options;

    private sealed record Escenario(
        Guid AsignaturaBd,
        Guid AsignaturaMate,
        Guid ProgramaInformatica,
        Guid ProgramaMatematica,
        Guid Coordinador,
        Guid OtroCoordinador);

    /// <summary>
    /// Crea dos asignaturas, un programa por asignatura y dos coordinadores.
    /// </summary>
    private static async Task<Escenario> CrearEscenarioAsync(TimeblockDbContext db)
    {
        var personas = new PersonasService(new PersonaRepository(db));
        Persona coordinador = await personas.CrearAsync(
            new CrearPersonaCommand("Luis García", new PerfilCoordinadorCrearCommand("Bases de datos")),
            CancellationToken.None);
        Persona otroCoordinador = await personas.CrearAsync(
            new CrearPersonaCommand("Marta Díaz", new PerfilCoordinadorCrearCommand("Matemática")),
            CancellationToken.None);

        Asignatura bd = Asignatura.Crear(Guid.NewGuid(), "Bases de datos");
        Asignatura mate = Asignatura.Crear(Guid.NewGuid(), "Álgebra");
        db.Asignaturas.Add(bd);
        db.Asignaturas.Add(mate);

        ProgramaAcademico informatica =
            ProgramaAcademico.Crear(Guid.NewGuid(), "Informática", "Plan 2026");
        informatica.AsociarAsignatura(bd.Id);
        ProgramaAcademico matematica =
            ProgramaAcademico.Crear(Guid.NewGuid(), "Matemática", "Plan 2026");
        matematica.AsociarAsignatura(mate.Id);
        db.Programas.Add(informatica);
        db.Programas.Add(matematica);
        await db.SaveChangesAsync();

        return new Escenario(
            bd.Id, mate.Id, informatica.Id, matematica.Id, coordinador.Id, otroCoordinador.Id);
    }

    private static Task<BloqueContenido> CrearBloque(
        BloquesService servicio,
        string descripcion,
        Guid asignatura,
        Guid coordinador,
        TipoActividad tipo = TipoActividad.Teoria,
        NivelComplejidad nivel = NivelComplejidad.Medio,
        params Guid[] programas) =>
        servicio.CrearAsync(
            new CrearBloqueCommand(descripcion, tipo, nivel, asignatura, coordinador, programas),
            CancellationToken.None);

    [PostgreSqlFact]
    public async Task Bloques_SeGuardanConsultanFiltranYActualizan()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        Escenario e = await CrearEscenarioAsync(db);
        var servicio = new BloquesService(new BloqueRepository(db));

        BloqueContenido teoria = await CrearBloque(
            servicio, "Introducción a las bases de datos", e.AsignaturaBd, e.Coordinador,
            programas: [e.ProgramaInformatica]);
        BloqueContenido practica = await CrearBloque(
            servicio, "Consultas SQL", e.AsignaturaBd, e.Coordinador,
            TipoActividad.Practica, NivelComplejidad.Alto);
        BloqueContenido algebra = await CrearBloque(
            servicio, "Matrices", e.AsignaturaMate, e.OtroCoordinador,
            TipoActividad.Laboratorio, NivelComplejidad.Bajo, [e.ProgramaMatematica]);

        // Lo guardado se recupera con sus programas asociados.
        db.ChangeTracker.Clear();
        BloqueContenido leido = await servicio.ObtenerAsync(teoria.Id, CancellationToken.None);
        Assert.Equal("Introducción a las bases de datos", leido.Descripcion);
        Assert.Equal(TipoActividad.Teoria, leido.TipoActividad);
        Assert.Equal(e.Coordinador, leido.CoordinadorCreadorId);
        Assert.Equal(e.ProgramaInformatica, Assert.Single(leido.Programas).ProgramaId);

        // Filtros traducidos a SQL.
        Task<PaginaDeBloques> Listar(BloqueFilter filtro, int? page = null, int? size = null) =>
            servicio.ListarAsync(filtro, page, size, CancellationToken.None);

        Assert.Equal(teoria.Id, Assert.Single((await Listar(new(ProgramaId: e.ProgramaInformatica))).Items).Id);
        Assert.Equal(algebra.Id, Assert.Single((await Listar(new(AsignaturaId: e.AsignaturaMate))).Items).Id);
        Assert.Equal(algebra.Id, Assert.Single((await Listar(new(CoordinadorId: e.OtroCoordinador))).Items).Id);
        Assert.Equal(practica.Id, Assert.Single((await Listar(new(TipoActividad: TipoActividad.Practica))).Items).Id);
        Assert.Equal(practica.Id, Assert.Single((await Listar(new(NivelComplejidad: NivelComplejidad.Alto))).Items).Id);

        PaginaDeBloques primeraPagina = await Listar(new(AsignaturaId: e.AsignaturaBd), 1, 1);
        Assert.Equal(2, primeraPagina.TotalItems);
        Assert.Equal(practica.Id, Assert.Single(primeraPagina.Items).Id); // "consultas sql" < "introducción..."

        // Actualización persistida.
        await servicio.ActualizarAsync(
            practica.Id,
            new ActualizarBloqueCommand(Descripcion: "  Consultas   SQL avanzadas ", NivelComplejidad: NivelComplejidad.Medio),
            CancellationToken.None);
        db.ChangeTracker.Clear();
        BloqueContenido actualizado = await servicio.ObtenerAsync(practica.Id, CancellationToken.None);
        Assert.Equal("Consultas SQL avanzadas", actualizado.Descripcion);
        Assert.Equal("consultas sql avanzadas", actualizado.DescripcionNormalizada);
        Assert.Equal(NivelComplejidad.Medio, actualizado.NivelComplejidad);

        // Duplicidad detectada por Application (misma clave tras normalizar).
        await Assert.ThrowsAsync<ResourceConflictException>(() => CrearBloque(
            servicio, "  INTRODUCCIÓN a las   bases de datos ", e.AsignaturaBd, e.OtroCoordinador));
        await Assert.ThrowsAsync<ResourceConflictException>(() => servicio.ActualizarAsync(
            practica.Id,
            new ActualizarBloqueCommand(Descripcion: "introducción a las bases de datos", TipoActividad: TipoActividad.Teoria),
            CancellationToken.None));

        // El intento fallido dejó cambios sin guardar en el bloque rastreado;
        // en la API cada solicitud usa su propio contexto y no ocurre.
        db.ChangeTracker.Clear();

        // Asociación idempotente y regla RII03.
        await servicio.AsociarProgramaAsync(practica.Id, e.ProgramaInformatica, CancellationToken.None);
        await servicio.AsociarProgramaAsync(practica.Id, e.ProgramaInformatica, CancellationToken.None);
        db.ChangeTracker.Clear();
        BloqueContenido asociado = await servicio.ObtenerAsync(practica.Id, CancellationToken.None);
        Assert.Equal(e.ProgramaInformatica, Assert.Single(asociado.Programas).ProgramaId);

        await Assert.ThrowsAsync<InvalidRequestException>(() => servicio.AsociarProgramaAsync(
            practica.Id, e.ProgramaMatematica, CancellationToken.None));
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => servicio.AsociarProgramaAsync(
            practica.Id, Guid.NewGuid(), CancellationToken.None));
    }

    [PostgreSqlFact]
    public async Task Referencias_Inexistentes_SeRechazan()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        Escenario e = await CrearEscenarioAsync(db);
        var servicio = new BloquesService(new BloqueRepository(db));

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => CrearBloque(
            servicio, "Bloque", Guid.NewGuid(), e.Coordinador));
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => CrearBloque(
            servicio, "Bloque", e.AsignaturaBd, Guid.NewGuid()));
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => CrearBloque(
            servicio, "Bloque", e.AsignaturaBd, e.Coordinador, programas: [Guid.NewGuid()]));
        await Assert.ThrowsAsync<InvalidRequestException>(() => CrearBloque(
            servicio, "Bloque", e.AsignaturaBd, e.Coordinador, programas: [e.ProgramaMatematica]));

        // Una persona que no es coordinador tampoco puede crear bloques.
        var personas = new PersonasService(new PersonaRepository(db));
        Persona jefe = await personas.CrearAsync(
            new CrearPersonaCommand("Ana Ruiz", new PerfilJefeDepartamentoCrearCommand()),
            CancellationToken.None);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => CrearBloque(
            servicio, "Bloque", e.AsignaturaBd, jefe.Id));
    }

    [PostgreSqlFact]
    public async Task Descripcion_ConMayusculasAcentuadas_CumpleLaRestriccionDeNormalizacion()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        Escenario e = await CrearEscenarioAsync(db);
        var servicio = new BloquesService(new BloqueRepository(db));

        // La restricción CK_BLOQUE_CONTENIDO_DescripcionNormalizada usa lower()
        // de PostgreSQL; debe coincidir con la normalización del dominio.
        BloqueContenido bloque = await CrearBloque(
            servicio, "ÁLGEBRA   Lineal Ñandú", e.AsignaturaMate, e.OtroCoordinador);

        db.ChangeTracker.Clear();
        BloqueContenido leido = await servicio.ObtenerAsync(bloque.Id, CancellationToken.None);
        Assert.Equal("ÁLGEBRA Lineal Ñandú", leido.Descripcion);
        Assert.Equal("álgebra lineal ñandú", leido.DescripcionNormalizada);
    }

    [PostgreSqlFact]
    public async Task Unicidad_LaProtegeLaPersistenciaAunSinComprobacionPrevia()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        Escenario e = await CrearEscenarioAsync(db);
        var repositorio = new BloqueRepository(db);

        await repositorio.AgregarAsync(
            BloqueContenido.Crear(
                "Modelo relacional", TipoActividad.Teoria, NivelComplejidad.Bajo,
                e.AsignaturaBd, e.Coordinador),
            CancellationToken.None);

        // Se omite ExisteDuplicadoAsync: debe rechazarlo el índice único.
        // Va al final porque PostgreSQL aborta la transacción tras el error.
        await Assert.ThrowsAsync<ResourceConflictException>(() => repositorio.AgregarAsync(
            BloqueContenido.Crear(
                "  MODELO   relacional ", TipoActividad.Teoria, NivelComplejidad.Alto,
                e.AsignaturaBd, e.OtroCoordinador),
            CancellationToken.None));
    }

    [PostgreSqlFact]
    public async Task AsociarPrograma_DosSolicitudesConcurrentes_NoDuplicaLaRelacion()
    {
        Escenario escenario;
        Guid bloqueId = Guid.NewGuid();

        await using (var seedBd = new TimeblockDbContext(Opciones()))
        {
            await seedBd.Database.MigrateAsync();
            escenario = await CrearEscenarioAsync(seedBd);
            
            seedBd.Bloques.Add(BloqueContenido.Crear(bloqueId, "Concurrente", TipoActividad.Teoria, NivelComplejidad.Medio, escenario.AsignaturaBd, escenario.Coordinador));

            await seedBd.SaveChangesAsync();
        }

        try
        {
            await using var dbA = new TimeblockDbContext(Opciones());
            await using var dbB = new TimeblockDbContext(Opciones());

            var repoA = new BloqueRepository(dbA);
            var repoB = new BloqueRepository(dbB);

            BloqueContenido bloqueA = await repoA.ObtenerAsync(bloqueId, CancellationToken.None)
            ?? throw new InvalidOperationException("No se encontró el bloque en la base de datos A.");
            
            BloqueContenido bloqueB = await repoB.ObtenerAsync(bloqueId, CancellationToken.None)
            ?? throw new InvalidOperationException("No se encontró el bloque en la base de datos B.");

            ProgramaAcademico programaA = Assert.Single(await repoA.ObtenerProgramasAsync([escenario.ProgramaInformatica], CancellationToken.None));
            ProgramaAcademico programaB = Assert.Single(await repoB.ObtenerProgramasAsync([escenario.ProgramaInformatica], CancellationToken.None));

            bloqueA.AsociarPrograma(programaA);
            bloqueB.AsociarPrograma(programaB);

            Task guardarA = repoA.GuardarCambiosAsync(CancellationToken.None);
            Task guardarB = repoB.GuardarCambiosAsync(CancellationToken.None);

            await Task.WhenAll(guardarA, guardarB);

            await using var dbVerificacion = new TimeblockDbContext(Opciones());

            int relacionesGuardadas = await dbVerificacion.BloquesProgramas.CountAsync(relacion => relacion.BloqueId == bloqueId && relacion.ProgramaId == escenario.ProgramaInformatica);

            Assert.Equal(1, relacionesGuardadas);
        }
        finally
        {
            await using var cleanupDb = new TimeblockDbContext(Opciones());
            await cleanupDb.BloquesProgramas.Where(relacion => relacion.BloqueId == bloqueId).ExecuteDeleteAsync();
            await cleanupDb.Bloques.Where(bloque => bloque.Id == bloqueId).ExecuteDeleteAsync();
            await cleanupDb.ProgramasAsignaturas.Where(relacion => relacion.ProgramaId == escenario.ProgramaInformatica || relacion.ProgramaId == escenario.ProgramaMatematica).ExecuteDeleteAsync();
            await cleanupDb.Programas.Where(programa => programa.Id == escenario.ProgramaInformatica || programa.Id == escenario.ProgramaMatematica).ExecuteDeleteAsync();
            await cleanupDb.Asignaturas.Where(asignatura => asignatura.Id == escenario.AsignaturaBd || asignatura.Id == escenario.AsignaturaMate).ExecuteDeleteAsync();
            await cleanupDb.Coordinadores.Where(coordinador => coordinador.IdCoordinador == escenario.Coordinador || coordinador.IdCoordinador == escenario.OtroCoordinador).ExecuteDeleteAsync();
            await cleanupDb.Personas.Where(persona => persona.IdPersona == escenario.Coordinador || persona.IdPersona == escenario.OtroCoordinador).ExecuteDeleteAsync();
        }
    }
}
