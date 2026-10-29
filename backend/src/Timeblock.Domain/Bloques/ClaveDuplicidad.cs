using Timeblock.Domain.Common;

namespace Timeblock.Domain.Bloques;

/// <summary>
/// Clave lógica de duplicidad técnica de un bloque (M4): asignatura, tipo de
/// actividad y descripción normalizada. La complejidad y el coordinador no
/// intervienen.
/// </summary>
public readonly record struct ClaveDuplicidad(
    Guid AsignaturaId,
    TipoActividad TipoActividad,
    string DescripcionNormalizada)
{
    /// <summary>
    /// Calcula la clave a partir de datos sin normalizar, por ejemplo los de una
    /// solicitud, para consultar si ya existe un bloque equivalente.
    /// </summary>
    public static ClaveDuplicidad Crear(
        Guid asignaturaId,
        TipoActividad tipoActividad,
        string? descripcion) =>
        new(
            Guard.IdValido(asignaturaId, "asignaturaId"),
            Guard.EnumValido(tipoActividad, "tipoActividad"),
            DescripcionBloque.Normalizar(descripcion));
}
