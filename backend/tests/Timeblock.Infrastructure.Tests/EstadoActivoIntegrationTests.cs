using Microsoft.EntityFrameworkCore;
using Timeblock.Application.Personas;
using Timeblock.Application.Catalogo;
using Timeblock.Application.Bloques;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Matriculas;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Personas;
using Timeblock.Infrastructure.Persistence;

namespace Timeblock.Infrastructure.Tests;

public sealed class EstadoActivoIntegrationTests
{
    [PostgreSqlFact]
    public async Task CoordinadorInactivo_NoPermiteNuevosEstudiantesAutorizacionesNiBloques()
    {
        var opciones = new DbContextOptionsBuilder<TimeblockDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION")!).Options;
        await using var db = new TimeblockDbContext(opciones);
        await db.Database.MigrateAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        var ct = CancellationToken.None;
        var repoPersonas = new PersonaRepository(db);
        var personas = new PersonasService(repoPersonas);
        var coordinador = await personas.CrearAsync(new CrearPersonaCommand("Coordinador", new PerfilCoordinadorCrearCommand("Sistemas")), ct);
        var catalogo = new CatalogoService(new CatalogoRepository(db));
        var programa = await catalogo.CrearProgramaAsync($"Programa {Guid.NewGuid()}", "Plan", ct);
        var asignatura = await catalogo.CrearAsignaturaAsync($"Asignatura {Guid.NewGuid()}", ct);
        coordinador.CambiarEstado(false);
        await repoPersonas.GuardarCambiosAsync(coordinador, ct);

        await tx.CreateSavepointAsync("antes_estudiante", ct);
        await Assert.ThrowsAsync<ResourceConflictException>(() => personas.CrearAsync(
            new CrearPersonaCommand("Estudiante", new PerfilEstudianteCrearCommand(20, "C1", coordinador.Id)), ct));
        await tx.RollbackToSavepointAsync("antes_estudiante", ct);
        db.ChangeTracker.Clear();
        await tx.CreateSavepointAsync("antes_autorizacion", ct);
        await Assert.ThrowsAsync<ResourceConflictException>(() => personas.CrearAutorizacionAsync(
            coordinador.Id, programa.Id, new DateOnly(2026, 1, 1), null, true, false, ct));
        await tx.RollbackToSavepointAsync("antes_autorizacion", ct);
        db.ChangeTracker.Clear();
        await tx.CreateSavepointAsync("antes_bloque", ct);
        var bloques = new BloquesService(new BloqueRepository(db));
        await Assert.ThrowsAsync<ResourceConflictException>(() => bloques.CrearAsync(
            new CrearBloqueCommand("Bloque", TipoActividad.Teoria, NivelComplejidad.Bajo, asignatura.Id, coordinador.Id, []), ct));
        await tx.RollbackToSavepointAsync("antes_bloque", ct);
        Assert.False(await db.Autorizaciones.AsNoTracking().AnyAsync(x => x.CoordinadorId == coordinador.Id, ct));
        Assert.False(await db.Bloques.AsNoTracking().AnyAsync(x => x.CoordinadorCreadorId == coordinador.Id, ct));
        await tx.RollbackAsync(ct);
    }

