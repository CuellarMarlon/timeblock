using Timeblock.Application.Common.Pagination;

namespace Timeblock.Application.Tests.Common.Pagination;

public sealed class PagedResultTests
{
    [Fact]
    public void Create_ConservaItemsYDatosDePaginacion()
    {
        PageRequest solicitud = PageRequest.Create(2, 3);

        PagedResult<string> resultado =
            PagedResult<string>.Create(["d", "e", "f"], solicitud, 10);

        Assert.Equal(new[] { "d", "e", "f" }, resultado.Items);
        Assert.Equal(2, resultado.Page);
        Assert.Equal(3, resultado.PageSize);
        Assert.Equal(10, resultado.TotalItems);
    }

    [Fact]
    public void Create_ConTotalNegativo_Lanza()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PagedResult<string>.Create([], PageRequest.Default, -1));
    }

    [Fact]
    public void Create_SinItems_Lanza()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PagedResult<string>.Create(null!, PageRequest.Default, 0));
    }

    [Fact]
    public void Map_ConvierteLosItemsYConservaLaPaginacion()
    {
        PagedResult<int> original = PagedResult<int>.Create(
            [1, 2, 3],
            PageRequest.Create(4, 3),
            30);

        PagedResult<string> convertido = original.Map(n => $"#{n}");

        Assert.Equal(new[] { "#1", "#2", "#3" }, convertido.Items);
        Assert.Equal(4, convertido.Page);
        Assert.Equal(3, convertido.PageSize);
        Assert.Equal(30, convertido.TotalItems);
    }
}
