using Microsoft.EntityFrameworkCore;
using Npgsql;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Application.Horarios;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Personas;

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

   
    /// El horario y sus asociaciones se guardan con una sola llamada a
    /// <c>SaveChangesAsync</c>, que PostgreSQL ejecuta en una transacción: si
    /// algo falla no queda un horario parcial. El contexto agrega en esa misma
    /// operación las fechas que aún no existan.
   
    public async Task AgregarAsync(Horario horario, CancellationToken ct)
    {
        db.Horarios.Add(horario);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ResourceConflictException(
                "El horario entra en conflicto con datos ya existentes.", ex);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            throw new ResourceConflictException(
                "El horario hace referencia a datos que cambiaron o ya no existen.", ex);
        }
    }

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
