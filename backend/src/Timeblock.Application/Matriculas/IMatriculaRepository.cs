using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Matriculas;

namespace Timeblock.Application.Matriculas;

public interface IMatriculaRepository
{
    Task<bool> ExistePersonaAsync(Guid personaId, CancellationToken cancellationToken);

    Task<bool> ExisteEstudianteAsync(Guid estudianteId, CancellationToken cancellationToken);

    Task<bool> ExisteProgramaAsync(Guid programaId, CancellationToken cancellationToken);

    /// <summary>
    /// Consulta de forma puntual si la pareja estudiante + programa ya existe.
    /// </summary>
    Task<bool> ExisteAsync(Guid estudianteId, Guid programaId, CancellationToken cancellationToken);

    /// <summary>
    /// Guarda la matrícula. Si otra solicitud guardó la misma pareja antes,
    /// lanza <see cref="Common.Exceptions.ResourceConflictException"/>.
    /// </summary>
    Task AgregarAsync(Matricula matricula, CancellationToken cancellationToken);

    Task<PagedResult<Matricula>> ListarPorProgramaAsync(
        Guid programaId, PageRequest page, CancellationToken cancellationToken);

    Task<PagedResult<Matricula>> ListarPorEstudianteAsync(
        Guid estudianteId, PageRequest page, CancellationToken cancellationToken);
}
