using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Timeblock.Application;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Common;
using Timeblock.Domain.Personas;

namespace Timeblock.Api.Tests.Support;

/// <summary>Caso de uso de prueba, registrado por el escaneo de Application.</summary>
public sealed class EcoCasoDeUso : ICasoDeUso
{
    public string Ejecutar(string texto) => $"eco: {texto}";
}

public sealed class SolicitudPrueba
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string? Nombre { get; set; }

    [Range(1, 120, ErrorMessage = "La edad debe ser mayor que cero.")]
    public int Edad { get; set; }
}

public sealed record MuestraJson(
    Guid Id,
    DateOnly Fecha,
    DateTimeOffset CreadoEn,
    TipoPerfil TipoPerfil,
    TipoActividad TipoActividad,
    string Nombre);

public sealed record EnumSolicitud(TipoPerfil TipoPerfil);

/// <summary>
/// Endpoints que solo existen en las pruebas para ejercitar la configuración
/// común de la API.
/// </summary>
[ApiController]
[Route("api/_tests")]
public sealed class PruebasController(EcoCasoDeUso eco) : ControllerBase
{
    [HttpGet("errores/{tipo}")]
    public IActionResult LanzarDomainException(TipoErrorDominio tipo) =>
        throw new DomainException($"Mensaje de prueba ({tipo}).", tipo);

    [HttpGet("inesperado")]
    public IActionResult LanzarErrorInesperado() =>
        throw new InvalidOperationException("secreto-interno-clave=123");

    [HttpPost("validacion")]
    public IActionResult Validar(SolicitudPrueba solicitud) => Ok(solicitud);

    [HttpGet("json")]
    public MuestraJson ObtenerJson() => new(
        Guid.Parse("3f2504e0-4f89-41d3-9a0c-0305e82c3301"),
        new DateOnly(2026, 10, 12),
        new DateTimeOffset(2026, 10, 12, 14, 30, 0, TimeSpan.FromHours(-5)),
        TipoPerfil.JefeDepartamento,
        TipoActividad.Teoria,
        "Acción");

    [HttpPost("enum")]
    public EnumSolicitud RecibirEnum(EnumSolicitud solicitud) => solicitud;

    [HttpGet("paginacion")]
    public PagedResult<int> Paginar(int? page, int? pageSize)
    {
        const int total = 250;
        PageRequest solicitud = PageRequest.Create(page, pageSize);

        List<int> items = Enumerable
            .Range(1, total)
            .Skip(solicitud.Skip)
            .Take(solicitud.PageSize)
            .ToList();

        return PagedResult<int>.Create(items, solicitud, total);
    }

    [HttpGet("caso-de-uso")]
    public object EjecutarCasoDeUso() => new { mensaje = eco.Ejecutar("hola") };

    [HttpPost("creado")]
    public IActionResult Crear() =>
        Created("/api/_tests/creado/1", new { id = 1 });
}
