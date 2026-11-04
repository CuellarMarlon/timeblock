using Microsoft.EntityFrameworkCore;
using Npgsql;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Application.Matriculas;
using Timeblock.Domain.Matriculas;

namespace Timeblock.Infrastructure.Persistence;

public sealed class MatriculaRepository(TimeblockDbContext db) : IMatriculaRepository
{
    public Task<bool> ExistePersonaAsync(Guid id, CancellationToken ct) =>
        db.Personas.AnyAsync(x => x.IdPersona == id, ct);

    public Task<bool> ExisteEstudianteAsync(Guid id, CancellationToken ct) =>
        db.Estudiantes.AnyAsync(x => x.IdEstudiante == id, ct);

    public Task<bool> ExisteProgramaAsync(Guid id, CancellationToken ct) =>
        db.Programas.AnyAsync(x => x.Id == id, ct);

    public Task<bool> ExisteAsync(Guid estudianteId, Guid programaId, CancellationToken ct) =>
        db.Matriculas.AnyAsync(x => x.EstudianteId == estudianteId && x.ProgramaId == programaId, ct);

    public async Task AgregarAsync(Matricula matricula, CancellationToken ct)
    {
        db.Matriculas.Add(matricula);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Otra solicitud guardó la misma pareja entre la comprobación y el guardado.
            db.Entry(matricula).State = EntityState.Detached;
            throw new ResourceConflictException("El estudiante ya está matriculado en ese programa.", ex);
        }
    }

    public Task<PagedResult<Matricula>> ListarPorProgramaAsync(Guid programaId, PageRequest page, CancellationToken ct) =>
        db.Matriculas.AsNoTracking()
            .Where(x => x.ProgramaId == programaId)
            .OrderBy(x => x.EstudianteId)
            .ToPagedResultAsync(page, ct);

    public Task<PagedResult<Matricula>> ListarPorEstudianteAsync(Guid estudianteId, PageRequest page, CancellationToken ct) =>
        db.Matriculas.AsNoTracking()
            .Where(x => x.EstudianteId == estudianteId)
            .OrderBy(x => x.ProgramaId)
            .ToPagedResultAsync(page, ct);
}
