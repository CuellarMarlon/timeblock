namespace Timeblock.Application.Common.Pagination;


public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems)
{
    public static PagedResult<T> Create(
        IReadOnlyList<T> items,
        PageRequest request,
        int totalItems)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfNegative(totalItems);

        return new PagedResult<T>(
            items,
            request.Page,
            request.PageSize,
            totalItems);
    }

    /// <summary>
    /// Convierte los elementos (por ejemplo, de entidades a DTO) conservando
    /// los datos de paginación.
    /// </summary>
    public PagedResult<TResult> Map<TResult>(Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(selector);

        return new PagedResult<TResult>(
            Items.Select(selector).ToList(),
            Page,
            PageSize,
            TotalItems);
    }
}
