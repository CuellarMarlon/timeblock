using Timeblock.Domain.Personas;

namespace Timeblock.Application.Personas;

public interface IPersonaRepository
{
    Task<bool> ExisteCoordinadorAsync(Guid coordinadorId, CancellationToken cancellationToken);
    Task<bool> ExisteProgramaAsync(Guid programaId, CancellationToken cancellationToken);
    Task AgregarAsync(Persona persona, CancellationToken cancellationToken);
    Task<Persona?> ObtenerAsync(Guid personaId, CancellationToken cancellationToken);
    Task<PagedResult<Persona>> ListarAsync(PersonaFilter filter, CancellationToken cancellationToken);
    Task GuardarCambiosAsync(Persona persona, CancellationToken cancellationToken);
    Task<IReadOnlyList<AutorizacionCoordinador>> ListarAutorizacionesAsync(
        Guid coordinadorId, AutorizacionFilter filter, CancellationToken cancellationToken);
    Task AgregarAutorizacionAsync(AutorizacionCoordinador autorizacion, CancellationToken cancellationToken);
    Task<AutorizacionCoordinador?> ObtenerAutorizacionAsync(
        Guid coordinadorId, Guid programaId, DateOnly fechaInicio, CancellationToken cancellationToken);
    Task GuardarAutorizacionAsync(AutorizacionCoordinador autorizacion, CancellationToken cancellationToken);
}
