using Microsoft.EntityFrameworkCore;
using Npgsql;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Personas;
using Timeblock.Infrastructure.Persistence;

namespace Timeblock.Infrastructure.Tests;

/// <summary>
/// Alta de FECHA ante guardados simultáneos (D10 de la revisión de
/// cobertura). Los datos se confirman para que las dos conexiones los vean,
/// así que llevan nombres y fechas únicos y se borran al terminar.
/// </summary>
public sealed class FechaConcurrenciaIntegrationTests
{
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(10);

    private static string Conexion =>
        Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION")!;

    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>Programa con un bloque y un coordinador autorizado a generar.</summary>
    private sealed record Escenario(
        Guid Coordinador,
        Guid Asignatura,
        Guid Programa,
        Guid Bloque,
        DateOnly InicioAutorizacion);

    /// <summary>
    /// Dos horarios usan a la vez una fecha que todavía no existe en FECHA.
    /// El segundo espera al primero y, cuando este confirma, se guarda igual
    /// en lugar de fallar por la clave primaria de FECHA.
    /// </summary>
    [PostgreSqlFact]
    public async Task AltaSimultaneaDeUnaFechaNueva_GuardaLosDosHorarios()
    {
        Escenario e = await CrearEscenarioAsync();
        DateOnly fecha = FechaUnica();
        try
        {
            await using var db1 = new TimeblockDbContext(Opciones());
            await using var db2 = new TimeblockDbContext(Opciones());
            Horario horario1 = await PrepararHorarioAsync(db1, e, fecha);
            Horario horario2 = await PrepararHorarioAsync(db2, e, fecha);

            await using var transaccion1 = await db1.Database.BeginTransactionAsync();
            await new HorarioRepository(db1).AgregarAsync(horario1, CancellationToken.None);

            await db2.Database.OpenConnectionAsync();
            int pid = ((NpgsqlConnection)db2.Database.GetDbConnection()).ProcessID;
            Task<Exception?> segunda = IntentarAsync(
                () => new HorarioRepository(db2).AgregarAsync(horario2, CancellationToken.None));
            await EsperarBloqueoOFinAsync(pid, segunda);

            await transaccion1.CommitAsync();

            Assert.Null(await segunda);
            Assert.Equal(2, await ContarHorariosAsync(e));
            Assert.Equal(1, await ContarFechaAsync(fecha));
        }
        finally
        {
            await LimpiarAsync(e, fecha);
        }
    }

    /// <summary>
    /// Si la primera transacción se deshace, la fecha no queda guardada por
    /// ella y la segunda la crea.
    /// </summary>
    [PostgreSqlFact]
    public async Task AltaSimultaneaConLaPrimeraDeshecha_LaSegundaCreaLaFecha()
    {
        Escenario e = await CrearEscenarioAsync();
        DateOnly fecha = FechaUnica();
        try
        {
            await using var db1 = new TimeblockDbContext(Opciones());
            await using var db2 = new TimeblockDbContext(Opciones());
            Horario horario1 = await PrepararHorarioAsync(db1, e, fecha);
            Horario horario2 = await PrepararHorarioAsync(db2, e, fecha);

            await using var transaccion1 = await db1.Database.BeginTransactionAsync();
            await new HorarioRepository(db1).AgregarAsync(horario1, CancellationToken.None);

            await db2.Database.OpenConnectionAsync();
            int pid = ((NpgsqlConnection)db2.Database.GetDbConnection()).ProcessID;
            Task<Exception?> segunda = IntentarAsync(
                () => new HorarioRepository(db2).AgregarAsync(horario2, CancellationToken.None));
            await EsperarBloqueoOFinAsync(pid, segunda);

            await transaccion1.RollbackAsync();

            Assert.Null(await segunda);
            Assert.Equal(1, await ContarHorariosAsync(e));
            Assert.Equal(1, await ContarFechaAsync(fecha));
        }
        finally
        {
            await LimpiarAsync(e, fecha);
        }
    }

    /// <summary>
    /// El alta de la fecha y el resto del guardado van en la misma
    /// transacción: si el horario se rechaza, la fecha nueva tampoco queda.
    /// </summary>
    [PostgreSqlFact]
    public async Task GuardadoRechazado_NoDejaLaFechaNueva()
    {
        Escenario e = await CrearEscenarioAsync();
        DateOnly fecha = FechaUnica();
        try
        {
            await using var db = new TimeblockDbContext(Opciones());
            Horario horario = await PrepararHorarioAsync(db, e, fecha);

            // La autorización deja de permitir generar después de que la
            // aplicación la leyera, así que PostgreSQL rechaza el horario.
            await using (var otra = new NpgsqlConnection(Conexion))
            {
                await otra.OpenAsync();
                await EjecutarAsync(otra, null,
                    $"""UPDATE "COORDINADOR_EN_PROGRAMA" SET "puedeGenerar" = false WHERE "idCoordinador" = '{e.Coordinador}' AND "idPrograma" = '{e.Programa}'""");
            }

            await Assert.ThrowsAnyAsync<Exception>(
                () => new HorarioRepository(db).AgregarAsync(horario, CancellationToken.None));

            Assert.Equal(0, await ContarHorariosAsync(e));
            Assert.Equal(0, await ContarFechaAsync(fecha));
        }
        finally
        {
            await LimpiarAsync(e, fecha);
        }
    }

