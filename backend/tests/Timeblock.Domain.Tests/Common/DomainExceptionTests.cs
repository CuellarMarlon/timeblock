using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Common;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Matriculas;
using Timeblock.Domain.Personas;

namespace Timeblock.Domain.Tests.Common;

public sealed class DomainExceptionTests
{
    [Fact]
    public void Constructor_SoloConMensaje_EsErrorDeValidacion()
    {
        var excepcion = new DomainException("Mensaje");

        Assert.Equal("Mensaje", excepcion.Message);
        Assert.Equal(TipoErrorDominio.Validacion, excepcion.Tipo);
    }

    [Theory]
    [InlineData(TipoErrorDominio.Validacion)]
    [InlineData(TipoErrorDominio.AutorizacionNegocio)]
    [InlineData(TipoErrorDominio.NoEncontrado)]
    [InlineData(TipoErrorDominio.Conflicto)]
    public void Constructor_ConTipo_ConservaElTipo(TipoErrorDominio tipo)
    {
        var excepcion = new DomainException("Mensaje", tipo);

        Assert.Equal(tipo, excepcion.Tipo);
    }

    [Fact]
    public void Constructor_ConTipoInexistente_LanzaDomainException()
    {
        Assert.Throws<DomainException>(
            () => new DomainException("Mensaje", (TipoErrorDominio)99));
    }

    [Fact]
    public void IdentificadorVacio_EsErrorDeValidacion()
    {
        var excepcion = Assert.Throws<DomainException>(
            () => Asignatura.Crear(Guid.Empty, "Bases de Datos"));

        Assert.Equal(TipoErrorDominio.Validacion, excepcion.Tipo);
    }

    [Fact]
    public void AsignaturaRepetidaEnPrograma_EsConflicto()
    {
        var programa = ProgramaAcademico.Crear(
            Guid.NewGuid(),
            "Ingeniería Informática",
            "Plan 2024");
        Guid asignaturaId = Guid.NewGuid();
        programa.AsociarAsignatura(asignaturaId);

        var excepcion = Assert.Throws<DomainException>(
            () => programa.AsociarAsignatura(asignaturaId));

        Assert.Equal(TipoErrorDominio.Conflicto, excepcion.Tipo);
    }

    [Fact]
    public void MatriculaRepetida_EsConflicto()
    {
        var registro = new RegistroMatriculas();
        Guid estudianteId = Guid.NewGuid();
        Guid programaId = Guid.NewGuid();
        registro.Matricular(estudianteId, programaId);

        var excepcion = Assert.Throws<DomainException>(
            () => registro.Matricular(estudianteId, programaId));

        Assert.Equal(TipoErrorDominio.Conflicto, excepcion.Tipo);
    }

    [Fact]
    public void HorarioSinPermisoDeGeneracion_EsAutorizacionDeNegocio()
    {
        Guid coordinadorId = Guid.NewGuid();
        Guid programaId = Guid.NewGuid();
        var autorizacion = new AutorizacionCoordinador(
            coordinadorId,
            programaId,
            new DateOnly(2026, 9, 7),
            fechaFin: null,
            puedeGenerar: false,
            puedeValidar: false);

        var excepcion = Assert.Throws<DomainException>(() =>
            Horario.Crear(
                Guid.NewGuid(),
                new DateTimeOffset(2026, 10, 12, 14, 30, 0, TimeSpan.Zero),
                "2026-2027",
                programaId,
                coordinadorId,
                autorizacion));

        Assert.Equal(TipoErrorDominio.AutorizacionNegocio, excepcion.Tipo);
    }
}
