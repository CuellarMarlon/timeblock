using Timeblock.Domain.Common;
using Timeblock.Domain.Catalogo;

namespace Timeblock.Domain.Tests.Catalogo;

public sealed class AsignaturaTests
{
    [Fact]
    public void Crear_ConDatosValidos_CreaAsignatura()
    {
        Guid id = Guid.NewGuid();

        Asignatura asignatura = Asignatura.Crear(id, "  Bases de Datos  ");

        Assert.Equal(id, asignatura.Id);
        Assert.Equal("Bases de Datos", asignatura.Nombre);
    }

    [Fact]
    public void Crear_ConIdVacio_LanzaDomainException()
    {
        Assert.Throws<DomainException>(
            () => Asignatura.Crear(Guid.Empty, "Bases de Datos"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Crear_ConNombreVacio_LanzaDomainException(string nombre)
    {
        Assert.Throws<DomainException>(
            () => Asignatura.Crear(Guid.NewGuid(), nombre));
    }

    [Fact]
    public void CambiarNombre_ConNombreValido_ActualizaNombre()
    {
        Asignatura asignatura =
            Asignatura.Crear(Guid.NewGuid(), "Bases de Datos I");

        asignatura.CambiarNombre("  Bases de Datos II  ");

        Assert.Equal("Bases de Datos II", asignatura.Nombre);
    }

    [Fact]
    public void CambiarNombre_ConNombreVacio_LanzaDomainException()
    {
        Asignatura asignatura =
            Asignatura.Crear(Guid.NewGuid(), "Bases de Datos I");

        Assert.Throws<DomainException>(
            () => asignatura.CambiarNombre(" "));
    }
}
