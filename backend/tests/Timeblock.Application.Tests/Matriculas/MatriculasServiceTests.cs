using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Application.Matriculas;
using Timeblock.Domain.Matriculas;

namespace Timeblock.Application.Tests.Matriculas;

public sealed class MatriculasServiceTests
{
    private static readonly Guid Estudiante = Guid.NewGuid();
    private static readonly Guid Programa = Guid.NewGuid();

    private static (MatriculasService Service, FakeMatriculaRepository Repository) Crear()
    {
        var repository = new FakeMatriculaRepository();
        repository.Personas.Add(Estudiante);
        repository.Estudiantes.Add(Estudiante);
        repository.Programas.Add(Programa);
        return (new MatriculasService(repository), repository);
    }

    [Fact]
    public async Task Matricular_Valida_GuardaLaPareja()
    {
        var (service, repository) = Crear();

        Matricula result = await service.MatricularAsync(Programa, Estudiante, CancellationToken.None);

        Assert.Equal(Estudiante, result.EstudianteId);
        Assert.Equal(Programa, result.ProgramaId);
        Assert.Same(result, Assert.Single(repository.Matriculas));
    }

    [Fact]
    public async Task Matricular_IdsVacios_LanzaBadRequestSinConsultar()
    {
        var (service, repository) = Crear();

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.MatricularAsync(Programa, Guid.Empty, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.MatricularAsync(Guid.Empty, Estudiante, CancellationToken.None));
        Assert.Equal(0, repository.Consultas);
    }

    [Fact]
    public async Task Matricular_PersonaInexistente_LanzaNotFound()
    {
        var (service, repository) = Crear();

        var ex = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.MatricularAsync(Programa, Guid.NewGuid(), CancellationToken.None));
        Assert.Contains("No existe la persona", ex.Message);
        Assert.Empty(repository.Matriculas);
    }

    [Fact]
    public async Task Matricular_PersonaSinPerfilDeEstudiante_LanzaNotFound()
    {
        var (service, repository) = Crear();
        Guid coordinador = Guid.NewGuid();
        repository.Personas.Add(coordinador);

        var ex = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.MatricularAsync(Programa, coordinador, CancellationToken.None));
        Assert.Contains("no tiene perfil de estudiante", ex.Message);
        Assert.Empty(repository.Matriculas);
    }

    [Fact]
    public async Task Matricular_ProgramaInexistente_LanzaNotFound()
    {
        var (service, repository) = Crear();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.MatricularAsync(Guid.NewGuid(), Estudiante, CancellationToken.None));
        Assert.Empty(repository.Matriculas);
    }

    [Fact]
    public async Task Matricular_Duplicada_LanzaConflictYNoDuplica()
    {
        var (service, repository) = Crear();
        await service.MatricularAsync(Programa, Estudiante, CancellationToken.None);

        await Assert.ThrowsAsync<ResourceConflictException>(
            () => service.MatricularAsync(Programa, Estudiante, CancellationToken.None));
        Assert.Single(repository.Matriculas);
    }

    [Fact]
    public async Task ListarPorPrograma_SinPaginacion_UsaValoresPorDefecto()
    {
        var (service, repository) = Crear();
        await service.MatricularAsync(Programa, Estudiante, CancellationToken.None);

        PagedResult<Matricula> page = await service.ListarPorProgramaAsync(Programa, null, null, CancellationToken.None);

        Assert.Equal(PageRequest.DefaultPage, page.Page);
        Assert.Equal(PageRequest.DefaultPageSize, page.PageSize);
        Assert.Equal(1, page.TotalItems);
        Assert.Equal(Estudiante, Assert.Single(page.Items).EstudianteId);
    }

    [Fact]
    public async Task ListarPorEstudiante_DevuelveSusProgramas()
    {
        var (service, repository) = Crear();
        Guid otroPrograma = Guid.NewGuid();
        repository.Programas.Add(otroPrograma);
        await service.MatricularAsync(Programa, Estudiante, CancellationToken.None);
        await service.MatricularAsync(otroPrograma, Estudiante, CancellationToken.None);

        PagedResult<Matricula> page = await service.ListarPorEstudianteAsync(Estudiante, 1, 20, CancellationToken.None);

        Assert.Equal(2, page.TotalItems);
        Assert.All(page.Items, m => Assert.Equal(Estudiante, m.EstudianteId));
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Listar_PaginacionFueraDeRango_LanzaBadRequest(int page, int pageSize)
    {
        var (service, _) = Crear();

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.ListarPorProgramaAsync(Programa, page, pageSize, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => service.ListarPorEstudianteAsync(Estudiante, page, pageSize, CancellationToken.None));
    }

    [Fact]
    public async Task ListarPorPrograma_ProgramaInexistente_LanzaNotFound()
    {
        var (service, _) = Crear();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.ListarPorProgramaAsync(Guid.NewGuid(), null, null, CancellationToken.None));
    }

    [Fact]
    public async Task ListarPorEstudiante_SinPerfilDeEstudiante_LanzaNotFound()
    {
        var (service, repository) = Crear();
        Guid jefe = Guid.NewGuid();
        repository.Personas.Add(jefe);

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => service.ListarPorEstudianteAsync(jefe, null, null, CancellationToken.None));
    }

    private sealed class FakeMatriculaRepository : IMatriculaRepository
    {
        public HashSet<Guid> Personas { get; } = [];
        public HashSet<Guid> Estudiantes { get; } = [];
        public HashSet<Guid> Programas { get; } = [];
        public List<Matricula> Matriculas { get; } = [];
        public int Consultas { get; private set; }

        public Task<bool> ExistePersonaAsync(Guid id, CancellationToken ct) => Consultar(Personas.Contains(id));
        public Task<bool> ExisteEstudianteAsync(Guid id, CancellationToken ct) => Consultar(Estudiantes.Contains(id));
        public Task<bool> ExisteProgramaAsync(Guid id, CancellationToken ct) => Consultar(Programas.Contains(id));

        public Task<bool> ExisteAsync(Guid estudianteId, Guid programaId, CancellationToken ct) =>
            Consultar(Matriculas.Any(m => m.EstudianteId == estudianteId && m.ProgramaId == programaId));

        public Task AgregarAsync(Matricula matricula, CancellationToken ct)
        {
            Matriculas.Add(matricula);
            return Task.CompletedTask;
        }

        public Task<PagedResult<Matricula>> ListarPorProgramaAsync(Guid programaId, PageRequest page, CancellationToken ct) =>
            Paginar(Matriculas.Where(m => m.ProgramaId == programaId).ToList(), page);

        public Task<PagedResult<Matricula>> ListarPorEstudianteAsync(Guid estudianteId, PageRequest page, CancellationToken ct) =>
            Paginar(Matriculas.Where(m => m.EstudianteId == estudianteId).ToList(), page);

        private Task<bool> Consultar(bool resultado)
        {
            Consultas++;
            return Task.FromResult(resultado);
        }

        private static Task<PagedResult<Matricula>> Paginar(List<Matricula> items, PageRequest page) =>
            Task.FromResult(PagedResult<Matricula>.Create(
                items.Skip(page.Skip).Take(page.PageSize).ToList(), page, items.Count));
    }
}
