using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Timeblock.Api.Tests.Support;
using Timeblock.Application.Bloques;
using Timeblock.Application.Tests.Bloques;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;

namespace Timeblock.Api.Tests;

public sealed class BloquesEndpointsTests : IClassFixture<TimeblockApiFactory>
{
    private const string Descripcion = "Introducción a las bases de datos";

    private readonly TimeblockApiFactory _factory;
    private readonly FakeBloqueRepository _repositorio = new();
    private readonly Guid _asignatura;
    private readonly Guid _coordinador;
    private readonly ProgramaAcademico _programa;

    public BloquesEndpointsTests(TimeblockApiFactory factory)
    {
        _factory = factory;
        _asignatura = _repositorio.AgregarAsignatura();
        _coordinador = _repositorio.AgregarCoordinador();
        _programa = _repositorio.AgregarPrograma(_asignatura);
    }

    private HttpClient CrearCliente() =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
                services.Replace(ServiceDescriptor.Scoped<IBloqueRepository>(_ => _repositorio))))
            .CreateClient();

    private string CuerpoCrear(
        string descripcion = Descripcion,
        string tipoActividad = "teoria",
        string nivelComplejidad = "medio",
        object? asignaturaId = null,
        object? coordinadorId = null,
        Guid[]? programasIds = null) =>
        JsonSerializer.Serialize(new
        {
            descripcion,
            tipoActividad,
            nivelComplejidad,
            asignaturaId = asignaturaId ?? _asignatura,
            coordinadorId = coordinadorId ?? _coordinador,
            programasIds = programasIds ?? [],
        });

    private BloqueContenido Sembrar(
        string descripcion = Descripcion,
        TipoActividad tipo = TipoActividad.Teoria,
        NivelComplejidad nivel = NivelComplejidad.Medio,
        Guid? asignaturaId = null,
        Guid? coordinadorId = null,
        params ProgramaAcademico[] programas)
    {
        BloqueContenido bloque = BloqueContenido.Crear(
            descripcion,
            tipo,
            nivel,
            asignaturaId ?? _asignatura,
            coordinadorId ?? _coordinador);

        foreach (ProgramaAcademico programa in programas)
        {
            bloque.AsociarPrograma(programa);
        }

        _repositorio.Bloques.Add(bloque);
        return bloque;
    }

    private static void AssertProblema(HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        Assert.Equal(esperado, respuesta.StatusCode);
        Assert.Equal(
            "application/problem+json",
            respuesta.Content.Headers.ContentType?.MediaType);
    }

    private static Task<HttpResponseMessage> Put(HttpClient cliente, string ruta, string cuerpo) =>
        cliente.PutAsync(ruta, RespuestaExtensions.Json(cuerpo));

    // ---------------------------------------------------------------- Crear

    [Fact]
    public async Task Crear_Valido_Responde201ConLocationYCuerpoDelContrato()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/bloques",
            RespuestaExtensions.Json(CuerpoCrear(programasIds: [_programa.Id])));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Guid id = json.GetProperty("id").GetGuid();
        Assert.EndsWith($"/api/bloques/{id}", respuesta.Headers.Location!.ToString(),
            StringComparison.OrdinalIgnoreCase);
        Assert.Equal(
            new[]
            {
                "asignaturaId", "coordinadorId", "descripcion", "id",
                "nivelComplejidad", "programasIds", "tipoActividad",
            },
            json.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(Descripcion, json.GetProperty("descripcion").GetString());
        Assert.Equal("teoria", json.GetProperty("tipoActividad").GetString());
        Assert.Equal("medio", json.GetProperty("nivelComplejidad").GetString());
        Assert.Equal(_asignatura, json.GetProperty("asignaturaId").GetGuid());
        Assert.Equal(_coordinador, json.GetProperty("coordinadorId").GetGuid());
        Assert.Equal(_programa.Id, Assert.Single(json.GetProperty("programasIds").EnumerateArray()).GetGuid());
        Assert.Single(_repositorio.Bloques);
    }

    [Fact]
    public async Task Crear_SinProgramas_Responde201ConListaVacia()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/bloques", RespuestaExtensions.Json(CuerpoCrear()));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(0, json.GetProperty("programasIds").GetArrayLength());
    }

    [Fact]
    public async Task Crear_SinCamposObligatorios_Responde400()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/bloques", RespuestaExtensions.Json("{}"));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
        Assert.Empty(_repositorio.Bloques);
    }

    [Theory]
    [InlineData("seminario", "medio")]
    [InlineData("teoria", "extremo")]
    [InlineData("TEORICA", "medio")]
    public async Task Crear_ConEnumeracionInvalida_Responde400(string tipo, string nivel)
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/bloques",
            RespuestaExtensions.Json(CuerpoCrear(tipoActividad: tipo, nivelComplejidad: nivel)));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
        Assert.Empty(_repositorio.Bloques);
    }

    [Fact]
    public async Task Crear_ConEnumeracionNumerica_Responde400()
    {
        using HttpClient cliente = CrearCliente();
        string cuerpo = CuerpoCrear().Replace("\"teoria\"", "0");

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/bloques", RespuestaExtensions.Json(cuerpo));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Crear_DescripcionVacia_Responde400(string descripcion)
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/bloques", RespuestaExtensions.Json(CuerpoCrear(descripcion: descripcion)));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
        Assert.Empty(_repositorio.Bloques);
    }

    [Fact]
    public async Task Crear_ConIdentificadorMalFormado_Responde400()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/bloques", RespuestaExtensions.Json(CuerpoCrear(asignaturaId: "no-es-uuid")));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Crear_AsignaturaInexistente_Responde404()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/bloques", RespuestaExtensions.Json(CuerpoCrear(asignaturaId: Guid.NewGuid())));

        AssertProblema(respuesta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Crear_CoordinadorInexistente_Responde404()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/bloques", RespuestaExtensions.Json(CuerpoCrear(coordinadorId: Guid.NewGuid())));

        AssertProblema(respuesta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Crear_ProgramaInexistente_Responde404()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/bloques",
            RespuestaExtensions.Json(CuerpoCrear(programasIds: [Guid.NewGuid()])));

        AssertProblema(respuesta, HttpStatusCode.NotFound);
        Assert.Empty(_repositorio.Bloques);
    }

    [Fact]
    public async Task Crear_ConProgramaQueNoIncluyeLaAsignatura_Responde400()
    {
        ProgramaAcademico otroPrograma = _repositorio.AgregarPrograma(Guid.NewGuid());
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/bloques",
            RespuestaExtensions.Json(CuerpoCrear(programasIds: [otroPrograma.Id])));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
        Assert.Empty(_repositorio.Bloques);
    }

    [Theory]
    [InlineData("Introducción a las bases de datos")]
    [InlineData("  INTRODUCCIÓN   a las  bases de datos ")]
    public async Task Crear_Duplicado_Responde409(string descripcion)
    {
        Sembrar();
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/bloques", RespuestaExtensions.Json(CuerpoCrear(descripcion: descripcion)));

        AssertProblema(respuesta, HttpStatusCode.Conflict);
        Assert.Single(_repositorio.Bloques);
    }

    [Fact]
    public async Task Crear_DescripcionQueSoloDifiereEnLosAcentos_Responde201()
    {
        Sembrar();
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PostAsync(
            "/api/bloques",
            RespuestaExtensions.Json(CuerpoCrear(descripcion: "Introduccion a las bases de datos")));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        Assert.Equal(2, _repositorio.Bloques.Count);
    }

    // ------------------------------------------------------------- Obtener

    [Fact]
    public async Task Obtener_Existente_Responde200ConElBloqueYSusProgramas()
    {
        BloqueContenido bloque = Sembrar(programas: _programa);
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.GetAsync($"/api/bloques/{bloque.Id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(bloque.Id, json.GetProperty("id").GetGuid());
        Assert.Equal(_programa.Id, Assert.Single(json.GetProperty("programasIds").EnumerateArray()).GetGuid());
    }

    [Fact]
    public async Task Obtener_Inexistente_Responde404()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.GetAsync($"/api/bloques/{Guid.NewGuid()}");

        AssertProblema(respuesta, HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------- Listar

    [Fact]
    public async Task Listar_UsaLaPaginacionComun()
    {
        Sembrar("Bloque 1");
        Sembrar("Bloque 2");
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.GetAsync("/api/bloques");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(
            new[] { "items", "page", "pageSize", "totalItems" },
            json.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(1, json.GetProperty("page").GetInt32());
        Assert.Equal(20, json.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, json.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, json.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Listar_FiltraPorCadaParametroDelContrato()
    {
        Guid otraAsignatura = _repositorio.AgregarAsignatura();
        ProgramaAcademico otroPrograma = _repositorio.AgregarPrograma(otraAsignatura);
        Guid otroCoordinador = _repositorio.AgregarCoordinador();

        BloqueContenido teoria = Sembrar(
            "Teoría A", TipoActividad.Teoria, NivelComplejidad.Bajo, programas: _programa);
        BloqueContenido laboratorio = Sembrar(
            "Laboratorio B", TipoActividad.Laboratorio, NivelComplejidad.Alto,
            otraAsignatura, otroCoordinador, otroPrograma);
        using HttpClient cliente = CrearCliente();

        await AssertSoloAsync(cliente, $"programaId={_programa.Id}", teoria);
        await AssertSoloAsync(cliente, $"asignaturaId={otraAsignatura}", laboratorio);
        await AssertSoloAsync(cliente, $"coordinadorId={otroCoordinador}", laboratorio);
        await AssertSoloAsync(cliente, "tipoActividad=teoria", teoria);
        await AssertSoloAsync(cliente, "nivelComplejidad=alto", laboratorio);
        await AssertSoloAsync(cliente, "tipoActividad=laboratorio&nivelComplejidad=alto", laboratorio);
    }

    private static async Task AssertSoloAsync(HttpClient cliente, string consulta, BloqueContenido esperado)
    {
        using HttpResponseMessage respuesta = await cliente.GetAsync($"/api/bloques?{consulta}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(1, json.GetProperty("totalItems").GetInt32());
        Assert.Equal(esperado.Id, Assert.Single(json.GetProperty("items").EnumerateArray())
            .GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Listar_Paginado_DevuelveLaPaginaSolicitada()
    {
        Sembrar("Bloque 1");
        Sembrar("Bloque 2");
        Sembrar("Bloque 3");
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.GetAsync("/api/bloques?page=2&pageSize=2");

        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(2, json.GetProperty("page").GetInt32());
        Assert.Equal(2, json.GetProperty("pageSize").GetInt32());
        Assert.Equal(3, json.GetProperty("totalItems").GetInt32());
        Assert.Equal(
            "Bloque 3",
            Assert.Single(json.GetProperty("items").EnumerateArray()).GetProperty("descripcion").GetString());
    }

    [Theory]
    [InlineData("tipoActividad=seminario")]
    [InlineData("tipoActividad=99")]
    [InlineData("nivelComplejidad=extremo")]
    [InlineData("programaId=no-es-uuid")]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    public async Task Listar_ConParametrosInvalidos_Responde400(string consulta)
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.GetAsync($"/api/bloques?{consulta}");

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
    }

    // ---------------------------------------------------------- Actualizar

    [Fact]
    public async Task Actualizar_Valido_Responde200ConElBloqueActualizado()
    {
        BloqueContenido bloque = Sembrar(programas: _programa);
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Put(
            cliente,
            $"/api/bloques/{bloque.Id}",
            """{"descripcion":"Modelo relacional","tipoActividad":"practica","nivelComplejidad":"alto"}""");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal("Modelo relacional", json.GetProperty("descripcion").GetString());
        Assert.Equal("practica", json.GetProperty("tipoActividad").GetString());
        Assert.Equal("alto", json.GetProperty("nivelComplejidad").GetString());
        Assert.Equal(_programa.Id, Assert.Single(json.GetProperty("programasIds").EnumerateArray()).GetGuid());
    }

    [Fact]
    public async Task Actualizar_SoloLaDescripcion_ConservaElResto()
    {
        BloqueContenido bloque = Sembrar(tipo: TipoActividad.Laboratorio, nivel: NivelComplejidad.Bajo);
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Put(
            cliente, $"/api/bloques/{bloque.Id}", """{"descripcion":"Consultas SQL"}""");

        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal("Consultas SQL", json.GetProperty("descripcion").GetString());
        Assert.Equal("laboratorio", json.GetProperty("tipoActividad").GetString());
        Assert.Equal("bajo", json.GetProperty("nivelComplejidad").GetString());
    }

    [Fact]
    public async Task Actualizar_CambiandoLaAsignatura_Responde200()
    {
        Guid nueva = _repositorio.AgregarAsignatura();
        _programa.AsociarAsignatura(nueva);
        BloqueContenido bloque = Sembrar(programas: _programa);
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Put(
            cliente, $"/api/bloques/{bloque.Id}", $$"""{"asignaturaId":"{{nueva}}"}""");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(nueva, json.GetProperty("asignaturaId").GetGuid());
    }

    [Fact]
    public async Task Actualizar_AsignaturaQueUnProgramaNoIncluye_Responde400()
    {
        Guid nueva = _repositorio.AgregarAsignatura();
        BloqueContenido bloque = Sembrar(programas: _programa);
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Put(
            cliente, $"/api/bloques/{bloque.Id}", $$"""{"asignaturaId":"{{nueva}}"}""");

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
        Assert.Equal(_asignatura, bloque.AsignaturaId);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"descripcion":"   "}""")]
    [InlineData("""{"tipoActividad":"seminario"}""")]
    [InlineData("""{"nivelComplejidad":2}""")]
    public async Task Actualizar_ConDatosInvalidos_Responde400(string cuerpo)
    {
        BloqueContenido bloque = Sembrar();
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Put(cliente, $"/api/bloques/{bloque.Id}", cuerpo);

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
        Assert.Equal(Descripcion, bloque.Descripcion);
    }

    [Fact]
    public async Task Actualizar_BloqueInexistente_Responde404()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Put(
            cliente, $"/api/bloques/{Guid.NewGuid()}", """{"descripcion":"Otra"}""");

        AssertProblema(respuesta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Actualizar_QueGeneraUnDuplicado_Responde409()
    {
        Sembrar("Modelo relacional");
        BloqueContenido otro = Sembrar("Normalización");
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Put(
            cliente, $"/api/bloques/{otro.Id}", """{"descripcion":"  MODELO relacional"}""");

        AssertProblema(respuesta, HttpStatusCode.Conflict);
    }

    // ------------------------------------------------------ AsociarPrograma

    [Fact]
    public async Task AsociarPrograma_Responde204YEsIdempotente()
    {
        BloqueContenido bloque = Sembrar();
        using HttpClient cliente = CrearCliente();
        string ruta = $"/api/bloques/{bloque.Id}/programas/{_programa.Id}";

        using HttpResponseMessage primera = await cliente.PutAsync(ruta, null);
        using HttpResponseMessage repetida = await cliente.PutAsync(ruta, null);

        Assert.Equal(HttpStatusCode.NoContent, primera.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, repetida.StatusCode);
        Assert.Equal(0, (await repetida.Content.ReadAsStringAsync()).Length);

        using HttpResponseMessage consulta = await cliente.GetAsync($"/api/bloques/{bloque.Id}");
        JsonElement json = await consulta.LeerJsonAsync();
        Assert.Equal(_programa.Id, Assert.Single(json.GetProperty("programasIds").EnumerateArray()).GetGuid());
    }

    [Fact]
    public async Task AsociarPrograma_QueNoIncluyeLaAsignatura_Responde400()
    {
        ProgramaAcademico otroPrograma = _repositorio.AgregarPrograma(Guid.NewGuid());
        BloqueContenido bloque = Sembrar();
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PutAsync(
            $"/api/bloques/{bloque.Id}/programas/{otroPrograma.Id}", null);

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
        Assert.Empty(bloque.Programas);
    }

    [Fact]
    public async Task AsociarPrograma_ProgramaInexistente_Responde404()
    {
        BloqueContenido bloque = Sembrar();
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PutAsync(
            $"/api/bloques/{bloque.Id}/programas/{Guid.NewGuid()}", null);

        AssertProblema(respuesta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AsociarPrograma_BloqueInexistente_Responde404()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.PutAsync(
            $"/api/bloques/{Guid.NewGuid()}/programas/{_programa.Id}", null);

        AssertProblema(respuesta, HttpStatusCode.NotFound);
    }
}
