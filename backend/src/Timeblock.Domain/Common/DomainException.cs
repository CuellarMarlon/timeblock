namespace Timeblock.Domain.Common;

/// <summary>
/// Se lanza cuando una operación incumple una regla del dominio.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Crea una excepción de <see cref="TipoErrorDominio.Validacion"/>.
    /// </summary>
    public DomainException(string message)
        : this(message, TipoErrorDominio.Validacion)
    {
    }

    public DomainException(string message, TipoErrorDominio tipo)
        : base(message)
    {
        Tipo = Guard.EnumValido(tipo, nameof(tipo));
    }

    public TipoErrorDominio Tipo { get; }
}
