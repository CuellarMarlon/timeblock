using Timeblock.Application.Personas;

namespace Timeblock.Api.Contracts.Personas;

public static class PersonaRequestMapper
{
    public static CrearPersonaCommand ToCommand(CrearPersonaRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        PerfilCrearCommand perfil = request.Perfil switch
        {
            PerfilEstudianteRequest estudiante =>
                new PerfilEstudianteCrearCommand(
                    estudiante.Edad,
                    estudiante.Grupo,
                    estudiante.CoordinadorId),

            PerfilCoordinadorRequest coordinador =>
                new PerfilCoordinadorCrearCommand(
                    coordinador.Especialidad),

            PerfilJefeDepartamentoRequest =>
                new PerfilJefeDepartamentoCrearCommand(),

            _ => throw new ArgumentException(
                "El tipo de perfil no está soportado.",
                nameof(request))
        };

        return new CrearPersonaCommand(request.Nombre, perfil);
    }

    public static PerfilCrearCommand ToCommand(PerfilRequest request) => request switch
    {
        PerfilEstudianteRequest estudiante => new PerfilEstudianteCrearCommand(
            estudiante.Edad, estudiante.Grupo, estudiante.CoordinadorId),
        PerfilCoordinadorRequest coordinador => new PerfilCoordinadorCrearCommand(coordinador.Especialidad),
        PerfilJefeDepartamentoRequest => new PerfilJefeDepartamentoCrearCommand(),
        _ => throw new ArgumentException("El tipo de perfil no está soportado.", nameof(request))
    };
}
