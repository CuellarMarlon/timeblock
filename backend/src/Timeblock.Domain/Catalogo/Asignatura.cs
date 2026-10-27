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
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la asignatura es obligatorio.",
                nameof(id));
        }

        return new Asignatura(id, NormalizarNombre(nombre));
    }

    public void CambiarNombre(string nombre)
    {
        Nombre = NormalizarNombre(nombre);
    }

    private static string NormalizarNombre(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            throw new ArgumentException(
                "El nombre de la asignatura es obligatorio.",
                nameof(nombre));
        }

        return nombre.Trim();
    }
}