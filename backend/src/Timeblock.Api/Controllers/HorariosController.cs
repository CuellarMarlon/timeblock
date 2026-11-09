using Microsoft.AspNetCore.Mvc;
using Timeblock.Api.Contracts.Horarios;
using Timeblock.Application.Common.Pagination;
using Timeblock.Application.Horarios;
using Timeblock.Domain.Horarios;

namespace Timeblock.Api.Controllers;

[ApiController]
[Route("api/horarios")]
public sealed class HorariosController(HorariosService horarios) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<HorarioResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<HorarioResponse>> Crear(
        CrearHorarioRequest request,
        CancellationToken ct)
    {
        HorarioResponse creado = HorarioResponse.FromDetalle(
            await horarios.CrearAsync(HorariosRequestMapper.ToCommand(request), ct));

        return CreatedAtAction(nameof(Obtener), new { horarioId = creado.Id }, creado);
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<HorarioResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<HorarioResponse>>> Listar(
        [FromQuery] Guid? programaId,
        [FromQuery] string? periodoAcademico,
        [FromQuery] EstadoHorario? estado,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken ct)
    {
        PagedResult<HorarioDetalle> resultado = await horarios.ListarAsync(
            new HorarioFilter(programaId, periodoAcademico, estado),
            page,
            pageSize,
            ct);

        return Ok(resultado.Map(HorarioResponse.FromDetalle));
    }

    [HttpGet("{horarioId:guid}")]
    [ProducesResponseType<HorarioResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HorarioResponse>> Obtener(Guid horarioId, CancellationToken ct) =>
        Ok(HorarioResponse.FromDetalle(await horarios.ObtenerAsync(horarioId, ct)));
}
