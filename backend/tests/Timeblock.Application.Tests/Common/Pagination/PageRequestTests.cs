using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Common;

namespace Timeblock.Application.Tests.Common.Pagination;

public sealed class PageRequestTests
{
    [Fact]
    public void Default_UsaLosValoresDelContrato()
    {
        PageRequest solicitud = PageRequest.Default;

        Assert.Equal(1, solicitud.Page);
        Assert.Equal(20, solicitud.PageSize);
        Assert.Equal(0, solicitud.Skip);
    }

    [Fact]
    public void Create_SinParametros_UsaLosValoresPorDefecto()
    {
        PageRequest solicitud = PageRequest.Create(null, null);

        Assert.Equal(PageRequest.DefaultPage, solicitud.Page);
        Assert.Equal(PageRequest.DefaultPageSize, solicitud.PageSize);
    }

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(3, 20, 40)]
    [InlineData(2, 100, 100)]
    public void Create_ConValoresValidos_ConservaLosValoresYCalculaSkip(
        int page,
        int pageSize,
        int skip)
    {
        PageRequest solicitud = PageRequest.Create(page, pageSize);

        Assert.Equal(page, solicitud.Page);
        Assert.Equal(pageSize, solicitud.PageSize);
        Assert.Equal(skip, solicitud.Skip);
    }

    [Fact]
    public void Create_ConElTamanoMaximo_EsValido()
    {
        PageRequest solicitud = PageRequest.Create(1, PageRequest.MaxPageSize);

        Assert.Equal(100, solicitud.PageSize);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    [InlineData(1, 101)]
    public void Create_ConValoresFueraDeRango_LanzaErrorDeValidacion(
        int page,
        int pageSize)
    {
        var excepcion = Assert.Throws<DomainException>(
            () => PageRequest.Create(page, pageSize));

        Assert.Equal(TipoErrorDominio.Validacion, excepcion.Tipo);
    }

    [Fact]
    public void Create_ConUnaPaginaQueDesbordaElDesplazamiento_LanzaErrorDeValidacion()
    {
        var excepcion = Assert.Throws<DomainException>(
            () => PageRequest.Create(int.MaxValue, PageRequest.MaxPageSize));

        Assert.Equal(TipoErrorDominio.Validacion, excepcion.Tipo);
    }
}
