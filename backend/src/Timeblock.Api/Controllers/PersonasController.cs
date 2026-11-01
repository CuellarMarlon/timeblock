using Microsoft.AspNetCore.Mvc;
using Timeblock.Api.Contracts.Personas;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Personas;
using Timeblock.Domain.Personas;

namespace Timeblock.Api.Controllers;

[ApiController]
[Route("api/personas")]
public sealed class PersonasController(PersonasService personas) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<PersonaResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PersonaResponse>> Crear(
        CrearPersonaRequest request, CancellationToken ct)
    {
        var created = PersonaResponse.FromDomain(await personas.CrearAsync(
            PersonaRequestMapper.ToCommand(request), ct));
        return CreatedAtAction(nameof(Obtener), new { personaId = created.Id }, created);
    }

    [HttpGet]
    [ProducesResponseType<PageResponse<PersonaResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PageResponse<PersonaResponse>>> Listar(
        [FromQuery] string? nombre,
        [FromQuery] string? tipoPerfil,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var type = ParseTipoPerfil(tipoPerfil);
        var result = await personas.ListarAsync(new PersonaFilter(nombre, type, page, pageSize), ct);
        return Ok(new PageResponse<PersonaResponse>(result.Items.Select(PersonaResponse.FromDomain).ToArray(),
            result.Page, result.PageSize, result.TotalItems));
    }

    [HttpGet("{personaId:guid}")]
    [ProducesResponseType<PersonaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonaResponse>> Obtener(Guid personaId, CancellationToken ct) =>
        Ok(PersonaResponse.FromDomain(await personas.ObtenerAsync(personaId, ct)));

    [HttpPut("{personaId:guid}")]
    [ProducesResponseType<PersonaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonaResponse>> Actualizar(Guid personaId, ActualizarPersonaRequest request, CancellationToken ct) =>
        Ok(PersonaResponse.FromDomain(await personas.ActualizarNombreAsync(personaId, request.Nombre, ct)));

    [HttpPut("{personaId:guid}/perfil")]
    [ProducesResponseType<PersonaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonaResponse>> ActualizarPerfil(Guid personaId, PerfilRequest request, CancellationToken ct) =>
        Ok(PersonaResponse.FromDomain(await personas.ActualizarPerfilAsync(
            personaId, PersonaRequestMapper.ToCommand(request), ct)));

    private static TipoPerfil? ParseTipoPerfil(string? value)
    {
        if (value is null) return null;
        return value switch
        {
            "estudiante" => TipoPerfil.Estudiante,
            "coordinador" => TipoPerfil.Coordinador,
            "jefeDepartamento" => TipoPerfil.JefeDepartamento,
            _ => throw new InvalidRequestException("tipoPerfil debe ser estudiante, coordinador o jefeDepartamento.")
        };
    }
}
