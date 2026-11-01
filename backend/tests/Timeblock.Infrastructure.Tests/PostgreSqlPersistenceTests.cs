using Microsoft.EntityFrameworkCore;
using Npgsql;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Matriculas;
using Timeblock.Domain.Personas;
using Timeblock.Infrastructure.Persistence;

namespace Timeblock.Infrastructure.Tests;

public sealed class PostgreSqlPersistenceTests
{
    [PostgreSqlFact]
    public async Task M4_GuardaYRecuperaEntidadesYRelacionesPrincipales()
    {
        string connectionString = Environment.GetEnvironmentVariable(
            "TIMEBLOCK_TEST_CONNECTION")!;

        var options = new DbContextOptionsBuilder<TimeblockDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new TimeblockDbContext(options);
        await context.Database.MigrateAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        Guid coordinadorId = Guid.NewGuid();
        Guid estudianteId = Guid.NewGuid();
        Guid programaId = Guid.NewGuid();
        Guid otroProgramaId = Guid.NewGuid();
        Guid asignaturaId = Guid.NewGuid();
        Guid otraAsignaturaId = Guid.NewGuid();
        Guid bloqueId = Guid.NewGuid();
        Guid horarioId = Guid.NewGuid();
        DateOnly fechaInicioAutorizacion = new(2026, 9, 7);
        DateOnly fechaBloque = new(2026, 10, 20);
        DateTimeOffset fechaAsignacion = new(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);

        var programa = ProgramaAcademico.Crear(
            programaId,
            "Ingeniería Informática",
            "Plan 2024");
        var otroPrograma = ProgramaAcademico.Crear(
            otroProgramaId,
            "Matemática",
            "Plan 2024");
        var asignatura = Asignatura.Crear(asignaturaId, "Bases de Datos");
        var otraAsignatura = Asignatura.Crear(otraAsignaturaId, "Álgebra");
        programa.AsociarAsignatura(asignaturaId);
        otroPrograma.AsociarAsignatura(otraAsignaturaId);

        var autorizacion = new AutorizacionCoordinador(
            coordinadorId,
            programaId,
            fechaInicioAutorizacion,
            fechaFin: null,
            puedeGenerar: true,
            puedeValidar: false);

        PersonaPersistenceRows coordinadorRows = PersonaPersistenceMapper.ToRows(
            Persona.Crear(
                coordinadorId,
                "Luis García",
                new PerfilCoordinador("Bases de datos")));
        PersonaPersistenceRows estudianteRows = PersonaPersistenceMapper.ToRows(
            Persona.Crear(
                estudianteId,
                "Ana Pérez",
                new PerfilEstudiante(20, "C311", coordinadorId)));

        context.Personas.AddRange(coordinadorRows.Persona, estudianteRows.Persona);
        context.Coordinadores.Add(coordinadorRows.Coordinador!);
        context.Estudiantes.Add(estudianteRows.Estudiante!);
        context.Programas.Add(programa);
        context.Programas.Add(otroPrograma);
        context.Asignaturas.Add(asignatura);
        context.Asignaturas.Add(otraAsignatura);
        context.Autorizaciones.Add(autorizacion);
        context.Fechas.AddRange(
            new FechaRecord { Fecha = fechaInicioAutorizacion },
            new FechaRecord { Fecha = fechaBloque });

        // Persiste primero programa, asignatura y asociación para que la regla
        // RII03 vea la pertenencia antes de insertar BLOQUE_PROGRAMA.
        await context.SaveChangesAsync();

        var bloque = BloqueContenido.Crear(
            bloqueId,
            "Introducción a bases de datos",
            TipoActividad.Teoria,
            NivelComplejidad.Medio,
            asignaturaId,
            coordinadorId);
        bloque.AsociarPrograma(programa);
        context.Bloques.Add(bloque);

        var horario = Horario.Crear(
            horarioId,
            new DateTimeOffset(2026, 10, 12, 14, 30, 0, TimeSpan.Zero),
            "2026-2027",
            programaId,
            coordinadorId,
            autorizacion);
        horario.AgregarBloque(bloque, fechaBloque);
        horario.AgregarBloque(bloque, fechaBloque.AddDays(1));
        context.Horarios.Add(horario);
        context.Matriculas.Add(new Matricula(estudianteId, programaId));
        context.EsquemaLlave.Add(new EsquemaLlaveRecord
        {
            IdPersona = coordinadorId,
            IdCoordinador = coordinadorId,
            FechaInicio = fechaInicioAutorizacion,
            Fecha = fechaBloque,
            FechaAsignacion = fechaAsignacion,
            IdBloque = bloqueId,
            IdValidacion = Guid.NewGuid(),
            IdRevision = Guid.NewGuid(),
            IdSolicitud = Guid.NewGuid()
        });

        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var programaRecuperado = await context.Programas
            .Include(item => item.Asignaturas)
            .SingleAsync(item => item.Id == programaId);
        var horarioRecuperado = await context.Horarios
            .Include(item => item.Bloques)
            .SingleAsync(item => item.Id == horarioId);
        var estudianteRecuperado = await context.Estudiantes
            .SingleAsync(item => item.IdEstudiante == estudianteId);
        var coordinadorRecuperado = await context.Coordinadores
            .SingleAsync(item => item.IdCoordinador == coordinadorId);
        var autorizacionRecuperada = await context.Autorizaciones
            .SingleAsync(item => item.CoordinadorId == coordinadorId && item.ProgramaId == programaId);
        var personaEstudianteRecuperada = PersonaPersistenceMapper.ToDomain(
            new PersonaPersistenceRows(
                await context.Personas.SingleAsync(item => item.IdPersona == estudianteId),
                await context.Estudiantes.SingleAsync(item => item.IdEstudiante == estudianteId),
                null,
                null));

        Assert.Equal("Ingeniería Informática", programaRecuperado.Nombre);
        Assert.Equal(asignaturaId, Assert.Single(programaRecuperado.Asignaturas).AsignaturaId);
        Assert.Equal(EstadoHorario.Generado, horarioRecuperado.Estado);
        Assert.Equal(20, estudianteRecuperado.Edad);
        Assert.Equal(coordinadorId, estudianteRecuperado.CoordinadorAsignadoId);
        Assert.Equal("Bases de datos", coordinadorRecuperado.Especialidad);
        Assert.Equal(fechaInicioAutorizacion, autorizacionRecuperada.FechaInicio);
        Assert.Equal("Ana Pérez", personaEstudianteRecuperada.Nombre);
        Assert.Equal(TipoPerfil.Estudiante, personaEstudianteRecuperada.Perfil.Tipo);

        Assert.Equal(2, horarioRecuperado.Bloques.Count);
        var bloqueRecuperado = horarioRecuperado.Bloques
            .Single(item => item.Fecha == fechaBloque);
        Assert.Equal(bloqueId, bloqueRecuperado.BloqueId);
        Assert.Equal(fechaBloque, bloqueRecuperado.Fecha);
        Assert.Null(bloqueRecuperado.HoraInicio);
        Assert.Null(bloqueRecuperado.HoraFin);
        Assert.Null(bloqueRecuperado.PeriodoImp);
        Assert.Null(bloqueRecuperado.AulaId);

        Assert.True(await context.Matriculas.AnyAsync(matricula =>
            matricula.EstudianteId == estudianteId && matricula.ProgramaId == programaId));
        Assert.True(await context.EsquemaLlave.AnyAsync(clave =>
            clave.IdBloque == bloqueId && clave.IdPersona == coordinadorId));

        await AssertPostgreSqlFailureAsync(
            context,
            "23514",
            $"""INSERT INTO "BLOQUE_PROGRAMA" ("idBloque", "idPrograma") VALUES ({bloqueId}, {otroProgramaId})""");

        await AssertPostgreSqlFailureAsync(
            context,
            "23505",
            $"""INSERT INTO "BLOQUE_CONTENIDO" ("idBloque", "descripcion", "descripcionNormalizada", "tipoActividad", "nivelComplejidad", "idAsignatura", "idCoordinadorCreador") VALUES ({Guid.NewGuid()}, ' Introducción   a bases de datos ', 'introducción a bases de datos', 'TEORICA', 'MEDIO', {asignaturaId}, {coordinadorId})""");

        await AssertPostgreSqlFailureAsync(
            context,
            "23514",
            $"""INSERT INTO "BLOQUE_EN_HORARIO" ("idHorario", "idBloque", fecha, "horaInicio") VALUES ({horarioId}, {bloqueId}, DATE '2026-10-21', TIME '08:00')""");

        await AssertPostgreSqlFailureAsync(
            context,
            "23514",
            $"""UPDATE "BLOQUE_CONTENIDO" SET "idAsignatura" = {otraAsignaturaId} WHERE "idBloque" = {bloqueId}""");

        await AssertPostgreSqlFailureAsync(
            context,
            "23514",
            $"""DELETE FROM "PROGRAMA_ASIGNATURA" WHERE "idPrograma" = {programaId} AND "idAsignatura" = {asignaturaId}""");

        await transaction.RollbackAsync();
    }

    private static async Task AssertPostgreSqlFailureAsync(
        TimeblockDbContext context,
        string expectedSqlState,
        FormattableString command)
    {
        const string savepoint = "expected_constraint_violation";
        await context.Database.ExecuteSqlRawAsync($"SAVEPOINT {savepoint}");

        PostgresException exception = await Assert.ThrowsAsync<PostgresException>(
            () => context.Database.ExecuteSqlInterpolatedAsync(command));

        await context.Database.ExecuteSqlRawAsync($"ROLLBACK TO SAVEPOINT {savepoint}");
        await context.Database.ExecuteSqlRawAsync($"RELEASE SAVEPOINT {savepoint}");

        Assert.Equal(expectedSqlState, exception.SqlState);
    }
}
