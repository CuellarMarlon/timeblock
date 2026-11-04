namespace Timeblock.Api.Contracts.Common;

public sealed record PageResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems);
