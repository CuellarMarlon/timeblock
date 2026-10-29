using Timeblock.Domain.Common;
using Timeblock.Domain.Horarios;

namespace Timeblock.Domain.Tests.Horarios;

public sealed class BloqueEnHorarioTests
{
    [Fact]
    public void Crear_ConDatosM4_GuardaFechaYDejaImparticionNula()
    {
        Guid horarioId = Guid.NewGuid();
        Guid bloqueId = Guid.NewGuid();
        DateOnly fecha = new(2026, 10, 20);

        BloqueEnHorario asociacion =
            BloqueEnHorario.Crear(horarioId, bloqueId, fecha);

        Assert.Equal(horarioId, asociacion.HorarioId);
        Assert.Equal(bloqueId, asociacion.BloqueId);
        Assert.Equal(fecha, asociacion.Fecha);
        Assert.Null(asociacion.HoraInicio);
        Assert.Null(asociacion.HoraFin);
        Assert.Null(asociacion.PeriodoImp);
        Assert.Null(asociacion.AulaId);
    }

    [Fact]
    public void Crear_ConProgramacionCompletaYOrdenValido_EsValida()
    {
        BloqueEnHorario asociacion = BloqueEnHorario.Crear(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 10, 20),
            new TimeOnly(8, 0),
            new TimeOnly(10, 0),
            "mañana",
            Guid.NewGuid());

        Assert.Equal(new TimeOnly(8, 0), asociacion.HoraInicio);
        Assert.Equal(new TimeOnly(10, 0), asociacion.HoraFin);
        Assert.Equal("mañana", asociacion.PeriodoImp);
        Assert.NotNull(asociacion.AulaId);
    }

    [Fact]
    public void Crear_ConProgramacionParcial_LanzaDomainException()
    {
        Assert.Throws<DomainException>(() =>
            BloqueEnHorario.Crear(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(2026, 10, 20),
                horaInicio: new TimeOnly(8, 0)));
    }

    [Fact]
    public void Crear_ConHoraInicioPosteriorOIgualALaFinal_LanzaDomainException()
    {
        Assert.Throws<DomainException>(() =>
            BloqueEnHorario.Crear(
                Guid.NewGuid(),
                Guid.NewGuid(),
                new DateOnly(2026, 10, 20),
                new TimeOnly(10, 0),
                new TimeOnly(10, 0),
                "mañana",
                Guid.NewGuid()));
    }
}