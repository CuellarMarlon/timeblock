using Microsoft.AspNetCore.Mvc;
using Timeblock.Api.Contracts.Aulas;
using Timeblock.Api.Contracts.Common;
using Timeblock.Application.Aulas;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Aulas;

namespace Timeblock.Api.Controllers;

/// <summary>
/// Catálogo de aulas (M6-06). Contrato: <c>docs/api/endpoints/aulas.md</c>.
/// </summary>
/// <remarks>
/// Pendiente de #84: aún no existen cuentas ni sesión, así que estos
/// endpoints no comprueban permisos. Cuando existan, crear y editar serán de
/// administrador y consultar, de cualquier usuario con sesión (401 y 403 según
/// el contrato).
/// </remarks>
[ApiController]
[Route("api/aulas")]
public sealed class AulasController(AulasService aulas) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<AulaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AulaResponse>> Crear(
        AulaRequest request,
        CancellationToken ct)
    {
        AulaResponse creada = AulaResponse.FromDomain(
            await aulas.CrearAsync(request.Edificio, request.Capacidad, ct));

        return CreatedAtAction(nameof(Obtener), new { aulaId = creada.Id }, creada);
    }

    [HttpGet]
    [ProducesResponseType<PageResponse<AulaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PageResponse<AulaResponse>>> Listar(
        [FromQuery] string? edificio,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        PagedResult<Aula> resultado =
            await aulas.ListarAsync(edificio, page, pageSize, ct);

        return Ok(new PageResponse<AulaResponse>(
            resultado.Items.Select(AulaResponse.FromDomain).ToList(),
            resultado.Page,
            resultado.PageSize,
            resultado.TotalItems));
    }

    [HttpGet("{aulaId:guid}")]
    [ProducesResponseType<AulaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AulaResponse>> Obtener(
        Guid aulaId,
        CancellationToken ct) =>
        Ok(AulaResponse.FromDomain(await aulas.ObtenerAsync(aulaId, ct)));

    [HttpPut("{aulaId:guid}")]
    [ProducesResponseType<AulaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AulaResponse>> Actualizar(
        Guid aulaId,
        AulaRequest request,
        CancellationToken ct) =>
        Ok(AulaResponse.FromDomain(
            await aulas.ActualizarAsync(
                aulaId, request.Edificio, request.Capacidad, ct)));
}
