using Timeblock.Domain.Common;
using Timeblock.Domain.Catalogo;

namespace Timeblock.Domain.Tests.Catalogo;

public sealed class ProgramaAsignaturaTests
{
    [Fact]
    public void Crear_ConIdsValidos_CreaAsociacion()
    {
        Guid programaId = Guid.NewGuid();
        Guid asignaturaId = Guid.NewGuid();

        ProgramaAsignatura asociacion =
            ProgramaAsignatura.Crear(programaId, asignaturaId);

        Assert.Equal(programaId, asociacion.ProgramaId);
        Assert.Equal(asignaturaId, asociacion.AsignaturaId);
    }

    [Fact]
    public void Crear_ConProgramaIdVacio_LanzaDomainException()
    {
        Assert.Throws<DomainException>(
            () => ProgramaAsignatura.Crear(
                Guid.Empty,
                Guid.NewGuid()));
    }

    [Fact]
    public void Crear_ConAsignaturaIdVacio_LanzaDomainException()
    {
        Assert.Throws<DomainException>(
            () => ProgramaAsignatura.Crear(
                Guid.NewGuid(),
                Guid.Empty));
    }
}
