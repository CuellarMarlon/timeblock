using Timeblock.Domain.Common;

namespace Timeblock.Domain.Personas;

/// <summary>
/// Autorización de un coordinador sobre un programa académico. Se identifica
/// mediante coordinador, programa y fecha de inicio.
/// </summary>
public sealed class AutorizacionCoordinador
{
    public AutorizacionCoordinador(
        Guid coordinadorId,
        Guid programaId,
        DateOnly fechaInicio,
        DateOnly? fechaFin,
        bool puedeGenerar,
        bool puedeValidar)
    {
        CoordinadorId = Guard.IdValido(coordinadorId, "coordinadorId");
        ProgramaId = Guard.IdValido(programaId, "programaId");
        FechaInicio = fechaInicio;
        Actualizar(fechaFin, puedeGenerar, puedeValidar);
    }

    public Guid CoordinadorId { get; }

    public Guid ProgramaId { get; }

    public DateOnly FechaInicio { get; }

    public DateOnly? FechaFin { get; private set; }

    public bool PuedeGenerar { get; private set; }

    public bool PuedeValidar { get; private set; }

    /// <summary>
    /// Actualiza el fin de vigencia y los permisos. La clave no cambia.
    /// </summary>
    public void Actualizar(DateOnly? fechaFin, bool puedeGenerar, bool puedeValidar)
    {
        if (fechaFin < FechaInicio)
        {
            throw new DomainException(
                "La fecha de fin no puede ser anterior a la fecha de inicio.");
        }

        FechaFin = fechaFin;
        PuedeGenerar = puedeGenerar;
        PuedeValidar = puedeValidar;
    }

    /// <summary>
    /// Indica si la autorización está vigente en la fecha dada. Ambos extremos
    /// del período son inclusivos.
    /// </summary>
    public bool EstaVigenteEn(DateOnly fecha) =>
        fecha >= FechaInicio && (FechaFin is null || fecha <= FechaFin);

    public bool PermiteGenerarEn(DateOnly fecha) => PuedeGenerar && EstaVigenteEn(fecha);

    public bool PermiteValidarEn(DateOnly fecha) => PuedeValidar && EstaVigenteEn(fecha);
}
