using Timeblock.Domain.Bloques;
using Timeblock.Domain.Common;

namespace Timeblock.Domain.Tests.Bloques;

public class DescripcionBloqueTests
{
    [Theory]
    [InlineData("  Bases de datos  ", "Bases de datos")]
    [InlineData("Bases    de \t datos", "Bases de datos")]
    [InlineData("\n Bases \r\n de   datos \n", "Bases de datos")]
    public void Limpiar_QuitaEspaciosExtremosYReduceConsecutivos(string entrada, string esperado)
    {
        Assert.Equal(esperado, DescripcionBloque.Limpiar(entrada));
    }

    [Fact]
    public void Limpiar_ConservaMayusculasYAcentos()
    {
        Assert.Equal(
            "Introducción a Álgebra",
            DescripcionBloque.Limpiar("  Introducción   a Álgebra "));
    }

    [Theory]
    [InlineData("  INTRODUCCIÓN   a   Álgebra ", "introducción a álgebra")]
    [InlineData("Bases de DATOS", "bases de datos")]
    public void Normalizar_IgnoraMayusculasYConservaAcentos(string entrada, string esperado)
    {
        Assert.Equal(esperado, DescripcionBloque.Normalizar(entrada));
    }

    [Fact]
    public void Normalizar_NoIgnoraAcentos()
    {
        Assert.NotEqual(
            DescripcionBloque.Normalizar("Camión"),
            DescripcionBloque.Normalizar("Camion"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void Limpiar_Vacia_Falla(string? entrada)
    {
        Assert.Throws<DomainException>(() => DescripcionBloque.Limpiar(entrada));
    }
}
