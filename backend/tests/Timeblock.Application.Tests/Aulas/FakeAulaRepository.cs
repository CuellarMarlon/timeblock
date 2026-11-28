using Timeblock.Application.Aulas;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Aulas;

namespace Timeblock.Application.Tests.Aulas;

public sealed class FakeAulaRepository : IAulaRepository
{
    public Dictionary<Guid, Aula> Aulas { get; } = [];

    public int Consultas { get; private set; }

    public string? UltimoFiltro { get; private set; }

    public PageRequest? UltimaPagina { get; private set; }

    public Task AgregarAsync(Aula aula, CancellationToken ct)
    {
        Aulas.Add(aula.Id, aula);
        return Task.CompletedTask;
    }

    public Task<Aula?> ObtenerAsync(Guid aulaId, CancellationToken ct)
    {
        Consultas++;
        return Task.FromResult(Aulas.GetValueOrDefault(aulaId));
    }

    public Task<PagedResult<Aula>> ListarAsync(
        string? edificio,
        PageRequest page,
        CancellationToken ct)
    {
        Consultas++;
        UltimoFiltro = edificio;
        UltimaPagina = page;

        List<Aula> items = Aulas.Values
            .Where(aula => edificio is null
                || aula.Edificio.Contains(edificio, StringComparison.OrdinalIgnoreCase))
            .OrderBy(aula => aula.Edificio, StringComparer.Ordinal)
            .ThenBy(aula => aula.Capacidad)
            .ThenBy(aula => aula.Id)
            .ToList();

        return Task.FromResult(PagedResult<Aula>.Create(
            items.Skip(page.Skip).Take(page.PageSize).ToList(),
            page,
            items.Count));
    }

    public Task<bool> ActualizarAsync(Aula aula, CancellationToken ct)
    {
        bool existe = Aulas.ContainsKey(aula.Id);
        if (existe)
        {
            Aulas[aula.Id] = aula;
        }

        return Task.FromResult(existe);
    }
}
