using Microsoft.EntityFrameworkCore;
using Npgsql;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Application.Horarios;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Personas;
using Timeblock.Infrastructure.Persistence.Configurations;

namespace Timeblock.Infrastructure.Persistence;

public sealed class HorarioRepository(TimeblockDbContext db) : IHorarioRepository
{
    public Task<bool> ExisteProgramaAsync(Guid programaId, CancellationToken ct) =>
        db.Programas.AnyAsync(programa => programa.Id == programaId, ct);

    public Task<bool> ExisteCoordinadorAsync(Guid coordinadorId, CancellationToken ct) =>
        db.Coordinadores.AnyAsync(coordinador => coordinador.IdCoordinador == coordinadorId, ct);

    public async Task<IReadOnlyList<AutorizacionCoordinador>> ObtenerAutorizacionesAsync(
        Guid coordinadorId,
        Guid programaId,
        CancellationToken ct) =>
        await db.Autorizaciones
            .AsNoTracking()
            .Where(autorizacion => autorizacion.CoordinadorId == coordinadorId
                && autorizacion.ProgramaId == programaId)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<BloqueContenido>> ObtenerBloquesAsync(
        IReadOnlyCollection<Guid> bloquesIds,
        CancellationToken ct)
    {
        Guid[] ids = bloquesIds.ToArray();

        return await db.Bloques
            .AsNoTracking()
            .Include(bloque => bloque.Programas)
            .Where(bloque => ids.Contains(bloque.Id))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlySet<Guid>> ObtenerAulasExistentesAsync(
        IReadOnlyCollection<Guid> aulasIds,
        CancellationToken ct)
    {
        Guid[] ids = aulasIds.ToArray();

        return (await db.Aulas
                .AsNoTracking()
                .Where(aula => ids.Contains(aula.IdAula))
                .Select(aula => aula.IdAula)
                .ToListAsync(ct))
            .ToHashSet();
    }

    /// <summary>
    /// El horario y sus asociaciones se guardan con una sola llamada a
    /// <c>SaveChangesAsync</c>, que PostgreSQL ejecuta en una transacción: si
    /// algo falla no queda un horario parcial. El contexto agrega en esa misma
    /// operación las fechas que aún no existan.
    /// </summary>
    public async Task AgregarAsync(Horario horario, CancellationToken ct)
    {
        db.Horarios.Add(horario);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException error && OcupaAulaOcupada(error))
        {
            throw new ResourceConflictException(
                "El aula ya tiene otra impartición que se solapa con ese intervalo.", ex);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ExclusionViolation,
                ConstraintName: RestriccionesImparticion.HorarioSinSolapes,
            })
        {
            throw new ResourceConflictException(
                "La impartición se solapa con otra del mismo horario en esa fecha.", ex);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ResourceConflictException(
                "El horario entra en conflicto con datos ya existentes.", ex);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException error && error.SqlState == PostgresErrorCodes.CheckViolation &&
                  error.MessageText.StartsWith("AUTORIZACION_HORARIO:", StringComparison.Ordinal))
        {
            throw new ResourceConflictException("La autorización cambió y ya no permite crear este horario.", ex);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new ResourceConflictException(
                "El horario hace referencia a datos que cambiaron o ya no existen.", ex);
        }
    }

    /// <summary>
    /// RII04: la restricción de exclusión rechaza intervalos solapados en la
    /// misma aula y fecha, y el índice único de la clave alternativa del
    /// diseño, dos imparticiones que empiezan a la vez en la misma aula.
    /// </summary>
    private static bool OcupaAulaOcupada(PostgresException error) =>
        (error.SqlState == PostgresErrorCodes.ExclusionViolation &&
         error.ConstraintName == RestriccionesImparticion.AulaSinSolapes) ||
        (error.SqlState == PostgresErrorCodes.UniqueViolation &&
         error.ConstraintName == RestriccionesImparticion.AulaFechaHoraInicio);

    public Task<Horario?> ObtenerAsync(Guid horarioId, CancellationToken ct) =>
        db.Horarios
            .AsNoTracking()
            .Include(horario => horario.Bloques)
            .SingleOrDefaultAsync(horario => horario.Id == horarioId, ct);

    public async Task<PagedResult<Horario>> ListarAsync(
        HorarioFilter filter,
        PageRequest page,
        CancellationToken ct)
    {
        IQueryable<Horario> consulta = db.Horarios
            .AsNoTracking()
            .Include(horario => horario.Bloques);

        if (filter.ProgramaId is { } programaId)
        {
            consulta = consulta.Where(horario => horario.ProgramaId == programaId);
        }

        if (filter.PeriodoAcademico is { } periodo)
        {
            consulta = consulta.Where(horario => horario.PeriodoAcademico == periodo);
        }

        if (filter.Estado is { } estado)
        {
            consulta = consulta.Where(horario => horario.Estado == estado);
        }

        return await consulta
            .OrderByDescending(horario => horario.FechaCreacion)
            .ThenBy(horario => horario.Id)
            .ToPagedResultAsync(page, ct);
    }
}
