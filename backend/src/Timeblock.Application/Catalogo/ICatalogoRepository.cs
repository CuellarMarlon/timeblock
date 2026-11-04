using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Catalogo;

namespace Timeblock.Application.Catalogo;

public interface ICatalogoRepository
{
    Task AgregarProgramaAsync(ProgramaAcademico programa, CancellationToken cancellationToken);
    Task<ProgramaAcademico?> ObtenerProgramaAsync(Guid programaId, CancellationToken cancellationToken);
    Task<PagedResult<ProgramaAcademico>> ListarProgramasAsync(
        string? nombre, PageRequest page, CancellationToken cancellationToken);
    Task GuardarProgramaAsync(CancellationToken cancellationToken);

    Task AgregarAsignaturaAsync(Asignatura asignatura, CancellationToken cancellationToken);
    Task<Asignatura?> ObtenerAsignaturaAsync(Guid asignaturaId, CancellationToken cancellationToken);
    Task<PagedResult<Asignatura>> ListarAsignaturasAsync(
        string? nombre, PageRequest page, CancellationToken cancellationToken);
    Task GuardarAsignaturaAsync(CancellationToken cancellationToken);

    Task<bool> ExisteProgramaAsync(Guid programaId, CancellationToken cancellationToken);
    Task<bool> ExisteAsignaturaAsync(Guid asignaturaId, CancellationToken cancellationToken);
    Task<bool> EstaAsociadaAsync(Guid programaId, Guid asignaturaId, CancellationToken cancellationToken);
    Task AsociarAsync(Guid programaId, Guid asignaturaId, CancellationToken cancellationToken);
    Task<PagedResult<Asignatura>> ListarAsignaturasDeProgramaAsync(
        Guid programaId, PageRequest page, CancellationToken cancellationToken);
}
