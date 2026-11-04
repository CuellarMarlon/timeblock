namespace Timeblock.Api.Contracts.Catalogo;

public sealed record CrearProgramaRequest
{
    public required string Nombre { get; init; }
    public required string PlanEstudio { get; init; }
}

public sealed record ActualizarProgramaRequest
{
    public required string Nombre { get; init; }
    public required string PlanEstudio { get; init; }
}

public sealed record CrearAsignaturaRequest
{
    public required string Nombre { get; init; }
}

public sealed record ActualizarAsignaturaRequest
{
    public required string Nombre { get; init; }
}
