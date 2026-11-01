namespace Timeblock.Api.Configuration;

public static class CorsConfiguration
{
    public const string PolicyName = "Frontend";

    /// <summary>
    /// Permite que el frontend acceda a la API desde los orígenes indicados en
    /// <c>Cors:AllowedOrigins</c>. Sin orígenes configurados no se autoriza
    /// ningún origen externo.
    /// </summary>
    public static IServiceCollection AddTimeblockCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string[] origenes =
            configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];

        services.AddCors(options => options.AddPolicy(PolicyName, policy =>
            policy
                .WithOrigins(origenes)
                .AllowAnyHeader()
                .AllowAnyMethod()
                // Las respuestas 201 indican el recurso creado en Location.
                .WithExposedHeaders("Location")));

        return services;
    }
}
