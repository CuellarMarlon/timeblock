namespace Timeblock.Domain.Bloques;

/// <summary>
/// Tipo de actividad de un bloque de contenido. Los valores empiezan en 1 para
/// que el valor por defecto (0) nunca se tome por un tipo válido.
/// </summary>
public enum TipoActividad
{
    Teoria = 1,
    Practica = 2,
    Laboratorio = 3,
}
