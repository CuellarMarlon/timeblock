using Timeblock.Domain.Horarios;

namespace Timeblock.Application.Horarios;

public sealed record HorarioFilter(
    Guid? ProgramaId = null,
    string? PeriodoAcademico = null,
    EstadoHorario? Estado = null);
