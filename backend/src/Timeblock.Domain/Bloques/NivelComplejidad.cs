namespace Timeblock.Domain.Bloques;

/// <summary>
/// Nivel de complejidad de un bloque de contenido. Los valores empiezan en 1
/// para que el valor por defecto (0) nunca se tome por un nivel válido.
/// </summary>
public enum NivelComplejidad
{
    Bajo = 1,
    Medio = 2,
    Alto = 3,
}
