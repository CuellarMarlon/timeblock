using Microsoft.EntityFrameworkCore;
using Timeblock.Application.Common.Pagination;

namespace Timeblock.Infrastructure.Persistence;

public static class PagedQueryExtensions
{
    /// <summary>
    /// Ejecuta la consulta y devuelve la página solicitada junto con el total
    /// de elementos. La consulta debe tener un orden determinista
    /// (<c>OrderBy</c>) para que las páginas sean estables.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(
        this IQueryable<T> query,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(page);

        int total = await query.CountAsync(cancellationToken);

        List<T> items = await query
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return PagedResult<T>.Create(items, page, total);
    }
}
