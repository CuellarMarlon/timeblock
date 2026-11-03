using Timeblock.Domain.Bloques;

namespace Timeblock.Application.Bloques;

public sealed record BloqueFilter(
    Guid? ProgramaId = null,
    Guid? AsignaturaId = null,
    Guid? CoordinadorId = null,
    TipoActividad? TipoActividad = null,
    NivelComplejidad? NivelComplejidad = null);
