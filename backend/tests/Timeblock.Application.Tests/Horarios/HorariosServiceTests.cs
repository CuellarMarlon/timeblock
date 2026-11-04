using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Application.Horarios;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Common;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Personas;

namespace Timeblock.Application.Tests.Horarios;

public sealed class HorariosServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 12, 14, 30, 0, TimeSpan.Zero);
    private static readonly DateOnly Hoy = new(2026, 10, 12);
    private static readonly DateOnly InicioAutorizacion = new(2026, 9, 7);
    private static readonly DateOnly Fecha1 = new(2026, 10, 20);
    private static readonly DateOnly Fecha2 = new(2026, 10, 22);

    private readonly FakeHorarioRepository _repositorio = new();
    private readonly HorariosService _servicio;
    private readonly ProgramaAcademico _programa;
    private readonly Guid _coordinador = Guid.NewGuid();
    private readonly Guid _asignatura = Guid.NewGuid();
    private readonly BloqueContenido _teoria;
    private readonly BloqueContenido _practica;

    public HorariosServiceTests()
    {
        _servicio = new HorariosService(_repositorio, new RelojFijo(Ahora));

        _programa = ProgramaAcademico.Crear(Guid.NewGuid(), "Informática", "Plan 2026");
        _programa.AsociarAsignatura(_asignatura);
        _repositorio.Programas.Add(_programa.Id);
        _repositorio.Coordinadores.Add(_coordinador);
        _repositorio.Autorizaciones.Add(Autorizacion(InicioAutorizacion, null, puedeGenerar: true));

        _teoria = Sembrar("Introducción a las bases de datos", TipoActividad.Teoria);
        _practica = Sembrar("Consultas y operaciones SQL", TipoActividad.Practica);
    }

    private AutorizacionCoordinador Autorizacion(
        DateOnly inicio,
        DateOnly? fin,
        bool puedeGenerar,
        Guid? programaId = null) =>
        new(_coordinador, programaId ?? _programa.Id, inicio, fin, puedeGenerar, puedeValidar: false);

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

    private CrearHorarioCommand Comando(
        params BloqueEnHorarioCommand[] bloques) =>
        new(_programa.Id, "2026-2027", _coordinador, bloques);

    private Task<HorarioDetalle> Crear(params BloqueEnHorarioCommand[] bloques) =>
        _servicio.CrearAsync(Comando(bloques), CancellationToken.None);

    private BloqueEnHorarioCommand En(BloqueContenido bloque, DateOnly fecha) => new(bloque.Id, fecha);

    // ---------------------------------------------------------------- Crear

    [Fact]
    public async Task Crear_Valido_GuardaElHorarioGeneradoConLaAutorizacionUsada()
    {
        HorarioDetalle detalle = await Crear(En(_teoria, Fecha1), En(_practica, Fecha2));

        Horario horario = Assert.Single(_repositorio.Horarios);
        Assert.Same(horario, detalle.Horario);
        Assert.Equal(EstadoHorario.Generado, horario.Estado);
        Assert.Equal(Ahora, horario.FechaCreacion);
        Assert.Equal("2026-2027", horario.PeriodoAcademico);
        Assert.Equal(_programa.Id, horario.ProgramaId);
        Assert.Equal(_coordinador, horario.CoordinadorId);
        Assert.Equal(InicioAutorizacion, horario.FechaInicioAutorizacion);
        Assert.Equal(2, detalle.CantTotalBloques);
        Assert.Equal(
            new[] { _teoria.Id, _practica.Id },
            detalle.Bloques.Select(item => item.Bloque.Id));
    }

    [Fact]
    public async Task Crear_EnM4_HoraPeriodoDeImparticionYAulaQuedanNulos()
    {
        HorarioDetalle detalle = await Crear(En(_teoria, Fecha1));

        BloqueEnHorario asociacion = Assert.Single(detalle.Bloques).Asociacion;
        Assert.Equal(Fecha1, asociacion.Fecha);
        Assert.Null(asociacion.HoraInicio);
        Assert.Null(asociacion.HoraFin);
        Assert.Null(asociacion.PeriodoImp);
        Assert.Null(asociacion.AulaId);
    }

    [Fact]
    public async Task Crear_MismoBloqueEnFechasDistintas_EsValidoYCuentaUnaSolaVez()
    {
        HorarioDetalle detalle = await Crear(En(_teoria, Fecha1), En(_teoria, Fecha2));

        Assert.Equal(2, detalle.Bloques.Count);
        Assert.Equal(1, detalle.CantTotalBloques);
    }

    [Fact]
    public async Task Crear_UsaLaAutorizacionVigenteConPermisoDeGeneracion()
    {
        _repositorio.Autorizaciones.Clear();
        _repositorio.Autorizaciones.Add(Autorizacion(new DateOnly(2026, 1, 1), new DateOnly(2026, 6, 30), true));
        _repositorio.Autorizaciones.Add(Autorizacion(new DateOnly(2026, 8, 1), null, false));
        _repositorio.Autorizaciones.Add(Autorizacion(new DateOnly(2026, 9, 1), null, true));
        _repositorio.Autorizaciones.Add(Autorizacion(new DateOnly(2027, 1, 1), null, true));

        HorarioDetalle detalle = await Crear(En(_teoria, Fecha1));

        Assert.Equal(new DateOnly(2026, 9, 1), detalle.Horario.FechaInicioAutorizacion);
    }

    [Fact]
    public async Task Crear_ConVariasAutorizacionesVigentes_UsaLaDeInicioMasReciente()
    {
        _repositorio.Autorizaciones.Add(Autorizacion(new DateOnly(2026, 10, 1), null, true));

        HorarioDetalle detalle = await Crear(En(_teoria, Fecha1));

        Assert.Equal(new DateOnly(2026, 10, 1), detalle.Horario.FechaInicioAutorizacion);
    }

    [Fact]
    public async Task Crear_ConUnaAutorizacionQueVenceHoy_LaConsideraVigente()
    {
        _repositorio.Autorizaciones.Clear();
        _repositorio.Autorizaciones.Add(Autorizacion(InicioAutorizacion, Hoy, true));

        HorarioDetalle detalle = await Crear(En(_teoria, Fecha1));

        Assert.Equal(InicioAutorizacion, detalle.Horario.FechaInicioAutorizacion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Crear_PeriodoVacio_LanzaSolicitudInvalidaSinConsultar(string periodo)
    {
        await Assert.ThrowsAsync<InvalidRequestException>(() => _servicio.CrearAsync(
            Comando(En(_teoria, Fecha1)) with { PeriodoAcademico = periodo },
            CancellationToken.None));

        Assert.Equal(0, _repositorio.Consultas);
    }

    [Fact]
    public async Task Crear_ConIdentificadoresVacios_LanzaSolicitudInvalidaSinConsultar()
    {
        CrearHorarioCommand valido = Comando(En(_teoria, Fecha1));

        await Assert.ThrowsAsync<InvalidRequestException>(() => _servicio.CrearAsync(
            valido with { ProgramaId = Guid.Empty }, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidRequestException>(() => _servicio.CrearAsync(
            valido with { CoordinadorId = Guid.Empty }, CancellationToken.None));
        await Assert.ThrowsAsync<InvalidRequestException>(() => Crear(
            new BloqueEnHorarioCommand(Guid.Empty, Fecha1)));

        Assert.Equal(0, _repositorio.Consultas);
    }

    [Fact]
    public async Task Crear_SinBloques_LanzaSolicitudInvalidaSinConsultar()
    {
        await Assert.ThrowsAsync<InvalidRequestException>(() => Crear());

        Assert.Equal(0, _repositorio.Consultas);
    }

    [Fact]
    public async Task Crear_ProgramaInexistente_LanzaNoEncontrado()
    {
        var excepcion = await Assert.ThrowsAsync<ResourceNotFoundException>(() => _servicio.CrearAsync(
            Comando(En(_teoria, Fecha1)) with { ProgramaId = Guid.NewGuid() },
            CancellationToken.None));

        Assert.Contains("programa", excepcion.Message);
        Assert.Empty(_repositorio.Horarios);
    }

    [Fact]
    public async Task Crear_CoordinadorInexistente_LanzaNoEncontrado()
    {
        var excepcion = await Assert.ThrowsAsync<ResourceNotFoundException>(() => _servicio.CrearAsync(
            Comando(En(_teoria, Fecha1)) with { CoordinadorId = Guid.NewGuid() },
            CancellationToken.None));

        Assert.Contains("coordinador", excepcion.Message);
        Assert.Empty(_repositorio.Horarios);
    }

    [Fact]
    public async Task Crear_BloqueInexistente_LanzaNoEncontradoYNoGuardaNada()
    {
        var excepcion = await Assert.ThrowsAsync<ResourceNotFoundException>(() => Crear(
            En(_teoria, Fecha1),
            new BloqueEnHorarioCommand(Guid.NewGuid(), Fecha2)));

        Assert.Contains("bloque", excepcion.Message);
        Assert.Empty(_repositorio.Horarios);
    }

    [Theory]
    [InlineData("sinAutorizacion")]
    [InlineData("vencida")]
    [InlineData("sinPermisoDeGenerar")]
    [InlineData("aunNoInicia")]
    [InlineData("deOtroPrograma")]
    public async Task Crear_SinAutorizacionVigenteConPermiso_LanzaAutorizacionDeNegocio(string caso)
    {
        _repositorio.Autorizaciones.Clear();
        switch (caso)
        {
            case "vencida":
                _repositorio.Autorizaciones.Add(Autorizacion(InicioAutorizacion, Hoy.AddDays(-1), true));
                break;
            case "sinPermisoDeGenerar":
                _repositorio.Autorizaciones.Add(Autorizacion(InicioAutorizacion, null, false));
                break;
            case "aunNoInicia":
                _repositorio.Autorizaciones.Add(Autorizacion(Hoy.AddDays(1), null, true));
                break;
            case "deOtroPrograma":
                _repositorio.Autorizaciones.Add(Autorizacion(InicioAutorizacion, null, true, Guid.NewGuid()));
                break;
        }

        var excepcion = await Assert.ThrowsAsync<DomainException>(() => Crear(En(_teoria, Fecha1)));

        Assert.Equal(TipoErrorDominio.AutorizacionNegocio, excepcion.Tipo);
        Assert.Empty(_repositorio.Horarios);
    }

    [Fact]
    public async Task Crear_SinAutorizacionYConBloqueInexistente_PriorizaLaAutorizacion()
    {
        _repositorio.Autorizaciones.Clear();

        var excepcion = await Assert.ThrowsAsync<DomainException>(() => Crear(
            new BloqueEnHorarioCommand(Guid.NewGuid(), Fecha1)));

        Assert.Equal(TipoErrorDominio.AutorizacionNegocio, excepcion.Tipo);
    }

    [Fact]
    public async Task Crear_BloqueNoAsociadoAlPrograma_LanzaConflictoYNoGuardaNada()
    {
        BloqueContenido ajeno = Sembrar("Bloque de otro programa", TipoActividad.Laboratorio, asociarAlPrograma: false);

        var excepcion = await Assert.ThrowsAsync<DomainException>(() => Crear(
            En(_teoria, Fecha1),
            En(ajeno, Fecha2)));

        Assert.Equal(TipoErrorDominio.Conflicto, excepcion.Tipo);
        Assert.Empty(_repositorio.Horarios);
    }

    [Fact]
    public async Task Crear_MismoBloqueYFechaRepetidos_LanzaConflictoYNoGuardaNada()
    {
        var excepcion = await Assert.ThrowsAsync<DomainException>(() => Crear(
            En(_teoria, Fecha1),
            En(_practica, Fecha1),
            En(_teoria, Fecha1)));

        Assert.Equal(TipoErrorDominio.Conflicto, excepcion.Tipo);
        Assert.Empty(_repositorio.Horarios);
    }

    [Fact]
    public async Task Crear_ConErrorDePersistencia_LoPropagaYNoQuedaHorario()
    {
        _repositorio.ErrorAlGuardar = new ResourceConflictException("Conflicto de integridad.");

        await Assert.ThrowsAsync<ResourceConflictException>(() => Crear(En(_teoria, Fecha1)));

        Assert.Empty(_repositorio.Horarios);
    }

    // ------------------------------------------------------------- Obtener

    [Fact]
    public async Task Obtener_Existente_DevuelveElHorarioConSusBloquesOrdenadosPorFecha()
    {
        HorarioDetalle creado = await Crear(En(_practica, Fecha2), En(_teoria, Fecha1));

        HorarioDetalle obtenido = await _servicio.ObtenerAsync(creado.Horario.Id, CancellationToken.None);

        Assert.Same(creado.Horario, obtenido.Horario);
        Assert.Equal(new[] { Fecha1, Fecha2 }, obtenido.Bloques.Select(item => item.Asociacion.Fecha));
        Assert.Equal("Introducción a las bases de datos", obtenido.Bloques[0].Bloque.Descripcion);
    }

    [Fact]
    public async Task Obtener_Inexistente_LanzaNoEncontrado()
    {
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _servicio.ObtenerAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Obtener_ConIdentificadorVacio_LanzaSolicitudInvalidaSinConsultar()
    {
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => _servicio.ObtenerAsync(Guid.Empty, CancellationToken.None));

        Assert.Equal(0, _repositorio.Consultas);
    }

    // -------------------------------------------------------------- Listar

    private Task<PagedResult<HorarioDetalle>> Listar(
        HorarioFilter? filtro = null,
        int? page = null,
        int? pageSize = null) =>
        _servicio.ListarAsync(filtro ?? new HorarioFilter(), page, pageSize, CancellationToken.None);

    [Fact]
    public async Task Listar_SinFiltros_UsaLaPaginacionPorDefectoIncluyeBloquesYOrdenaDelMasReciente()
    {
        HorarioDetalle antiguo = await Crear(En(_teoria, Fecha1));
        var reloj = new HorariosService(_repositorio, new RelojFijo(Ahora.AddDays(1)));
        HorarioDetalle reciente = await reloj.CrearAsync(
            Comando(En(_practica, Fecha2)), CancellationToken.None);

        PagedResult<HorarioDetalle> resultado = await Listar();

        Assert.Equal(1, resultado.Page);
        Assert.Equal(20, resultado.PageSize);
        Assert.Equal(2, resultado.TotalItems);
        Assert.Equal(
            new[] { reciente.Horario.Id, antiguo.Horario.Id },
            resultado.Items.Select(item => item.Horario.Id));
        Assert.Equal(_practica.Id, Assert.Single(resultado.Items[0].Bloques).Bloque.Id);
    }

    [Fact]
    public async Task Listar_FiltraPorProgramaPeriodoYEstado()
    {
        ProgramaAcademico otroPrograma = ProgramaAcademico.Crear(Guid.NewGuid(), "Matemática", "Plan 2026");
        _repositorio.Programas.Add(otroPrograma.Id);
        _repositorio.Autorizaciones.Add(Autorizacion(InicioAutorizacion, null, true, otroPrograma.Id));
        BloqueContenido bloqueOtro = BloqueContenido.Crear(
            "Matrices", TipoActividad.Teoria, NivelComplejidad.Bajo, Guid.NewGuid(), _coordinador);
        otroPrograma.AsociarAsignatura(bloqueOtro.AsignaturaId);
        bloqueOtro.AsociarPrograma(otroPrograma);
        _repositorio.Bloques.Add(bloqueOtro.Id, bloqueOtro);

        HorarioDetalle informatica = await Crear(En(_teoria, Fecha1));
        HorarioDetalle matematica = await _servicio.CrearAsync(
            new CrearHorarioCommand(
                otroPrograma.Id, "2027-2028", _coordinador, [En(bloqueOtro, Fecha2)]),
            CancellationToken.None);

        Assert.Equal(
            informatica.Horario.Id,
            Assert.Single((await Listar(new(ProgramaId: _programa.Id))).Items).Horario.Id);
        Assert.Equal(
            matematica.Horario.Id,
            Assert.Single((await Listar(new(PeriodoAcademico: "  2027-2028 "))).Items).Horario.Id);
        Assert.Equal(2, (await Listar(new(Estado: EstadoHorario.Generado))).TotalItems);
        Assert.Empty((await Listar(new(PeriodoAcademico: "2030-2031"))).Items);
    }

    [Fact]
    public async Task Listar_Paginado_DevuelveLaPaginaSolicitadaYElTotal()
    {
        await Crear(En(_teoria, Fecha1));
        await Crear(En(_teoria, Fecha2));
        await Crear(En(_practica, Fecha1));

        PagedResult<HorarioDetalle> resultado = await Listar(page: 2, pageSize: 2);

        Assert.Equal(2, resultado.Page);
        Assert.Equal(2, resultado.PageSize);
        Assert.Equal(3, resultado.TotalItems);
        Assert.Single(resultado.Items);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Listar_ConPaginacionInvalida_LanzaErrorDeValidacion(int page, int pageSize)
    {
        var excepcion = await Assert.ThrowsAsync<DomainException>(
            () => Listar(page: page, pageSize: pageSize));

        Assert.Equal(TipoErrorDominio.Validacion, excepcion.Tipo);
    }

    [Fact]
    public async Task Listar_ConEstadoInvalido_LanzaSolicitudInvalida()
    {
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => Listar(new HorarioFilter(Estado: (EstadoHorario)99)));
    }
}
