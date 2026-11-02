using Microsoft.AspNetCore.Mvc;
using Timeblock.Api.Contracts.Matriculas;
using Timeblock.Application.Common.Pagination;
using Timeblock.Application.Matriculas;

namespace Timeblock.Api.Controllers;

[ApiController]
public sealed class MatriculasController(MatriculasService matriculas) : ControllerBase
{
    [HttpPost("api/programas/{programaId:guid}/matriculas")]
    [ProducesResponseType<MatriculaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<MatriculaResponse>> Crear(
        Guid programaId, CrearMatriculaRequest request, CancellationToken ct)
    {
        var created = MatriculaResponse.FromDomain(
            await matriculas.MatricularAsync(programaId, request.EstudianteId, ct));
        return CreatedAtAction(nameof(ListarPorPrograma), new { programaId }, created);
    }

    [HttpGet("api/programas/{programaId:guid}/matriculas")]
    [ProducesResponseType<PagedResult<MatriculaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<MatriculaResponse>>> ListarPorPrograma(
        Guid programaId, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        var result = await matriculas.ListarPorProgramaAsync(programaId, page, pageSize, ct);
        return Ok(result.Map(MatriculaResponse.FromDomain));
    }

    [HttpGet("api/estudiantes/{estudianteId:guid}/matriculas")]
    [ProducesResponseType<PagedResult<MatriculaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<MatriculaResponse>>> ListarPorEstudiante(
        Guid estudianteId, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        var result = await matriculas.ListarPorEstudianteAsync(estudianteId, page, pageSize, ct);
        return Ok(result.Map(MatriculaResponse.FromDomain));
    }
}
