using Timeblock.Application.Common.Pagination;
using Timeblock.Application.Horarios;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Personas;

namespace Timeblock.Application.Tests.Horarios;

///Repositorio en memoria para probar los casos de uso de horarios.
public sealed class FakeHorarioRepository : IHorarioRepository
{
    public HashSet<Guid> Programas { get; } = [];

    public HashSet<Guid> Coordinadores { get; } = [];

    public List<AutorizacionCoordinador> Autorizaciones { get; } = [];

    public Dictionary<Guid, BloqueContenido> Bloques { get; } = [];

    public List<Horario> Horarios { get; } = [];


    public Exception? ErrorAlGuardar { get; set; }

    public int Consultas { get; private set; }

    public Task<bool> ExisteProgramaAsync(Guid programaId, CancellationToken ct)
    {
        Consultas++;
        return Task.FromResult(Programas.Contains(programaId));
    }

    public Task<bool> ExisteCoordinadorAsync(Guid coordinadorId, CancellationToken ct)
    {
        Consultas++;
        return Task.FromResult(Coordinadores.Contains(coordinadorId));
    }

    public Task<IReadOnlyList<AutorizacionCoordinador>> ObtenerAutorizacionesAsync(
        Guid coordinadorId,
        Guid programaId,
        CancellationToken ct)
    {
        Consultas++;
        IReadOnlyList<AutorizacionCoordinador> encontradas = Autorizaciones
            .Where(autorizacion => autorizacion.CoordinadorId == coordinadorId
                && autorizacion.ProgramaId == programaId)
            .ToList();
        return Task.FromResult(encontradas);
    }

    public Task<IReadOnlyList<BloqueContenido>> ObtenerBloquesAsync(
        IReadOnlyCollection<Guid> bloquesIds,
        CancellationToken ct)
    {
        Consultas++;
        IReadOnlyList<BloqueContenido> encontrados = bloquesIds
            .Where(Bloques.ContainsKey)
            .Select(id => Bloques[id])
            .ToList();
        return Task.FromResult(encontrados);
    }

    public Task AgregarAsync(Horario horario, CancellationToken ct)
    {
        if (ErrorAlGuardar is not null)
        {
            throw ErrorAlGuardar;
        }

        Horarios.Add(horario);
        return Task.CompletedTask;
    }

    public Task<Horario?> ObtenerAsync(Guid horarioId, CancellationToken ct)
    {
        Consultas++;
        return Task.FromResult(Horarios.FirstOrDefault(horario => horario.Id == horarioId));
    }

    public Task<PagedResult<Horario>> ListarAsync(
        HorarioFilter filter,
        PageRequest page,
        CancellationToken ct)
    {
        Consultas++;
        IEnumerable<Horario> consulta = Horarios;

        if (filter.ProgramaId is { } programaId)
        {
            consulta = consulta.Where(horario => horario.ProgramaId == programaId);
        }

        if (filter.PeriodoAcademico is { } periodo)
        {
            consulta = consulta.Where(horario => horario.PeriodoAcademico == periodo);
        }

        if (filter.Estado is { } estado)
        {
            consulta = consulta.Where(horario => horario.Estado == estado);
        }

        List<Horario> filtrados = consulta
            .OrderByDescending(horario => horario.FechaCreacion)
            .ThenBy(horario => horario.Id)
            .ToList();

        List<Horario> pagina = filtrados.Skip(page.Skip).Take(page.PageSize).ToList();
        return Task.FromResult(PagedResult<Horario>.Create(pagina, page, filtrados.Count));
    }
}
