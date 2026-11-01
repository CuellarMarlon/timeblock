namespace Timeblock.Application.Personas;

public sealed record CrearPersonaCommand(
    string Nombre,
    PerfilCrearCommand Perfil);

public abstract record PerfilCrearCommand;

public sealed record PerfilEstudianteCrearCommand(
    int Edad,
    string Grupo,
    Guid CoordinadorId) : PerfilCrearCommand;

public sealed record PerfilCoordinadorCrearCommand(
    string Especialidad) : PerfilCrearCommand;

public sealed record PerfilJefeDepartamentoCrearCommand : PerfilCrearCommand;
