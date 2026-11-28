using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Common;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Personas;

namespace Timeblock.Domain.Tests.Horarios;

/// <summary>
/// Imparticiones completas: hora de inicio, hora de fin, período y aula, y
/// clases sin solapar dentro de un mismo horario.
/// </summary>
public sealed class ImparticionesHorarioTests
{
    private static readonly DateOnly Lunes = new(2026, 10, 19);
    private static readonly Guid AulaA = Guid.NewGuid();
    private static readonly Guid AulaB = Guid.NewGuid();

    private static ProgramacionImparticion De(int inicio, int fin, Guid aula) =>
        new(new TimeOnly(inicio, 0), new TimeOnly(fin, 0), aula);

    [Fact]
    public void AgregarBloque_ConProgramacion_GuardaHorasAulaYElPeriodoDelHorario()
    {
        (Horario horario, BloqueContenido teoria, _) = CrearHorario();

        horario.AgregarBloque(teoria, Lunes, De(8, 10, AulaA));

        BloqueEnHorario imparticion = Assert.Single(horario.Bloques);
        Assert.True(imparticion.EstaCompleta);
        Assert.Equal(new TimeOnly(8, 0), imparticion.HoraInicio);
        Assert.Equal(new TimeOnly(10, 0), imparticion.HoraFin);
        Assert.Equal(AulaA, imparticion.AulaId);
        Assert.Equal(horario.PeriodoAcademico, imparticion.PeriodoImp);
    }

    [Theory]
    [InlineData(8, 10, 9, 11)]
    [InlineData(8, 10, 7, 9)]
    [InlineData(8, 12, 9, 10)]
    [InlineData(8, 10, 8, 10)]
    public void AgregarBloque_SolapadoEnElMismoHorario_EsConflictoAunqueCambieElAula(
        int inicio1,
        int fin1,
        int inicio2,
        int fin2)
    {
        (Horario horario, BloqueContenido teoria, BloqueContenido practica) = CrearHorario();
        horario.AgregarBloque(teoria, Lunes, De(inicio1, fin1, AulaA));

        var error = Assert.Throws<DomainException>(
            () => horario.AgregarBloque(practica, Lunes, De(inicio2, fin2, AulaB)));

        Assert.Equal(TipoErrorDominio.Conflicto, error.Tipo);
        Assert.Single(horario.Bloques);
    }

    [Fact]
    public void AgregarBloque_Contiguo_SePermite()
    {
        (Horario horario, BloqueContenido teoria, BloqueContenido practica) = CrearHorario();

        horario.AgregarBloque(teoria, Lunes, De(8, 10, AulaA));
        horario.AgregarBloque(practica, Lunes, De(10, 12, AulaA));

        Assert.Equal(2, horario.Bloques.Count);
    }

    [Fact]
    public void AgregarBloque_MismaFranjaEnOtraFecha_SePermite()
    {
        (Horario horario, BloqueContenido teoria, BloqueContenido practica) = CrearHorario();

        horario.AgregarBloque(teoria, Lunes, De(8, 10, AulaA));
        horario.AgregarBloque(practica, Lunes.AddDays(1), De(8, 10, AulaA));

        Assert.Equal(2, horario.Bloques.Count);
    }

    [Fact]
    public void AgregarBloque_SinProgramacion_NoSeComparaConLasCompletas()
    {
        (Horario horario, BloqueContenido teoria, BloqueContenido practica) = CrearHorario();

        horario.AgregarBloque(teoria, Lunes, De(8, 10, AulaA));
        horario.AgregarBloque(practica, Lunes);

        Assert.Equal(2, horario.Bloques.Count);
        Assert.False(horario.Bloques.Single(b => b.BloqueId == practica.Id).EstaCompleta);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(10, 8)]
    public void AgregarBloque_ConIntervaloVacioOInvertido_EsValidacion(int inicio, int fin)
    {
        (Horario horario, BloqueContenido teoria, _) = CrearHorario();

        var error = Assert.Throws<DomainException>(
            () => horario.AgregarBloque(teoria, Lunes, De(inicio, fin, AulaA)));

        Assert.Equal(TipoErrorDominio.Validacion, error.Tipo);
        Assert.Empty(horario.Bloques);
    }

    [Fact]
    public void AgregarBloque_ConAulaVacia_EsValidacion()
    {
        (Horario horario, BloqueContenido teoria, _) = CrearHorario();

        var error = Assert.Throws<DomainException>(
            () => horario.AgregarBloque(teoria, Lunes, De(8, 10, Guid.Empty)));

        Assert.Equal(TipoErrorDominio.Validacion, error.Tipo);
    }

    private static (Horario Horario, BloqueContenido Teoria, BloqueContenido Practica) CrearHorario()
    {
        Guid coordinadorId = Guid.NewGuid();
        Guid asignaturaId = Guid.NewGuid();
        var programa = ProgramaAcademico.Crear(Guid.NewGuid(), "Ingeniería Informática", "Plan 2024");
        programa.AsociarAsignatura(asignaturaId);

        BloqueContenido teoria = BloqueContenido.Crear(
            Guid.NewGuid(), "Introducción", TipoActividad.Teoria, NivelComplejidad.Medio,
            asignaturaId, coordinadorId);
        BloqueContenido practica = BloqueContenido.Crear(
            Guid.NewGuid(), "Consultas", TipoActividad.Practica, NivelComplejidad.Alto,
            asignaturaId, coordinadorId);
        teoria.AsociarPrograma(programa);
        practica.AsociarPrograma(programa);

        var autorizacion = new AutorizacionCoordinador(
            coordinadorId, programa.Id, new DateOnly(2026, 9, 7), fechaFin: null,
            puedeGenerar: true, puedeValidar: false);
        Horario horario = Horario.Crear(
            Guid.NewGuid(), new DateTimeOffset(2026, 10, 12, 14, 30, 0, TimeSpan.Zero),
            "2026-2027", programa.Id, coordinadorId, autorizacion);

        return (horario, teoria, practica);
    }
}
