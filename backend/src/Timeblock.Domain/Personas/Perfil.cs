using Timeblock.Domain.Common;

namespace Timeblock.Domain.Personas;

/// <summary>
/// Perfil exclusivo de una persona. Cada persona tiene exactamente uno.
/// </summary>
public abstract class Perfil
{
    public abstract TipoPerfil Tipo { get; }
}

public sealed class PerfilEstudiante : Perfil
{
    public PerfilEstudiante(int edad, string grupo, Guid coordinadorId)
    {
        if (edad <= 0)
        {
            throw new DomainException("La edad debe ser mayor que cero.");
        }

        Edad = edad;
        Grupo = Guard.Requerido(grupo, "grupo");
        CoordinadorId = Guard.IdValido(coordinadorId, "coordinadorId");
    }

    public override TipoPerfil Tipo => TipoPerfil.Estudiante;

    public int Edad { get; }

    public string Grupo { get; }

    /// <summary>
    /// Coordinador asignado. Que corresponda a una persona con perfil de
    /// coordinador se comprueba en Application y persistencia.
    /// </summary>
    public Guid CoordinadorId { get; }
}

public sealed class PerfilCoordinador : Perfil
{
    public PerfilCoordinador(string especialidad)
    {
        Especialidad = Guard.Requerido(especialidad, "especialidad");
    }

    public override TipoPerfil Tipo => TipoPerfil.Coordinador;

    public string Especialidad { get; }
}

public sealed class PerfilJefeDepartamento : Perfil
{
    public override TipoPerfil Tipo => TipoPerfil.JefeDepartamento;
}
