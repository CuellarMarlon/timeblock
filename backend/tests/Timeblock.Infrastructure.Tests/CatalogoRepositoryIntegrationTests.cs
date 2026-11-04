using Microsoft.EntityFrameworkCore;
using Timeblock.Application.Catalogo;
using Timeblock.Infrastructure.Persistence;

namespace Timeblock.Infrastructure.Tests;

public sealed class CatalogoRepositoryIntegrationTests
{
    [PostgreSqlFact]
    public async Task Catalogo_PersisteFiltraActualizaYAsociaDeFormaIdempotente()
    {
        string connectionString = Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION")!;
        var options = new DbContextOptionsBuilder<TimeblockDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new TimeblockDbContext(options);
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();

        var repository = new CatalogoRepository(db);
        var service = new CatalogoService(repository);
        var program = await service.CrearProgramaAsync(
            "Ingeniería Informática", "Plan 2026", CancellationToken.None);
        var subject = await service.CrearAsignaturaAsync("Bases de Datos II", CancellationToken.None);

        await service.AsociarAsignaturaAsync(program.Id, subject.Id, CancellationToken.None);
        await service.AsociarAsignaturaAsync(program.Id, subject.Id, CancellationToken.None);
        // También ejercita la protección de la clave compuesta si la carrera
        // ocurre entre la consulta previa y el INSERT en PostgreSQL.
        await repository.AsociarAsync(program.Id, subject.Id, CancellationToken.None);

        var programs = await service.ListarProgramasAsync(
            "informática", 1, 20, CancellationToken.None);
        var associatedSubjects = await service.ListarAsignaturasDeProgramaAsync(
            program.Id, 1, 20, CancellationToken.None);
        var subjects = await service.ListarAsignaturasAsync(
            "bases de datos", 1, 20, CancellationToken.None);

        Assert.Equal(1, programs.TotalItems);
        Assert.Equal(program.Id, Assert.Single(programs.Items).Id);
        Assert.Equal(1, associatedSubjects.TotalItems);
        Assert.Equal(subject.Id, Assert.Single(associatedSubjects.Items).Id);
        Assert.Equal(subject.Id, Assert.Single(subjects.Items).Id);
        Assert.Equal(1, await db.ProgramasAsignaturas.CountAsync(x =>
            x.ProgramaId == program.Id && x.AsignaturaId == subject.Id));

        var updatedProgram = await service.ActualizarProgramaAsync(
            program.Id, "Ingeniería de Software", "Plan 2027", CancellationToken.None);
        var updatedSubject = await service.ActualizarAsignaturaAsync(
            subject.Id, "Bases de Datos Avanzadas", CancellationToken.None);

        Assert.Equal("Ingeniería de Software", updatedProgram.Nombre);
        Assert.Equal("Plan 2027", updatedProgram.PlanEstudio);
        Assert.Equal("Bases de Datos Avanzadas", updatedSubject.Nombre);

        await transaction.RollbackAsync();
    }
}
