using Timeblock.Application.Bloques;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;

namespace Timeblock.Application.Tests.Bloques;

public sealed class BloquesServiceTests
{
    private const string Descripcion = "Introducción a las bases de datos";

    private readonly FakeBloqueRepository _repositorio = new();
    private readonly BloquesService _servicio;
    private readonly Guid _asignatura;
    private readonly Guid _coordinador;
    private readonly ProgramaAcademico _programa;

    public BloquesServiceTests()
    {
        _servicio = new BloquesService(_repositorio);
        _asignatura = _repositorio.AgregarAsignatura();
        _coordinador = _repositorio.AgregarCoordinador();
        _programa = _repositorio.AgregarPrograma(_asignatura);
    }

    private CrearBloqueCommand Comando(
        string? descripcion = Descripcion,
        TipoActividad tipo = TipoActividad.Teoria,
        NivelComplejidad nivel = NivelComplejidad.Medio,
        Guid? asignaturaId = null,
        Guid? coordinadorId = null,
        IReadOnlyCollection<Guid>? programasIds = null) =>
        new(
            descripcion!,
            tipo,
            nivel,
            asignaturaId ?? _asignatura,
            coordinadorId ?? _coordinador,
            programasIds ?? []);

    private Task<BloqueContenido> CrearBloque(
        string descripcion = Descripcion,
        TipoActividad tipo = TipoActividad.Teoria,
        NivelComplejidad nivel = NivelComplejidad.Medio,
        Guid? asignaturaId = null,
        Guid? coordinadorId = null,
        IReadOnlyCollection<Guid>? programasIds = null) =>
        _servicio.CrearAsync(
            Comando(descripcion, tipo, nivel, asignaturaId, coordinadorId, programasIds),
            CancellationToken.None);

    // ---------------------------------------------------------------- Crear

    [Fact]
    public async Task Crear_Valido_GuardaElBloqueConSusProgramas()
    {
        BloqueContenido bloque = await CrearBloque(programasIds: [_programa.Id]);

        Assert.Same(bloque, Assert.Single(_repositorio.Bloques));
        Assert.Equal(Descripcion, bloque.Descripcion);
        Assert.Equal(TipoActividad.Teoria, bloque.TipoActividad);
        Assert.Equal(NivelComplejidad.Medio, bloque.NivelComplejidad);
        Assert.Equal(_asignatura, bloque.AsignaturaId);
        Assert.Equal(_coordinador, bloque.CoordinadorCreadorId);
        Assert.True(bloque.EstaAsociadoAPrograma(_programa.Id));
    }

    [Fact]
    public async Task Crear_SinProgramas_CreaElBloqueSinAsociaciones()
    {
        BloqueContenido bloque = await CrearBloque();

        Assert.Empty(bloque.Programas);
        Assert.Single(_repositorio.Bloques);
    }

    [Fact]
    public async Task Crear_ConProgramasRepetidos_NoDuplicaLasAsociaciones()
    {
        BloqueContenido bloque = await CrearBloque(
            programasIds: [_programa.Id, _programa.Id]);

        Assert.Single(bloque.Programas);
    }

    [Fact]
    public async Task Crear_ElBloqueDeOtroCoordinador_NoSeLimitaPorQuienLoCreo()
    {
        Guid otroCoordinador = _repositorio.AgregarCoordinador();
        await CrearBloque(coordinadorId: _coordinador, programasIds: [_programa.Id]);

        BloqueContenido otro = await CrearBloque(
            descripcion: "Normalización",
            coordinadorId: otroCoordinador,
            programasIds: [_programa.Id]);

        Assert.Equal(otroCoordinador, otro.CoordinadorCreadorId);
        Assert.Equal(2, _repositorio.Bloques.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Crear_DescripcionVacia_LanzaSolicitudInvalidaSinConsultar(string? descripcion)
    {
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => _servicio.CrearAsync(Comando(descripcion), CancellationToken.None));

        Assert.Equal(0, _repositorio.Consultas);
        Assert.Empty(_repositorio.Bloques);
    }

    [Fact]
    public async Task Crear_ConEnumeracionesInvalidas_LanzaSolicitudInvalida()
    {
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => _servicio.CrearAsync(
                Comando(tipo: (TipoActividad)99),
                CancellationToken.None));
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => _servicio.CrearAsync(
                Comando(nivel: (NivelComplejidad)99),
                CancellationToken.None));

