using System.Net;
using System.Text.Json;
using Timeblock.Api.Tests.Support;

namespace Timeblock.Api.Tests;

public sealed class JsonConfigurationTests(TimeblockApiFactory factory)
    : IClassFixture<TimeblockApiFactory>
{
    [Fact]
    public async Task Respuesta_UsaCamelCaseEnumeracionesComoCadenasYFechasDelContrato()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage respuesta =
            await client.GetAsync("/api/_tests/json");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);

        string cuerpo = await respuesta.Content.ReadAsStringAsync();
        JsonElement json = JsonDocument.Parse(cuerpo).RootElement;

        Assert.Equal(
            "3f2504e0-4f89-41d3-9a0c-0305e82c3301",
            json.GetProperty("id").GetString());
        Assert.Equal("2026-10-12", json.GetProperty("fecha").GetString());
        Assert.Equal(
            "2026-10-12T19:30:00Z",
            json.GetProperty("creadoEn").GetString());
        Assert.Equal(
            "jefeDepartamento",
            json.GetProperty("tipoPerfil").GetString());
        Assert.Equal("teoria", json.GetProperty("tipoActividad").GetString());

        // Los acentos se envían tal cual, sin secuencias \uXXXX.
        Assert.Contains("Acción", cuerpo);
    }

    [Fact]
    public async Task Solicitud_AceptaEnumeracionesComoCadenas()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage respuesta = await client.PostAsync(
            "/api/_tests/enum",
            RespuestaExtensions.Json("""{"tipoPerfil": "coordinador"}"""));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal("coordinador", json.GetProperty("tipoPerfil").GetString());
    }

    [Theory]
    [InlineData("""{"tipoPerfil": 1}""")]
    [InlineData("""{"tipoPerfil": "desconocido"}""")]
    public async Task Solicitud_RechazaEnumeracionesNumericasOInexistentes(
        string cuerpo)
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage respuesta = await client.PostAsync(
            "/api/_tests/enum",
            RespuestaExtensions.Json(cuerpo));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }
}
