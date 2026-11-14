using Microsoft.EntityFrameworkCore;
using Npgsql;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Personas;
using Timeblock.Infrastructure.Persistence;

namespace Timeblock.Infrastructure.Tests;

/// <summary>
/// RII03 (la asignatura del bloque pertenece a cada programa del bloque) ante
/// dos transacciones concurrentes que, por separado, son válidas pero juntas
/// la incumplen. La primera ejecuta su operación sin confirmar; la segunda se
/// lanza en paralelo y, con la protección, debe esperar a la primera y
/// rechazarse con 23514 al ver su cambio.
/// </summary>
public sealed class Rii03ConcurrenciaIntegrationTests
{
    private static readonly TimeSpan EsperaMaxima = TimeSpan.FromSeconds(10);

    private static string Conexion =>
        Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION")!;

    /// <summary>
    /// Bloque de la asignatura A1 asociado a P1. P1 imparte A1 y A2; P2 solo
    /// A1. Cada operación es válida por sí sola.
    /// </summary>
    private sealed record Escenario(
        Guid Coordinador,
        Guid A1,
        Guid A2,
        Guid P1,
        Guid P2,
        Guid Bloque);

    public enum Operacion
    {
        /// <summary>Asocia el bloque a P2 (P2 imparte A1).</summary>
        AsociarBloqueAP2,

        /// <summary>Cambia la asignatura del bloque a A2 (P1 imparte A2).</summary>
        CambiarAsignaturaDelBloqueAA2,

        /// <summary>Quita A1 de P2 (P2 aún no tiene bloques de A1).</summary>
        QuitarA1DeP2,

        /// <summary>Quita A2 de P1 (P1 aún no tiene bloques de A2).</summary>
        QuitarA2DeP1,
    }

    [PostgreSqlTheory]
    [InlineData(Operacion.AsociarBloqueAP2, Operacion.QuitarA1DeP2)]
    [InlineData(Operacion.QuitarA1DeP2, Operacion.AsociarBloqueAP2)]
    [InlineData(Operacion.AsociarBloqueAP2, Operacion.CambiarAsignaturaDelBloqueAA2)]
    [InlineData(Operacion.CambiarAsignaturaDelBloqueAA2, Operacion.AsociarBloqueAP2)]
    [InlineData(Operacion.CambiarAsignaturaDelBloqueAA2, Operacion.QuitarA2DeP1)]
    [InlineData(Operacion.QuitarA2DeP1, Operacion.CambiarAsignaturaDelBloqueAA2)]
    public async Task OperacionesConcurrentes_NoConfirmanUnEstadoQueIncumplaRii03(
        Operacion primera,
        Operacion segunda)
    {
        Escenario e = await CrearEscenarioAsync();
        try
        {
            await using var conexion1 = new NpgsqlConnection(Conexion);
            await using var conexion2 = new NpgsqlConnection(Conexion);
            await conexion1.OpenAsync();
            await conexion2.OpenAsync();
            await using NpgsqlTransaction transaccion1 = await conexion1.BeginTransactionAsync();
            await using NpgsqlTransaction transaccion2 = await conexion2.BeginTransactionAsync();

            // La primera es válida y queda pendiente de confirmar.
            await EjecutarAsync(conexion1, transaccion1, Sql(primera, e));

            Task<PostgresException?> segundaTarea =
                IntentarAsync(conexion2, transaccion2, Sql(segunda, e));
            await EsperarBloqueoOFinAsync(conexion2.ProcessID, segundaTarea);

            await transaccion1.CommitAsync();
            PostgresException? errorSegunda = await segundaTarea;
            if (errorSegunda is null)
            {
                errorSegunda = await IntentarConfirmarAsync(transaccion2);
            }

            Assert.Equal(0, await ContarViolacionesAsync(e));
            Assert.NotNull(errorSegunda);
            Assert.Equal(PostgresErrorCodes.CheckViolation, errorSegunda.SqlState);
        }
        finally
        {
            await LimpiarAsync(e);
        }
    }

