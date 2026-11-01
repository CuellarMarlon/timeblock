namespace Timeblock.Application.Tests;

public sealed class DependenciasTests
{
    [Fact]
    public void Application_NoDependeDeInfrastructureNiDeApi()
    {
        string?[] referenciados = typeof(DependencyInjection).Assembly
            .GetReferencedAssemblies()
            .Select(ensamblado => ensamblado.Name)
            .ToArray();

        Assert.Contains("Timeblock.Domain", referenciados);
        Assert.DoesNotContain("Timeblock.Infrastructure", referenciados);
        Assert.DoesNotContain("Timeblock.Api", referenciados);
    }
}
