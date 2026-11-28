using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Aulas;

namespace Timeblock.Application.Aulas;

public interface IAulaRepository
{
    Task AgregarAsync(Aula aula, CancellationToken cancellationToken);

    Task<Aula?> ObtenerAsync(Guid aulaId, CancellationToken cancellationToken);

    Task<PagedResult<Aula>> ListarAsync(
        string? edificio,
        PageRequest page,
        CancellationToken cancellationToken);

    /// Guarda el edificio y la capacidad del aula. Devuelve <c>false</c> si
    /// el aula ya no existe.
    
    Task<bool> ActualizarAsync(Aula aula, CancellationToken cancellationToken);
}
