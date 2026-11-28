namespace Timeblock.Application.Horarios;

/// Un bloque que se programa en una fecha del horario. Hora de inicio, hora de
/// fin y aula son opcionales, pero van juntas: con ellas la impartición queda
/// completa.
public sealed record BloqueEnHorarioCommand(
    Guid BloqueId,
    DateOnly Fecha,
    TimeOnly? HoraInicio = null,
    TimeOnly? HoraFin = null,
    Guid? AulaId = null);

/// Datos para crear un horario armado manualmente.
public sealed record CrearHorarioCommand(
    Guid ProgramaId,
    string PeriodoAcademico,
    Guid CoordinadorId,
    IReadOnlyCollection<BloqueEnHorarioCommand> Bloques);
