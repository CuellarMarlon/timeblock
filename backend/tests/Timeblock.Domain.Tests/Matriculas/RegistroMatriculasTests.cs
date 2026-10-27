using Timeblock.Domain.Common;
using Timeblock.Domain.Matriculas;

namespace Timeblock.Domain.Tests.Matriculas;

public class RegistroMatriculasTests
{
    private static readonly Guid Ana = Guid.NewGuid();
    private static readonly Guid Luis = Guid.NewGuid();
    private static readonly Guid Computacion = Guid.NewGuid();
    private static readonly Guid Matematica = Guid.NewGuid();

    [Fact]
    public void Matricular_Valida_AgregaMatricula()
    {
        var registro = new RegistroMatriculas();

        var matricula = registro.Matricular(Ana, Computacion);

        Assert.Equal(Ana, matricula.EstudianteId);
        Assert.Equal(Computacion, matricula.ProgramaId);
        Assert.Same(matricula, Assert.Single(registro.Matriculas));
        Assert.True(registro.EstaMatriculado(Ana, Computacion));
    }

    [Fact]
    public void Matricular_ParejaRepetida_FallaYNoDuplica()
    {
        var registro = new RegistroMatriculas();
        registro.Matricular(Ana, Computacion);

        Assert.Throws<DomainException>(() => registro.Matricular(Ana, Computacion));
        Assert.Single(registro.Matriculas);
    }

    [Fact]
    public void Matricular_MismoEstudianteEnOtroPrograma_EsValida()
    {
        var registro = new RegistroMatriculas();
        registro.Matricular(Ana, Computacion);

        registro.Matricular(Ana, Matematica);

        Assert.Equal(2, registro.Matriculas.Count);
    }

    [Fact]
    public void Matricular_OtroEstudianteEnMismoPrograma_EsValida()
    {
        var registro = new RegistroMatriculas();
        registro.Matricular(Ana, Computacion);

        registro.Matricular(Luis, Computacion);

        Assert.Equal(2, registro.Matriculas.Count);
    }

    [Fact]
    public void Matricular_IdsInvalidos_FallaYNoAgrega()
    {
        var registro = new RegistroMatriculas();

        Assert.Throws<DomainException>(() => registro.Matricular(Guid.Empty, Computacion));
        Assert.Throws<DomainException>(() => registro.Matricular(Ana, Guid.Empty));
        Assert.Empty(registro.Matriculas);
    }

    [Fact]
    public void Crear_ConExistentes_LasConserva()
    {
        var registro = new RegistroMatriculas(
        [
            new Matricula(Ana, Computacion),
            new Matricula(Luis, Computacion),
        ]);

        Assert.Equal(2, registro.Matriculas.Count);
        Assert.True(registro.EstaMatriculado(Luis, Computacion));
    }

    [Fact]
    public void Crear_ConExistentesRepetidas_Falla()
    {
        Assert.Throws<DomainException>(() => new RegistroMatriculas(
        [
            new Matricula(Ana, Computacion),
            new Matricula(Ana, Computacion),
        ]));
    }

    [Fact]
    public void Crear_ConExistentesNulas_Falla()
    {
        Assert.Throws<ArgumentNullException>(() => new RegistroMatriculas(null!));
    }

    [Fact]
    public void EstaMatriculado_SinMatricula_DevuelveFalso()
    {
        var registro = new RegistroMatriculas();
        registro.Matricular(Ana, Computacion);

        Assert.False(registro.EstaMatriculado(Ana, Matematica));
        Assert.False(registro.EstaMatriculado(Luis, Computacion));
    }

    [Fact]
    public void DeEstudiante_DevuelveSoloSusMatriculas()
    {
        var registro = new RegistroMatriculas();
        registro.Matricular(Ana, Computacion);
        registro.Matricular(Ana, Matematica);
        registro.Matricular(Luis, Computacion);

        var deAna = registro.DeEstudiante(Ana);

        Assert.Equal(2, deAna.Count);
        Assert.All(deAna, m => Assert.Equal(Ana, m.EstudianteId));
        Assert.Equal(
            new[] { Computacion, Matematica }.OrderBy(id => id),
            deAna.Select(m => m.ProgramaId).OrderBy(id => id));
    }

    [Fact]
    public void DePrograma_DevuelveSoloSusMatriculas()
    {
        var registro = new RegistroMatriculas();
        registro.Matricular(Ana, Computacion);
        registro.Matricular(Luis, Computacion);
        registro.Matricular(Ana, Matematica);

        var deComputacion = registro.DePrograma(Computacion);

        Assert.Equal(2, deComputacion.Count);
        Assert.All(deComputacion, m => Assert.Equal(Computacion, m.ProgramaId));
        Assert.Equal(
            new[] { Ana, Luis }.OrderBy(id => id),
            deComputacion.Select(m => m.EstudianteId).OrderBy(id => id));
    }

    [Fact]
    public void Consultas_SinCoincidencias_DevuelvenVacio()
    {
        var registro = new RegistroMatriculas();
        registro.Matricular(Ana, Computacion);

        Assert.Empty(registro.DeEstudiante(Luis));
        Assert.Empty(registro.DePrograma(Matematica));
    }
}
