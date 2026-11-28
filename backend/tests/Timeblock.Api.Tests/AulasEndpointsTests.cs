using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Timeblock.Api.Errors;
using Timeblock.Api.Tests.Support;
using Timeblock.Application.Aulas;
using Timeblock.Domain.Aulas;

namespace Timeblock.Api.Tests;

/// <summary>
/// Comprueba <c>docs/api/endpoints/aulas.md</c> con un repositorio en memoria.
/// </summary>
public sealed class AulasEndpointsTests : IClassFixture<TimeblockApiFactory>
{
    private readonly TimeblockApiFactory _factory;
    private readonly FakeAulaRepository _repositorio = new();

    public AulasEndpointsTests(TimeblockApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CrearCliente() =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.Replace(ServiceDescriptor.Scoped<IAulaRepository>(_ => _repositorio))))
            .CreateClient();

    private static StringContent Cuerpo(string json) => RespuestaExtensions.Json(json);

    private static void AssertProblema(HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        Assert.Equal(esperado, respuesta.StatusCode);
        Assert.Equal(
            "application/problem+json",
            respuesta.Content.Headers.ContentType?.MediaType);
    }

    // ---- POST /api/aulas ----

    [Fact]
    public async Task Crear_Valida_Devuelve201ConLocationYElAula()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/aulas",
            Cuerpo("""{"edificio": "  Edificio A ", "capacidad": 30}"""));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);

        JsonElement aula = await respuesta.LeerJsonAsync();
        Guid id = aula.GetProperty("id").GetGuid();
        Assert.NotEqual(Guid.Empty, id);
        Assert.Equal("Edificio A", aula.GetProperty("edificio").GetString());
        Assert.Equal(30, aula.GetProperty("capacidad").GetInt32());
        Assert.EndsWith($"/api/aulas/{id}", respuesta.Headers.Location?.ToString());
        Assert.True(_repositorio.Aulas.ContainsKey(id));
    }

    [Fact]
    public async Task Crear_LaRespuestaSoloTieneIdEdificioYCapacidad()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/aulas",
            Cuerpo("""{"edificio": "Edificio A", "capacidad": 30}"""));

        JsonElement aula = await respuesta.LeerJsonAsync();
        string[] propiedades = aula.EnumerateObject()
            .Select(propiedad => propiedad.Name)
            .Order()
            .ToArray();
        Assert.Equal(new[] { "capacidad", "edificio", "id" }, propiedades);
    }

    [Fact]
    public async Task Crear_DosAulasIguales_EsValido()
    {
        using HttpClient cliente = CrearCliente();
        const string cuerpo = """{"edificio": "Edificio A", "capacidad": 30}""";

        using HttpResponseMessage primera = await cliente.PostAsync("/api/aulas", Cuerpo(cuerpo));
        using HttpResponseMessage segunda = await cliente.PostAsync("/api/aulas", Cuerpo(cuerpo));

        Assert.Equal(HttpStatusCode.Created, primera.StatusCode);
        Assert.Equal(HttpStatusCode.Created, segunda.StatusCode);
        Assert.Equal(2, _repositorio.Aulas.Count);
    }

    [Fact]
    public async Task Crear_CapacidadCero_Devuelve400ConErroresPorCampo()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/aulas",
            Cuerpo("""{"edificio": "Edificio A", "capacidad": 0}"""));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
        JsonElement problema = await respuesta.LeerJsonAsync();
        Assert.Equal("about:blank", problema.GetProperty("type").GetString());
        Assert.Equal(ErroresHttp.TituloDe(400), problema.GetProperty("title").GetString());
        Assert.Equal(400, problema.GetProperty("status").GetInt32());
        Assert.Equal(ErroresHttp.DetalleValidacion, problema.GetProperty("detail").GetString());
        Assert.Equal(
            "La capacidad debe ser mayor que cero.",
            problema.GetProperty("errors").GetProperty("capacidad")[0].GetString());
        Assert.Empty(_repositorio.Aulas);
    }

    [Theory]
    [InlineData("""{"capacidad": 30}""", "edificio")]
    [InlineData("""{"edificio": null, "capacidad": 30}""", "edificio")]
    [InlineData("""{"edificio": "   ", "capacidad": 30}""", "edificio")]
    [InlineData("""{"edificio": "Edificio A"}""", "capacidad")]
    [InlineData("""{"edificio": "Edificio A", "capacidad": null}""", "capacidad")]
    [InlineData("""{"edificio": "Edificio A", "capacidad": -1}""", "capacidad")]
    [InlineData("""{"edificio": "Edificio A", "capacidad": 1001}""", "capacidad")]
    public async Task Crear_CampoAusenteOInvalido_ReportaEseCampoEnErrors(
        string json,
        string campo)
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/aulas", Cuerpo(json));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
        JsonElement errores = (await respuesta.LeerJsonAsync()).GetProperty("errors");
        Assert.True(errores.TryGetProperty(campo, out JsonElement mensajes));
        Assert.NotEmpty(mensajes.EnumerateArray());
        Assert.Empty(_repositorio.Aulas);
    }

    [Fact]
    public async Task Crear_EdificioMasLargoQueElMaximo_Devuelve400()
    {
        using HttpClient cliente = CrearCliente();
        string edificio = new('a', Aula.EdificioLongitudMaxima + 1);

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/aulas",
            Cuerpo($$"""{"edificio": "{{edificio}}", "capacidad": 30}"""));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
        JsonElement errores = (await respuesta.LeerJsonAsync()).GetProperty("errors");
        Assert.True(errores.TryGetProperty("edificio", out _));
    }

    [Fact]
    public async Task Crear_VariosCamposInvalidos_LosReportaTodos()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/aulas",
            Cuerpo("""{"edificio": "", "capacidad": 0}"""));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
        JsonElement errores = (await respuesta.LeerJsonAsync()).GetProperty("errors");
        Assert.True(errores.TryGetProperty("edificio", out _));
        Assert.True(errores.TryGetProperty("capacidad", out _));
    }

    [Theory]
    [InlineData("""{"edificio": "Edificio A", "capacidad": 30.5}""")]
    [InlineData("""{"edificio": "Edificio A", "capacidad": "30"}""")]
    [InlineData("""{"edificio": "Edificio A", "capacidad": true}""")]
    [InlineData("""{"edificio": 7, "capacidad": 30}""")]
    [InlineData("{")]
    public async Task Crear_TiposInvalidosOJsonMalFormado_Devuelve400(string json)
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/aulas", Cuerpo(json));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
        Assert.Empty(_repositorio.Aulas);
    }

    // ---- GET /api/aulas/{aulaId} ----

    [Fact]
    public async Task Obtener_Existente_Devuelve200ConElAula()
    {
        Aula aula = _repositorio.Sembrar("Edificio A", 30);
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.GetAsync($"/api/aulas/{aula.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(aula.Id, json.GetProperty("id").GetGuid());
        Assert.Equal("Edificio A", json.GetProperty("edificio").GetString());
        Assert.Equal(30, json.GetProperty("capacidad").GetInt32());
    }

    [Fact]
    public async Task Obtener_Inexistente_Devuelve404()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.GetAsync($"/api/aulas/{Guid.NewGuid()}");

        AssertProblema(respuesta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Obtener_IdentificadorConFormatoInvalido_Devuelve404()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.GetAsync("/api/aulas/no-es-un-uuid");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    [Fact]
    public async Task Obtener_UuidVacio_Devuelve400()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.GetAsync($"/api/aulas/{Guid.Empty}");

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
    }

    // ---- GET /api/aulas ----

    [Fact]
    public async Task Listar_SinAulas_DevuelvePaginaVaciaConLosValoresPorDefecto()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.GetAsync("/api/aulas");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Empty(json.GetProperty("items").EnumerateArray());
        Assert.Equal(1, json.GetProperty("page").GetInt32());
        Assert.Equal(20, json.GetProperty("pageSize").GetInt32());
        Assert.Equal(0, json.GetProperty("totalItems").GetInt32());
    }

    [Fact]
    public async Task Listar_OrdenaPorEdificioCapacidadEIdYPagina()
    {
        _repositorio.Sembrar("Edificio B", 20);
        _repositorio.Sembrar("Edificio A", 45);
        _repositorio.Sembrar("Edificio A", 30);
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage primera = await cliente.GetAsync("/api/aulas?page=1&pageSize=2");
        using HttpResponseMessage segunda = await cliente.GetAsync("/api/aulas?page=2&pageSize=2");

        JsonElement pagina1 = await primera.LeerJsonAsync();
        JsonElement pagina2 = await segunda.LeerJsonAsync();
        Assert.Equal(3, pagina1.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, pagina1.GetProperty("pageSize").GetInt32());

        string[] ordenPrimera = pagina1.GetProperty("items").EnumerateArray()
            .Select(aula => $"{aula.GetProperty("edificio").GetString()}:{aula.GetProperty("capacidad").GetInt32()}")
            .ToArray();
        Assert.Equal(new[] { "Edificio A:30", "Edificio A:45" }, ordenPrimera);

        JsonElement unica = Assert.Single(pagina2.GetProperty("items").EnumerateArray());
        Assert.Equal("Edificio B", unica.GetProperty("edificio").GetString());
        Assert.Equal(2, pagina2.GetProperty("page").GetInt32());
    }

    [Fact]
    public async Task Listar_FiltraPorEdificioSinDistinguirMayusculas()
    {
        _repositorio.Sembrar("Edificio Central", 30);
        _repositorio.Sembrar("Anexo", 30);
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.GetAsync("/api/aulas?edificio=CENTRAL");

        JsonElement json = await respuesta.LeerJsonAsync();
        JsonElement unica = Assert.Single(json.GetProperty("items").EnumerateArray());
        Assert.Equal("Edificio Central", unica.GetProperty("edificio").GetString());
        Assert.Equal(1, json.GetProperty("totalItems").GetInt32());
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("page=-1")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=abc")]
    public async Task Listar_PaginacionFueraDeRango_Devuelve400(string consulta)
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.GetAsync($"/api/aulas?{consulta}");

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
    }

    // ---- PUT /api/aulas/{aulaId} ----

    [Fact]
    public async Task Actualizar_Valida_Devuelve200ConElAulaActualizadaYElMismoId()
    {
        Aula aula = _repositorio.Sembrar("Edificio A", 30);
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PutAsync(
            $"/api/aulas/{aula.Id}",
            Cuerpo("""{"edificio": " Edificio B ", "capacidad": 45}"""));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(aula.Id, json.GetProperty("id").GetGuid());
        Assert.Equal("Edificio B", json.GetProperty("edificio").GetString());
        Assert.Equal(45, json.GetProperty("capacidad").GetInt32());
        Assert.Equal("Edificio B", _repositorio.Aulas[aula.Id].Edificio);
        Assert.Equal(45, _repositorio.Aulas[aula.Id].Capacidad);
    }

    [Fact]
    public async Task Actualizar_IgnoraUnIdEnElCuerpo()
    {
        Aula aula = _repositorio.Sembrar("Edificio A", 30);
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PutAsync(
            $"/api/aulas/{aula.Id}",
            Cuerpo($$"""{"id": "{{Guid.NewGuid()}}", "edificio": "Edificio B", "capacidad": 45}"""));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(aula.Id, (await respuesta.LeerJsonAsync()).GetProperty("id").GetGuid());
        Assert.Single(_repositorio.Aulas);
    }

    [Fact]
    public async Task Actualizar_Inexistente_Devuelve404()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PutAsync(
            $"/api/aulas/{Guid.NewGuid()}",
            Cuerpo("""{"edificio": "Edificio B", "capacidad": 45}"""));

        AssertProblema(respuesta, HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("""{"capacidad": 45}""", "edificio")]
    [InlineData("""{"edificio": "Edificio B"}""", "capacidad")]
    [InlineData("""{"edificio": "Edificio B", "capacidad": 0}""", "capacidad")]
    public async Task Actualizar_DatosInvalidos_Devuelve400YNoModificaElAula(
        string json,
        string campo)
    {
        Aula aula = _repositorio.Sembrar("Edificio A", 30);
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PutAsync(
            $"/api/aulas/{aula.Id}", Cuerpo(json));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
        JsonElement errores = (await respuesta.LeerJsonAsync()).GetProperty("errors");
        Assert.True(errores.TryGetProperty(campo, out _));
        Assert.Equal("Edificio A", _repositorio.Aulas[aula.Id].Edificio);
        Assert.Equal(30, _repositorio.Aulas[aula.Id].Capacidad);
    }

    [Fact]
    public async Task Actualizar_DatosInvalidosEnAulaInexistente_Devuelve400Antes404()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PutAsync(
            $"/api/aulas/{Guid.NewGuid()}",
            Cuerpo("""{"edificio": "", "capacidad": 45}"""));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Actualizar_UuidVacio_Devuelve400()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PutAsync(
            $"/api/aulas/{Guid.Empty}",
            Cuerpo("""{"edificio": "Edificio B", "capacidad": 45}"""));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
    }

    // ---- Fuera de alcance ----

    [Fact]
    public async Task NoExisteDelete()
    {
        Aula aula = _repositorio.Sembrar("Edificio A", 30);
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.DeleteAsync($"/api/aulas/{aula.Id}");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, respuesta.StatusCode);
        Assert.True(_repositorio.Aulas.ContainsKey(aula.Id));
    }
}
