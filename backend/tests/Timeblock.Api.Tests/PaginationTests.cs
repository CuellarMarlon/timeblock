using System.Net;
using System.Text.Json;
using Timeblock.Api.Tests.Support;

namespace Timeblock.Api.Tests;

public sealed class PaginationTests(TimeblockApiFactory factory)
    : IClassFixture<TimeblockApiFactory>
{
    [Fact]
    public async Task SinParametros_UsaPaginaUnoYVeinteElementos()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage respuesta =
            await client.GetAsync("/api/_tests/paginacion");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();

        Assert.Equal(1, json.GetProperty("page").GetInt32());
        Assert.Equal(20, json.GetProperty("pageSize").GetInt32());
        Assert.Equal(250, json.GetProperty("totalItems").GetInt32());
        Assert.Equal(20, json.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Respuesta_TieneExactamenteLasPropiedadesDelContrato()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage respuesta =
            await client.GetAsync("/api/_tests/paginacion");

        JsonElement json = await respuesta.LeerJsonAsync();
        string[] propiedades = json.EnumerateObject()
            .Select(propiedad => propiedad.Name)
            .Order()
            .ToArray();

        Assert.Equal(new[] { "items", "page", "pageSize", "totalItems" }, propiedades);
    }

    [Fact]
    public async Task UltimaPagina_DevuelveLosElementosRestantes()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage respuesta = await client.GetAsync(
            "/api/_tests/paginacion?page=3&pageSize=100");

        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(3, json.GetProperty("page").GetInt32());
        Assert.Equal(100, json.GetProperty("pageSize").GetInt32());
        Assert.Equal(50, json.GetProperty("items").GetArrayLength());
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    public async Task ParametrosFueraDeRango_DevuelvenProblemDetails400(
        string consulta)
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage respuesta = await client.GetAsync(
            $"/api/_tests/paginacion?{consulta}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(
            "application/problem+json",
            respuesta.Content.Headers.ContentType?.MediaType);
    }
}
