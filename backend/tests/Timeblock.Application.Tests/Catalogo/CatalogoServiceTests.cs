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

    [Fact]
    public async Task DesasociarAsignatura_EliminaSoloLaRelacionYRechazaRepeticion()
    {
        var repository = new FakeCatalogoRepository();
        Guid programa = Guid.NewGuid(), asignatura = Guid.NewGuid(), otra = Guid.NewGuid();
        repository.Asociaciones.Add((programa, asignatura));
        repository.Asociaciones.Add((programa, otra));
        var service = new CatalogoService(repository);

        await service.DesasociarAsignaturaAsync(programa, asignatura, CancellationToken.None);

        Assert.DoesNotContain((programa, asignatura), repository.Asociaciones);
        Assert.Contains((programa, otra), repository.Asociaciones);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            service.DesasociarAsignaturaAsync(programa, asignatura, CancellationToken.None));
    }

    [Fact]
    public async Task DesasociarAsignatura_IdsVacios_NoModificaRelaciones()
    {
        var repository = new FakeCatalogoRepository();
        Guid programa = Guid.NewGuid(), asignatura = Guid.NewGuid();
        repository.Asociaciones.Add((programa, asignatura));
        var service = new CatalogoService(repository);

        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            service.DesasociarAsignaturaAsync(Guid.Empty, asignatura, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            service.DesasociarAsignaturaAsync(programa, Guid.Empty, CancellationToken.None));
        Assert.Contains((programa, asignatura), repository.Asociaciones);
    }

    [Fact]
    public async Task EliminarPrograma_ExistenteLoQuitaYRepetidoLanzaNotFound()
    {
        var repository = new FakeCatalogoRepository();
        var programa = ProgramaAcademico.Crear(Guid.NewGuid(), "Programa", "Plan");
        repository.Programas.Add(programa.Id, programa);
        var service = new CatalogoService(repository);
        await service.EliminarProgramaAsync(programa.Id, CancellationToken.None);
        Assert.Empty(repository.Programas);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.EliminarProgramaAsync(programa.Id, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidRequestException>(() => service.EliminarProgramaAsync(Guid.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task EliminarAsignatura_ExistenteLoQuitaYRepetidaLanzaNotFound()
    {
        var repository = new FakeCatalogoRepository();
        var asignatura = Asignatura.Crear(Guid.NewGuid(), "Asignatura");
        repository.Asignaturas.Add(asignatura.Id, asignatura);
        var service = new CatalogoService(repository);
        await service.EliminarAsignaturaAsync(asignatura.Id, CancellationToken.None);
        Assert.Empty(repository.Asignaturas);
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.EliminarAsignaturaAsync(asignatura.Id, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidRequestException>(() => service.EliminarAsignaturaAsync(Guid.Empty, CancellationToken.None));
    }

    [Fact]
    public async Task CambiarEstado_CatalogoInactivaYReactiva()
    {
        var repository = new FakeCatalogoRepository();
        var programa = ProgramaAcademico.Crear(Guid.NewGuid(), "Programa", "Plan");
        var asignatura = Asignatura.Crear(Guid.NewGuid(), "Asignatura");
        repository.Programas.Add(programa.Id, programa);
        repository.Asignaturas.Add(asignatura.Id, asignatura);
        var service = new CatalogoService(repository);
        Assert.False((await service.CambiarEstadoProgramaAsync(programa.Id, false, CancellationToken.None)).Activo);
        Assert.False((await service.CambiarEstadoAsignaturaAsync(asignatura.Id, false, CancellationToken.None)).Activo);
        Assert.True((await service.CambiarEstadoProgramaAsync(programa.Id, true, CancellationToken.None)).Activo);
        Assert.True((await service.CambiarEstadoAsignaturaAsync(asignatura.Id, true, CancellationToken.None)).Activo);
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
        public Task<bool> EliminarProgramaAsync(Guid id, CancellationToken ct) => Task.FromResult(Programas.Remove(id));

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
        public Task<bool> EliminarAsignaturaAsync(Guid id, CancellationToken ct) => Task.FromResult(Asignaturas.Remove(id));
        public Task<bool> ExisteProgramaAsync(Guid id, CancellationToken ct) => Task.FromResult(Programas.ContainsKey(id));
        public Task<bool> ExisteAsignaturaAsync(Guid id, CancellationToken ct) => Task.FromResult(Asignaturas.ContainsKey(id));
        public Task<bool> EstaAsociadaAsync(Guid programId, Guid subjectId, CancellationToken ct) =>
            Task.FromResult(Asociaciones.Contains((programId, subjectId)));

        public Task AsociarAsync(Guid programId, Guid subjectId, CancellationToken ct)
        {
            Asociaciones.Add((programId, subjectId));
            return Task.CompletedTask;
        }

        public Task<bool> DesasociarAsync(Guid programId, Guid subjectId, CancellationToken ct) =>
            Task.FromResult(Asociaciones.Remove((programId, subjectId)));

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
