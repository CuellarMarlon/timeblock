using Timeblock.Domain.Common;
using Timeblock.Domain.Catalogo;

namespace Timeblock.Domain.Tests.Catalogo;

public sealed class ProgramaAcademicoTests
{
    [Fact]
    public void Crear_ConDatosValidos_CreaPrograma()
    {
        Guid id = Guid.NewGuid();

        ProgramaAcademico programa = ProgramaAcademico.Crear(
            id,
            "  Ingeniería Informática  ",
            "  Plan 2024  ");

        Assert.Equal(id, programa.Id);
        Assert.Equal("Ingeniería Informática", programa.Nombre);
        Assert.Equal("Plan 2024", programa.PlanEstudio);
        Assert.Empty(programa.Asignaturas);
    }

    [Fact]
    public void Crear_ConIdVacio_LanzaDomainException()
    {
        Assert.Throws<DomainException>(
            () => ProgramaAcademico.Crear(
                Guid.Empty,
                "Ingeniería Informática",
                "Plan 2024"));
    }

    [Theory]
    [InlineData("", "Plan 2024")]
    [InlineData("Ingeniería Informática", " ")]
    public void Crear_ConTextoObligatorioVacio_LanzaDomainException(
        string nombre,
        string planEstudio)
    {
        Assert.Throws<DomainException>(
            () => ProgramaAcademico.Crear(
                Guid.NewGuid(),
                nombre,
                planEstudio));
    }

    [Fact]
    public void AsociarAsignatura_ConIdValido_AgregaAsociacion()
    {
        Guid programaId = Guid.NewGuid();
        Guid asignaturaId = Guid.NewGuid();

        ProgramaAcademico programa = ProgramaAcademico.Crear(
            programaId,
            "Ingeniería Informática",
            "Plan 2024");

        programa.AsociarAsignatura(asignaturaId);

        ProgramaAsignatura asociacion =
            Assert.Single(programa.Asignaturas);

        Assert.Equal(programaId, asociacion.ProgramaId);
        Assert.Equal(asignaturaId, asociacion.AsignaturaId);
    }

    [Fact]
    public void AsociarAsignatura_ConIdVacio_LanzaDomainException()
    {
        ProgramaAcademico programa = ProgramaAcademico.Crear(
            Guid.NewGuid(),
            "Ingeniería Informática",
            "Plan 2024");

        Assert.Throws<DomainException>(
            () => programa.AsociarAsignatura(Guid.Empty));
    }

    [Fact]
    public void AsociarAsignatura_DosVeces_LanzaDomainException()
    {
        Guid asignaturaId = Guid.NewGuid();

        ProgramaAcademico programa = ProgramaAcademico.Crear(
            Guid.NewGuid(),
            "Ingeniería Informática",
            "Plan 2024");

        programa.AsociarAsignatura(asignaturaId);

        Assert.Throws<DomainException>(
            () => programa.AsociarAsignatura(asignaturaId));
    }

    [Fact]
    public void AsociarAsignaturasDistintas_AmbasQuedanAsociadas()
    {
        ProgramaAcademico programa = ProgramaAcademico.Crear(
            Guid.NewGuid(),
            "Ingeniería Informática",
            "Plan 2024");

        Guid primeraAsignaturaId = Guid.NewGuid();
        Guid segundaAsignaturaId = Guid.NewGuid();

        programa.AsociarAsignatura(primeraAsignaturaId);
        programa.AsociarAsignatura(segundaAsignaturaId);

        Assert.Equal(2, programa.Asignaturas.Count);
    }

    [Fact]
    public void ActualizarDatos_ConDatosValidos_ActualizaNombreYPlan()
    {
        ProgramaAcademico programa = ProgramaAcademico.Crear(
            Guid.NewGuid(),
            "Ingeniería Informática",
            "Plan 2024");

        programa.ActualizarDatos(
            "Ingeniería de Software",
            "Plan 2026");

        Assert.Equal("Ingeniería de Software", programa.Nombre);
        Assert.Equal("Plan 2026", programa.PlanEstudio);
    }

    [Fact]
    public void ActualizarDatos_ConPlanVacio_NoModificaElPrograma()
    {
        ProgramaAcademico programa = ProgramaAcademico.Crear(
            Guid.NewGuid(),
            "Ingeniería Informática",
            "Plan 2024");

        Assert.Throws<DomainException>(
            () => programa.ActualizarDatos(
                "Ingeniería de Software",
                " "));

        Assert.Equal("Ingeniería Informática", programa.Nombre);
        Assert.Equal("Plan 2024", programa.PlanEstudio);
    }
}
