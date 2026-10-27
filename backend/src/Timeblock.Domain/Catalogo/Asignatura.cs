using Timeblock.Domain.Common;

namespace Timeblock.Domain.Catalogo;

public sealed class Asignatura
{
    public Guid Id { get; private set; }

    public string Nombre { get; private set; }

    private Asignatura(Guid id, string nombre)
    {
        Id = id;
        Nombre = nombre;
    }

    public static Asignatura Crear(Guid id, string nombre)
    {
        return new Asignatura(
            Guard.IdValido(id, "asignaturaId"),
            Guard.Requerido(nombre, "nombre"));
    }

    public void CambiarNombre(string nombre)
    {
        Nombre = Guard.Requerido(nombre, "nombre");
    }
}
