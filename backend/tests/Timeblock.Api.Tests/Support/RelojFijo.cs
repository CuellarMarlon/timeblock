namespace Timeblock.Api.Tests.Support;

/// Reloj con una hora fija, para fechas de creación predecibles.
public sealed class RelojFijo(DateTimeOffset ahora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => ahora;
}
