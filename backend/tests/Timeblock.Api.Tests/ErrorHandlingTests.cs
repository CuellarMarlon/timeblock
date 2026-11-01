using System.Net;
using System.Text.Json;
using Timeblock.Api.Errors;
using Timeblock.Api.Tests.Support;
using Timeblock.Domain.Common;

namespace Timeblock.Api.Tests;

public sealed class ErrorHandlingTests(TimeblockApiFactory factory)
    : IClassFixture<TimeblockApiFactory>
{
    [Theory]
    [InlineData(TipoErrorDominio.Validacion, 400)]
    [InlineData(TipoErrorDominio.AutorizacionNegocio, 403)]
    [InlineData(TipoErrorDominio.NoEncontrado, 404)]
    [InlineData(TipoErrorDominio.Conflicto, 409)]
    public async Task DomainException_SeTraduceAlCodigoYFormatoDelContrato(
        TipoErrorDominio tipo,
        int status)
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage respuesta =
            await client.GetAsync($"/api/_tests/errores/{tipo}");

        Assert.Equal(status, (int)respuesta.StatusCode);
        Assert.Equal(
            "application/problem+json",
            respuesta.Content.Headers.ContentType?.MediaType);

        JsonElement problema = await respuesta.LeerJsonAsync();
        Assert.Equal(status, problema.GetProperty("status").GetInt32());
        Assert.Equal("about:blank", problema.GetProperty("type").GetString());
        Assert.Equal(
            ErroresHttp.TituloDe(status),
            problema.GetProperty("title").GetString());
        Assert.Equal(
            $"Mensaje de prueba ({tipo}).",
            problema.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ErrorInesperado_Devuelve500SinDetallesInternos()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage respuesta =
            await client.GetAsync("/api/_tests/inesperado");

        Assert.Equal(HttpStatusCode.InternalServerError, respuesta.StatusCode);
        Assert.Equal(
            "application/problem+json",
            respuesta.Content.Headers.ContentType?.MediaType);

        string cuerpo = await respuesta.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secreto-interno", cuerpo);
        Assert.DoesNotContain("InvalidOperationException", cuerpo);
        Assert.DoesNotContain("StackTrace", cuerpo, StringComparison.OrdinalIgnoreCase);

        JsonElement problema = JsonDocument.Parse(cuerpo).RootElement;
        Assert.Equal(500, problema.GetProperty("status").GetInt32());
        Assert.Equal(
            ErroresHttp.DetalleErrorInesperado,
            problema.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task RutaInexistente_DevuelveProblemDetails404()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage respuesta =
            await client.GetAsync("/api/_tests/no-existe");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal(
            "application/problem+json",
            respuesta.Content.Headers.ContentType?.MediaType);

        JsonElement problema = await respuesta.LeerJsonAsync();
        Assert.Equal("about:blank", problema.GetProperty("type").GetString());
        Assert.Equal(
            ErroresHttp.TituloDe(404),
            problema.GetProperty("title").GetString());
    }

    [Fact]
    public async Task ErrorDeValidacionDeModelo_IncluyeErroresPorCampoEnCamelCase()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage respuesta = await client.PostAsync(
            "/api/_tests/validacion",
            RespuestaExtensions.Json("""{"nombre": "Ana", "edad": 0}"""));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(
            "application/problem+json",
            respuesta.Content.Headers.ContentType?.MediaType);

        JsonElement problema = await respuesta.LeerJsonAsync();
        Assert.Equal("about:blank", problema.GetProperty("type").GetString());
        Assert.Equal(
            ErroresHttp.TituloDe(400),
            problema.GetProperty("title").GetString());
        Assert.Equal(
            ErroresHttp.DetalleValidacion,
            problema.GetProperty("detail").GetString());

        JsonElement errores = problema.GetProperty("errors");
        Assert.Equal(
            "La edad debe ser mayor que cero.",
            errores.GetProperty("edad")[0].GetString());
    }

    [Fact]
    public async Task JsonMalFormado_DevuelveProblemDetails400()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage respuesta = await client.PostAsync(
            "/api/_tests/validacion",
            RespuestaExtensions.Json("{"));

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal(
            "application/problem+json",
            respuesta.Content.Headers.ContentType?.MediaType);
    }
}
