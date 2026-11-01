using Microsoft.AspNetCore.Mvc;
using Timeblock.Api.Contracts.Personas;
using Timeblock.Application.Personas;

namespace Timeblock.Api.Controllers;

[ApiController]
[Route("api/coordinadores/{coordinadorId:guid}/autorizaciones")]
public sealed class AutorizacionesController(PersonasService personas) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<AutorizacionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AutorizacionResponse>> Crear(
        Guid coordinadorId, CrearAutorizacionRequest request, CancellationToken ct)
    {
        var result = await personas.CrearAutorizacionAsync(coordinadorId, request.ProgramaId,
            request.FechaInicio, request.FechaFin, request.PuedeGenerar, request.PuedeValidar, ct);
        var response = AutorizacionResponse.FromDomain(result);
        return CreatedAtAction(nameof(Listar), new { coordinadorId, programaId = response.ProgramaId }, response);
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<AutorizacionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<AutorizacionResponse>>> Listar(
        Guid coordinadorId, [FromQuery] Guid? programaId, [FromQuery] DateOnly? vigenteEn, CancellationToken ct)
    {
        var list = await personas.ListarAutorizacionesAsync(coordinadorId,
            new AutorizacionFilter(programaId, vigenteEn), ct);
        return Ok(list.Select(AutorizacionResponse.FromDomain).ToArray());
    }

    [HttpPut("{programaId:guid}/{fechaInicio}")]
    [ProducesResponseType<AutorizacionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AutorizacionResponse>> Actualizar(
        Guid coordinadorId, Guid programaId, DateOnly fechaInicio,
        ActualizarAutorizacionRequest request, CancellationToken ct)
    {
        var result = await personas.ActualizarAutorizacionAsync(coordinadorId, programaId, fechaInicio,
            request.FechaFin, request.PuedeGenerar, request.PuedeValidar, ct);
        return Ok(AutorizacionResponse.FromDomain(result));
    }
}
