using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Aulas;

namespace Timeblock.Application.Aulas;

public sealed class AulasService(IAulaRepository aulas) : ICasoDeUso
{
    public async Task<Aula> CrearAsync(
        string? edificio,
        int? capacidad,
        CancellationToken cancellationToken)
    {
        // Las reglas de campo lanzan ValidacionCamposException (400).
        Aula aula = Aula.Crear(Guid.NewGuid(), edificio, capacidad);

        await aulas.AgregarAsync(aula, cancellationToken);
        return aula;
    }

    public async Task<Aula> ObtenerAsync(
        Guid aulaId,
        CancellationToken cancellationToken)
    {
        ValidarId(aulaId);

        return await aulas.ObtenerAsync(aulaId, cancellationToken)
            ?? throw NoExiste(aulaId);
    }

    public async Task<PagedResult<Aula>> ListarAsync(
        string? edificio,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        // PageRequest rechaza con una DomainException (400) los valores
        // fuera de rango.
        PageRequest pagina = PageRequest.Create(page, pageSize);

        string? filtro = string.IsNullOrWhiteSpace(edificio)
            ? null
            : edificio.Trim();

        return await aulas.ListarAsync(filtro, pagina, cancellationToken);
    }

    public async Task<Aula> ActualizarAsync(
        Guid aulaId,
        string? edificio,
        int? capacidad,
        CancellationToken cancellationToken)
    {
        // Orden del contrato: formato (400) antes de comprobar existencia (404).
        ValidarId(aulaId);
        (string edificioValido, int capacidadValida) =
            Aula.Validar(edificio, capacidad);

        Aula aula = await aulas.ObtenerAsync(aulaId, cancellationToken)
            ?? throw NoExiste(aulaId);

        aula.Actualizar(edificioValido, capacidadValida);

        if (!await aulas.ActualizarAsync(aula, cancellationToken))
        {
            throw NoExiste(aulaId);
        }

        return aula;
    }

    private static void ValidarId(Guid aulaId)
    {
        if (aulaId == Guid.Empty)
        {
            throw new InvalidRequestException(
                "El identificador del aula no es válido.");
        }
    }

    private static ResourceNotFoundException NoExiste(Guid aulaId) =>
        new($"No existe el aula '{aulaId}'.");
}
