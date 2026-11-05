using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Personas;

namespace Timeblock.Application.Horarios;


public interface IHorarioRepository
{
    Task<bool> ExisteProgramaAsync(Guid programaId, CancellationToken cancellationToken);


    Task<bool> ExisteCoordinadorAsync(Guid coordinadorId, CancellationToken cancellationToken);


    Task<IReadOnlyList<AutorizacionCoordinador>> ObtenerAutorizacionesAsync(
        Guid coordinadorId,
        Guid programaId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<BloqueContenido>> ObtenerBloquesAsync(
        IReadOnlyCollection<Guid> bloquesIds,
        CancellationToken cancellationToken);

    Task AgregarAsync(Horario horario, CancellationToken cancellationToken);

    Task<Horario?> ObtenerAsync(Guid horarioId, CancellationToken cancellationToken);

    Task<PagedResult<Horario>> ListarAsync(
        HorarioFilter filter,
        PageRequest page,
        CancellationToken cancellationToken);
}
