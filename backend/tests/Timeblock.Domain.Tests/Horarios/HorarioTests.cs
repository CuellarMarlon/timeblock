using Timeblock.Domain.Bloques;
using Timeblock.Domain.Common;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Personas;
using Timeblock.Domain.Catalogo;

namespace Timeblock.Domain.Tests.Horarios;

public sealed class HorarioTests
{
    private static readonly DateTimeOffset FechaCreacion =
        new(2026, 10, 12, 14, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Crear_ConAutorizacionValida_ConservaDatosYEstadoGenerado()
    {
        Guid programaId = Guid.NewGuid();
        Guid coordinadorId = Guid.NewGuid();
        DateOnly inicioAutorizacion = new(2026, 9, 7);

        var autorizacion = CrearAutorizacion(
            coordinadorId,
            programaId,
            inicioAutorizacion);

        Horario horario = Horario.Crear(
            Guid.NewGuid(),
            FechaCreacion,
            "2026-2027",
            programaId,
            coordinadorId,
            autorizacion);

        Assert.Equal(FechaCreacion, horario.FechaCreacion);
        Assert.Equal("2026-2027", horario.PeriodoAcademico);
        Assert.Equal(programaId, horario.ProgramaId);
        Assert.Equal(coordinadorId, horario.CoordinadorId);
        Assert.Equal(inicioAutorizacion, horario.FechaInicioAutorizacion);
        Assert.Equal(EstadoHorario.Generado, horario.Estado);
        Assert.Empty(horario.Bloques);
    }

    [Fact]
    public void Crear_ConAutorizacionDeOtroPrograma_LanzaDomainException()
    {
        Guid coordinadorId = Guid.NewGuid();
        Guid programaId = Guid.NewGuid();

        var autorizacion = CrearAutorizacion(
            coordinadorId,
            Guid.NewGuid(),
            new DateOnly(2026, 9, 7));

        Assert.Throws<DomainException>(() =>
            Horario.Crear(
                Guid.NewGuid(),
                FechaCreacion,
                "2026-2027",
                programaId,
                coordinadorId,
                autorizacion));
    }

    [Fact]
    public void Crear_SinPermisoDeGeneracion_LanzaDomainException()
    {
        Guid coordinadorId = Guid.NewGuid();
        Guid programaId = Guid.NewGuid();

        var autorizacion = new AutorizacionCoordinador(
            coordinadorId,
            programaId,
            new DateOnly(2026, 9, 7),
            null,
            puedeGenerar: false,
            puedeValidar: false);

        Assert.Throws<DomainException>(() =>
            Horario.Crear(
                Guid.NewGuid(),
                FechaCreacion,
                "2026-2027",
                programaId,
                coordinadorId,
                autorizacion));
    }

    [Fact]
    public void AgregarBloque_AsociadoAlPrograma_AgregaFechaConImparticionVacia()
    {
        (Horario horario, BloqueContenido bloque) =
            CrearHorarioYBloqueAsociado();

        DateOnly fecha = new(2026, 10, 20);

        horario.AgregarBloque(bloque, fecha);

        BloqueEnHorario asociacion = Assert.Single(horario.Bloques);
        Assert.Equal(bloque.Id, asociacion.BloqueId);
        Assert.Equal(fecha, asociacion.Fecha);
        Assert.Null(asociacion.HoraInicio);
        Assert.Null(asociacion.HoraFin);
        Assert.Null(asociacion.PeriodoImp);
        Assert.Null(asociacion.AulaId);
    }

    [Fact]
    public void AgregarBloque_MismaCombinacionHorarioBloqueFecha_LanzaDomainException()
    {
        (Horario horario, BloqueContenido bloque) =
            CrearHorarioYBloqueAsociado();

        DateOnly fecha = new(2026, 10, 20);
        horario.AgregarBloque(bloque, fecha);

        Assert.Throws<DomainException>(() =>
            horario.AgregarBloque(bloque, fecha));

        Assert.Single(horario.Bloques);
    }

    [Fact]
    public void AgregarBloque_MismoBloqueEnOtraFecha_PermiteLaAsociacion()
    {
        (Horario horario, BloqueContenido bloque) =
            CrearHorarioYBloqueAsociado();

        horario.AgregarBloque(bloque, new DateOnly(2026, 10, 20));
        horario.AgregarBloque(bloque, new DateOnly(2026, 10, 22));

        Assert.Equal(2, horario.Bloques.Count);
    }

    [Fact]
    public void AgregarBloque_NoAsociadoAlPrograma_LanzaDomainException()
    {
        (Horario horario, BloqueContenido bloque) =
            CrearHorarioYBloqueAsociado();

        BloqueContenido bloqueDeOtroPrograma = BloqueContenido.Crear(
            Guid.NewGuid(),
            "Bloque no asociado",
            TipoActividad.Teoria,
            NivelComplejidad.Medio,
            bloque.AsignaturaId,
            Guid.NewGuid());

        Assert.Throws<DomainException>(() =>
            horario.AgregarBloque(
                bloqueDeOtroPrograma,
                new DateOnly(2026, 10, 20)));

        Assert.Empty(horario.Bloques);
    }

    private static AutorizacionCoordinador CrearAutorizacion(
        Guid coordinadorId,
        Guid programaId,
        DateOnly fechaInicio) =>
        new(
            coordinadorId,
            programaId,
            fechaInicio,
            fechaFin: null,
            puedeGenerar: true,
            puedeValidar: false);

    private static (Horario Horario, BloqueContenido Bloque)
        CrearHorarioYBloqueAsociado()
    {
        Guid programaId = Guid.NewGuid();
        Guid coordinadorId = Guid.NewGuid();
        Guid asignaturaId = Guid.NewGuid();

        var programa = ProgramaAcademico.Crear(
            programaId,
            "Ingeniería Informática",
            "Plan 2024");

        programa.AsociarAsignatura(asignaturaId);

        BloqueContenido bloque = BloqueContenido.Crear(
            Guid.NewGuid(),
            "Introducción",
            TipoActividad.Teoria,
            NivelComplejidad.Medio,
            asignaturaId,
            coordinadorId);

        bloque.AsociarPrograma(programa);

        var autorizacion = CrearAutorizacion(
            coordinadorId,
            programaId,
            new DateOnly(2026, 9, 7));

        Horario horario = Horario.Crear(
            Guid.NewGuid(),
            FechaCreacion,
            "2026-2027",
            programaId,
            coordinadorId,
            autorizacion);

        return (horario, bloque);
    }
}