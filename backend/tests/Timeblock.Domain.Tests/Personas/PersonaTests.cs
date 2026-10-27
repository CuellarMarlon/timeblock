using Timeblock.Domain.Common;
using Timeblock.Domain.Personas;

namespace Timeblock.Domain.Tests.Personas;

public class PersonaTests
{
    private static readonly Guid CoordinadorId = Guid.NewGuid();

    [Fact]
    public void Crear_EstudianteValido_ConservaDatosYPerfil()
    {
        var persona = Persona.Crear("  Ana Pérez ", new PerfilEstudiante(20, " C311 ", CoordinadorId));

        Assert.NotEqual(Guid.Empty, persona.Id);
        Assert.Equal("Ana Pérez", persona.Nombre);
        var perfil = Assert.IsType<PerfilEstudiante>(persona.Perfil);
        Assert.Equal(TipoPerfil.Estudiante, perfil.Tipo);
        Assert.Equal(20, perfil.Edad);
        Assert.Equal("C311", perfil.Grupo);
        Assert.Equal(CoordinadorId, perfil.CoordinadorId);
    }

    [Fact]
    public void Crear_Coordinador_ConservaEspecialidad()
    {
        var persona = Persona.Crear("Luis García", new PerfilCoordinador("Bases de datos"));

        var perfil = Assert.IsType<PerfilCoordinador>(persona.Perfil);
        Assert.Equal(TipoPerfil.Coordinador, perfil.Tipo);
        Assert.Equal("Bases de datos", perfil.Especialidad);
    }

    [Fact]
    public void Crear_JefeDepartamento_TienePerfilDeJefe()
    {
        var persona = Persona.Crear("María Rodríguez", new PerfilJefeDepartamento());

        Assert.Equal(TipoPerfil.JefeDepartamento, persona.Perfil.Tipo);
    }

    [Fact]
    public void Crear_ConIdExplicito_ConservaId()
    {
        var id = Guid.NewGuid();

        var persona = Persona.Crear(id, "Luis García", new PerfilCoordinador("Redes"));

        Assert.Equal(id, persona.Id);
    }

    [Fact]
    public void Crear_DosPersonas_GeneraIdsDistintos()
    {
        var primera = Persona.Crear("Uno", new PerfilJefeDepartamento());
        var segunda = Persona.Crear("Dos", new PerfilJefeDepartamento());

        Assert.NotEqual(primera.Id, segunda.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_NombreVacio_Falla(string? nombre)
    {
        Assert.Throws<DomainException>(() => Persona.Crear(nombre!, new PerfilJefeDepartamento()));
    }

    [Fact]
    public void Crear_IdVacio_Falla()
    {
        Assert.Throws<DomainException>(
            () => Persona.Crear(Guid.Empty, "Ana", new PerfilJefeDepartamento()));
    }

    [Fact]
    public void Crear_SinPerfil_Falla()
    {
        Assert.Throws<ArgumentNullException>(() => Persona.Crear("Ana", null!));
    }

    [Fact]
    public void Crear_EstudianteQueEsSuPropioCoordinador_Falla()
    {
        var id = Guid.NewGuid();

        Assert.Throws<DomainException>(
            () => Persona.Crear(id, "Ana", new PerfilEstudiante(20, "C311", id)));
    }

    [Fact]
    public void ActualizarNombre_Valido_CambiaNombre()
    {
        var persona = Persona.Crear("Ana", new PerfilJefeDepartamento());

        persona.ActualizarNombre(" Ana Pérez ");

        Assert.Equal("Ana Pérez", persona.Nombre);
    }

    [Fact]
    public void ActualizarNombre_Vacio_FallaYConservaNombre()
    {
        var persona = Persona.Crear("Ana", new PerfilJefeDepartamento());

        Assert.Throws<DomainException>(() => persona.ActualizarNombre(" "));
        Assert.Equal("Ana", persona.Nombre);
    }

    [Fact]
    public void ActualizarPerfil_MismoTipo_ReemplazaDatos()
    {
        var persona = Persona.Crear("Ana", new PerfilEstudiante(20, "C311", CoordinadorId));
        var nuevoCoordinador = Guid.NewGuid();

        persona.ActualizarPerfil(new PerfilEstudiante(21, "C312", nuevoCoordinador));

        var perfil = Assert.IsType<PerfilEstudiante>(persona.Perfil);
        Assert.Equal(21, perfil.Edad);
        Assert.Equal("C312", perfil.Grupo);
        Assert.Equal(nuevoCoordinador, perfil.CoordinadorId);
    }

    [Fact]
    public void ActualizarPerfil_OtroTipo_FallaYConservaPerfil()
    {
        var original = new PerfilEstudiante(20, "C311", CoordinadorId);
        var persona = Persona.Crear("Ana", original);

        Assert.Throws<DomainException>(
            () => persona.ActualizarPerfil(new PerfilCoordinador("Bases de datos")));
        Assert.Same(original, persona.Perfil);
    }

    [Fact]
    public void ActualizarPerfil_EstudianteConSuPropioId_Falla()
    {
        var persona = Persona.Crear("Ana", new PerfilEstudiante(20, "C311", CoordinadorId));

        Assert.Throws<DomainException>(
            () => persona.ActualizarPerfil(new PerfilEstudiante(20, "C311", persona.Id)));
    }

    [Fact]
    public void ActualizarPerfil_Nulo_Falla()
    {
        var persona = Persona.Crear("Ana", new PerfilJefeDepartamento());

        Assert.Throws<ArgumentNullException>(() => persona.ActualizarPerfil(null!));
    }
}
