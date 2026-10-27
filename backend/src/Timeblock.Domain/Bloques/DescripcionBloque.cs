using Timeblock.Domain.Common;

namespace Timeblock.Domain.Bloques;

/// <summary>
/// Normalización de la descripción de un bloque, acordada para M4:
/// quitar espacios iniciales y finales, reducir cada secuencia de espacios en
/// blanco a uno solo, ignorar mayúsculas y conservar los acentos.
/// </summary>
public static class DescripcionBloque
{
    /// <summary>
    /// Devuelve la descripción sin espacios sobrantes. Conserva mayúsculas y
    /// acentos, por lo que sirve para mostrarla.
    /// </summary>
    public static string Limpiar(string? descripcion)
    {
        string requerida = Guard.Requerido(descripcion, "descripción");

        return string.Join(
            ' ',
            requerida.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    /// Devuelve la forma usada para comparar descripciones: la descripción
    /// limpia y en minúsculas, sin quitar acentos.
    /// </summary>
    public static string Normalizar(string? descripcion) =>
        Limpiar(descripcion).ToLowerInvariant();
}
