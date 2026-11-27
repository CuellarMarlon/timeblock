using Timeblock.Domain.Catalogo;

namespace Timeblock.Api.Contracts.Catalogo;

public sealed record ProgramaResponse(Guid Id, string Nombre, string PlanEstudio, bool Activo)
{
    public static ProgramaResponse FromDomain(ProgramaAcademico programa) =>
        new(programa.Id, programa.Nombre, programa.PlanEstudio, programa.Activo);
}

public sealed record AsignaturaResponse(Guid Id, string Nombre, bool Activo)
{
    public static AsignaturaResponse FromDomain(Asignatura asignatura) =>
        new(asignatura.Id, asignatura.Nombre, asignatura.Activo);
}
