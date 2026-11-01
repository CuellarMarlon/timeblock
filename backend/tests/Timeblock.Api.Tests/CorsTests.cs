using System.Net;
using Timeblock.Api.Tests.Support;

namespace Timeblock.Api.Tests;

public sealed class CorsTests(TimeblockApiFactory factory)
    : IClassFixture<TimeblockApiFactory>
{
    private const string OrigenFrontend = "http://localhost:5173";

    [Fact]
    public async Task Preflight_DesdeElFrontendLocal_EsPermitido()
    {
        using HttpClient client = factory.CreateClient();
        using var solicitud = new HttpRequestMessage(
            HttpMethod.Options,
            "/api/_tests/creado");
        solicitud.Headers.Add("Origin", OrigenFrontend);
        solicitud.Headers.Add("Access-Control-Request-Method", "POST");
        solicitud.Headers.Add("Access-Control-Request-Headers", "content-type");

        using HttpResponseMessage respuesta = await client.SendAsync(solicitud);

        Assert.Equal(HttpStatusCode.NoContent, respuesta.StatusCode);
        Assert.Equal(OrigenFrontend, ValorDe(respuesta, "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Preflight_DesdeOtroOrigen_NoSeAutoriza()
    {
        using HttpClient client = factory.CreateClient();
        using var solicitud = new HttpRequestMessage(
            HttpMethod.Options,
            "/api/_tests/creado");
        solicitud.Headers.Add("Origin", "http://otro-sitio.example");
        solicitud.Headers.Add("Access-Control-Request-Method", "POST");

        using HttpResponseMessage respuesta = await client.SendAsync(solicitud);

        Assert.Null(ValorDe(respuesta, "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task RespuestaCreated_ExponeLaCabeceraLocationAlFrontend()
    {
        using HttpClient client = factory.CreateClient();
        using var solicitud = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/_tests/creado");
        solicitud.Headers.Add("Origin", OrigenFrontend);

        using HttpResponseMessage respuesta = await client.SendAsync(solicitud);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.NotNull(respuesta.Headers.Location);
        Assert.Contains(
            "Location",
            ValorDe(respuesta, "Access-Control-Expose-Headers") ?? string.Empty);
    }

    private static string? ValorDe(HttpResponseMessage respuesta, string cabecera) =>
        respuesta.Headers.TryGetValues(cabecera, out IEnumerable<string>? valores)
            ? string.Join(",", valores)
            : null;
}
