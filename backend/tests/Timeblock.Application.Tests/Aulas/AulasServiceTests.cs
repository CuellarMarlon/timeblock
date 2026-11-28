using Timeblock.Application.Aulas;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Aulas;
using Timeblock.Domain.Common;

namespace Timeblock.Application.Tests.Aulas;

public sealed class AulasServiceTests
{
    private readonly FakeAulaRepository _repositorio = new();
    private readonly AulasService _servicio;

    public AulasServiceTests()
    {
        _servicio = new AulasService(_repositorio);
    }

    private Aula Sembrar(string edificio, int capacidad)
    {
        Aula aula = Aula.Crear(Guid.NewGuid(), edificio, capacidad);
        _repositorio.Aulas.Add(aula.Id, aula);
        return aula;
    }

    [Fact]
    public async Task Crear_GeneraIdRecortaEdificioYGuarda()
    {
        Aula creada = await _servicio.CrearAsync(
            "  Edificio A ", 30, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, creada.Id);
        Assert.Equal("Edificio A", creada.Edificio);
        Assert.Equal(30, creada.Capacidad);
        Assert.Same(creada, _repositorio.Aulas[creada.Id]);
    }

    [Fact]
    public async Task Crear_DosAulasIgualesSonValidas()
    {
        Aula primera = await _servicio.CrearAsync("Edificio A", 30, CancellationToken.None);
        Aula segunda = await _servicio.CrearAsync("Edificio A", 30, CancellationToken.None);

        Assert.NotEqual(primera.Id, segunda.Id);
        Assert.Equal(2, _repositorio.Aulas.Count);
    }

    [Fact]
    public async Task Crear_ConDatosInvalidos_NoGuardaNada()
    {
        await Assert.ThrowsAsync<ValidacionCamposException>(() =>
            _servicio.CrearAsync("Edificio A", 0, CancellationToken.None));

        Assert.Empty(_repositorio.Aulas);
    }

    [Fact]
    public async Task Obtener_Existente_DevuelveElAula()
    {
        Aula aula = Sembrar("Edificio A", 30);

        Aula obtenida = await _servicio.ObtenerAsync(aula.Id, CancellationToken.None);

        Assert.Same(aula, obtenida);
    }

    [Fact]
    public async Task Obtener_Inexistente_LanzaNoEncontrado()
    {
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _servicio.ObtenerAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task Obtener_IdVacio_LanzaSolicitudInvalidaSinConsultar()
    {
        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            _servicio.ObtenerAsync(Guid.Empty, CancellationToken.None));

        Assert.Equal(0, _repositorio.Consultas);
    }

    [Fact]
    public async Task Actualizar_CambiaLosDatosYConservaElId()
    {
        Aula aula = Sembrar("Edificio A", 30);

        Aula actualizada = await _servicio.ActualizarAsync(
            aula.Id, " Edificio B ", 45, CancellationToken.None);

        Assert.Equal(aula.Id, actualizada.Id);
        Assert.Equal("Edificio B", _repositorio.Aulas[aula.Id].Edificio);
        Assert.Equal(45, _repositorio.Aulas[aula.Id].Capacidad);
    }

    [Fact]
    public async Task Actualizar_Inexistente_LanzaNoEncontrado()
    {
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            _servicio.ActualizarAsync(
                Guid.NewGuid(), "Edificio B", 45, CancellationToken.None));
    }

    [Fact]
    public async Task Actualizar_DatosInvalidos_SeRechazanAntesDeBuscarElAula()
    {
        // Orden del contrato: el formato (400) se comprueba antes de la
        // existencia (404).
        await Assert.ThrowsAsync<ValidacionCamposException>(() =>
            _servicio.ActualizarAsync(
                Guid.NewGuid(), "", 45, CancellationToken.None));

        Assert.Equal(0, _repositorio.Consultas);
    }

    [Fact]
    public async Task Actualizar_IdVacio_LanzaSolicitudInvalida()
    {
        await Assert.ThrowsAsync<InvalidRequestException>(() =>
            _servicio.ActualizarAsync(
                Guid.Empty, "Edificio B", 45, CancellationToken.None));
    }

    [Fact]
    public async Task Listar_OrdenaPorEdificioCapacidadYPagina()
    {
        Sembrar("Edificio B", 20);
        Sembrar("Edificio A", 45);
        Sembrar("Edificio A", 30);

        PagedResult<Aula> pagina = await _servicio.ListarAsync(
            null, 1, 2, CancellationToken.None);

        Assert.Equal(3, pagina.TotalItems);
        Assert.Equal(1, pagina.Page);
        Assert.Equal(2, pagina.PageSize);
        string[] orden = pagina.Items
            .Select(aula => $"{aula.Edificio}:{aula.Capacidad}")
            .ToArray();
        Assert.Equal(new[] { "Edificio A:30", "Edificio A:45" }, orden);
    }

    [Fact]
    public async Task Listar_SinParametros_UsaLaPaginacionPorDefecto()
    {
        await _servicio.ListarAsync(null, null, null, CancellationToken.None);

        Assert.Equal(PageRequest.DefaultPage, _repositorio.UltimaPagina!.Page);
        Assert.Equal(PageRequest.DefaultPageSize, _repositorio.UltimaPagina.PageSize);
    }

    [Fact]
    public async Task Listar_FiltraPorEdificioRecortadoSinDistinguirMayusculas()
    {
        Sembrar("Edificio Central", 30);
        Sembrar("Anexo", 30);

        PagedResult<Aula> pagina = await _servicio.ListarAsync(
            "  CENTRAL ", null, null, CancellationToken.None);

        Assert.Equal("CENTRAL", _repositorio.UltimoFiltro);
        Assert.Equal("Edificio Central", Assert.Single(pagina.Items).Edificio);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Listar_FiltroEnBlanco_NoFiltra(string filtro)
    {
        Sembrar("Edificio A", 30);

        await _servicio.ListarAsync(filtro, null, null, CancellationToken.None);

        Assert.Null(_repositorio.UltimoFiltro);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(null, 0)]
    [InlineData(null, PageRequest.MaxPageSize + 1)]
    public async Task Listar_PaginacionFueraDeRango_LanzaDomainException(
        int? page,
        int? pageSize)
    {
        DomainException error = await Assert.ThrowsAsync<DomainException>(() =>
            _servicio.ListarAsync(null, page, pageSize, CancellationToken.None));

        Assert.Equal(TipoErrorDominio.Validacion, error.Tipo);
    }
}
