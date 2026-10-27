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
        if (programaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del programa es obligatorio.",
                nameof(programaId));
        }

        if (asignaturaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la asignatura es obligatorio.",
                nameof(asignaturaId));
        }

        return new ProgramaAsignatura(programaId, asignaturaId);
    }
}