namespace Timeblock.Infrastructure.Tests;

/// <summary>
/// Los tests de persistencia real se ejecutan cuando se proporciona una base
/// PostgreSQL de pruebas mediante TIMEBLOCK_TEST_CONNECTION.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION")))
        {
            Skip = "Define TIMEBLOCK_TEST_CONNECTION para ejecutar pruebas PostgreSQL.";
        }
    }
}
