using Timeblock.Domain.Common;

namespace Timeblock.Domain.Catalogo;

public sealed class ProgramaAsignatura
{
    public Guid ProgramaId { get; private set; }

    public Guid AsignaturaId { get; private set; }

    private ProgramaAsignatura(Guid programaId, Guid asignaturaId)
    {
        ProgramaId = programaId;
        AsignaturaId = asignaturaId;
    }

    public static ProgramaAsignatura Crear(Guid programaId, Guid asignaturaId)
    {
        return new ProgramaAsignatura(
            Guard.IdValido(programaId, "programaId"),
            Guard.IdValido(asignaturaId, "asignaturaId"));
    }
}
