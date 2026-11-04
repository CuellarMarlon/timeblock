using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Timeblock.Api.Tests.Support;
using Timeblock.Application.Common.Pagination;
using Timeblock.Application.Matriculas;
using Timeblock.Domain.Matriculas;

namespace Timeblock.Api.Tests;

public sealed class MatriculasEndpointsTests(TimeblockApiFactory factory)
    : IClassFixture<TimeblockApiFactory>
{
    private static readonly Guid Estudiante = Guid.NewGuid();
    private static readonly Guid Programa = Guid.NewGuid();

    private HttpClient CrearCliente(MemoriaMatriculaRepository repository) =>
        factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Scoped<IMatriculaRepository>(_ => repository))))
        .CreateClient();

    private static MemoriaMatriculaRepository RepositorioConDatos()
    {
        var repository = new MemoriaMatriculaRepository();
        repository.Personas.Add(Estudiante);
        repository.Estudiantes.Add(Estudiante);
        repository.Programas.Add(Programa);
        return repository;
    }

    [Fact]
    public async Task Crear_Valida_Responde201ConLocationYCuerpoDelContrato()
    {
        using HttpClient client = CrearCliente(RepositorioConDatos());

        using HttpResponseMessage respuesta = await client.PostAsync(
            $"/api/programas/{Programa}/matriculas",
            RespuestaExtensions.Json($$"""{"estudianteId":"{{Estudiante}}"}"""));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.EndsWith($"/api/programas/{Programa}/matriculas", respuesta.Headers.Location!.ToString(),
            StringComparison.OrdinalIgnoreCase);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(["estudianteId", "programaId"], json.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(Estudiante, json.GetProperty("estudianteId").GetGuid());
        Assert.Equal(Programa, json.GetProperty("programaId").GetGuid());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"estudianteId":"no-es-uuid"}""")]
    public async Task Crear_SinEstudianteIdOMalFormado_Responde400(string cuerpo)
    {
        using HttpClient client = CrearCliente(RepositorioConDatos());

        using HttpResponseMessage respuesta = await client.PostAsync(
            $"/api/programas/{Programa}/matriculas", RespuestaExtensions.Json(cuerpo));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Crear_EstudianteInexistente_Responde404()
    {
        using HttpClient client = CrearCliente(RepositorioConDatos());

        using HttpResponseMessage respuesta = await client.PostAsync(
            $"/api/programas/{Programa}/matriculas",
            RespuestaExtensions.Json($$"""{"estudianteId":"{{Guid.NewGuid()}}"}"""));

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Crear_Duplicada_Responde409()
    {
        MemoriaMatriculaRepository repository = RepositorioConDatos();
        repository.Matriculas.Add(new Matricula(Estudiante, Programa));
        using HttpClient client = CrearCliente(repository);

        using HttpResponseMessage respuesta = await client.PostAsync(
            $"/api/programas/{Programa}/matriculas",
            RespuestaExtensions.Json($$"""{"estudianteId":"{{Estudiante}}"}"""));

        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ListarPorPrograma_UsaLaPaginacionComun()
    {
        MemoriaMatriculaRepository repository = RepositorioConDatos();
        repository.Matriculas.Add(new Matricula(Estudiante, Programa));
        using HttpClient client = CrearCliente(repository);

        using HttpResponseMessage respuesta = await client.GetAsync($"/api/programas/{Programa}/matriculas");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(["items", "page", "pageSize", "totalItems"],
            json.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(1, json.GetProperty("page").GetInt32());
        Assert.Equal(20, json.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, json.GetProperty("totalItems").GetInt32());
        Assert.Equal(Estudiante, json.GetProperty("items")[0].GetProperty("estudianteId").GetGuid());
    }

    [Fact]
    public async Task ListarPorEstudiante_DevuelveSusProgramas()
    {
        MemoriaMatriculaRepository repository = RepositorioConDatos();
        repository.Matriculas.Add(new Matricula(Estudiante, Programa));
        using HttpClient client = CrearCliente(repository);

        using HttpResponseMessage respuesta = await client.GetAsync($"/api/estudiantes/{Estudiante}/matriculas");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(Programa, json.GetProperty("items")[0].GetProperty("programaId").GetGuid());
    }

    [Fact]
    public async Task Listar_PageSizeFueraDeRango_Responde400()
    {
        using HttpClient client = CrearCliente(RepositorioConDatos());

        using HttpResponseMessage respuesta = await client.GetAsync(
            $"/api/programas/{Programa}/matriculas?pageSize=101");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task ListarPorEstudiante_Inexistente_Responde404()
    {
        using HttpClient client = CrearCliente(RepositorioConDatos());

        using HttpResponseMessage respuesta = await client.GetAsync($"/api/estudiantes/{Guid.NewGuid()}/matriculas");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    private sealed class MemoriaMatriculaRepository : IMatriculaRepository
    {
        public HashSet<Guid> Personas { get; } = [];
        public HashSet<Guid> Estudiantes { get; } = [];
        public HashSet<Guid> Programas { get; } = [];
        public List<Matricula> Matriculas { get; } = [];

        public Task<bool> ExistePersonaAsync(Guid id, CancellationToken ct) => Task.FromResult(Personas.Contains(id));
        public Task<bool> ExisteEstudianteAsync(Guid id, CancellationToken ct) => Task.FromResult(Estudiantes.Contains(id));
        public Task<bool> ExisteProgramaAsync(Guid id, CancellationToken ct) => Task.FromResult(Programas.Contains(id));

        public Task<bool> ExisteAsync(Guid estudianteId, Guid programaId, CancellationToken ct) =>
            Task.FromResult(Matriculas.Any(m => m.EstudianteId == estudianteId && m.ProgramaId == programaId));

        public Task AgregarAsync(Matricula matricula, CancellationToken ct)
        {
            Matriculas.Add(matricula);
            return Task.CompletedTask;
        }

        public Task<PagedResult<Matricula>> ListarPorProgramaAsync(Guid programaId, PageRequest page, CancellationToken ct) =>
            Paginar(Matriculas.Where(m => m.ProgramaId == programaId).ToList(), page);

        public Task<PagedResult<Matricula>> ListarPorEstudianteAsync(Guid estudianteId, PageRequest page, CancellationToken ct) =>
            Paginar(Matriculas.Where(m => m.EstudianteId == estudianteId).ToList(), page);

        private static Task<PagedResult<Matricula>> Paginar(List<Matricula> items, PageRequest page) =>
            Task.FromResult(PagedResult<Matricula>.Create(
                items.Skip(page.Skip).Take(page.PageSize).ToList(), page, items.Count));
    }
}