    /// <summary>
    /// Lee la autorización y el bloque como lo hace la aplicación y arma un
    /// horario válido con ellos, sin guardarlo.
    /// </summary>
    private static async Task<Horario> PrepararHorarioAsync(
        TimeblockDbContext db,
        Escenario e,
        DateOnly fecha)
    {
        var repositorio = new HorarioRepository(db);
        AutorizacionCoordinador autorizacion = Assert.Single(
            await repositorio.ObtenerAutorizacionesAsync(e.Coordinador, e.Programa, CancellationToken.None));
        BloqueContenido bloque = Assert.Single(
            await repositorio.ObtenerBloquesAsync([e.Bloque], CancellationToken.None));

        Horario horario = Horario.Crear(
            Guid.NewGuid(), DateTimeOffset.UtcNow, "2026-2027", e.Programa, e.Coordinador, autorizacion);
        horario.AgregarBloque(bloque, fecha);
        return horario;
    }

    /// <summary>
    /// Una fecha lejana y aleatoria, para que no exista en FECHA ni coincida
    /// con la de otra prueba en paralelo.
    /// </summary>
    private static DateOnly FechaUnica() =>
        new DateOnly(3000, 1, 1).AddDays(Random.Shared.Next(0, 1_000_000));

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

    private static async Task<long> ContarFechaAsync(DateOnly fecha)
    {
        await using var conexion = new NpgsqlConnection(Conexion);
        await conexion.OpenAsync();
        return await ContarAsync(conexion,
            $"""SELECT count(*) FROM "FECHA" WHERE "fecha" = '{fecha:yyyy-MM-dd}'""");
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

    private static async Task<Escenario> CrearEscenarioAsync()
    {
        await using var db = new TimeblockDbContext(Opciones());
        await db.Database.MigrateAsync();

        string sufijo = Guid.NewGuid().ToString("N");
        Guid coordinador = Guid.NewGuid();
        PersonaPersistenceRows filas = PersonaPersistenceMapper.ToRows(
            Persona.Crear(coordinador, $"Coordinador FECHA {sufijo}", new PerfilCoordinador("Concurrencia")));
        db.Personas.Add(filas.Persona);
        db.Coordinadores.Add(filas.Coordinador!);

        Asignatura asignatura = Asignatura.Crear(Guid.NewGuid(), $"FECHA A {sufijo}");
        ProgramaAcademico programa = ProgramaAcademico.Crear(Guid.NewGuid(), $"FECHA P {sufijo}", "Plan 2026");
        programa.AsociarAsignatura(asignatura.Id);
        db.Asignaturas.Add(asignatura);
        db.Programas.Add(programa);

        DateOnly inicio = Hoy.AddDays(-30);
        db.Autorizaciones.Add(new AutorizacionCoordinador(
            coordinador, programa.Id, inicio, null, puedeGenerar: true, puedeValidar: false));

        // RII03 debe ver la pertenencia antes de insertar BLOQUE_PROGRAMA.
        await db.SaveChangesAsync();

        BloqueContenido bloque = BloqueContenido.Crear(
            Guid.NewGuid(), $"FECHA {sufijo}", TipoActividad.Teoria,
            NivelComplejidad.Medio, asignatura.Id, coordinador);
        bloque.AsociarPrograma(programa);
        db.Bloques.Add(bloque);
        await db.SaveChangesAsync();

        return new Escenario(coordinador, asignatura.Id, programa.Id, bloque.Id, inicio);
    }

    private static async Task LimpiarAsync(Escenario e, DateOnly? fecha = null)
    {
        await using var db = new TimeblockDbContext(Opciones());

        Guid[] horarios = await db.Horarios
            .Where(horario => horario.CoordinadorId == e.Coordinador)
            .Select(horario => horario.Id)
            .ToArrayAsync();
        await db.BloquesEnHorario.Where(b => horarios.Contains(b.HorarioId)).ExecuteDeleteAsync();
        await db.Horarios.Where(horario => horarios.Contains(horario.Id)).ExecuteDeleteAsync();
        await db.BloquesProgramas.Where(relacion => relacion.BloqueId == e.Bloque).ExecuteDeleteAsync();
        await db.Bloques.Where(bloque => bloque.Id == e.Bloque).ExecuteDeleteAsync();
        await db.Autorizaciones.Where(a => a.CoordinadorId == e.Coordinador).ExecuteDeleteAsync();
        await db.ProgramasAsignaturas.Where(relacion => relacion.ProgramaId == e.Programa).ExecuteDeleteAsync();
        await db.Programas.Where(programa => programa.Id == e.Programa).ExecuteDeleteAsync();
        await db.Asignaturas.Where(asignatura => asignatura.Id == e.Asignatura).ExecuteDeleteAsync();
        await db.Coordinadores.Where(c => c.IdCoordinador == e.Coordinador).ExecuteDeleteAsync();
        await db.Personas.Where(persona => persona.IdPersona == e.Coordinador).ExecuteDeleteAsync();

        if (fecha is DateOnly usada)
        {
            await db.Fechas.Where(f => f.Fecha == usada).ExecuteDeleteAsync();
        }
    }
}
