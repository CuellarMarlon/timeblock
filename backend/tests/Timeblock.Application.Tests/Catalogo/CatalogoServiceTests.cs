using Timeblock.Application.Catalogo;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Common;

namespace Timeblock.Application.Tests.Catalogo;

public sealed class CatalogoServiceTests
{
    [Fact]
    public async Task CrearPrograma_ValidaYDevuelveLaEntidad()
    {
        var repository = new FakeCatalogoRepository();
        var service = new CatalogoService(repository);

        ProgramaAcademico created = await service.CrearProgramaAsync(
            " Ingeniería Informática ", " Plan 2026 ", CancellationToken.None);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal("Ingeniería Informática", created.Nombre);
        Assert.Equal("Plan 2026", created.PlanEstudio);
        Assert.Contains(created.Id, repository.Programas.Keys);
    }

    [Fact]
    public async Task CrearPrograma_NombreVacio_DevuelveErrorDeSolicitud()
    {
        var service = new CatalogoService(new FakeCatalogoRepository());

        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            service.CrearProgramaAsync("  ", "Plan 2026", CancellationToken.None));
    }

    [Fact]
    public async Task ObtenerPrograma_Inexistente_DevuelveNotFound()
    {
        var service = new CatalogoService(new FakeCatalogoRepository());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.ObtenerProgramaAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ActualizarAsignatura_CambiaNombreSinCambiarIdentificador()
    {
        var repository = new FakeCatalogoRepository();
        Asignatura subject = Asignatura.Crear(Guid.NewGuid(), "Bases de datos");
        repository.Asignaturas.Add(subject.Id, subject);
        var service = new CatalogoService(repository);

        Asignatura updated = await service.ActualizarAsignaturaAsync(
            subject.Id, "Bases de datos II", CancellationToken.None);

        Assert.Equal(subject.Id, updated.Id);
        Assert.Equal("Bases de datos II", updated.Nombre);
    }

    [Fact]
    public async Task AsociarAsignatura_RepetirLaOperacionEsIdempotente()
    {
        var repository = new FakeCatalogoRepository();
        ProgramaAcademico program = ProgramaAcademico.Crear(Guid.NewGuid(), "Programa", "Plan");
        Asignatura subject = Asignatura.Crear(Guid.NewGuid(), "Asignatura");
        repository.Programas.Add(program.Id, program);
        repository.Asignaturas.Add(subject.Id, subject);
        var service = new CatalogoService(repository);

        await service.AsociarAsignaturaAsync(program.Id, subject.Id, CancellationToken.None);
        await service.AsociarAsignaturaAsync(program.Id, subject.Id, CancellationToken.None);

        Assert.Single(repository.Asociaciones);
    }

    [Fact]
    public async Task AsociarAsignatura_ProgramaInexistente_DevuelveNotFound()
    {
        var repository = new FakeCatalogoRepository();
        Asignatura subject = Asignatura.Crear(Guid.NewGuid(), "Asignatura");
        repository.Asignaturas.Add(subject.Id, subject);
        var service = new CatalogoService(repository);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.AsociarAsignaturaAsync(Guid.NewGuid(), subject.Id, CancellationToken.None));
    }

    [Fact]
    public async Task AsociarAsignatura_AsignaturaInexistente_DevuelveNotFound()
    {
        var repository = new FakeCatalogoRepository();
        ProgramaAcademico program = ProgramaAcademico.Crear(Guid.NewGuid(), "Programa", "Plan");
        repository.Programas.Add(program.Id, program);
        var service = new CatalogoService(repository);

        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.AsociarAsignaturaAsync(program.Id, Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task ListarProgramas_UsaFiltroYPaginacionValidados()
    {
        var repository = new FakeCatalogoRepository();
        ProgramaAcademico program = ProgramaAcademico.Crear(Guid.NewGuid(), "Ingeniería", "Plan");
        repository.Programas.Add(program.Id, program);
        var service = new CatalogoService(repository);

        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            service.ListarProgramasAsync(null, 1, 101, CancellationToken.None));
    }

    private sealed class FakeCatalogoRepository : ICatalogoRepository
    {
        public Dictionary<Guid, ProgramaAcademico> Programas { get; } = [];
        public Dictionary<Guid, Asignatura> Asignaturas { get; } = [];
        public HashSet<(Guid ProgramaId, Guid AsignaturaId)> Asociaciones { get; } = [];

        public Task AgregarProgramaAsync(ProgramaAcademico programa, CancellationToken ct)
        {
            Programas.Add(programa.Id, programa);
            return Task.CompletedTask;
        }

        public Task<ProgramaAcademico?> ObtenerProgramaAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Programas.GetValueOrDefault(id));

        public Task<PagedResult<ProgramaAcademico>> ListarProgramasAsync(string? nombre, PageRequest page, CancellationToken ct) =>
            Task.FromResult(Paginate(Programas.Values.Where(x => Matches(x.Nombre, nombre)), page));

        public Task GuardarProgramaAsync(CancellationToken ct) => Task.CompletedTask;

        public Task AgregarAsignaturaAsync(Asignatura asignatura, CancellationToken ct)
        {
            Asignaturas.Add(asignatura.Id, asignatura);
            return Task.CompletedTask;
        }

        public Task<Asignatura?> ObtenerAsignaturaAsync(Guid id, CancellationToken ct) =>
            Task.FromResult(Asignaturas.GetValueOrDefault(id));

        public Task<PagedResult<Asignatura>> ListarAsignaturasAsync(string? nombre, PageRequest page, CancellationToken ct) =>
            Task.FromResult(Paginate(Asignaturas.Values.Where(x => Matches(x.Nombre, nombre)), page));

        public Task GuardarAsignaturaAsync(CancellationToken ct) => Task.CompletedTask;
        public Task<bool> ExisteProgramaAsync(Guid id, CancellationToken ct) => Task.FromResult(Programas.ContainsKey(id));
        public Task<bool> ExisteAsignaturaAsync(Guid id, CancellationToken ct) => Task.FromResult(Asignaturas.ContainsKey(id));
        public Task<bool> EstaAsociadaAsync(Guid programId, Guid subjectId, CancellationToken ct) =>
            Task.FromResult(Asociaciones.Contains((programId, subjectId)));

        public Task AsociarAsync(Guid programId, Guid subjectId, CancellationToken ct)
        {
            Asociaciones.Add((programId, subjectId));
            return Task.CompletedTask;
        }

        public Task<PagedResult<Asignatura>> ListarAsignaturasDeProgramaAsync(Guid programId, PageRequest page, CancellationToken ct)
        {
            IEnumerable<Asignatura> subjects = Asociaciones
                .Where(x => x.ProgramaId == programId)
                .Select(x => Asignaturas[x.AsignaturaId]);
            return Task.FromResult(Paginate(subjects, page));
        }

        private static PagedResult<T> Paginate<T>(IEnumerable<T> source, PageRequest page)
        {
            List<T> values = source.ToList();
            return PagedResult<T>.Create(values.Skip(page.Skip).Take(page.PageSize).ToArray(), page, values.Count);
        }

        private static bool Matches(string value, string? filter) =>
            string.IsNullOrWhiteSpace(filter) || value.Contains(filter.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
