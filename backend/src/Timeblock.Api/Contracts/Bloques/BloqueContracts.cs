using Timeblock.Domain.Bloques;

namespace Timeblock.Api.Contracts.Bloques;

public sealed record CrearBloqueRequest
{
    public required string Descripcion { get; init; }

    public required TipoActividad TipoActividad { get; init; }

    public required NivelComplejidad NivelComplejidad { get; init; }

    public required Guid AsignaturaId { get; init; }

    public required Guid CoordinadorId { get; init; }

    /// <summary>Programas donde el bloque puede utilizarse; puede ir vacío.</summary>
    public IReadOnlyList<Guid> ProgramasIds { get; init; } = [];
}


public sealed record ActualizarBloqueRequest
{
    public string? Descripcion { get; init; }

    public TipoActividad? TipoActividad { get; init; }

    public NivelComplejidad? NivelComplejidad { get; init; }

    public Guid? AsignaturaId { get; init; }
}

public sealed record BloqueResponse(
    Guid Id,
    string Descripcion,
    TipoActividad TipoActividad,
    NivelComplejidad NivelComplejidad,
    Guid AsignaturaId,
    Guid CoordinadorId,
    IReadOnlyList<Guid> ProgramasIds)
{
    public static BloqueResponse FromDomain(BloqueContenido bloque)
    {
        ArgumentNullException.ThrowIfNull(bloque);

        return new BloqueResponse(
            bloque.Id,
            bloque.Descripcion,
            bloque.TipoActividad,
            bloque.NivelComplejidad,
            bloque.AsignaturaId,
            bloque.CoordinadorCreadorId,
            bloque.Programas
                .Select(asociacion => asociacion.ProgramaId)
                .Order()
                .ToArray());
    }
}
