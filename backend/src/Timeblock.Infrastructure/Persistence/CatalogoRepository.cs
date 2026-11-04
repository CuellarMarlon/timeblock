using Microsoft.EntityFrameworkCore;
using Npgsql;
using Timeblock.Application.Catalogo;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Catalogo;

namespace Timeblock.Infrastructure.Persistence;

public sealed class CatalogoRepository(TimeblockDbContext db) : ICatalogoRepository
{
    public async Task AgregarProgramaAsync(ProgramaAcademico programa, CancellationToken ct)
    {
        db.Programas.Add(programa);
        await SaveWithConflictTranslationAsync(ct);
    }

    public Task<ProgramaAcademico?> ObtenerProgramaAsync(Guid id, CancellationToken ct) =>
        db.Programas.SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<PagedResult<ProgramaAcademico>> ListarProgramasAsync(
        string? nombre, PageRequest page, CancellationToken ct)
    {
        IQueryable<ProgramaAcademico> query = db.Programas.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            string pattern = $"%{EscapeLike(nombre.Trim())}%";
            query = query.Where(x => EF.Functions.ILike(x.Nombre, pattern));
        }
        return await PaginarAsync(query, page, ct);
    }

    public Task GuardarProgramaAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task AgregarAsignaturaAsync(Asignatura asignatura, CancellationToken ct)
    {
        db.Asignaturas.Add(asignatura);
        await SaveWithConflictTranslationAsync(ct);
    }

    public Task<Asignatura?> ObtenerAsignaturaAsync(Guid id, CancellationToken ct) =>
        db.Asignaturas.SingleOrDefaultAsync(x => x.Id == id, ct);

    public async Task<PagedResult<Asignatura>> ListarAsignaturasAsync(
        string? nombre, PageRequest page, CancellationToken ct)
    {
        IQueryable<Asignatura> query = db.Asignaturas.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(nombre))
        {
            string pattern = $"%{EscapeLike(nombre.Trim())}%";
            query = query.Where(x => EF.Functions.ILike(x.Nombre, pattern));
        }
        return await PaginarAsync(query, page, ct);
    }

    public Task GuardarAsignaturaAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public Task<bool> ExisteProgramaAsync(Guid id, CancellationToken ct) =>
        db.Programas.AnyAsync(x => x.Id == id, ct);

    public Task<bool> ExisteAsignaturaAsync(Guid id, CancellationToken ct) =>
        db.Asignaturas.AnyAsync(x => x.Id == id, ct);

    public Task<bool> EstaAsociadaAsync(Guid programaId, Guid asignaturaId, CancellationToken ct) =>
        db.ProgramasAsignaturas.AnyAsync(x => x.ProgramaId == programaId && x.AsignaturaId == asignaturaId, ct);

    public async Task AsociarAsync(Guid programaId, Guid asignaturaId, CancellationToken ct)
    {
        if (db.ProgramasAsignaturas.Local.Any(x =>
            x.ProgramaId == programaId && x.AsignaturaId == asignaturaId))
            return;

        db.ProgramasAsignaturas.Add(ProgramaAsignatura.Crear(programaId, asignaturaId));
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Otra petición pudo insertar la misma relación después de nuestra
            // consulta inicial. La clave compuesta confirma que el PUT ya se cumplió.
            db.Entry(db.ChangeTracker.Entries<ProgramaAsignatura>()
                .Single(entry => entry.State == EntityState.Added).Entity).State = EntityState.Detached;
            if (!await EstaAsociadaAsync(programaId, asignaturaId, ct))
                throw new ResourceConflictException("No se pudo crear la asociación por un conflicto de unicidad.", ex);
        }
    }

    public async Task<PagedResult<Asignatura>> ListarAsignaturasDeProgramaAsync(
        Guid programaId, PageRequest page, CancellationToken ct)
    {
        IQueryable<Asignatura> query = db.Asignaturas.AsNoTracking()
            .Where(asignatura => db.ProgramasAsignaturas.Any(relacion =>
                relacion.ProgramaId == programaId && relacion.AsignaturaId == asignatura.Id));
        return await PaginarAsync(query, page, ct);
    }

    private static async Task<PagedResult<T>> PaginarAsync<T>(
        IQueryable<T> query, PageRequest page, CancellationToken ct)
    {
        int totalItems = await query.CountAsync(ct);
        List<T> items = await query
            .OrderBy(item => EF.Property<Guid>(item!, "Id"))
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(ct);
        return PagedResult<T>.Create(items, page, totalItems);
    }

    private async Task SaveWithConflictTranslationAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ResourceConflictException("El recurso ya existe o su clave está duplicada.", ex);
        }
    }

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
