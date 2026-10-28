using Timeblock.Domain.Common;

namespace Timeblock.Domain.Bloques;

/// <summary>
/// Asociación entre un bloque de contenido y un programa académico donde puede
/// utilizarse.
/// </summary>
public sealed class BloquePrograma
{
    public Guid BloqueId { get; private set; }

    public Guid ProgramaId { get; private set; }

    private BloquePrograma(Guid bloqueId, Guid programaId)
    {
        BloqueId = bloqueId;
        ProgramaId = programaId;
    }

    public static BloquePrograma Crear(Guid bloqueId, Guid programaId) =>
        new(
            Guard.IdValido(bloqueId, "bloqueId"),
            Guard.IdValido(programaId, "programaId"));
}
