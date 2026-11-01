using Timeblock.Domain.Personas;

namespace Timeblock.Application.Personas;

public sealed record PersonaFilter(
    string? Nombre,
    TipoPerfil? TipoPerfil,
    int Page,
    int PageSize);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalItems);

public sealed record AutorizacionFilter(Guid? ProgramaId, DateOnly? VigenteEn);
