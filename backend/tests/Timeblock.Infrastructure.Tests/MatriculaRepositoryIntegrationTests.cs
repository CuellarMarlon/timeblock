using Microsoft.EntityFrameworkCore;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Matriculas;
using Timeblock.Application.Personas;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Matriculas;
using Timeblock.Domain.Personas;
using Timeblock.Infrastructure.Persistence;

namespace Timeblock.Infrastructure.Tests;

public sealed class MatriculaRepositoryIntegrationTests
{
    private static DbContextOptions<TimeblockDbContext> Opciones() =>
        new DbContextOptionsBuilder<TimeblockDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION")!)
            .Options;

    [PostgreSqlFact]
    public async Task Matriculas_SeGuardanConsultanYRechazanDuplicados()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var personas = new PersonasService(new PersonaRepository(db));
        var service = new MatriculasService(new MatriculaRepository(db));

        Persona coordinador = await personas.CrearAsync(
            new CrearPersonaCommand("Luis García", new PerfilCoordinadorCrearCommand("Bases de datos")),
            CancellationToken.None);
        Persona estudiante = await personas.CrearAsync(
            new CrearPersonaCommand("Ana Pérez", new PerfilEstudianteCrearCommand(20, "C311", coordinador.Id)),
            CancellationToken.None);
        Guid informatica = Guid.NewGuid();
        Guid matematica = Guid.NewGuid();
        db.Programas.Add(ProgramaAcademico.Crear(informatica, "Informática", "Plan 2026"));
        db.Programas.Add(ProgramaAcademico.Crear(matematica, "Matemática", "Plan 2026"));
        await db.SaveChangesAsync();

        await service.MatricularAsync(informatica, estudiante.Id, CancellationToken.None);
        await service.MatricularAsync(matematica, estudiante.Id, CancellationToken.None);

        var porPrograma = await service.ListarPorProgramaAsync(informatica, null, null, CancellationToken.None);
        Assert.Equal(estudiante.Id, Assert.Single(porPrograma.Items).EstudianteId);
        var porEstudiante = await service.ListarPorEstudianteAsync(estudiante.Id, 1, 1, CancellationToken.None);
        Assert.Equal(2, porEstudiante.TotalItems);
        Assert.Single(porEstudiante.Items);

        await Assert.ThrowsAsync<ResourceConflictException>(
            () => service.MatricularAsync(informatica, estudiante.Id, CancellationToken.None));
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.MatricularAsync(informatica, coordinador.Id, CancellationToken.None));

        await transaction.RollbackAsync();
    }

    [PostgreSqlFact]
    public async Task ListarGeneral_FiltraYPaginaEnPostgreSql()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var personas = new PersonasService(new PersonaRepository(db));
        var service = new MatriculasService(new MatriculaRepository(db));
        var ct = CancellationToken.None;

        Persona coordinador = await personas.CrearAsync(
            new CrearPersonaCommand("Coordinador", new PerfilCoordinadorCrearCommand("Informática")), ct);
        Persona estudiante = await personas.CrearAsync(
            new CrearPersonaCommand("Estudiante uno", new PerfilEstudianteCrearCommand(20, "C311", coordinador.Id)), ct);
        Persona otroEstudiante = await personas.CrearAsync(
            new CrearPersonaCommand("Estudiante dos", new PerfilEstudianteCrearCommand(21, "C312", coordinador.Id)), ct);
        Guid programa = Guid.NewGuid();
        Guid otroPrograma = Guid.NewGuid();
        db.Programas.Add(ProgramaAcademico.Crear(programa, $"Programa {programa}", "Plan 2026"));
        db.Programas.Add(ProgramaAcademico.Crear(otroPrograma, $"Programa {otroPrograma}", "Plan 2026"));
        await db.SaveChangesAsync(ct);
        await service.MatricularAsync(programa, estudiante.Id, ct);
        await service.MatricularAsync(otroPrograma, estudiante.Id, ct);
        await service.MatricularAsync(programa, otroEstudiante.Id, ct);

        var porEstudiante = await service.ListarAsync(estudiante.Id, null, 1, 10, ct);
        Assert.Equal(2, porEstudiante.TotalItems);
        Assert.All(porEstudiante.Items, m => Assert.Equal(estudiante.Id, m.EstudianteId));

        var porPrograma = await service.ListarAsync(null, programa, 1, 10, ct);
        Assert.Equal(2, porPrograma.TotalItems);
        Assert.All(porPrograma.Items, m => Assert.Equal(programa, m.ProgramaId));

        var porAmbos = await service.ListarAsync(estudiante.Id, programa, 1, 10, ct);
        Assert.Equal(1, porAmbos.TotalItems);
        var coincidencia = Assert.Single(porAmbos.Items);
        Assert.Equal(estudiante.Id, coincidencia.EstudianteId);
        Assert.Equal(programa, coincidencia.ProgramaId);

        var vacia = await service.ListarAsync(otroEstudiante.Id, otroPrograma, 1, 10, ct);
        Assert.Equal(0, vacia.TotalItems);
        Assert.Empty(vacia.Items);

        var primera = await service.ListarAsync(estudiante.Id, null, 1, 1, ct);
        var segunda = await service.ListarAsync(estudiante.Id, null, 2, 1, ct);
        Assert.Equal(2, primera.TotalItems);
        Assert.Equal(2, segunda.TotalItems);
        Assert.NotEqual(Assert.Single(primera.Items).ProgramaId, Assert.Single(segunda.Items).ProgramaId);

        await transaction.RollbackAsync();
    }

    [PostgreSqlFact]
    public async Task Agregar_DuplicadoQueEsquivaLaComprobacion_SeTraduceAConflicto()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var personas = new PersonasService(new PersonaRepository(db));
        var repository = new MatriculaRepository(db);

        Persona coordinador = await personas.CrearAsync(
            new CrearPersonaCommand("Luis", new PerfilCoordinadorCrearCommand("Redes")),
            CancellationToken.None);
        Persona estudiante = await personas.CrearAsync(
            new CrearPersonaCommand("Ana", new PerfilEstudianteCrearCommand(20, "C311", coordinador.Id)),
            CancellationToken.None);
        Guid programa = Guid.NewGuid();
        db.Programas.Add(ProgramaAcademico.Crear(programa, "Informática", "Plan 2026"));
        await db.SaveChangesAsync();
        await repository.AgregarAsync(new Matricula(estudiante.Id, programa), CancellationToken.None);
        db.ChangeTracker.Clear();

        // Simula una segunda solicitud concurrente que pasó la comprobación previa.
        await Assert.ThrowsAsync<ResourceConflictException>(
            () => repository.AgregarAsync(new Matricula(estudiante.Id, programa), CancellationToken.None));

        await transaction.RollbackAsync();
    }
}
