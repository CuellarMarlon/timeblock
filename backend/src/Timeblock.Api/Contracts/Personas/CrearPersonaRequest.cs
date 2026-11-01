using System.Text.Json.Serialization;

namespace Timeblock.Api.Contracts.Personas;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "tipo")]
[JsonDerivedType(typeof(PerfilEstudianteRequest), "estudiante")]
[JsonDerivedType(typeof(PerfilCoordinadorRequest), "coordinador")]
[JsonDerivedType(typeof(PerfilJefeDepartamentoRequest), "jefeDepartamento")]
public abstract record PerfilRequest;

public sealed record PerfilEstudianteRequest : PerfilRequest
{
    public required int Edad { get; init; }

    public required string Grupo { get; init; }

    public required Guid CoordinadorId { get; init; }
}

public sealed record PerfilCoordinadorRequest : PerfilRequest
{
    public required string Especialidad { get; init; }
}

public sealed record PerfilJefeDepartamentoRequest : PerfilRequest;

public sealed record CrearPersonaRequest
{
    public required string Nombre { get; init; }

    public required PerfilRequest Perfil { get; init; }
}