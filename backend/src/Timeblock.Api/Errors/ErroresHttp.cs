using Timeblock.Domain.Common;

namespace Timeblock.Api.Errors;

/// <summary>
/// Traducción de los errores de la aplicación a los códigos y textos HTTP
/// acordados en el contrato de la API.
/// </summary>
public static class ErroresHttp
{
    public const string TipoProblema = "about:blank";

    public const string DetalleErrorInesperado =
        "No se pudo procesar la solicitud. Inténtalo de nuevo más tarde.";

    public const string DetalleValidacion = "Revisa los campos indicados.";

    public static int StatusDe(TipoErrorDominio tipo) => tipo switch
    {
        TipoErrorDominio.Validacion => StatusCodes.Status400BadRequest,
        TipoErrorDominio.AutorizacionNegocio => StatusCodes.Status403Forbidden,
        TipoErrorDominio.NoEncontrado => StatusCodes.Status404NotFound,
        TipoErrorDominio.Conflicto => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError,
    };

    public static string? TituloDe(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "La solicitud contiene datos inválidos",
        StatusCodes.Status403Forbidden => "La operación no está autorizada",
        StatusCodes.Status404NotFound => "El recurso solicitado no existe",
        StatusCodes.Status409Conflict =>
            "La operación entra en conflicto con una regla del sistema",
        StatusCodes.Status500InternalServerError => "Ocurrió un error inesperado",
        _ => null,
    };
}
