using Timeblock.Domain.Common;

namespace Timeblock.Domain.Personas;

/// <summary>
/// Persona que participa en el sistema. El perfil se crea junto con la persona
/// para que nunca exista una persona sin perfil o con varios perfiles.
/// </summary>
public sealed class Persona
{
    private Persona(Guid id, string nombre, Perfil perfil)
    {
        Id = id;
        Nombre = nombre;
        Perfil = perfil;
    }

    public Guid Id { get; }

    public string Nombre { get; private set; }

    public Perfil Perfil { get; private set; }

    public static Persona Crear(string nombre, Perfil perfil) =>
        Crear(Guid.NewGuid(), nombre, perfil);

    public static Persona Crear(Guid id, string nombre, Perfil perfil)
    {
        ArgumentNullException.ThrowIfNull(perfil);

        var persona = new Persona(
            Guard.IdValido(id, "personaId"),
            Guard.Requerido(nombre, "nombre"),
            perfil);
        persona.ComprobarCoordinadorAsignado(perfil);
        return persona;
    }

    public void ActualizarNombre(string nombre)
    {
        Nombre = Guard.Requerido(nombre, "nombre");
    }

    /// <summary>
    /// Actualiza los datos del perfil existente. El tipo de perfil no puede
    /// cambiar.
    /// </summary>
    public void ActualizarPerfil(Perfil perfil)
    {
        ArgumentNullException.ThrowIfNull(perfil);

        if (perfil.Tipo != Perfil.Tipo)
        {
            throw new DomainException(
                $"No se puede cambiar el perfil {Perfil.Tipo} por {perfil.Tipo}.");
        }

        ComprobarCoordinadorAsignado(perfil);
        Perfil = perfil;
    }

    private void ComprobarCoordinadorAsignado(Perfil perfil)
    {
        if (perfil is PerfilEstudiante estudiante && estudiante.CoordinadorId == Id)
        {
            throw new DomainException(
                "Un estudiante no puede ser su propio coordinador.");
        }
    }
}
