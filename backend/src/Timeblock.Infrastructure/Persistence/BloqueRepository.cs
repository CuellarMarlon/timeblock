using Microsoft.EntityFrameworkCore;
using Npgsql;
using Timeblock.Application.Bloques;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;

namespace Timeblock.Infrastructure.Persistence;

public sealed class BloqueRepository(TimeblockDbContext db) : IBloqueRepository
{
    private const string IndiceClaveDuplicidad = "UX_BLOQUE_CONTENIDO_ClaveDuplicidad";

    public Task<bool> ExisteAsignaturaAsync(Guid asignaturaId, CancellationToken ct) =>
        db.Asignaturas.AnyAsync(asignatura => asignatura.Id == asignaturaId, ct);

    public Task<bool> ExisteCoordinadorAsync(Guid coordinadorId, CancellationToken ct) =>
        db.Coordinadores.AnyAsync(coordinador => coordinador.IdCoordinador == coordinadorId, ct);

    public async Task<IReadOnlyList<ProgramaAcademico>> ObtenerProgramasAsync(
        IReadOnlyCollection<Guid> programasIds,
        CancellationToken ct)
    {
        Guid[] ids = programasIds.ToArray();

        return await db.Programas
            .AsNoTracking()
            .Include(programa => programa.Asignaturas)
            .Where(programa => ids.Contains(programa.Id))
            .ToListAsync(ct);
    }

    public Task<bool> ExisteDuplicadoAsync(
        ClaveDuplicidad clave,
        Guid? excluirBloqueId,
        CancellationToken ct)
    {
        Guid asignaturaId = clave.AsignaturaId;
        TipoActividad tipoActividad = clave.TipoActividad;
        string descripcionNormalizada = clave.DescripcionNormalizada;

        return db.Bloques.AnyAsync(
            bloque => bloque.AsignaturaId == asignaturaId
                && bloque.TipoActividad == tipoActividad
                && bloque.DescripcionNormalizada == descripcionNormalizada
                && (excluirBloqueId == null || bloque.Id != excluirBloqueId),
            ct);
    }

    public async Task AgregarAsync(BloqueContenido bloque, CancellationToken ct)
    {
        db.Bloques.Add(bloque);
        await GuardarTraduciendoConflictosAsync(ct);
    }

    public Task<BloqueContenido?> ObtenerAsync(Guid bloqueId, CancellationToken ct) =>
        db.Bloques
            .Include(bloque => bloque.Programas)
            .SingleOrDefaultAsync(bloque => bloque.Id == bloqueId, ct);

    public async Task<PagedResult<BloqueContenido>> ListarAsync(
        BloqueFilter filter,
        PageRequest page,
        CancellationToken ct)
    {
        IQueryable<BloqueContenido> consulta = db.Bloques
            .AsNoTracking()
            .Include(bloque => bloque.Programas);

        if (filter.ProgramaId is { } programaId)
        {
            consulta = consulta.Where(bloque =>
                bloque.Programas.Any(asociacion => asociacion.ProgramaId == programaId));
        }

        if (filter.AsignaturaId is { } asignaturaId)
        {
            consulta = consulta.Where(bloque => bloque.AsignaturaId == asignaturaId);
        }

        if (filter.CoordinadorId is { } coordinadorId)
        {
            consulta = consulta.Where(bloque => bloque.CoordinadorCreadorId == coordinadorId);
        }

        if (filter.TipoActividad is { } tipoActividad)
        {
            consulta = consulta.Where(bloque => bloque.TipoActividad == tipoActividad);
        }

        if (filter.NivelComplejidad is { } nivelComplejidad)
        {
            consulta = consulta.Where(bloque => bloque.NivelComplejidad == nivelComplejidad);
        }

        return await consulta
            .OrderBy(bloque => bloque.DescripcionNormalizada)
            .ThenBy(bloque => bloque.Id)
            .ToPagedResultAsync(page, ct);
    }

    public Task GuardarCambiosAsync(CancellationToken ct) =>
        GuardarTraduciendoConflictosAsync(ct);

    /// <summary>
    /// La restricción de unicidad de la clave de duplicidad protege los bloques
    /// aunque dos solicitudes pasen a la vez la comprobación previa.
    /// </summary>
    private async Task GuardarTraduciendoConflictosAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } error)
        {
            string mensaje = error.ConstraintName == IndiceClaveDuplicidad
                ? "Ya existe un bloque con la misma asignatura, tipo de actividad y descripción."
                : "La relación indicada ya existe.";

            throw new ResourceConflictException(mensaje, ex);
        }
    }
}
