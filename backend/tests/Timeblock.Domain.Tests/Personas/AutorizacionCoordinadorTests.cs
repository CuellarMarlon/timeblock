using Timeblock.Domain.Common;
using Timeblock.Domain.Personas;

namespace Timeblock.Domain.Tests.Personas;

public class AutorizacionCoordinadorTests
{
    private static readonly Guid CoordinadorId = Guid.NewGuid();
    private static readonly Guid ProgramaId = Guid.NewGuid();
    private static readonly DateOnly Inicio = new(2026, 10, 12);

    private static AutorizacionCoordinador Crear(
        DateOnly? fechaFin = null,
        bool puedeGenerar = true,
        bool puedeValidar = false) =>
        new(CoordinadorId, ProgramaId, Inicio, fechaFin, puedeGenerar, puedeValidar);

    [Fact]
    public void Crear_Valida_ConservaClaveYPermisos()
    {
        var autorizacion = Crear(new DateOnly(2027, 1, 31), puedeGenerar: true, puedeValidar: true);

        Assert.Equal(CoordinadorId, autorizacion.CoordinadorId);
        Assert.Equal(ProgramaId, autorizacion.ProgramaId);
        Assert.Equal(Inicio, autorizacion.FechaInicio);
        Assert.Equal(new DateOnly(2027, 1, 31), autorizacion.FechaFin);
        Assert.True(autorizacion.PuedeGenerar);
        Assert.True(autorizacion.PuedeValidar);
    }

    [Fact]
    public void Crear_SinFechaFin_EsValida()
    {
        var autorizacion = Crear(fechaFin: null);

        Assert.Null(autorizacion.FechaFin);
    }

    [Fact]
    public void Crear_FechaFinIgualAInicio_EsValida()
    {
        var autorizacion = Crear(fechaFin: Inicio);

        Assert.Equal(Inicio, autorizacion.FechaFin);
    }

    [Fact]
    public void Crear_FechaFinAnteriorAInicio_Falla()
    {
        Assert.Throws<DomainException>(() => Crear(fechaFin: Inicio.AddDays(-1)));
    }

    [Fact]
    public void Crear_CoordinadorVacio_Falla()
    {
        Assert.Throws<DomainException>(
            () => new AutorizacionCoordinador(Guid.Empty, ProgramaId, Inicio, null, true, false));
    }

    [Fact]
    public void Crear_ProgramaVacio_Falla()
    {
        Assert.Throws<DomainException>(
            () => new AutorizacionCoordinador(CoordinadorId, Guid.Empty, Inicio, null, true, false));
    }

    [Fact]
    public void Actualizar_Valida_CambiaFinYPermisos()
    {
        var autorizacion = Crear();

        autorizacion.Actualizar(Inicio.AddMonths(3), puedeGenerar: false, puedeValidar: true);

        Assert.Equal(Inicio.AddMonths(3), autorizacion.FechaFin);
        Assert.False(autorizacion.PuedeGenerar);
        Assert.True(autorizacion.PuedeValidar);
    }

    [Fact]
    public void Actualizar_FechaFinAnteriorAInicio_FallaYConservaDatos()
    {
        var autorizacion = Crear(fechaFin: null, puedeGenerar: true, puedeValidar: false);

        Assert.Throws<DomainException>(
            () => autorizacion.Actualizar(Inicio.AddDays(-1), puedeGenerar: false, puedeValidar: true));
        Assert.Null(autorizacion.FechaFin);
        Assert.True(autorizacion.PuedeGenerar);
        Assert.False(autorizacion.PuedeValidar);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(15, true)]
    [InlineData(30, true)]
    [InlineData(31, false)]
    public void EstaVigenteEn_ConFechaFin_IncluyeAmbosExtremos(int dias, bool esperado)
    {
        var autorizacion = Crear(fechaFin: Inicio.AddDays(30));

        Assert.Equal(esperado, autorizacion.EstaVigenteEn(Inicio.AddDays(dias)));
    }

    [Fact]
    public void EstaVigenteEn_SinFechaFin_NoCaducaNunca()
    {
        var autorizacion = Crear(fechaFin: null);

        Assert.False(autorizacion.EstaVigenteEn(Inicio.AddDays(-1)));
        Assert.True(autorizacion.EstaVigenteEn(Inicio.AddYears(10)));
    }

    [Fact]
    public void PermiteGenerarEn_RequierePermisoYVigencia()
    {
        var conPermiso = Crear(fechaFin: Inicio.AddDays(10), puedeGenerar: true);
        var sinPermiso = Crear(fechaFin: Inicio.AddDays(10), puedeGenerar: false);

        Assert.True(conPermiso.PermiteGenerarEn(Inicio));
        Assert.False(conPermiso.PermiteGenerarEn(Inicio.AddDays(11)));
        Assert.False(sinPermiso.PermiteGenerarEn(Inicio));
    }

    [Fact]
    public void PermiteValidarEn_RequierePermisoYVigencia()
    {
        var conPermiso = Crear(fechaFin: Inicio.AddDays(10), puedeValidar: true);
        var sinPermiso = Crear(fechaFin: Inicio.AddDays(10), puedeValidar: false);

        Assert.True(conPermiso.PermiteValidarEn(Inicio.AddDays(10)));
        Assert.False(conPermiso.PermiteValidarEn(Inicio.AddDays(-1)));
        Assert.False(sinPermiso.PermiteValidarEn(Inicio));
    }
}
