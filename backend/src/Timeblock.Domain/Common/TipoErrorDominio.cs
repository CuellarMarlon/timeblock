namespace Timeblock.Domain.Common;

/// <summary>
/// Categoría de una <see cref="DomainException"/>. Permite que las capas
/// superiores decidan cómo comunicar el error sin depender de su mensaje.
/// </summary>
public enum TipoErrorDominio
{
    /// <summary>Datos o parámetros inválidos.</summary>
    Validacion = 0,

    /// <summary>
    /// La autorización de negocio del coordinador no permite la operación.
    /// No representa autenticación de usuario.
    /// </summary>
    AutorizacionNegocio = 1,

    /// <summary>El recurso indicado no existe.</summary>
    NoEncontrado = 2,

    /// <summary>Duplicado o conflicto con una regla de integridad.</summary>
    Conflicto = 3,
}
