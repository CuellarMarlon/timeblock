using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Timeblock.Domain.Common;
using Timeblock.Application.Common.Exceptions;

namespace Timeblock.Api.Errors;


/// Una <see cref="DomainException"/> se traduce según su tipo; cualquier otra
/// excepción produce un 500 genérico, sin detalles internos.

internal sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        (int status, string? detalle) = exception switch
        {
            DomainException dominio =>
                (ErroresHttp.StatusDe(dominio.Tipo), dominio.Message),

            // Solicitudes mal formadas detectadas por el servidor (por
            // ejemplo, cuerpo demasiado grande). Su mensaje es técnico.
            BadHttpRequestException solicitud =>
                (solicitud.StatusCode, null),
            
            InvalidRequestException invalida =>
                (StatusCodes.Status400BadRequest, invalida.Message),

            ResourceNotFoundException =>
                (StatusCodes.Status404NotFound, exception.Message),

            ResourceConflictException =>
                (StatusCodes.Status409Conflict, exception.Message),

            ArgumentException invalida =>
                (StatusCodes.Status400BadRequest, invalida.Message),

            _ => (StatusCodes.Status500InternalServerError,
                ErroresHttp.DetalleErrorInesperado),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Error inesperado al procesar {Method} {Path}.",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation(
                "Solicitud rechazada con {Status}: {Message}",
                status,
                exception.Message);
        }

        httpContext.Response.StatusCode = status;

        // La excepción no se entrega al escritor para no exponer su contenido.
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = ErroresHttp.TituloDe(status),
                Detail = detalle,
                Type = ErroresHttp.TipoProblema,
            },
        });

        return true;
    }
}
