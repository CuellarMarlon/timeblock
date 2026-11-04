namespace Timeblock.Application.Horarios;

/// Un bloque que se programa en una fecha del horario.
public sealed record BloqueEnHorarioCommand(Guid BloqueId, DateOnly Fecha);

/// Datos para crear un horario armado manualmente.
public sealed record CrearHorarioCommand(
    Guid ProgramaId,
    string PeriodoAcademico,
    Guid CoordinadorId,
    IReadOnlyCollection<BloqueEnHorarioCommand> Bloques);
