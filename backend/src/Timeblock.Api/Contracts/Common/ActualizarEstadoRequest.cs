namespace Timeblock.Api.Contracts.Common;

public sealed record ActualizarEstadoRequest
{
    public required bool Activo { get; init; }
}
