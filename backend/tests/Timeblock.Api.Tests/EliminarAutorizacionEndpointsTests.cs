using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Timeblock.Api.Tests.Support;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Personas;
using Timeblock.Domain.Personas;

namespace Timeblock.Api.Tests;

public sealed class EliminarAutorizacionEndpointsTests(TimeblockApiFactory factory) : IClassFixture<TimeblockApiFactory>
{
    [Theory]
    [InlineData("existe", HttpStatusCode.NoContent)]
    [InlineData("ausente", HttpStatusCode.NotFound)]
    [InlineData("utilizada", HttpStatusCode.Conflict)]
    public async Task Eliminar_RespondeSegunResultado(string escenario, HttpStatusCode esperado)
    {
        IPersonaRepository repo = DispatchProxy.Create<IPersonaRepository, PersonaProxy>();
        ((PersonaProxy)repo).Escenario = escenario;
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Scoped<IPersonaRepository>(_ => repo))));
        using var client = app.CreateClient();
        using var respuesta = await client.DeleteAsync($"/api/coordinadores/{Guid.NewGuid()}/autorizaciones/{Guid.NewGuid()}/2026-01-01");
        Assert.Equal(esperado, respuesta.StatusCode);
        if (esperado != HttpStatusCode.NoContent)
            Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("00000000-0000-0000-0000-000000000000", "2026-01-01")]
    [InlineData("11111111-1111-1111-1111-111111111111", "fecha-invalida")]
    public async Task Eliminar_DatosInvalidos_Responde400(string programa, string fecha)
    {
        using var client = factory.CreateClient();
        using var respuesta = await client.DeleteAsync($"/api/coordinadores/{Guid.NewGuid()}/autorizaciones/{programa}/{fecha}");
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    [Fact]
    public async Task Actualizar_SiInvalidaHorarios_Responde409()
    {
        IPersonaRepository repo = DispatchProxy.Create<IPersonaRepository, PersonaProxy>();
        ((PersonaProxy)repo).Escenario = "actualizar-conflicto";
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Scoped<IPersonaRepository>(_ => repo))));
        using var client = app.CreateClient();
        using var respuesta = await client.PutAsync(
            $"/api/coordinadores/{Guid.NewGuid()}/autorizaciones/{Guid.NewGuid()}/2026-01-01",
            RespuestaExtensions.Json("""{"fechaFin":null,"puedeGenerar":false,"puedeValidar":false}"""));
        Assert.Equal(HttpStatusCode.Conflict, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("existe", HttpStatusCode.NoContent)]
    [InlineData("ausente", HttpStatusCode.NotFound)]
    [InlineData("utilizada", HttpStatusCode.Conflict)]
    public async Task EliminarPersona_RespondeSegunResultado(string escenario, HttpStatusCode esperado)
    {
        IPersonaRepository repo = DispatchProxy.Create<IPersonaRepository, PersonaProxy>();
        ((PersonaProxy)repo).Escenario = escenario;
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            services.Replace(ServiceDescriptor.Scoped<IPersonaRepository>(_ => repo))));
        using var client = app.CreateClient();
        using var respuesta = await client.DeleteAsync($"/api/personas/{Guid.NewGuid()}");
        Assert.Equal(esperado, respuesta.StatusCode);
    }

    public class PersonaProxy : DispatchProxy
    {
        public string Escenario { get; set; } = "";
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (Escenario == "actualizar-conflicto")
            {
                if (targetMethod?.Name == nameof(IPersonaRepository.ObtenerAutorizacionAsync))
                    return Task.FromResult<AutorizacionCoordinador?>(new AutorizacionCoordinador(
                        (Guid)args![0]!, (Guid)args[1]!, (DateOnly)args[2]!, null, true, false));
                if (targetMethod?.Name == nameof(IPersonaRepository.GuardarAutorizacionAsync))
                    return Task.FromException(new ResourceConflictException("Cambio inválido."));
            }
            return targetMethod?.Name is nameof(IPersonaRepository.EliminarAutorizacionAsync) or nameof(IPersonaRepository.EliminarAsync)
                ? Escenario switch
                {
                    "existe" => Task.FromResult(true),
                    "ausente" => Task.FromResult(false),
                    "utilizada" => Task.FromException<bool>(new ResourceConflictException("Autorización utilizada.")),
                    _ => throw new InvalidOperationException(),
                }
                : throw new NotSupportedException();
        }
    }
}
