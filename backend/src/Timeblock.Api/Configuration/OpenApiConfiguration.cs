namespace Timeblock.Api.Configuration;

public static class OpenApiConfiguration
{
    public static IServiceCollection AddTimeblockOpenApi(
        this IServiceCollection services)
    {
        services.AddOpenApi(options =>
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new()
                {
                    Title = "Timeblock API",
                    Version = "v1",
                    Description = "API para la gestión, generación y análisis de " +
                        "horarios docentes. Las convenciones comunes están " +
                        "descritas en docs/api/contrato-api.md.",
                };

                return Task.CompletedTask;
            }));

        return services;
    }
}
