using Microsoft.AspNetCore.Mvc;
using Timeblock.Api.Contracts.Catalogo;
using Timeblock.Api.Contracts.Common;
using Timeblock.Application.Catalogo;
using Timeblock.Application.Common.Exceptions;

namespace Timeblock.Api.Controllers;

[ApiController]
[Route("api/programas")]
public sealed class ProgramasController(CatalogoService catalogo) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ProgramaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProgramaResponse>> Crear(CrearProgramaRequest request, CancellationToken ct)
    {
        ProgramaResponse response = ProgramaResponse.FromDomain(
            await catalogo.CrearProgramaAsync(request.Nombre, request.PlanEstudio, ct));
        return CreatedAtAction(nameof(Obtener), new { programaId = response.Id }, response);
    }

    [HttpGet]
    [ProducesResponseType<PageResponse<ProgramaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PageResponse<ProgramaResponse>>> Listar(
        [FromQuery] string? nombre,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        CancellationToken ct = default)
    {
        var result = await catalogo.ListarProgramasAsync(nombre, page, pageSize, ct);
        return Ok(new PageResponse<ProgramaResponse>(result.Items.Select(ProgramaResponse.FromDomain).ToArray(),
            result.Page, result.PageSize, result.TotalItems));
    }

    [HttpGet("{programaId:guid}")]
    [ProducesResponseType<ProgramaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgramaResponse>> Obtener(Guid programaId, CancellationToken ct) =>
        Ok(ProgramaResponse.FromDomain(await catalogo.ObtenerProgramaAsync(programaId, ct)));

    [HttpPut("{programaId:guid}")]
    [ProducesResponseType<ProgramaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProgramaResponse>> Actualizar(
        Guid programaId, ActualizarProgramaRequest request, CancellationToken ct) =>
        Ok(ProgramaResponse.FromDomain(await catalogo.ActualizarProgramaAsync(
            programaId, request.Nombre, request.PlanEstudio, ct)));

    [HttpGet("{programaId:guid}/asignaturas")]
    [ProducesResponseType<PageResponse<AsignaturaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PageResponse<AsignaturaResponse>>> ListarAsignaturas(
        Guid programaId,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        CancellationToken ct = default)
    {
        var result = await catalogo.ListarAsignaturasDeProgramaAsync(programaId, page, pageSize, ct);
        return Ok(new PageResponse<AsignaturaResponse>(result.Items.Select(AsignaturaResponse.FromDomain).ToArray(),
            result.Page, result.PageSize, result.TotalItems));
    }

    [HttpPut("{programaId:guid}/asignaturas/{asignaturaId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AsociarAsignatura(
        Guid programaId, Guid asignaturaId, CancellationToken ct)
    {
        await catalogo.AsociarAsignaturaAsync(programaId, asignaturaId, ct);
        return NoContent();
    }
}
