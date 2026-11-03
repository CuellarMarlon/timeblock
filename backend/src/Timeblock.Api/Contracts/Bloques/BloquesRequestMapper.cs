using Timeblock.Application.Bloques;

namespace Timeblock.Api.Contracts.Bloques;

public static class BloquesRequestMapper
{
    public static CrearBloqueCommand ToCommand(CrearBloqueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CrearBloqueCommand(
            request.Descripcion,
            request.TipoActividad,
            request.NivelComplejidad,
            request.AsignaturaId,
            request.CoordinadorId,
            request.ProgramasIds);
    }

    public static ActualizarBloqueCommand ToCommand(ActualizarBloqueRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new ActualizarBloqueCommand(
            request.Descripcion,
            request.TipoActividad,
            request.NivelComplejidad,
            request.AsignaturaId);
    }
}
