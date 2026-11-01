using Timeblock.Domain.Common;

namespace Timeblock.Application.Common.Pagination;

/// <summary>
/// Parámetros de paginación de los listados (<c>page</c> y <c>pageSize</c>),
/// con los valores por defecto y el máximo definidos en el contrato de la API.
/// </summary>
public sealed class PageRequest
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    private PageRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }

    public int PageSize { get; }

    /// <summary>Cantidad de elementos que se omiten antes de la página.</summary>
    public int Skip => (Page - 1) * PageSize;

    public static PageRequest Default { get; } =
        new(DefaultPage, DefaultPageSize);

    /// <summary>
    /// Crea la solicitud aplicando los valores por defecto cuando un parámetro
    /// no se informa. Los valores fuera de rango se rechazan con una
    /// <see cref="DomainException"/> de validación.
    /// </summary>
    public static PageRequest Create(int? page, int? pageSize)
    {
        int paginaSolicitada = page ?? DefaultPage;
        int tamano = pageSize ?? DefaultPageSize;

        if (paginaSolicitada < 1)
        {
            throw new DomainException(
                "El parámetro page debe ser mayor o igual que 1.");
        }

        if (tamano is < 1 or > MaxPageSize)
        {
            throw new DomainException(
                $"El parámetro pageSize debe estar entre 1 y {MaxPageSize}.");
        }

        if ((long)(paginaSolicitada - 1) * tamano > int.MaxValue)
        {
            throw new DomainException(
                "La página solicitada excede el rango permitido.");
        }

        return new PageRequest(paginaSolicitada, tamano);
    }
}
