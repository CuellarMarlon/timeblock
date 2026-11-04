using Timeblock.Application.Personas;
using Timeblock.Domain.Personas;
using System.Text.Json.Serialization;
using Timeblock.Api.Contracts.Common;

namespace Timeblock.Api.Contracts.Personas;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "tipo")]
[JsonDerivedType(typeof(PerfilEstudianteResponse), "estudiante")]
[JsonDerivedType(typeof(PerfilCoordinadorResponse), "coordinador")]
[JsonDerivedType(typeof(PerfilJefeDepartamentoResponse), "jefeDepartamento")]
public abstract record PerfilResponse;

public sealed record PersonaResponse(Guid Id, string Nombre, PerfilResponse Perfil)
{
    public static PersonaResponse FromDomain(Persona persona) => new(
        persona.Id,
        persona.Nombre,
        persona.Perfil switch
        {
            PerfilEstudiante p => new PerfilEstudianteResponse(p.Edad, p.Grupo, p.CoordinadorId),
            PerfilCoordinador p => new PerfilCoordinadorResponse(p.Especialidad),
            PerfilJefeDepartamento => new PerfilJefeDepartamentoResponse(),
            _ => throw new InvalidOperationException("Perfil no soportado.")
        });
}

public sealed record PerfilEstudianteResponse(int Edad, string Grupo, Guid CoordinadorId) : PerfilResponse;
public sealed record PerfilCoordinadorResponse(string Especialidad) : PerfilResponse;
public sealed record PerfilJefeDepartamentoResponse : PerfilResponse;
public sealed record ActualizarPersonaRequest
{
    public required string Nombre { get; init; }
}

public sealed record CrearAutorizacionRequest
{
    public required Guid ProgramaId { get; init; }
    public required DateOnly FechaInicio { get; init; }
    public DateOnly? FechaFin { get; init; }
    public required bool PuedeGenerar { get; init; }
    public required bool PuedeValidar { get; init; }
}

public sealed record ActualizarAutorizacionRequest
{
    public DateOnly? FechaFin { get; init; }
    public required bool PuedeGenerar { get; init; }
    public required bool PuedeValidar { get; init; }
}

public sealed record AutorizacionResponse(
    Guid CoordinadorId, Guid ProgramaId, DateOnly FechaInicio, DateOnly? FechaFin,
    bool PuedeGenerar, bool PuedeValidar)
{
    public static AutorizacionResponse FromDomain(AutorizacionCoordinador a) => new(
        a.CoordinadorId, a.ProgramaId, a.FechaInicio, a.FechaFin, a.PuedeGenerar, a.PuedeValidar);
}