        Assert.Equal(0, _repositorio.Consultas);
    }

    [Fact]
    public async Task Crear_ConIdentificadoresVacios_LanzaSolicitudInvalidaSinConsultar()
    {
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => _servicio.CrearAsync(Comando(asignaturaId: Guid.Empty), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => _servicio.CrearAsync(Comando(coordinadorId: Guid.Empty), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => _servicio.CrearAsync(
                Comando(programasIds: [Guid.Empty]),
                CancellationToken.None));

        Assert.Equal(0, _repositorio.Consultas);
    }

    [Fact]
    public async Task Crear_AsignaturaInexistente_LanzaNoEncontrado()
    {
        var excepcion = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _servicio.CrearAsync(
                Comando(asignaturaId: Guid.NewGuid()),
                CancellationToken.None));

        Assert.Contains("asignatura", excepcion.Message);
        Assert.Empty(_repositorio.Bloques);
    }

    [Fact]
    public async Task Crear_CoordinadorInexistente_LanzaNoEncontrado()
    {
        var excepcion = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => _servicio.CrearAsync(
                Comando(coordinadorId: Guid.NewGuid()),
                CancellationToken.None));

        Assert.Contains("coordinador", excepcion.Message);
        Assert.Empty(_repositorio.Bloques);
    }

    [Fact]
    public async Task Crear_ProgramaInexistente_LanzaNoEncontradoYNoGuardaNada()
    {
        var excepcion = await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => CrearBloque(programasIds: [_programa.Id, Guid.NewGuid()]));

        Assert.Contains("programa", excepcion.Message);
        Assert.Empty(_repositorio.Bloques);
        Assert.Equal(0, _repositorio.Guardados);
    }

    [Fact]
    public async Task Crear_ConProgramaQueNoIncluyeLaAsignatura_LanzaSolicitudInvalida()
    {
        ProgramaAcademico otroPrograma = _repositorio.AgregarPrograma(Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => CrearBloque(programasIds: [otroPrograma.Id]));

        Assert.Empty(_repositorio.Bloques);
    }

    [Fact]
    public async Task Crear_Duplicado_LanzaConflicto()
    {
        await CrearBloque();

        await Assert.ThrowsAsync<ResourceConflictException>(() => CrearBloque());

        Assert.Single(_repositorio.Bloques);
    }

    [Fact]
    public async Task Crear_DuplicadoConOtrosEspaciosYMayusculas_LanzaConflicto()
    {
        await CrearBloque("Introducción a las bases de datos");

        await Assert.ThrowsAsync<ResourceConflictException>(
            () => CrearBloque("   INTRODUCCIÓN   a las\tBASES  de datos  "));
    }

    [Fact]
    public async Task Crear_DescripcionQueSoloDifiereEnLosAcentos_NoEsDuplicado()
    {
        await CrearBloque("Introducción a las bases de datos");

        BloqueContenido sinAcento = await CrearBloque("Introduccion a las bases de datos");

        Assert.Equal(2, _repositorio.Bloques.Count);
        Assert.NotNull(sinAcento);
    }

    [Fact]
    public async Task Crear_MismaDescripcionConOtroTipoOAsignatura_NoEsDuplicado()
    {
        Guid otraAsignatura = _repositorio.AgregarAsignatura();
        await CrearBloque(tipo: TipoActividad.Teoria);

        await CrearBloque(tipo: TipoActividad.Practica);
        await CrearBloque(asignaturaId: otraAsignatura);

        Assert.Equal(3, _repositorio.Bloques.Count);
    }

    [Fact]
    public async Task Crear_MismaClaveConOtraComplejidadYCoordinador_EsDuplicado()
    {
        Guid otroCoordinador = _repositorio.AgregarCoordinador();
        await CrearBloque(nivel: NivelComplejidad.Bajo);

        await Assert.ThrowsAsync<ResourceConflictException>(
            () => CrearBloque(nivel: NivelComplejidad.Alto, coordinadorId: otroCoordinador));
    }

    // ------------------------------------------------------------- Obtener

    [Fact]
    public async Task Obtener_Existente_DevuelveElBloque()
    {
        BloqueContenido creado = await CrearBloque();

        BloqueContenido obtenido = await _servicio.ObtenerAsync(creado.Id, CancellationToken.None);

        Assert.Same(creado, obtenido);
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

    private async Task<PagedResult<BloqueContenido>> Listar(
        BloqueFilter? filtro = null,
        int? page = null,
        int? pageSize = null) =>
        await _servicio.ListarAsync(
            filtro ?? new BloqueFilter(),
            page,
            pageSize,
            CancellationToken.None);

    [Fact]
    public async Task Listar_SinFiltros_UsaLaPaginacionPorDefectoYOrdenaPorDescripcion()
    {
        await CrearBloque("Joins");
        await CrearBloque("Álgebra relacional");
        await CrearBloque("Bases de datos");

        PagedResult<BloqueContenido> resultado = await Listar();

        Assert.Equal(1, resultado.Page);
        Assert.Equal(20, resultado.PageSize);
        Assert.Equal(3, resultado.TotalItems);
        Assert.Equal(
            new[] { "Bases de datos", "Joins", "Álgebra relacional" },
            resultado.Items.Select(bloque => bloque.Descripcion));
    }

    [Fact]
    public async Task Listar_FiltraPorCadaCriterio()
    {
        Guid otraAsignatura = _repositorio.AgregarAsignatura();
        ProgramaAcademico otroPrograma = _repositorio.AgregarPrograma(otraAsignatura);
        Guid otroCoordinador = _repositorio.AgregarCoordinador();

        BloqueContenido teoria = await CrearBloque(
            "Teoría A", TipoActividad.Teoria, NivelComplejidad.Bajo, programasIds: [_programa.Id]);
        BloqueContenido laboratorio = await CrearBloque(
            "Laboratorio B", TipoActividad.Laboratorio, NivelComplejidad.Alto,
            otraAsignatura, otroCoordinador, [otroPrograma.Id]);

        Assert.Same(teoria, Assert.Single((await Listar(new BloqueFilter(ProgramaId: _programa.Id))).Items));
        Assert.Same(laboratorio, Assert.Single((await Listar(new BloqueFilter(AsignaturaId: otraAsignatura))).Items));
        Assert.Same(laboratorio, Assert.Single((await Listar(new BloqueFilter(CoordinadorId: otroCoordinador))).Items));
        Assert.Same(teoria, Assert.Single((await Listar(new BloqueFilter(TipoActividad: TipoActividad.Teoria))).Items));
        Assert.Same(laboratorio, Assert.Single((await Listar(new BloqueFilter(NivelComplejidad: NivelComplejidad.Alto))).Items));
    }

    [Fact]
    public async Task Listar_CombinaLosFiltros()
    {
        await CrearBloque("Teoría A", TipoActividad.Teoria, NivelComplejidad.Bajo);
        BloqueContenido buscado = await CrearBloque(
            "Teoría B", TipoActividad.Teoria, NivelComplejidad.Alto);
        await CrearBloque("Práctica C", TipoActividad.Practica, NivelComplejidad.Alto);

        PagedResult<BloqueContenido> resultado = await Listar(new BloqueFilter(
            TipoActividad: TipoActividad.Teoria,
            NivelComplejidad: NivelComplejidad.Alto));

        Assert.Equal(1, resultado.TotalItems);
        Assert.Same(buscado, Assert.Single(resultado.Items));
    }

    [Fact]
    public async Task Listar_ConProgramaSinBloques_DevuelveUnaPaginaVacia()
    {
        await CrearBloque(programasIds: [_programa.Id]);

        PagedResult<BloqueContenido> resultado = await Listar(
            new BloqueFilter(ProgramaId: Guid.NewGuid()));

        Assert.Empty(resultado.Items);
        Assert.Equal(0, resultado.TotalItems);
    }

    [Fact]
    public async Task Listar_Paginado_DevuelveLaPaginaSolicitadaYElTotal()
    {
        await CrearBloque("Bloque 1");
        await CrearBloque("Bloque 2");
        await CrearBloque("Bloque 3");

        PagedResult<BloqueContenido> resultado = await Listar(page: 2, pageSize: 2);

        Assert.Equal(2, resultado.Page);
        Assert.Equal(2, resultado.PageSize);
        Assert.Equal(3, resultado.TotalItems);
        Assert.Equal("Bloque 3", Assert.Single(resultado.Items).Descripcion);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Listar_ConPaginacionInvalida_LanzaSolicitudInvalida(int page, int pageSize)
    {
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => Listar(page: page, pageSize: pageSize));
    }

    [Fact]
    public async Task Listar_ConEnumeracionesInvalidas_LanzaSolicitudInvalida()
    {
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => Listar(new BloqueFilter(TipoActividad: (TipoActividad)99)));
        await Assert.ThrowsAsync<InvalidRequestException>(
            () => Listar(new BloqueFilter(NivelComplejidad: (NivelComplejidad)99)));
    }

    // ---------------------------------------------------------- Actualizar

    private Task<BloqueContenido> Actualizar(Guid bloqueId, ActualizarBloqueCommand comando) =>
        _servicio.ActualizarAsync(bloqueId, comando, CancellationToken.None);

    [Fact]
    public async Task Actualizar_Descripcion_ConservaElResto()
    {
        BloqueContenido bloque = await CrearBloque(
            tipo: TipoActividad.Practica, nivel: NivelComplejidad.Alto, programasIds: [_programa.Id]);
        int guardados = _repositorio.Guardados;

        BloqueContenido resultado = await Actualizar(
            bloque.Id,
            new ActualizarBloqueCommand(Descripcion: "  Modelo   relacional "));

        Assert.Equal("Modelo relacional", resultado.Descripcion);
        Assert.Equal("modelo relacional", resultado.DescripcionNormalizada);
        Assert.Equal(TipoActividad.Practica, resultado.TipoActividad);
        Assert.Equal(NivelComplejidad.Alto, resultado.NivelComplejidad);
        Assert.Equal(_asignatura, resultado.AsignaturaId);
        Assert.True(resultado.EstaAsociadoAPrograma(_programa.Id));
        Assert.Equal(guardados + 1, _repositorio.Guardados);
    }

    [Fact]
    public async Task Actualizar_ActividadYComplejidad_CambiaSoloEsosDatos()
    {
        BloqueContenido bloque = await CrearBloque();

        BloqueContenido resultado = await Actualizar(
            bloque.Id,
            new ActualizarBloqueCommand(
                TipoActividad: TipoActividad.Laboratorio,
                NivelComplejidad: NivelComplejidad.Bajo));

        Assert.Equal(Descripcion, resultado.Descripcion);
        Assert.Equal(TipoActividad.Laboratorio, resultado.TipoActividad);
        Assert.Equal(NivelComplejidad.Bajo, resultado.NivelComplejidad);
    }

    [Fact]
    public async Task Actualizar_SinNingunDato_LanzaSolicitudInvalida()
    {
        BloqueContenido bloque = await CrearBloque();

        await Assert.ThrowsAsync<InvalidRequestException>(
            () => Actualizar(bloque.Id, new ActualizarBloqueCommand()));
    }

    [Fact]
    public async Task Actualizar_DescripcionVacia_LanzaSolicitudInvalidaYNoModificaElBloque()
    {
        BloqueContenido bloque = await CrearBloque();
        int guardados = _repositorio.Guardados;

        await Assert.ThrowsAsync<InvalidRequestException>(() => Actualizar(
            bloque.Id,
            new ActualizarBloqueCommand(Descripcion: "   ", TipoActividad: TipoActividad.Practica)));

        Assert.Equal(Descripcion, bloque.Descripcion);
        Assert.Equal(TipoActividad.Teoria, bloque.TipoActividad);
        Assert.Equal(guardados, _repositorio.Guardados);
    }

    [Fact]
    public async Task Actualizar_ConEnumeracionesInvalidas_LanzaSolicitudInvalida()
    {
        BloqueContenido bloque = await CrearBloque();

        await Assert.ThrowsAsync<InvalidRequestException>(() => Actualizar(
            bloque.Id, new ActualizarBloqueCommand(TipoActividad: (TipoActividad)99)));
        await Assert.ThrowsAsync<InvalidRequestException>(() => Actualizar(
            bloque.Id, new ActualizarBloqueCommand(NivelComplejidad: (NivelComplejidad)99)));
    }

    [Fact]
    public async Task Actualizar_BloqueInexistente_LanzaNoEncontrado()
    {
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => Actualizar(
            Guid.NewGuid(), new ActualizarBloqueCommand(Descripcion: "Otra")));
    }

    [Fact]
    public async Task Actualizar_ConLosMismosDatos_NoSeConsideraDuplicadoDeSiMismo()
    {
        BloqueContenido bloque = await CrearBloque();

        BloqueContenido resultado = await Actualizar(
            bloque.Id,
            new ActualizarBloqueCommand(
                Descripcion: "  INTRODUCCIÓN a las bases de datos",
                NivelComplejidad: NivelComplejidad.Alto));

        Assert.Same(bloque, resultado);
        Assert.Equal(NivelComplejidad.Alto, resultado.NivelComplejidad);
    }

    [Fact]
    public async Task Actualizar_QueGeneraUnDuplicado_LanzaConflictoSinGuardar()
    {
        await CrearBloque("Modelo relacional");
        BloqueContenido otro = await CrearBloque("Normalización");
        int guardados = _repositorio.Guardados;

        await Assert.ThrowsAsync<ResourceConflictException>(() => Actualizar(
            otro.Id, new ActualizarBloqueCommand(Descripcion: "modelo   RELACIONAL")));

        Assert.Equal(guardados, _repositorio.Guardados);
    }

    [Fact]
    public async Task Actualizar_Asignatura_CuandoTodosSusProgramasLaIncluyen_LaCambia()
    {
        Guid nuevaAsignatura = _repositorio.AgregarAsignatura();
        _programa.AsociarAsignatura(nuevaAsignatura);
        BloqueContenido bloque = await CrearBloque(programasIds: [_programa.Id]);

        BloqueContenido resultado = await Actualizar(
            bloque.Id, new ActualizarBloqueCommand(AsignaturaId: nuevaAsignatura));

        Assert.Equal(nuevaAsignatura, resultado.AsignaturaId);
    }

    [Fact]
    public async Task Actualizar_Asignatura_QueUnProgramaNoIncluye_LanzaSolicitudInvalidaYLaConserva()
    {
        Guid nuevaAsignatura = _repositorio.AgregarAsignatura();
        BloqueContenido bloque = await CrearBloque(programasIds: [_programa.Id]);

        await Assert.ThrowsAsync<InvalidRequestException>(() => Actualizar(
            bloque.Id, new ActualizarBloqueCommand(AsignaturaId: nuevaAsignatura)));

        Assert.Equal(_asignatura, bloque.AsignaturaId);
    }

    [Fact]
    public async Task Actualizar_Asignatura_SinProgramas_LaCambiaLibremente()
    {
        Guid nuevaAsignatura = _repositorio.AgregarAsignatura();
        BloqueContenido bloque = await CrearBloque();

        BloqueContenido resultado = await Actualizar(
            bloque.Id, new ActualizarBloqueCommand(AsignaturaId: nuevaAsignatura));

        Assert.Equal(nuevaAsignatura, resultado.AsignaturaId);
    }

    [Fact]
    public async Task Actualizar_AsignaturaInexistente_LanzaNoEncontrado()
    {
        BloqueContenido bloque = await CrearBloque();

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => Actualizar(
            bloque.Id, new ActualizarBloqueCommand(AsignaturaId: Guid.NewGuid())));
    }

    [Fact]
    public async Task Actualizar_AsignaturaVacia_LanzaSolicitudInvalida()
    {
        BloqueContenido bloque = await CrearBloque();

        await Assert.ThrowsAsync<InvalidRequestException>(() => Actualizar(
            bloque.Id, new ActualizarBloqueCommand(AsignaturaId: Guid.Empty)));
    }

    [Fact]
    public async Task Actualizar_Asignatura_QueGeneraUnDuplicado_LanzaConflicto()
    {
        Guid otraAsignatura = _repositorio.AgregarAsignatura();
        await CrearBloque(asignaturaId: otraAsignatura);
        BloqueContenido bloque = await CrearBloque();

        await Assert.ThrowsAsync<ResourceConflictException>(() => Actualizar(
            bloque.Id, new ActualizarBloqueCommand(AsignaturaId: otraAsignatura)));
    }

    // ----------------------------------------------------- AsociarPrograma

    private Task Asociar(Guid bloqueId, Guid programaId) =>
        _servicio.AsociarProgramaAsync(bloqueId, programaId, CancellationToken.None);

    [Fact]
    public async Task AsociarPrograma_Valido_AsociaElBloque()
    {
        BloqueContenido bloque = await CrearBloque();
        int guardados = _repositorio.Guardados;

        await Asociar(bloque.Id, _programa.Id);

        Assert.True(bloque.EstaAsociadoAPrograma(_programa.Id));
        Assert.Equal(guardados + 1, _repositorio.Guardados);
    }

    [Fact]
    public async Task AsociarPrograma_Repetido_EsIdempotenteYNoDuplicaNiGuarda()
    {
        BloqueContenido bloque = await CrearBloque();
        await Asociar(bloque.Id, _programa.Id);
        int guardados = _repositorio.Guardados;

        await Asociar(bloque.Id, _programa.Id);
        await Asociar(bloque.Id, _programa.Id);

        Assert.Single(bloque.Programas);
        Assert.Equal(guardados, _repositorio.Guardados);
    }

    [Fact]
    public async Task AsociarPrograma_YaAsociadoDesdeLaCreacion_NoLanzaError()
    {
        BloqueContenido bloque = await CrearBloque(programasIds: [_programa.Id]);

        await Asociar(bloque.Id, _programa.Id);

        Assert.Single(bloque.Programas);
    }

    [Fact]
    public async Task AsociarPrograma_QueNoIncluyeLaAsignatura_LanzaSolicitudInvalida()
    {
        ProgramaAcademico otroPrograma = _repositorio.AgregarPrograma(Guid.NewGuid());
        BloqueContenido bloque = await CrearBloque();

        await Assert.ThrowsAsync<InvalidRequestException>(() => Asociar(bloque.Id, otroPrograma.Id));

        Assert.Empty(bloque.Programas);
    }

    [Fact]
    public async Task AsociarPrograma_BloqueInexistente_LanzaNoEncontrado()
    {
        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => Asociar(Guid.NewGuid(), _programa.Id));
    }

    [Fact]
    public async Task AsociarPrograma_ProgramaInexistente_LanzaNoEncontrado()
    {
        BloqueContenido bloque = await CrearBloque();

        await Assert.ThrowsAsync<ResourceNotFoundException>(
            () => Asociar(bloque.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task AsociarPrograma_ConIdentificadoresVacios_LanzaSolicitudInvalidaSinConsultar()
    {
        await Assert.ThrowsAsync<InvalidRequestException>(() => Asociar(Guid.Empty, _programa.Id));
        await Assert.ThrowsAsync<InvalidRequestException>(() => Asociar(Guid.NewGuid(), Guid.Empty));

        Assert.Equal(0, _repositorio.Consultas);
    }
}
