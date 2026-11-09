using Timeblock.Application.Horarios;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Horarios;

namespace Timeblock.Api.Contracts.Horarios;

public sealed record BloqueEnHorarioRequest
{
    public required Guid BloqueId { get; init; }

    public required DateOnly Fecha { get; init; }
}

/// <summary>
/// El cliente no envía el estado, la fecha de creación ni la autorización: las
/// determina el servidor.
/// </summary>
public sealed record CrearHorarioRequest
{
    public required Guid ProgramaId { get; init; }

    public required string PeriodoAcademico { get; init; }

    public required Guid CoordinadorId { get; init; }

    public required IReadOnlyList<BloqueEnHorarioRequest> Bloques { get; init; }
}

public sealed record BloqueEnHorarioResponse(
    Guid BloqueId,
    string Descripcion,
    Guid AsignaturaId,
    TipoActividad TipoActividad,
    NivelComplejidad NivelComplejidad,
    DateOnly Fecha,
    TimeOnly? HoraInicio,
    TimeOnly? HoraFin,
    string? PeriodoImp,
    Guid? AulaId);

public sealed record HorarioResponse(
    Guid Id,
    Guid ProgramaId,
    string PeriodoAcademico,
    Guid CoordinadorId,
    DateOnly FechaInicioAutorizacion,
    DateTimeOffset FechaCreacion,
    EstadoHorario Estado,
    int CantTotalBloques,
    IReadOnlyList<BloqueEnHorarioResponse> Bloques)
{
    public static HorarioResponse FromDetalle(HorarioDetalle detalle)
    {
        ArgumentNullException.ThrowIfNull(detalle);

        Horario horario = detalle.Horario;

        return new HorarioResponse(
            horario.Id,
            horario.ProgramaId,
            horario.PeriodoAcademico,
            horario.CoordinadorId,
            horario.FechaInicioAutorizacion,
            horario.FechaCreacion,
            horario.Estado,
            detalle.CantTotalBloques,
            detalle.Bloques
                .Select(item => new BloqueEnHorarioResponse(
                    item.Bloque.Id,
                    item.Bloque.Descripcion,
                    item.Bloque.AsignaturaId,
                    item.Bloque.TipoActividad,
                    item.Bloque.NivelComplejidad,
                    item.Asociacion.Fecha,
                    item.Asociacion.HoraInicio,
                    item.Asociacion.HoraFin,
                    item.Asociacion.PeriodoImp,
                    item.Asociacion.AulaId))
                .ToArray());
    }
}
