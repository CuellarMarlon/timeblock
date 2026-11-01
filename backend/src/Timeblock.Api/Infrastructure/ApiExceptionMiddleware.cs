using Timeblock.Application.Common.Exceptions;
using Timeblock.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Timeblock.Api.Infrastructure;

public sealed class ApiExceptionMiddleware(RequestDelegate next, ILogger<ApiExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            (int status, string title, string detail) = exception switch
            {
                InvalidRequestException or DomainException or ArgumentException =>
                    (StatusCodes.Status400BadRequest, "La solicitud contiene datos inválidos", exception.Message),
                ResourceNotFoundException =>
                    (StatusCodes.Status404NotFound, "No se encontró el recurso", exception.Message),
                ResourceConflictException =>
                    (StatusCodes.Status409Conflict, "Conflicto con el estado del recurso", exception.Message),
                _ => (StatusCodes.Status500InternalServerError, "Error interno", "Ocurrió un error inesperado.")
            };

            if (status == StatusCodes.Status500InternalServerError)
                logger.LogError(exception, "Error no controlado al procesar {Path}", context.Request.Path);

            context.Response.Clear();
            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Type = "about:blank",
                Title = title,
                Status = status,
                Detail = detail,
                Instance = context.Request.Path
            });
        }
    }
}
