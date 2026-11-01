using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Timeblock.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registra los servicios comunes de Application y todos sus casos de uso.
    /// </summary>
    public static IServiceCollection AddApplication(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);

        return services.AddCasosDeUso(typeof(DependencyInjection).Assembly);
    }

   
    public static IServiceCollection AddCasosDeUso(
        this IServiceCollection services,
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        IEnumerable<Type> casosDeUso = assembly.GetTypes().Where(tipo =>
            tipo is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false }
            && typeof(ICasoDeUso).IsAssignableFrom(tipo));

        foreach (Type casoDeUso in casosDeUso)
        {
            services.TryAddScoped(casoDeUso);
        }

        return services;
    }
}
