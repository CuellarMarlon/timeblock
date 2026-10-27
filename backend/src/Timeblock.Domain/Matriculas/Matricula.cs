using Timeblock.Domain.Common;

namespace Timeblock.Domain.Matriculas;

/// <summary>
/// Matrícula de un estudiante en un programa académico. Se identifica mediante
/// la pareja estudiante + programa.
/// </summary>
public sealed class Matricula
{
    public Matricula(Guid estudianteId, Guid programaId)
    {
        EstudianteId = Guard.IdValido(estudianteId, "estudianteId");
        ProgramaId = Guard.IdValido(programaId, "programaId");
    }

    /// <summary>
    /// Estudiante matriculado. Que corresponda a una persona con perfil de
    /// estudiante se comprueba en Application y persistencia.
    /// </summary>
    public Guid EstudianteId { get; }

    /// <summary>
    /// Programa de la matrícula. Su existencia se comprueba en Application y
    /// persistencia.
    /// </summary>
    public Guid ProgramaId { get; }
}
