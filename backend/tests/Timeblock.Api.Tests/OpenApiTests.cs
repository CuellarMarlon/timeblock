using System.Net;
using Microsoft.AspNetCore.Hosting;
using Timeblock.Api.Tests.Support;

namespace Timeblock.Api.Tests;

public sealed class OpenApiTests(TimeblockApiFactory factory)
    : IClassFixture<TimeblockApiFactory>
{
    [Fact]
    public async Task EnDesarrollo_PublicaElDocumentoOpenApiConLosEndpoints()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage respuesta =
            await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        string documento = await respuesta.Content.ReadAsStringAsync();
        Assert.Contains("Timeblock API", documento);
        Assert.Contains("/api/_tests/paginacion", documento);
        Assert.Contains("/api/programas", documento);
        Assert.Contains("/api/programas/{programaId}/asignaturas/{asignaturaId}", documento);
        Assert.Contains("/api/asignaturas", documento);
    }

    [Fact]
    public async Task FueraDeDesarrollo_NoPublicaElDocumentoOpenApi()
    {
        await using var produccion = factory.WithWebHostBuilder(
            builder => builder.UseEnvironment("Production"));
        using HttpClient client = produccion.CreateClient();

        using HttpResponseMessage respuesta =
            await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }
}
