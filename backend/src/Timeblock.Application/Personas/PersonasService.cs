using Timeblock.Application.Common.Exceptions;
using Timeblock.Domain.Common;
using Timeblock.Domain.Personas;

namespace Timeblock.Application.Personas;

public sealed class PersonasService(IPersonaRepository personas)
{
    public async Task<Persona> CrearAsync(CrearPersonaCommand command, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        try
        {
            Perfil perfil = command.Perfil switch
            {
                PerfilEstudianteCrearCommand p => await CrearEstudianteAsync(p, ct),
                PerfilCoordinadorCrearCommand p => new PerfilCoordinador(p.Especialidad),
                PerfilJefeDepartamentoCrearCommand => new PerfilJefeDepartamento(),
                _ => throw new InvalidRequestException("El tipo de perfil no está soportado.")
            };
            Persona persona = Persona.Crear(command.Nombre, perfil);
            await personas.AgregarAsync(persona, ct);
            return persona;
        }
        catch (DomainException ex) { throw new InvalidRequestException(ex.Message, ex); }
    }

    public async Task<Persona> ObtenerAsync(Guid id, CancellationToken ct) =>
        await personas.ObtenerAsync(id, ct) ?? throw new ResourceNotFoundException($"No existe la persona '{id}'.");

    public Task<PagedResult<Persona>> ListarAsync(PersonaFilter filter, CancellationToken ct)
    {
        ValidarPaginacion(filter.Page, filter.PageSize);
        if (filter.TipoPerfil is { } tipo && !Enum.IsDefined(tipo))
            throw new InvalidRequestException("El tipoPerfil no es válido.");
        return personas.ListarAsync(filter, ct);
    }

    public async Task<Persona> ActualizarNombreAsync(Guid id, string nombre, CancellationToken ct)
    {
        Persona persona = await ObtenerAsync(id, ct);
        try { persona.ActualizarNombre(nombre); }
        catch (DomainException ex) { throw new InvalidRequestException(ex.Message, ex); }
        await personas.GuardarCambiosAsync(persona, ct);
        return persona;
    }

    public async Task<Persona> ActualizarPerfilAsync(Guid id, PerfilCrearCommand perfilCommand, CancellationToken ct)
    {
        Persona persona = await ObtenerAsync(id, ct);
        try
        {
            Perfil perfil = perfilCommand switch
            {
                PerfilEstudianteCrearCommand p => await CrearEstudianteAsync(p, ct),
                PerfilCoordinadorCrearCommand p => new PerfilCoordinador(p.Especialidad),
                PerfilJefeDepartamentoCrearCommand => new PerfilJefeDepartamento(),
                _ => throw new InvalidRequestException("El tipo de perfil no está soportado.")
            };
            persona.ActualizarPerfil(perfil);
        }
        catch (DomainException ex) { throw new InvalidRequestException(ex.Message, ex); }
        await personas.GuardarCambiosAsync(persona, ct);
        return persona;
    }

    public async Task<AutorizacionCoordinador> CrearAutorizacionAsync(
        Guid coordinadorId, Guid programaId, DateOnly fechaInicio, DateOnly? fechaFin,
        bool puedeGenerar, bool puedeValidar, CancellationToken ct)
    {
        if (!await personas.ExisteCoordinadorAsync(coordinadorId, ct))
            throw new ResourceNotFoundException($"No existe el coordinador '{coordinadorId}'.");
        if (!await personas.ExisteProgramaAsync(programaId, ct))
            throw new ResourceNotFoundException($"No existe el programa '{programaId}'.");
        try
        {
            var autorizacion = new AutorizacionCoordinador(
                coordinadorId, programaId, fechaInicio, fechaFin, puedeGenerar, puedeValidar);
            await personas.AgregarAutorizacionAsync(autorizacion, ct);
            return autorizacion;
        }
        catch (DomainException ex) { throw new InvalidRequestException(ex.Message, ex); }
    }

    public async Task<IReadOnlyList<AutorizacionCoordinador>> ListarAutorizacionesAsync(
        Guid coordinadorId, AutorizacionFilter filter, CancellationToken ct)
    {
        if (!await personas.ExisteCoordinadorAsync(coordinadorId, ct))
            throw new ResourceNotFoundException($"No existe el coordinador '{coordinadorId}'.");
        return await personas.ListarAutorizacionesAsync(coordinadorId, filter, ct);
    }

    public async Task<AutorizacionCoordinador> ActualizarAutorizacionAsync(
        Guid coordinadorId, Guid programaId, DateOnly fechaInicio, DateOnly? fechaFin,
        bool puedeGenerar, bool puedeValidar, CancellationToken ct)
    {
        var autorizacion = await personas.ObtenerAutorizacionAsync(coordinadorId, programaId, fechaInicio, ct)
            ?? throw new ResourceNotFoundException("No existe la autorización indicada.");
        try { autorizacion.Actualizar(fechaFin, puedeGenerar, puedeValidar); }
        catch (DomainException ex) { throw new InvalidRequestException(ex.Message, ex); }
        await personas.GuardarAutorizacionAsync(autorizacion, ct);
        return autorizacion;
    }

    private async Task<PerfilEstudiante> CrearEstudianteAsync(PerfilEstudianteCrearCommand p, CancellationToken ct)
    {
        var perfil = new PerfilEstudiante(p.Edad, p.Grupo, p.CoordinadorId);
        if (!await personas.ExisteCoordinadorAsync(p.CoordinadorId, ct))
            throw new ResourceNotFoundException($"No existe el coordinador '{p.CoordinadorId}'.");
        return perfil;
    }

    private static void ValidarPaginacion(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100 || page - 1 > int.MaxValue / pageSize)
            throw new InvalidRequestException("page debe ser >= 1 y pageSize debe estar entre 1 y 100.");
    }
}
