using Timeblock.Application.Horarios;

namespace Timeblock.Api.Contracts.Horarios;

public static class HorariosRequestMapper
{
    public static CrearHorarioCommand ToCommand(CrearHorarioRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new CrearHorarioCommand(
            request.ProgramaId,
            request.PeriodoAcademico,
            request.CoordinadorId,
            request.Bloques
                .Select(bloque => new BloqueEnHorarioCommand(bloque.BloqueId, bloque.Fecha))
                .ToArray());
    }
}
