using Timeblock.Domain.Aulas;
using Timeblock.Domain.Common;

namespace Timeblock.Domain.Tests.Aulas;

public sealed class AulaTests
{
    [Fact]
    public void Crear_ConDatosValidos_RecortaElEdificio()
    {
        Guid id = Guid.NewGuid();

        Aula aula = Aula.Crear(id, "  Edificio A  ", 30);

        Assert.Equal(id, aula.Id);
        Assert.Equal("Edificio A", aula.Edificio);
        Assert.Equal(30, aula.Capacidad);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(Aula.CapacidadMaxima)]
    public void Crear_CapacidadEnLosLimites_EsValida(int capacidad)
    {
        Aula aula = Aula.Crear(Guid.NewGuid(), "Edificio A", capacidad);

        Assert.Equal(capacidad, aula.Capacidad);
    }

    [Fact]
    public void Crear_EdificioConLongitudMaxima_EsValido()
    {
        string edificio = new('a', Aula.EdificioLongitudMaxima);

        Aula aula = Aula.Crear(Guid.NewGuid(), $"  {edificio}  ", 10);

        Assert.Equal(edificio, aula.Edificio);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_EdificioAusenteOVacio_ReportaElCampo(string? edificio)
    {
        ValidacionCamposException error = Assert.Throws<ValidacionCamposException>(
            () => Aula.Crear(Guid.NewGuid(), edificio, 30));

        Assert.Equal("edificio", Assert.Single(error.Errores).Key);
        Assert.Equal("El edificio es obligatorio.", error.Errores["edificio"][0]);
        Assert.Equal(TipoErrorDominio.Validacion, error.Tipo);
    }

    [Fact]
    public void Crear_EdificioMasLargoQueElMaximo_ReportaElCampo()
    {
        string edificio = new('a', Aula.EdificioLongitudMaxima + 1);

        ValidacionCamposException error = Assert.Throws<ValidacionCamposException>(
            () => Aula.Crear(Guid.NewGuid(), edificio, 30));

        Assert.Equal("edificio", Assert.Single(error.Errores).Key);
    }

    [Fact]
    public void Crear_CapacidadAusente_ReportaElCampo()
    {
        ValidacionCamposException error = Assert.Throws<ValidacionCamposException>(
            () => Aula.Crear(Guid.NewGuid(), "Edificio A", null));

        Assert.Equal("capacidad", Assert.Single(error.Errores).Key);
        Assert.Equal("La capacidad es obligatoria.", error.Errores["capacidad"][0]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Crear_CapacidadNoPositiva_ReportaElCampo(int capacidad)
    {
        ValidacionCamposException error = Assert.Throws<ValidacionCamposException>(
            () => Aula.Crear(Guid.NewGuid(), "Edificio A", capacidad));

        Assert.Equal("capacidad", Assert.Single(error.Errores).Key);
        Assert.Equal(
            "La capacidad debe ser mayor que cero.",
            error.Errores["capacidad"][0]);
    }

    [Fact]
    public void Crear_CapacidadMayorQueElMaximo_ReportaElCampo()
    {
        ValidacionCamposException error = Assert.Throws<ValidacionCamposException>(
            () => Aula.Crear(Guid.NewGuid(), "Edificio A", Aula.CapacidadMaxima + 1));

        Assert.Equal("capacidad", Assert.Single(error.Errores).Key);
    }

    [Fact]
    public void Crear_ConVariosCamposInvalidos_LosReportaTodosJuntos()
    {
        ValidacionCamposException error = Assert.Throws<ValidacionCamposException>(
            () => Aula.Crear(Guid.NewGuid(), " ", 0));

        Assert.Equal(2, error.Errores.Count);
        Assert.Contains("edificio", error.Errores.Keys);
        Assert.Contains("capacidad", error.Errores.Keys);
    }

    [Fact]
    public void Crear_IdentificadorVacio_LanzaDomainException()
    {
        Assert.Throws<DomainException>(
            () => Aula.Crear(Guid.Empty, "Edificio A", 30));
    }

    [Fact]
    public void Actualizar_CambiaEdificioYCapacidadSinCambiarElId()
    {
        Aula aula = Aula.Crear(Guid.NewGuid(), "Edificio A", 30);
        Guid id = aula.Id;

        aula.Actualizar("  Edificio B ", 45);

        Assert.Equal(id, aula.Id);
        Assert.Equal("Edificio B", aula.Edificio);
        Assert.Equal(45, aula.Capacidad);
    }

    [Fact]
    public void Actualizar_ConDatosInvalidos_NoModificaElAula()
    {
        Aula aula = Aula.Crear(Guid.NewGuid(), "Edificio A", 30);

        Assert.Throws<ValidacionCamposException>(
            () => aula.Actualizar("Edificio B", 0));

        Assert.Equal("Edificio A", aula.Edificio);
        Assert.Equal(30, aula.Capacidad);
    }

    [Fact]
    public void Reconstituir_NoValida_ParaNoImpedirLeerFilasAnteriores()
    {
        string edificioLargo = new('a', Aula.EdificioLongitudMaxima + 50);

        Aula aula = Aula.Reconstituir(Guid.NewGuid(), edificioLargo, 5000);

        Assert.Equal(edificioLargo, aula.Edificio);
        Assert.Equal(5000, aula.Capacidad);
    }

    [Fact]
    public void ValidacionCamposException_SinErrores_NoSePuedeCrear()
    {
        Assert.Throws<ArgumentException>(
            () => new ValidacionCamposException(new Dictionary<string, string[]>()));
    }
}
