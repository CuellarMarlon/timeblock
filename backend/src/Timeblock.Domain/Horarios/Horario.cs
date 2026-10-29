using Timeblock.Domain.Bloques;
using Timeblock.Domain.Common;
using Timeblock.Domain.Personas;

namespace Timeblock.Domain.Horarios;

public sealed class Horario
{
    private readonly List<BloqueEnHorario> _bloques = [];

    private Horario(
        Guid id,
        DateTimeOffset fechaCreacion,
        string periodoAcademico,
        Guid programaId,
        Guid coordinadorId,
        DateOnly fechaInicioAutorizacion)
    {
        Id = Guard.IdValido(id, "horarioId");
        FechaCreacion = fechaCreacion.ToUniversalTime();
        PeriodoAcademico = Guard.Requerido(periodoAcademico, "periodoAcademico");
        ProgramaId = Guard.IdValido(programaId, "programaId");
        CoordinadorId = Guard.IdValido(coordinadorId, "coordinadorId");
        FechaInicioAutorizacion = fechaInicioAutorizacion;
        Estado = EstadoHorario.Generado;
    }

    public Guid Id { get; }

    public DateTimeOffset FechaCreacion { get; }

    public EstadoHorario Estado { get; }

    public string PeriodoAcademico { get; }

    public Guid ProgramaId { get; }

    public Guid CoordinadorId { get; }

    public DateOnly FechaInicioAutorizacion { get; }

    public IReadOnlyCollection<BloqueEnHorario> Bloques =>
        _bloques.AsReadOnly();

    public static Horario Crear(
        Guid id,
        DateTimeOffset fechaCreacion,
        string periodoAcademico,
        Guid programaId,
        Guid coordinadorId,
        AutorizacionCoordinador autorizacion)
    {
        ArgumentNullException.ThrowIfNull(autorizacion);

        Guard.IdValido(programaId, "programaId");
        Guard.IdValido(coordinadorId, "coordinadorId");

        DateOnly fechaCreacionUtc =
            DateOnly.FromDateTime(fechaCreacion.UtcDateTime);

        if (autorizacion.CoordinadorId != coordinadorId ||
            autorizacion.ProgramaId != programaId)
        {
            throw new DomainException(
                "La autorización no corresponde al coordinador y programa del horario.");
        }

        if (!autorizacion.PermiteGenerarEn(fechaCreacionUtc))
        {
            throw new DomainException(
                "La autorización no está vigente o no permite generar horarios.");
        }

        return new Horario(
            id,
            fechaCreacion,
            periodoAcademico,
            programaId,
            coordinadorId,
            autorizacion.FechaInicio);
    }

    public void AgregarBloque(BloqueContenido bloque, DateOnly fecha)
    {
        ArgumentNullException.ThrowIfNull(bloque);

        if (!bloque.PuedeUsarseEn(ProgramaId))
        {
            throw new DomainException(
                "El bloque no puede utilizarse en el programa del horario.");
        }

        bool yaExiste = _bloques.Any(asociacion =>
            asociacion.HorarioId == Id &&
            asociacion.BloqueId == bloque.Id &&
            asociacion.Fecha == fecha);

        if (yaExiste)
        {
            throw new DomainException(
                "El bloque ya está asociado al horario en esa fecha.");
        }

        _bloques.Add(
            BloqueEnHorario.Crear(
                Id,
                bloque.Id,
                fecha));
    }
}