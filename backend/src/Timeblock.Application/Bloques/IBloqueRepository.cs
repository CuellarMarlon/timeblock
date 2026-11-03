using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;

namespace Timeblock.Application.Bloques;

public interface IBloqueRepository
{
    Task<bool> ExisteAsignaturaAsync(Guid asignaturaId, CancellationToken cancellationToken);

   
    Task<bool> ExisteCoordinadorAsync(Guid coordinadorId, CancellationToken cancellationToken);


    Task<IReadOnlyList<ProgramaAcademico>> ObtenerProgramasAsync(
        IReadOnlyCollection<Guid> programasIds,
        CancellationToken cancellationToken);

  
    Task<bool> ExisteDuplicadoAsync(
        ClaveDuplicidad clave,
        Guid? excluirBloqueId,
        CancellationToken cancellationToken);


    Task AgregarAsync(BloqueContenido bloque, CancellationToken cancellationToken);

    Task<BloqueContenido?> ObtenerAsync(Guid bloqueId, CancellationToken cancellationToken);

    Task<PagedResult<BloqueContenido>> ListarAsync(
        BloqueFilter filter,
        PageRequest page,
        CancellationToken cancellationToken);

  
    Task GuardarCambiosAsync(CancellationToken cancellationToken);
}
