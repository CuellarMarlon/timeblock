using Microsoft.EntityFrameworkCore;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Personas;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Personas;
using Timeblock.Infrastructure.Persistence;

namespace Timeblock.Infrastructure.Tests;

public sealed class PersonaRepositoryIntegrationTests
{
    [PostgreSqlFact]
    public async Task EliminarPersona_EliminaPerfilYRechazaDependenciasSinCambiosParciales()
    {
        var options = new DbContextOptionsBuilder<TimeblockDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION")!).Options;
        await using var db = new TimeblockDbContext(options);
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var service = new PersonasService(new PersonaRepository(db));
        var ct = CancellationToken.None;
        Persona jefe = await service.CrearAsync(new CrearPersonaCommand("Jefe", new PerfilJefeDepartamentoCrearCommand()), ct);
        Persona coordinador = await service.CrearAsync(new CrearPersonaCommand("Coordinador", new PerfilCoordinadorCrearCommand("Sistemas")), ct);
        Persona estudiante = await service.CrearAsync(new CrearPersonaCommand("Estudiante", new PerfilEstudianteCrearCommand(20, "C311", coordinador.Id)), ct);
        await service.EliminarAsync(jefe.Id, ct);
        Assert.False(await db.Personas.AsNoTracking().AnyAsync(x => x.IdPersona == jefe.Id, ct));
        Assert.False(await db.JefesDepartamento.AsNoTracking().AnyAsync(x => x.IdJefeDepartamento == jefe.Id, ct));
        await Assert.ThrowsAsync<ResourceConflictException>(() => service.EliminarAsync(coordinador.Id, ct));
        Assert.True(await db.Personas.AsNoTracking().AnyAsync(x => x.IdPersona == coordinador.Id, ct));
        Assert.True(await db.Coordinadores.AsNoTracking().AnyAsync(x => x.IdCoordinador == coordinador.Id, ct));
        Assert.True(await db.Estudiantes.AsNoTracking().AnyAsync(x => x.IdEstudiante == estudiante.Id, ct));
        await service.EliminarAsync(estudiante.Id, ct);
        await service.EliminarAsync(coordinador.Id, ct);
        await transaction.RollbackAsync(ct);
    }

    [PostgreSqlFact]
    public async Task PersonasYAutorizaciones_SeGuardanConsultanActualizanYRespetanClaves()
    {
        string connectionString = Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION")!;
        var options = new DbContextOptionsBuilder<TimeblockDbContext>().UseNpgsql(connectionString).Options;
        await using var db = new TimeblockDbContext(options);
        await db.Database.MigrateAsync();
        await using var transaction = await db.Database.BeginTransactionAsync();
        var repository = new PersonaRepository(db);
        var service = new PersonasService(repository);

        Persona coordinator = await service.CrearAsync(
            new CrearPersonaCommand("Luis García", new PerfilCoordinadorCrearCommand("Bases de datos")),
            CancellationToken.None);
        Guid programId = Guid.NewGuid();
        db.Programas.Add(ProgramaAcademico.Crear(programId, "Informática", "Plan 2026"));
        await db.SaveChangesAsync();

        Persona student = await service.CrearAsync(
            new CrearPersonaCommand("Ana Pérez", new PerfilEstudianteCrearCommand(20, "C311", coordinator.Id)),
            CancellationToken.None);
        var page = await service.ListarAsync(new PersonaFilter("pÉrez", TipoPerfil.Estudiante, 1, 20), CancellationToken.None);
        Assert.Equal(1, page.TotalItems);
        Assert.Equal(student.Id, Assert.Single(page.Items).Id);

        Persona updated = await service.ActualizarPerfilAsync(
            student.Id,
            new PerfilEstudianteCrearCommand(21, "C312", coordinator.Id),
            CancellationToken.None);
        Assert.Equal(21, Assert.IsType<PerfilEstudiante>(updated.Perfil).Edad);

        DateOnly start = new(2026, 10, 12);
        AutorizacionCoordinador authorization = await service.CrearAutorizacionAsync(
            coordinator.Id, programId, start, null, true, false, CancellationToken.None);
        Assert.True(authorization.PermiteGenerarEn(start));
        var authorizations = await service.ListarAutorizacionesAsync(
            coordinator.Id, new AutorizacionFilter(programId, start), CancellationToken.None);
        Assert.Single(authorizations);

        await Assert.ThrowsAsync<ResourceConflictException>(() => service.CrearAutorizacionAsync(
            coordinator.Id, programId, start, null, true, false, CancellationToken.None));

        await transaction.RollbackAsync();
    }
}
