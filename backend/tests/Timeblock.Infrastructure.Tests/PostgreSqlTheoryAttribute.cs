namespace Timeblock.Infrastructure.Tests;

/// <summary>
/// Variante de <see cref="PostgreSqlFactAttribute"/> para pruebas con datos:
/// se ejecutan cuando TIMEBLOCK_TEST_CONNECTION indica una base PostgreSQL.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgreSqlTheoryAttribute : TheoryAttribute
{
    public PostgreSqlTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION")))
        {
            Skip = "Define TIMEBLOCK_TEST_CONNECTION para ejecutar pruebas PostgreSQL.";
        }
    }
}
