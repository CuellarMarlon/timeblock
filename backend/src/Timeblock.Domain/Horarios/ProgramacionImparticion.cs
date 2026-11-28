namespace Timeblock.Domain.Horarios;

/// <summary>
/// Hora de inicio, hora de fin y aula de una impartición. El período de
/// impartición no se pide: es el período académico del horario.
/// </summary>
public sealed record ProgramacionImparticion(TimeOnly HoraInicio, TimeOnly HoraFin, Guid AulaId);
