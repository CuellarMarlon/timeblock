using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Timeblock.Api.Tests.Support;
using Timeblock.Application.Horarios;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Personas;

namespace Timeblock.Api.Tests;

public sealed class HorariosEndpointsTests : IClassFixture<TimeblockApiFactory>
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 12, 14, 30, 0, TimeSpan.Zero);

    private readonly TimeblockApiFactory _factory;
    private readonly FakeHorarioRepository _repositorio = new();
    private readonly ProgramaAcademico _programa;
    private readonly Guid _coordinador = Guid.NewGuid();
    private readonly Guid _asignatura = Guid.NewGuid();
    private readonly BloqueContenido _teoria;
    private readonly BloqueContenido _practica;

    public HorariosEndpointsTests(TimeblockApiFactory factory)
    {
        _factory = factory;
        _programa = ProgramaAcademico.Crear(Guid.NewGuid(), "Informática", "Plan 2026");
        _programa.AsociarAsignatura(_asignatura);
        _repositorio.Programas.Add(_programa.Id);
        _repositorio.Coordinadores.Add(_coordinador);
        _repositorio.Autorizaciones.Add(new AutorizacionCoordinador(
            _coordinador, _programa.Id, new DateOnly(2026, 9, 7), null, true, false));

        _teoria = Sembrar("Introducción a las bases de datos", TipoActividad.Teoria);
        _practica = Sembrar("Consultas y operaciones SQL", TipoActividad.Practica);
    }

    private HttpClient CrearCliente(DateTimeOffset? ahora = null) =>
        _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
                services.Replace(ServiceDescriptor.Scoped<IHorarioRepository>(_ => _repositorio));
                services.Replace(ServiceDescriptor.Singleton<TimeProvider>(new RelojFijo(ahora ?? Ahora)));
            }))
            .CreateClient();

    private BloqueContenido Sembrar(
        string descripcion,
        TipoActividad tipo,
        bool asociarAlPrograma = true)
    {
        BloqueContenido bloque = BloqueContenido.Crear(
            descripcion, tipo, NivelComplejidad.Medio, _asignatura, _coordinador);
        if (asociarAlPrograma)
        {
            bloque.AsociarPrograma(_programa);
        }

        _repositorio.Bloques.Add(bloque.Id, bloque);
        return bloque;
    }

    private static object Bloque(BloqueContenido bloque, string fecha) =>
        new { bloqueId = bloque.Id, fecha };

    private string Cuerpo(
        object[]? bloques = null,
        string periodoAcademico = "2026-2027",
        object? programaId = null,
        object? coordinadorId = null) =>
        JsonSerializer.Serialize(new
        {
            programaId = programaId ?? _programa.Id,
            periodoAcademico,
            coordinadorId = coordinadorId ?? _coordinador,
            bloques = bloques ?? [Bloque(_teoria, "2026-10-20"), Bloque(_practica, "2026-10-22")],
        });

    private Task<HttpResponseMessage> Post(HttpClient cliente, string cuerpo) =>
        cliente.PostAsync("/api/horarios", RespuestaExtensions.Json(cuerpo));

    private static void AssertProblema(HttpResponseMessage respuesta, HttpStatusCode esperado)
    {
        Assert.Equal(esperado, respuesta.StatusCode);
        Assert.Equal(
            "application/problem+json",
            respuesta.Content.Headers.ContentType?.MediaType);
    }

    // ---------------------------------------------------------------- Crear

    [Fact]
    public async Task Crear_Valido_Responde201ConLaFormaDelContrato()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Post(cliente, Cuerpo());

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Guid id = json.GetProperty("id").GetGuid();
        Assert.EndsWith($"/api/horarios/{id}", respuesta.Headers.Location!.ToString(),
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            new[]
            {
                "bloques", "cantTotalBloques", "coordinadorId", "estado", "fechaCreacion",
                "fechaInicioAutorizacion", "id", "periodoAcademico", "programaId",
            },
            json.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(_programa.Id, json.GetProperty("programaId").GetGuid());
        Assert.Equal("2026-2027", json.GetProperty("periodoAcademico").GetString());
        Assert.Equal(_coordinador, json.GetProperty("coordinadorId").GetGuid());
        Assert.Equal("2026-09-07", json.GetProperty("fechaInicioAutorizacion").GetString());
        Assert.Equal("2026-10-12T14:30:00Z", json.GetProperty("fechaCreacion").GetString());
        Assert.Equal("generado", json.GetProperty("estado").GetString());
        Assert.Equal(2, json.GetProperty("cantTotalBloques").GetInt32());

        JsonElement primero = json.GetProperty("bloques")[0];
        Assert.Equal(
            new[]
            {
                "asignaturaId", "aulaId", "bloqueId", "descripcion", "fecha", "horaFin",
                "horaInicio", "nivelComplejidad", "periodoImp", "tipoActividad",
            },
            primero.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(_teoria.Id, primero.GetProperty("bloqueId").GetGuid());
        Assert.Equal("Introducción a las bases de datos", primero.GetProperty("descripcion").GetString());
        Assert.Equal(_asignatura, primero.GetProperty("asignaturaId").GetGuid());
        Assert.Equal("teoria", primero.GetProperty("tipoActividad").GetString());
        Assert.Equal("medio", primero.GetProperty("nivelComplejidad").GetString());
        Assert.Equal("2026-10-20", primero.GetProperty("fecha").GetString());
        foreach (string campo in new[] { "horaInicio", "horaFin", "periodoImp", "aulaId" })
        {
            Assert.Equal(JsonValueKind.Null, primero.GetProperty(campo).ValueKind);
        }

        Assert.Single(_repositorio.Horarios);
    }

    [Fact]
    public async Task Crear_IgnoraElEstadoQueEnvieElCliente()
    {
        using HttpClient cliente = CrearCliente();
        string cuerpo = Cuerpo().Replace("{", "{\"estado\":\"aprobado\",", StringComparison.Ordinal);

        using HttpResponseMessage respuesta = await Post(cliente, cuerpo);

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal("generado", json.GetProperty("estado").GetString());
    }

    [Fact]
    public async Task Crear_MismoBloqueEnFechasDistintas_Responde201YCuentaUnaVez()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Post(cliente, Cuerpo(
            [Bloque(_teoria, "2026-10-20"), Bloque(_teoria, "2026-10-22")]));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(1, json.GetProperty("cantTotalBloques").GetInt32());
        Assert.Equal(2, json.GetProperty("bloques").GetArrayLength());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"programaId":"3f2504e0-4f89-41d3-9a0c-0305e82c3301"}""")]
    public async Task Crear_SinCamposObligatorios_Responde400(string cuerpo)
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Post(cliente, cuerpo);

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
        Assert.Empty(_repositorio.Horarios);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Crear_PeriodoVacio_Responde400(string periodo)
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Post(cliente, Cuerpo(periodoAcademico: periodo));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
        Assert.Empty(_repositorio.Horarios);
    }

    [Fact]
    public async Task Crear_ListaDeBloquesVacia_Responde400()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Post(cliente, Cuerpo(bloques: []));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData("""[{"bloqueId":"3f2504e0-4f89-41d3-9a0c-0305e82c3301"}]""")]
    [InlineData("""[{"bloqueId":"3f2504e0-4f89-41d3-9a0c-0305e82c3301","fecha":"2026-13-40"}]""")]
    [InlineData("""[{"bloqueId":"no-es-uuid","fecha":"2026-10-20"}]""")]
    [InlineData("""[{"fecha":"2026-10-20"}]""")]
    public async Task Crear_BloqueConDatosInvalidos_Responde400(string bloques)
    {
        using HttpClient cliente = CrearCliente();
        string cuerpo = $$"""
            {"programaId":"{{_programa.Id}}","periodoAcademico":"2026-2027",
             "coordinadorId":"{{_coordinador}}","bloques":{{bloques}}}
            """;

        using HttpResponseMessage respuesta = await Post(cliente, cuerpo);

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Crear_ConIdentificadorMalFormado_Responde400()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Post(cliente, Cuerpo(programaId: "no-es-uuid"));

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Crear_ProgramaInexistente_Responde404()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Post(cliente, Cuerpo(programaId: Guid.NewGuid()));

        AssertProblema(respuesta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Crear_CoordinadorInexistente_Responde404()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Post(cliente, Cuerpo(coordinadorId: Guid.NewGuid()));

        AssertProblema(respuesta, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Crear_BloqueInexistente_Responde404YNoGuardaNada()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Post(cliente, Cuerpo(
            [Bloque(_teoria, "2026-10-20"), new { bloqueId = Guid.NewGuid(), fecha = "2026-10-22" }]));

        AssertProblema(respuesta, HttpStatusCode.NotFound);
        Assert.Empty(_repositorio.Horarios);
    }

    [Theory]
    [InlineData("vencida")]
    [InlineData("sinPermiso")]
    [InlineData("ninguna")]
    public async Task Crear_SinAutorizacionVigenteConPermiso_Responde403(string caso)
    {
        _repositorio.Autorizaciones.Clear();
        if (caso != "ninguna")
        {
            _repositorio.Autorizaciones.Add(new AutorizacionCoordinador(
                _coordinador,
                _programa.Id,
                new DateOnly(2026, 1, 1),
                caso == "vencida" ? new DateOnly(2026, 6, 30) : null,
                puedeGenerar: caso != "sinPermiso",
                puedeValidar: true));
        }

        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Post(cliente, Cuerpo());

        AssertProblema(respuesta, HttpStatusCode.Forbidden);
        Assert.Empty(_repositorio.Horarios);
    }

    [Fact]
    public async Task Crear_BloqueNoAsociadoAlPrograma_Responde409YNoGuardaNada()
    {
        BloqueContenido ajeno = Sembrar("Bloque de otro programa", TipoActividad.Laboratorio, asociarAlPrograma: false);
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Post(cliente, Cuerpo(
            [Bloque(_teoria, "2026-10-20"), Bloque(ajeno, "2026-10-22")]));

        AssertProblema(respuesta, HttpStatusCode.Conflict);
        Assert.Empty(_repositorio.Horarios);
    }

    [Fact]
    public async Task Crear_MismoBloqueYFechaRepetidos_Responde409YNoGuardaNada()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await Post(cliente, Cuerpo(
            [Bloque(_teoria, "2026-10-20"), Bloque(_teoria, "2026-10-20")]));

        AssertProblema(respuesta, HttpStatusCode.Conflict);
        Assert.Empty(_repositorio.Horarios);
    }

    // ------------------------------------------------------------- Obtener

    [Fact]
    public async Task Obtener_Existente_Responde200ConLosBloques()
    {
        using HttpClient cliente = CrearCliente();
        using HttpResponseMessage creado = await Post(cliente, Cuerpo());
        Guid id = (await creado.LeerJsonAsync()).GetProperty("id").GetGuid();

        using HttpResponseMessage respuesta = await cliente.GetAsync($"/api/horarios/{id}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(id, json.GetProperty("id").GetGuid());
        Assert.Equal(2, json.GetProperty("bloques").GetArrayLength());
    }

    [Fact]
    public async Task Crear_RelojConFraccionesDe100Ns_DevuelveLaFechaEnMicrosegundosAlCrearYConsultar()
    {
        // PostgreSQL conserva microsegundos: .9271507 se guarda como .927150.
        using HttpClient cliente = CrearCliente(Ahora.AddTicks(9_271_507));
        using HttpResponseMessage creado = await Post(cliente, Cuerpo());
        JsonElement json = await creado.LeerJsonAsync();
        Guid id = json.GetProperty("id").GetGuid();

        using HttpResponseMessage consultado = await cliente.GetAsync($"/api/horarios/{id}");
        JsonElement leido = await consultado.LeerJsonAsync();

        DateTimeOffset esperado = Ahora.AddTicks(9_271_500);
        Assert.Equal(esperado, json.GetProperty("fechaCreacion").GetDateTimeOffset());
        Assert.Equal(esperado, leido.GetProperty("fechaCreacion").GetDateTimeOffset());
    }

    [Fact]
    public async Task Obtener_Inexistente_Responde404()
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.GetAsync($"/api/horarios/{Guid.NewGuid()}");

        AssertProblema(respuesta, HttpStatusCode.NotFound);
    }

    // -------------------------------------------------------------- Listar

    [Fact]
    public async Task Listar_UsaLaPaginacionComunEIncluyeLosBloques()
    {
        using HttpClient cliente = CrearCliente();
        (await Post(cliente, Cuerpo())).Dispose();
        (await Post(cliente, Cuerpo(periodoAcademico: "2027-2028"))).Dispose();

        using HttpResponseMessage respuesta = await cliente.GetAsync("/api/horarios");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(
            new[] { "items", "page", "pageSize", "totalItems" },
            json.EnumerateObject().Select(p => p.Name).Order().ToArray());
        Assert.Equal(1, json.GetProperty("page").GetInt32());
        Assert.Equal(20, json.GetProperty("pageSize").GetInt32());
        Assert.Equal(2, json.GetProperty("totalItems").GetInt32());
        Assert.Equal(2, json.GetProperty("items")[0].GetProperty("bloques").GetArrayLength());
    }

    [Fact]
    public async Task Listar_FiltraPorProgramaPeriodoYEstado()
    {
        using HttpClient cliente = CrearCliente();
        (await Post(cliente, Cuerpo(periodoAcademico: "2026-2027"))).Dispose();
        (await Post(cliente, Cuerpo(periodoAcademico: "2027-2028"))).Dispose();

        await AssertTotalAsync(cliente, $"programaId={_programa.Id}", 2);
        await AssertTotalAsync(cliente, $"programaId={Guid.NewGuid()}", 0);
        await AssertTotalAsync(cliente, "periodoAcademico=2027-2028", 1);
        await AssertTotalAsync(cliente, "periodoAcademico=2030-2031", 0);
        await AssertTotalAsync(cliente, "estado=generado", 2);
        await AssertTotalAsync(cliente, $"programaId={_programa.Id}&periodoAcademico=2026-2027&estado=generado", 1);
    }

    private static async Task AssertTotalAsync(HttpClient cliente, string consulta, int esperado)
    {
        using HttpResponseMessage respuesta = await cliente.GetAsync($"/api/horarios?{consulta}");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal(esperado, json.GetProperty("totalItems").GetInt32());
    }

    [Theory]
    [InlineData("estado=aprobado")]
    [InlineData("estado=99")]
    [InlineData("programaId=no-es-uuid")]
    [InlineData("page=0")]
    [InlineData("pageSize=101")]
    public async Task Listar_ConParametrosInvalidos_Responde400(string consulta)
    {
        using HttpClient cliente = CrearCliente();

        using HttpResponseMessage respuesta = await cliente.GetAsync($"/api/horarios?{consulta}");

        AssertProblema(respuesta, HttpStatusCode.BadRequest);
    }
}
