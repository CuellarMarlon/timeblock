using Timeblock.Domain.Common;
using Timeblock.Domain.Matriculas;

namespace Timeblock.Domain.Tests.Matriculas;

public class MatriculaTests
{
    [Fact]
    public void Crear_Valida_ConservaEstudianteYPrograma()
    {
        var estudianteId = Guid.NewGuid();
        var programaId = Guid.NewGuid();

        var matricula = new Matricula(estudianteId, programaId);

        Assert.Equal(estudianteId, matricula.EstudianteId);
        Assert.Equal(programaId, matricula.ProgramaId);
    }

    [Fact]
    public void Crear_EstudianteVacio_Falla()
    {
        Assert.Throws<DomainException>(() => new Matricula(Guid.Empty, Guid.NewGuid()));
    }

    [Fact]
    public void Crear_ProgramaVacio_Falla()
    {
        Assert.Throws<DomainException>(() => new Matricula(Guid.NewGuid(), Guid.Empty));
    }
}
