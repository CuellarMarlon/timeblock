using Timeblock.Domain.Common;
using Timeblock.Domain.Personas;

namespace Timeblock.Domain.Tests.Personas;

public class PerfilTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Estudiante_EdadNoPositiva_Falla(int edad)
    {
        Assert.Throws<DomainException>(() => new PerfilEstudiante(edad, "C311", Guid.NewGuid()));
    }

    [Fact]
    public void Estudiante_EdadMinimaPositiva_EsValida()
    {
        var perfil = new PerfilEstudiante(1, "C311", Guid.NewGuid());

        Assert.Equal(1, perfil.Edad);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Estudiante_GrupoVacio_Falla(string? grupo)
    {
        Assert.Throws<DomainException>(() => new PerfilEstudiante(20, grupo!, Guid.NewGuid()));
    }

    [Fact]
    public void Estudiante_SinCoordinador_Falla()
    {
        Assert.Throws<DomainException>(() => new PerfilEstudiante(20, "C311", Guid.Empty));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Coordinador_EspecialidadVacia_Falla(string? especialidad)
    {
        Assert.Throws<DomainException>(() => new PerfilCoordinador(especialidad!));
    }

    [Fact]
    public void Coordinador_Especialidad_SeRecorta()
    {
        var perfil = new PerfilCoordinador("  Bases de datos ");

        Assert.Equal("Bases de datos", perfil.Especialidad);
    }
}
