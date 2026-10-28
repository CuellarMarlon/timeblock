using Timeblock.Domain.Bloques;
using Timeblock.Domain.Common;

namespace Timeblock.Domain.Tests.Bloques;

public class ClaveDuplicidadTests
{
    private static readonly Guid AsignaturaId = Guid.NewGuid();

    [Fact]
    public void Crear_DescripcionesEquivalentes_ProducenClavesIguales()
    {
        var primera = ClaveDuplicidad.Crear(AsignaturaId, TipoActividad.Teoria, "  Intro   a SQL ");
        var segunda = ClaveDuplicidad.Crear(AsignaturaId, TipoActividad.Teoria, "intro a sql");

        Assert.Equal(primera, segunda);
    }

    [Fact]
    public void Crear_DifiereEnTipoAsignaturaODescripcion_ProduceClavesDistintas()
    {
        var baseClave = ClaveDuplicidad.Crear(AsignaturaId, TipoActividad.Teoria, "intro");

        Assert.NotEqual(baseClave, ClaveDuplicidad.Crear(AsignaturaId, TipoActividad.Practica, "intro"));
        Assert.NotEqual(baseClave, ClaveDuplicidad.Crear(Guid.NewGuid(), TipoActividad.Teoria, "intro"));
        Assert.NotEqual(baseClave, ClaveDuplicidad.Crear(AsignaturaId, TipoActividad.Teoria, "otra"));
    }

    [Fact]
    public void Crear_AsignaturaVacia_Falla()
    {
        Assert.Throws<DomainException>(
            () => ClaveDuplicidad.Crear(Guid.Empty, TipoActividad.Teoria, "intro"));
    }

    [Fact]
    public void Crear_TipoInvalido_Falla()
    {
        Assert.Throws<DomainException>(
            () => ClaveDuplicidad.Crear(AsignaturaId, (TipoActividad)99, "intro"));
    }

    [Fact]
    public void Crear_DescripcionVacia_Falla()
    {
        Assert.Throws<DomainException>(
            () => ClaveDuplicidad.Crear(AsignaturaId, TipoActividad.Teoria, "  "));
    }
}
