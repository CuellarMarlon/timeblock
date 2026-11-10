namespace Timeblock.Domain.Common;

/// <summary>
/// Precisión común de las marcas de tiempo: microsegundos en UTC, la que
/// conserva PostgreSQL en <c>timestamp with time zone</c>. .NET trabaja con
/// fracciones de 100 ns, que se pierden al guardar.
/// </summary>
public static class PrecisionTemporal
{
    /// <summary>
    /// Convierte a UTC y descarta las fracciones menores de un microsegundo,
    /// para que el valor en memoria coincida con el que se lee de la base.
    /// </summary>
    public static DateTimeOffset EnMicrosegundosUtc(DateTimeOffset valor)
    {
        DateTimeOffset utc = valor.ToUniversalTime();

        return new DateTimeOffset(
            utc.Ticks - (utc.Ticks % TimeSpan.TicksPerMicrosecond),
            TimeSpan.Zero);
    }
}
