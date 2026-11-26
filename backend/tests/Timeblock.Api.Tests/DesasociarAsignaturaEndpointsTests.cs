using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Timeblock.Api.Tests.Support;
using Timeblock.Application.Catalogo;
using Timeblock.Application.Common.Exceptions;

namespace Timeblock.Api.Tests;

public sealed class DesasociarAsignaturaEndpointsTests(TimeblockApiFactory factory) : IClassFixture<TimeblockApiFactory>
{
    [Theory]
    [InlineData("existe", HttpStatusCode.NoContent)]
    [InlineData("ausente", HttpStatusCode.NotFound)]
    [InlineData("conflicto", HttpStatusCode.Conflict)]
    public async Task Desasociar_RespondeSegunResultado(string escenario, HttpStatusCode esperado)
    {
        ICatalogoRepository repo = DispatchProxy.Create<ICatalogoRepository, CatalogoProxy>();
        ((CatalogoProxy)repo).Escenario = escenario;
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Scoped<ICatalogoRepository>(_ => repo))));
        using HttpClient client = app.CreateClient();

        using HttpResponseMessage respuesta = await client.DeleteAsync(
            $"/api/programas/{Guid.NewGuid()}/asignaturas/{Guid.NewGuid()}");

        Assert.Equal(esperado, respuesta.StatusCode);
        if (esperado != HttpStatusCode.NoContent)
            Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Desasociar_IdVacio_Responde400()
    {
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage respuesta = await client.DeleteAsync(
            $"/api/programas/{Guid.Empty}/asignaturas/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("existe", HttpStatusCode.NoContent)]
    [InlineData("ausente", HttpStatusCode.NotFound)]
    [InlineData("conflicto", HttpStatusCode.Conflict)]
    public async Task EliminarPrograma_RespondeSegunResultado(string escenario, HttpStatusCode esperado)
    {
        ICatalogoRepository repo = DispatchProxy.Create<ICatalogoRepository, CatalogoProxy>();
        ((CatalogoProxy)repo).Escenario = escenario;
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Scoped<ICatalogoRepository>(_ => repo))));
        using HttpClient client = app.CreateClient();
        using var respuesta = await client.DeleteAsync($"/api/programas/{Guid.NewGuid()}");
        Assert.Equal(esperado, respuesta.StatusCode);
    }

    [Theory]
    [InlineData("existe", HttpStatusCode.NoContent)]
    [InlineData("ausente", HttpStatusCode.NotFound)]
    [InlineData("conflicto", HttpStatusCode.Conflict)]
    public async Task EliminarAsignatura_RespondeSegunResultado(string escenario, HttpStatusCode esperado)
    {
        ICatalogoRepository repo = DispatchProxy.Create<ICatalogoRepository, CatalogoProxy>();
        ((CatalogoProxy)repo).Escenario = escenario;
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Scoped<ICatalogoRepository>(_ => repo))));
        using HttpClient client = app.CreateClient();
        using var respuesta = await client.DeleteAsync($"/api/asignaturas/{Guid.NewGuid()}");
        Assert.Equal(esperado, respuesta.StatusCode);
    }

    public class CatalogoProxy : DispatchProxy
    {
        public string Escenario { get; set; } = "";

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(ICatalogoRepository.DesasociarAsync) &&
                targetMethod?.Name != nameof(ICatalogoRepository.EliminarProgramaAsync) &&
                targetMethod?.Name != nameof(ICatalogoRepository.EliminarAsignaturaAsync))
                throw new NotSupportedException();
            return Escenario switch
            {
                "existe" => Task.FromResult(true),
                "ausente" => Task.FromResult(false),
                "conflicto" => Task.FromException<bool>(new ResourceConflictException("Bloques asociados.")),
                _ => throw new InvalidOperationException(),
            };
        }
    }
}
