using Timeblock.Domain.Matriculas;

namespace Timeblock.Api.Contracts.Matriculas;

public sealed record CrearMatriculaRequest
{
    public required Guid EstudianteId { get; init; }
}

public sealed record MatriculaResponse(Guid EstudianteId, Guid ProgramaId)
{
    public static MatriculaResponse FromDomain(Matricula matricula) =>
        new(matricula.EstudianteId, matricula.ProgramaId);
}
