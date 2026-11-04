using Timeblock.Domain.Bloques;

namespace Timeblock.Application.Bloques;


public sealed record CrearBloqueCommand(
    string Descripcion,
    TipoActividad TipoActividad,
    NivelComplejidad NivelComplejidad,
    Guid AsignaturaId,
    Guid CoordinadorId,
    IReadOnlyCollection<Guid> ProgramasIds);


public sealed record ActualizarBloqueCommand(
    string? Descripcion = null,
    TipoActividad? TipoActividad = null,
    NivelComplejidad? NivelComplejidad = null,
    Guid? AsignaturaId = null);
