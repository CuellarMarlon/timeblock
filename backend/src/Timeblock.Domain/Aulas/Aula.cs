using Timeblock.Domain.Common;

namespace Timeblock.Domain.Aulas;


public sealed class Aula
{
    public const int EdificioLongitudMaxima = 100;

    public const int CapacidadMaxima = 1000;

    private Aula(Guid id, string edificio, int capacidad)
    {
        Id = id;
        Edificio = edificio;
        Capacidad = capacidad;
    }

    public Guid Id { get; }

    public string Edificio { get; private set; }

    public int Capacidad { get; private set; }

    public static Aula Crear(Guid id, string? edificio, int? capacidad)
    {
        Guard.IdValido(id, "aulaId");
        (string edificioValido, int capacidadValida) =
            Validar(edificio, capacidad);

        return new Aula(id, edificioValido, capacidadValida);
    }

    public static Aula Reconstituir(Guid id, string edificio, int capacidad) =>
        new(id, edificio, capacidad);

 
    public void Actualizar(string? edificio, int? capacidad)
    {
        (Edificio, Capacidad) = Validar(edificio, capacidad);
    }

    public static (string Edificio, int Capacidad) Validar(
        string? edificio,
        int? capacidad)
    {
        Dictionary<string, string[]> errores = [];

        string edificioNormalizado = edificio?.Trim() ?? string.Empty;
        if (edificioNormalizado.Length == 0)
        {
            errores["edificio"] = ["El edificio es obligatorio."];
        }
        else if (edificioNormalizado.Length > EdificioLongitudMaxima)
        {
            errores["edificio"] =
                [$"El edificio no puede superar {EdificioLongitudMaxima} caracteres."];
        }

        if (capacidad is null)
        {
            errores["capacidad"] = ["La capacidad es obligatoria."];
        }
        else if (capacidad <= 0)
        {
            errores["capacidad"] = ["La capacidad debe ser mayor que cero."];
        }
        else if (capacidad > CapacidadMaxima)
        {
            errores["capacidad"] =
                [$"La capacidad no puede superar {CapacidadMaxima}."];
        }

        if (errores.Count > 0)
        {
            throw new ValidacionCamposException(errores);
        }

        return (edificioNormalizado, capacidad!.Value);
    }
}
