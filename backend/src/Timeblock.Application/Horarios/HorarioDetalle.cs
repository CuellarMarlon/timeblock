using Timeblock.Domain.Bloques;
using Timeblock.Domain.Horarios;

namespace Timeblock.Application.Horarios;

public sealed record HorarioDetalle(
    Horario Horario,
    IReadOnlyList<BloqueEnHorarioDetalle> Bloques)
{
    /// Cantidad de bloques distintos: uno que aparece en varias fechas cuenta
    /// una sola vez.
    
    public int CantTotalBloques =>
        Bloques.Select(bloque => bloque.Bloque.Id).Distinct().Count();
}

public sealed record BloqueEnHorarioDetalle(
    BloqueEnHorario Asociacion,
    BloqueContenido Bloque);
