using Microsoft.Extensions.DependencyInjection;

namespace Timeblock.Application.Tests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistraElRelojDelSistema()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        ServiceDescriptor reloj = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(TimeProvider));
        Assert.Equal(ServiceLifetime.Singleton, reloj.Lifetime);
    }

    [Fact]
    public void AddApplication_LlamadoDosVeces_NoDuplicaRegistros()
    {
        var services = new ServiceCollection();

        services.AddApplication();
        int registros = services.Count;
        services.AddApplication();

        Assert.Equal(registros, services.Count);
    }

    [Fact]
    public void AddCasosDeUso_RegistraLasClasesConcretasQueImplementanICasoDeUso()
    {
        var services = new ServiceCollection();

        services.AddCasosDeUso(typeof(DependencyInjectionTests).Assembly);

        ServiceDescriptor registro = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(CasoDePrueba));
        Assert.Equal(ServiceLifetime.Scoped, registro.Lifetime);
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(CasoAbstracto));
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(NoEsCasoDeUso));
    }

    private sealed class CasoDePrueba : ICasoDeUso;

    private abstract class CasoAbstracto : ICasoDeUso;

    private sealed class NoEsCasoDeUso;
}