    [PostgreSqlFact]
    public async Task RegistrosInactivos_ImpidenNuevasRelacionesYConservanLasExistentes()
    {
        var opciones = new DbContextOptionsBuilder<TimeblockDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION")!).Options;
        await using var db = new TimeblockDbContext(opciones);
        await db.Database.MigrateAsync();
        await using var tx = await db.Database.BeginTransactionAsync();
        var ct = CancellationToken.None;
        var repoPersonas = new PersonaRepository(db);
        var personas = new PersonasService(repoPersonas);
        var coordinador = await personas.CrearAsync(new CrearPersonaCommand("Coordinador", new PerfilCoordinadorCrearCommand("Sistemas")), ct);
        var estudiante = await personas.CrearAsync(new CrearPersonaCommand("Estudiante", new PerfilEstudianteCrearCommand(20, "C1", coordinador.Id)), ct);
        var catalogo = new CatalogoService(new CatalogoRepository(db));
        var programa = await catalogo.CrearProgramaAsync($"Programa {Guid.NewGuid()}", "Plan", ct);
        var otroPrograma = await catalogo.CrearProgramaAsync($"Programa {Guid.NewGuid()}", "Plan", ct);
        var asignatura = await catalogo.CrearAsignaturaAsync($"Asignatura {Guid.NewGuid()}", ct);
        await catalogo.AsociarAsignaturaAsync(programa.Id, asignatura.Id, ct);
        var matriculas = new MatriculasService(new MatriculaRepository(db));
        await matriculas.MatricularAsync(programa.Id, estudiante.Id, ct);

        estudiante.CambiarEstado(false);
        await repoPersonas.GuardarCambiosAsync(estudiante, ct);
        await tx.CreateSavepointAsync("antes_inactivo", ct);
        await Assert.ThrowsAsync<ResourceConflictException>(() => matriculas.MatricularAsync(otroPrograma.Id, estudiante.Id, ct));
        await tx.RollbackToSavepointAsync("antes_inactivo", ct);
        db.ChangeTracker.Clear();
        Assert.True(await db.Matriculas.AsNoTracking().AnyAsync(m => m.EstudianteId == estudiante.Id && m.ProgramaId == programa.Id, ct));
        var persona = (await repoPersonas.ObtenerAsync(estudiante.Id, ct))!;
        persona.CambiarEstado(true);
        await repoPersonas.GuardarCambiosAsync(persona, ct);
        await matriculas.MatricularAsync(otroPrograma.Id, estudiante.Id, ct);

        db.ChangeTracker.Clear();
        var desactivada = await db.Asignaturas.SingleAsync(a => a.Id == asignatura.Id, ct);
        desactivada.CambiarEstado(false);
        await db.SaveChangesAsync(ct);
        await tx.CreateSavepointAsync("antes_asociar", ct);
        await Assert.ThrowsAsync<ResourceConflictException>(() => catalogo.AsociarAsignaturaAsync(otroPrograma.Id, asignatura.Id, ct));
        await tx.RollbackToSavepointAsync("antes_asociar", ct);
        db.ChangeTracker.Clear();
        Assert.True(await db.ProgramasAsignaturas.AsNoTracking().AnyAsync(x => x.ProgramaId == programa.Id && x.AsignaturaId == asignatura.Id, ct));
        await tx.RollbackAsync(ct);
    }

    [PostgreSqlFact]
    public async Task Entidades_ConservanEstadoAlRecargarYReactivar()
    {
        var opciones = new DbContextOptionsBuilder<TimeblockDbContext>()
            .UseNpgsql(Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION")!).Options;
        await using var db = new TimeblockDbContext(opciones);
        await db.Database.MigrateAsync();
        await using var transaccion = await db.Database.BeginTransactionAsync();
        var personas = new PersonaRepository(db);
        Persona coordinador = await new PersonasService(personas).CrearAsync(
            new CrearPersonaCommand("Coordinador", new PerfilCoordinadorCrearCommand("Sistemas")), CancellationToken.None);
        var programa = ProgramaAcademico.Crear(Guid.NewGuid(), "Programa", "Plan");
        var asignatura = Asignatura.Crear(Guid.NewGuid(), "Asignatura");
        db.Programas.Add(programa);
        db.Asignaturas.Add(asignatura);
        var bloque = BloqueContenido.Crear("Contenido", TipoActividad.Teoria, NivelComplejidad.Bajo, asignatura.Id, coordinador.Id);
        db.Bloques.Add(bloque);
        await db.SaveChangesAsync();
        Assert.True(coordinador.Activo && programa.Activo && asignatura.Activo && bloque.Activo);

        coordinador.CambiarEstado(false);
        programa.CambiarEstado(false);
        asignatura.CambiarEstado(false);
        bloque.CambiarEstado(false);
        await personas.GuardarCambiosAsync(coordinador, CancellationToken.None);
        db.ChangeTracker.Clear();
        Assert.False((await personas.ObtenerAsync(coordinador.Id, CancellationToken.None))!.Activo);
        Assert.False((await db.Programas.SingleAsync(x => x.Id == programa.Id)).Activo);
        Assert.False((await db.Asignaturas.SingleAsync(x => x.Id == asignatura.Id)).Activo);
        Assert.False((await db.Bloques.SingleAsync(x => x.Id == bloque.Id)).Activo);

        var personaRecuperada = (await personas.ObtenerAsync(coordinador.Id, CancellationToken.None))!;
        personaRecuperada.CambiarEstado(true);
        (await db.Programas.SingleAsync(x => x.Id == programa.Id)).CambiarEstado(true);
        (await db.Asignaturas.SingleAsync(x => x.Id == asignatura.Id)).CambiarEstado(true);
        (await db.Bloques.SingleAsync(x => x.Id == bloque.Id)).CambiarEstado(true);
        await personas.GuardarCambiosAsync(personaRecuperada, CancellationToken.None);
        db.ChangeTracker.Clear();
        Assert.True((await personas.ObtenerAsync(coordinador.Id, CancellationToken.None))!.Activo);
        Assert.True((await db.Programas.SingleAsync(x => x.Id == programa.Id)).Activo);
        Assert.True((await db.Asignaturas.SingleAsync(x => x.Id == asignatura.Id)).Activo);
        Assert.True((await db.Bloques.SingleAsync(x => x.Id == bloque.Id)).Activo);
        await transaccion.RollbackAsync();
    }
}