    [PostgreSqlFact]
    public async Task OperacionesSecuencialesValidas_SeConfirman()
    {
        Escenario e = await CrearEscenarioAsync();
        try
        {
            await using var conexion = new NpgsqlConnection(Conexion);
            await conexion.OpenAsync();

            // Cada una es válida tras la anterior: el bloque pasa a P2, luego
            // P1 deja de impartir A2 y P2 pasa a impartirla, y por último el
            // bloque cambia a A2 porque P1 vuelve a impartirla.
            await EjecutarAsync(conexion, null, Sql(Operacion.AsociarBloqueAP2, e));
            await EjecutarAsync(conexion, null, Sql(Operacion.QuitarA2DeP1, e));
            await EjecutarAsync(conexion, null,
                $"""INSERT INTO "PROGRAMA_ASIGNATURA" ("idPrograma", "idAsignatura") VALUES ('{e.P2}', '{e.A2}'), ('{e.P1}', '{e.A2}')""");
            await EjecutarAsync(conexion, null, Sql(Operacion.CambiarAsignaturaDelBloqueAA2, e));

            Assert.Equal(0, await ContarViolacionesAsync(e));
            Assert.Equal(2, await ContarAsync(conexion,
                $"""SELECT count(*) FROM "BLOQUE_PROGRAMA" WHERE "idBloque" = '{e.Bloque}'"""));
        }
        finally
        {
            await LimpiarAsync(e);
        }
    }

    private static string Sql(Operacion operacion, Escenario e) => operacion switch
    {
        Operacion.AsociarBloqueAP2 =>
            $"""INSERT INTO "BLOQUE_PROGRAMA" ("idBloque", "idPrograma") VALUES ('{e.Bloque}', '{e.P2}')""",
        Operacion.CambiarAsignaturaDelBloqueAA2 =>
            $"""UPDATE "BLOQUE_CONTENIDO" SET "idAsignatura" = '{e.A2}' WHERE "idBloque" = '{e.Bloque}'""",
        Operacion.QuitarA1DeP2 =>
            $"""DELETE FROM "PROGRAMA_ASIGNATURA" WHERE "idPrograma" = '{e.P2}' AND "idAsignatura" = '{e.A1}'""",
        Operacion.QuitarA2DeP1 =>
            $"""DELETE FROM "PROGRAMA_ASIGNATURA" WHERE "idPrograma" = '{e.P1}' AND "idAsignatura" = '{e.A2}'""",
        _ => throw new ArgumentOutOfRangeException(nameof(operacion)),
    };

    private static async Task EjecutarAsync(
        NpgsqlConnection conexion,
        NpgsqlTransaction? transaccion,
        string sql)
    {
        await using var comando = new NpgsqlCommand(sql, conexion, transaccion);
        await comando.ExecuteNonQueryAsync();
    }

    private static async Task<PostgresException?> IntentarAsync(
        NpgsqlConnection conexion,
        NpgsqlTransaction transaccion,
        string sql)
    {
        try
        {
            await EjecutarAsync(conexion, transaccion, sql);
            return null;
        }
        catch (PostgresException error)
        {
            return error;
        }
    }

    private static async Task<PostgresException?> IntentarConfirmarAsync(NpgsqlTransaction transaccion)
    {
        try
        {
            await transaccion.CommitAsync();
            return null;
        }
        catch (PostgresException error)
        {
            return error;
        }
    }

    /// <summary>
    /// Espera a que la segunda operación termine o quede bloqueada esperando
    /// un bloqueo de la primera, sin depender de pausas fijas.
    /// </summary>
    private static async Task EsperarBloqueoOFinAsync(int pid, Task segunda)
    {
        await using var monitor = new NpgsqlConnection(Conexion);
        await monitor.OpenAsync();
        DateTime limite = DateTime.UtcNow + EsperaMaxima;

        while (!segunda.IsCompleted)
        {
            long esperando = await ContarAsync(monitor,
                $"SELECT count(*) FROM pg_stat_activity WHERE pid = {pid} AND wait_event_type = 'Lock'");
            if (esperando > 0)
            {
                return;
            }

            if (DateTime.UtcNow > limite)
            {
                throw new TimeoutException("La segunda operación no terminó ni quedó bloqueada.");
            }

            await Task.Delay(20);
        }
    }

