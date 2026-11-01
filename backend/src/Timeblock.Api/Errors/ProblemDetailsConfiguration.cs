using Microsoft.AspNetCore.Mvc;

namespace Timeblock.Api.Errors;

public static class ProblemDetailsConfiguration
{
    /// <summary>
    /// Registra el formato común de errores (Problem Details,
    /// <c>application/problem+json</c>) y el manejo de excepciones.
    /// </summary>
    public static IServiceCollection AddTimeblockProblemDetails(
        this IServiceCollection services)
    {
        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                ProblemDetails problema = context.ProblemDetails;

                problema.Type = ErroresHttp.TipoProblema;

                int status = problema.Status
                    ?? context.HttpContext.Response.StatusCode;

                if (ErroresHttp.TituloDe(status) is { } titulo)
                {
                    problema.Title = titulo;
                }

                if (problema is ValidationProblemDetails)
                {
                    problema.Detail ??= ErroresHttp.DetalleValidacion;
                }
            });

        services.AddExceptionHandler<ApiExceptionHandler>();

        return services;
    }
}
