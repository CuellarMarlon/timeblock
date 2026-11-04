using Timeblock.Application.Bloques;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;

namespace Timeblock.Application.Tests.Bloques;

public sealed class FakeBloqueRepository : IBloqueRepository
{
    public HashSet<Guid> Asignaturas { get; } = [];

    public HashSet<Guid> Coordinadores { get; } = [];

    public Dictionary<Guid, ProgramaAcademico> Programas { get; } = [];

    public List<BloqueContenido> Bloques { get; } = [];

   
    public int Consultas { get; private set; }


    public int Guardados { get; private set; }

    public Guid AgregarAsignatura()
    {
        Guid id = Guid.NewGuid();
        Asignaturas.Add(id);
        return id;
    }

    public Guid AgregarCoordinador()
    {
        Guid id = Guid.NewGuid();
        Coordinadores.Add(id);
        return id;
    }

    
    public ProgramaAcademico AgregarPrograma(params Guid[] asignaturasIds)
    {
        ProgramaAcademico programa = ProgramaAcademico.Crear(
            Guid.NewGuid(),
            "Informática",
            "Plan 2026");

        foreach (Guid asignaturaId in asignaturasIds)
        {
            Asignaturas.Add(asignaturaId);
            programa.AsociarAsignatura(asignaturaId);
        }

        Programas.Add(programa.Id, programa);
        return programa;
    }

    public Task<bool> ExisteAsignaturaAsync(Guid asignaturaId, CancellationToken ct)
    {
        Consultas++;
        return Task.FromResult(Asignaturas.Contains(asignaturaId));
    }

    public Task<bool> ExisteCoordinadorAsync(Guid coordinadorId, CancellationToken ct)
    {
        Consultas++;
        return Task.FromResult(Coordinadores.Contains(coordinadorId));
    }

    public Task<IReadOnlyList<ProgramaAcademico>> ObtenerProgramasAsync(
        IReadOnlyCollection<Guid> programasIds,
        CancellationToken ct)
    {
        Consultas++;
        IReadOnlyList<ProgramaAcademico> encontrados = programasIds
            .Where(Programas.ContainsKey)
            .Select(id => Programas[id])
            .ToList();
        return Task.FromResult(encontrados);
    }

    public Task<bool> ExisteDuplicadoAsync(
        ClaveDuplicidad clave,
        Guid? excluirBloqueId,
        CancellationToken ct)
    {
        Consultas++;
        return Task.FromResult(Bloques.Any(bloque =>
            bloque.Id != excluirBloqueId && bloque.ClaveDuplicidad == clave));
    }

    public Task AgregarAsync(BloqueContenido bloque, CancellationToken ct)
    {
        if (Bloques.Any(existente => existente.EsDuplicadoDe(bloque)))
        {
            throw new ResourceConflictException(
                "Ya existe un bloque con la misma asignatura, tipo de actividad y descripción.");
        }

        Bloques.Add(bloque);
        Guardados++;
        return Task.CompletedTask;
    }

    public Task<BloqueContenido?> ObtenerAsync(Guid bloqueId, CancellationToken ct)
    {
        Consultas++;
        return Task.FromResult(Bloques.FirstOrDefault(bloque => bloque.Id == bloqueId));
    }

    public Task<PagedResult<BloqueContenido>> ListarAsync(
        BloqueFilter filter,
        PageRequest page,
        CancellationToken ct)
    {
        Consultas++;
        IEnumerable<BloqueContenido> consulta = Bloques;

        if (filter.ProgramaId is { } programaId)
        {
            consulta = consulta.Where(bloque => bloque.EstaAsociadoAPrograma(programaId));
        }

        if (filter.AsignaturaId is { } asignaturaId)
        {
            consulta = consulta.Where(bloque => bloque.AsignaturaId == asignaturaId);
        }

        if (filter.CoordinadorId is { } coordinadorId)
        {
            consulta = consulta.Where(bloque => bloque.CoordinadorCreadorId == coordinadorId);
        }

        if (filter.TipoActividad is { } tipoActividad)
        {
            consulta = consulta.Where(bloque => bloque.TipoActividad == tipoActividad);
        }

        if (filter.NivelComplejidad is { } nivelComplejidad)
        {
            consulta = consulta.Where(bloque => bloque.NivelComplejidad == nivelComplejidad);
        }

        List<BloqueContenido> filtrados = consulta
            .OrderBy(bloque => bloque.DescripcionNormalizada, StringComparer.Ordinal)
            .ThenBy(bloque => bloque.Id)
            .ToList();

        List<BloqueContenido> pagina = filtrados
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToList();

        return Task.FromResult(PagedResult<BloqueContenido>.Create(pagina, page, filtrados.Count));
    }

    public Task GuardarCambiosAsync(CancellationToken ct)
    {
        Guardados++;
        return Task.CompletedTask;
    }
}