    private static async Task<long> ContarViolacionesAsync(Escenario e)
    {
        await using var conexion = new NpgsqlConnection(Conexion);
        await conexion.OpenAsync();
        return await ContarAsync(conexion,
            $"""
            SELECT count(*)
            FROM "BLOQUE_PROGRAMA" AS bp
            JOIN "BLOQUE_CONTENIDO" AS b ON b."idBloque" = bp."idBloque"
            WHERE bp."idBloque" = '{e.Bloque}'
              AND NOT EXISTS (
                  SELECT 1
                  FROM "PROGRAMA_ASIGNATURA" AS pa
                  WHERE pa."idPrograma" = bp."idPrograma"
                    AND pa."idAsignatura" = b."idAsignatura")
            """);
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

        Guid coordinador = Guid.NewGuid();
        PersonaPersistenceRows filas = PersonaPersistenceMapper.ToRows(
            Persona.Crear(coordinador, "Coordinador RII03", new PerfilCoordinador("Concurrencia")));
        db.Personas.Add(filas.Persona);
        db.Coordinadores.Add(filas.Coordinador!);

        // Estos datos se confirman para que las dos conexiones los vean, así
        // que llevan nombres únicos para no aparecer en los filtros por nombre
        // de otras pruebas que se ejecutan en paralelo.
        string sufijo = Guid.NewGuid().ToString("N");
        Asignatura a1 = Asignatura.Crear(Guid.NewGuid(), $"RII03 A1 {sufijo}");
        Asignatura a2 = Asignatura.Crear(Guid.NewGuid(), $"RII03 A2 {sufijo}");
        ProgramaAcademico p1 = ProgramaAcademico.Crear(Guid.NewGuid(), $"RII03 P1 {sufijo}", "Plan 2026");
        ProgramaAcademico p2 = ProgramaAcademico.Crear(Guid.NewGuid(), $"RII03 P2 {sufijo}", "Plan 2026");
        p1.AsociarAsignatura(a1.Id);
        p1.AsociarAsignatura(a2.Id);
        p2.AsociarAsignatura(a1.Id);
        db.Asignaturas.AddRange(a1, a2);
        db.Programas.AddRange(p1, p2);

        // RII03 debe ver la pertenencia antes de insertar BLOQUE_PROGRAMA.
        await db.SaveChangesAsync();

        BloqueContenido bloque = BloqueContenido.Crear(
            Guid.NewGuid(), $"Concurrencia {Guid.NewGuid():N}", TipoActividad.Teoria,
            NivelComplejidad.Medio, a1.Id, coordinador);
        bloque.AsociarPrograma(p1);
        db.Bloques.Add(bloque);
        await db.SaveChangesAsync();

        return new Escenario(coordinador, a1.Id, a2.Id, p1.Id, p2.Id, bloque.Id);
    }

    private static async Task LimpiarAsync(Escenario e)
    {
        await using var db = new TimeblockDbContext(Opciones());
        Guid[] programas = [e.P1, e.P2];
        Guid[] asignaturas = [e.A1, e.A2];

        await db.BloquesProgramas.Where(relacion => relacion.BloqueId == e.Bloque).ExecuteDeleteAsync();
        await db.Bloques.Where(bloque => bloque.Id == e.Bloque).ExecuteDeleteAsync();
        await db.ProgramasAsignaturas.Where(relacion => programas.Contains(relacion.ProgramaId)).ExecuteDeleteAsync();
        await db.Programas.Where(programa => programas.Contains(programa.Id)).ExecuteDeleteAsync();
        await db.Asignaturas.Where(asignatura => asignaturas.Contains(asignatura.Id)).ExecuteDeleteAsync();
        await db.Coordinadores.Where(c => c.IdCoordinador == e.Coordinador).ExecuteDeleteAsync();
        await db.Personas.Where(persona => persona.IdPersona == e.Coordinador).ExecuteDeleteAsync();
    }
}
