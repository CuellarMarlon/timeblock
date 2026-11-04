using Microsoft.AspNetCore.Mvc;
using Timeblock.Api.Contracts.Catalogo;
using Timeblock.Api.Contracts.Common;
using Timeblock.Application.Catalogo;

namespace Timeblock.Api.Controllers;

[ApiController]
[Route("api/asignaturas")]
public sealed class AsignaturasController(CatalogoService catalogo) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<AsignaturaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AsignaturaResponse>> Crear(CrearAsignaturaRequest request, CancellationToken ct)
    {
        AsignaturaResponse response = AsignaturaResponse.FromDomain(
            await catalogo.CrearAsignaturaAsync(request.Nombre, ct));
        return CreatedAtAction(nameof(Obtener), new { asignaturaId = response.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType<PageResponse<AsignaturaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PageResponse<AsignaturaResponse>>> Listar(
        [FromQuery] string? nombre,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        CancellationToken ct = default)
    {
        var result = await catalogo.ListarAsignaturasAsync(nombre, page, pageSize, ct);
        return Ok(new PageResponse<AsignaturaResponse>(result.Items.Select(AsignaturaResponse.FromDomain).ToArray(),
            result.Page, result.PageSize, result.TotalItems));
    }

    [HttpGet("{asignaturaId:guid}")]
    [ProducesResponseType<AsignaturaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AsignaturaResponse>> Obtener(Guid asignaturaId, CancellationToken ct) =>
        Ok(AsignaturaResponse.FromDomain(await catalogo.ObtenerAsignaturaAsync(asignaturaId, ct)));

    [HttpPut("{asignaturaId:guid}")]
    [ProducesResponseType<AsignaturaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AsignaturaResponse>> Actualizar(
        Guid asignaturaId, ActualizarAsignaturaRequest request, CancellationToken ct) =>
        Ok(AsignaturaResponse.FromDomain(await catalogo.ActualizarAsignaturaAsync(
            asignaturaId, request.Nombre, ct)));
}
