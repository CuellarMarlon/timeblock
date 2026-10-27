namespace Timeblock.Domain.Common;

/// <summary>
/// Se lanza cuando una operación incumple una regla del dominio.
/// </summary>
public class DomainException(string message) : Exception(message);
