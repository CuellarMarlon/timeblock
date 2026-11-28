using Timeblock.Domain.Common;

namespace Timeblock.Domain.Horarios;

public sealed class BloqueEnHorario
{
    private BloqueEnHorario(
        Guid horarioId,
        Guid bloqueId,
        DateOnly fecha,
        TimeOnly? horaInicio,
        TimeOnly? horaFin,
        string? periodoImp,
        Guid? aulaId)
    {
        HorarioId = Guard.IdValido(horarioId, "horarioId");
        BloqueId = Guard.IdValido(bloqueId, "bloqueId");
        Fecha = fecha;

        bool hayProgramacionParcial =
            horaInicio.HasValue ||
            horaFin.HasValue ||
            periodoImp is not null ||
            aulaId.HasValue;

        if (hayProgramacionParcial &&
            (!horaInicio.HasValue ||
             !horaFin.HasValue ||
             string.IsNullOrWhiteSpace(periodoImp) ||
             !aulaId.HasValue))
        {
            throw new DomainException(
                "La hora de inicio, hora de fin, periodo y aula deben informarse juntos.");
        }

        if (horaInicio.HasValue &&
            horaFin.HasValue &&
            horaInicio.Value >= horaFin.Value)
        {
            throw new DomainException(
                "La hora de inicio debe ser anterior a la hora de fin.");
        }

        if (aulaId == Guid.Empty)
        {
            throw new DomainException("El identificador del aula no es válido.");
        }

        HoraInicio = horaInicio;
        HoraFin = horaFin;
        PeriodoImp = periodoImp;
        AulaId = aulaId;
    }

    public Guid HorarioId { get; }

    public Guid BloqueId { get; }

    public DateOnly Fecha { get; }

    public TimeOnly? HoraInicio { get; }

    public TimeOnly? HoraFin { get; }

    public string? PeriodoImp { get; }

    public Guid? AulaId { get; }

    /// <summary>Tiene hora de inicio, hora de fin, período y aula.</summary>
    public bool EstaCompleta => HoraInicio.HasValue;

    /// <summary>
    /// Dos imparticiones completas de la misma fecha se solapan si sus
    /// intervalos semiabiertos <c>[inicio, fin)</c> se cortan. Las contiguas,
    /// en las que una termina cuando empieza la otra, no se solapan (RII04).
    /// </summary>
    public bool SeSolapaCon(BloqueEnHorario otra)
    {
        ArgumentNullException.ThrowIfNull(otra);

        return EstaCompleta &&
               otra.EstaCompleta &&
               Fecha == otra.Fecha &&
               HoraInicio!.Value < otra.HoraFin!.Value &&
               otra.HoraInicio!.Value < HoraFin!.Value;
    }

    public static BloqueEnHorario Crear(
        Guid horarioId,
        Guid bloqueId,
        DateOnly fecha,
        TimeOnly? horaInicio = null,
        TimeOnly? horaFin = null,
        string? periodoImp = null,
        Guid? aulaId = null)
    {
        return new BloqueEnHorario(
            horarioId,
            bloqueId,
            fecha,
            horaInicio,
            horaFin,
            periodoImp,
            aulaId);
    }
}