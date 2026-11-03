using Microsoft.AspNetCore.Mvc;
using Timeblock.Api.Contracts.Bloques;
using Timeblock.Application.Bloques;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Bloques;

namespace Timeblock.Api.Controllers;

[ApiController]
[Route("api/bloques")]
public sealed class BloquesController(BloquesService bloques) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<BloqueResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BloqueResponse>> Crear(
        CrearBloqueRequest request,
        CancellationToken ct)
    {
        BloqueResponse creado = BloqueResponse.FromDomain(
            await bloques.CrearAsync(BloquesRequestMapper.ToCommand(request), ct));

        return CreatedAtAction(nameof(Obtener), new { bloqueId = creado.Id }, creado);
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<BloqueResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<BloqueResponse>>> Listar(
        [FromQuery] Guid? programaId,
        [FromQuery] Guid? asignaturaId,
        [FromQuery] Guid? coordinadorId,
        [FromQuery] TipoActividad? tipoActividad,
        [FromQuery] NivelComplejidad? nivelComplejidad,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        var filtro = new BloqueFilter(
            programaId,
            asignaturaId,
            coordinadorId,
            tipoActividad,
            nivelComplejidad);

        PagedResult<BloqueContenido> resultado =
            await bloques.ListarAsync(filtro, page, pageSize, ct);

        return Ok(resultado.Map(BloqueResponse.FromDomain));
    }

    [HttpGet("{bloqueId:guid}")]
    [ProducesResponseType<BloqueResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BloqueResponse>> Obtener(Guid bloqueId, CancellationToken ct) =>
        Ok(BloqueResponse.FromDomain(await bloques.ObtenerAsync(bloqueId, ct)));

    [HttpPut("{bloqueId:guid}")]
    [ProducesResponseType<BloqueResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BloqueResponse>> Actualizar(
        Guid bloqueId,
        ActualizarBloqueRequest request,
        CancellationToken ct) =>
        Ok(BloqueResponse.FromDomain(await bloques.ActualizarAsync(
            bloqueId,
            BloquesRequestMapper.ToCommand(request),
            ct)));

    [HttpPut("{bloqueId:guid}/programas/{programaId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AsociarPrograma(
        Guid bloqueId,
        Guid programaId,
        CancellationToken ct)
    {
        await bloques.AsociarProgramaAsync(bloqueId, programaId, ct);
        return NoContent();
    }
}
