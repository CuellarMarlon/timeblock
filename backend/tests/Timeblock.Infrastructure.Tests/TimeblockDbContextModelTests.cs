using Microsoft.EntityFrameworkCore;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Matriculas;
using Timeblock.Infrastructure.Persistence;

namespace Timeblock.Infrastructure.Tests;

public sealed class TimeblockDbContextModelTests
{
    [Fact]
    public void Modelo_EstableceTablasYClavesCompuestasDeM4()
    {
        var options = new DbContextOptionsBuilder<TimeblockDbContext>()
            .UseNpgsql(
                "Host=localhost;Database=timeblock_model_only;Username=unused;Password=unused")
            .Options;

        using var context = new TimeblockDbContext(options);
        var model = context.Model;

        var matricula = model.FindEntityType(typeof(Matricula));
        Assert.Equal("MATRICULA", matricula?.GetTableName());
        Assert.Equal(
            new[] { nameof(Matricula.EstudianteId), nameof(Matricula.ProgramaId) },
            matricula?.FindPrimaryKey()?.Properties.Select(property => property.Name));

        var programaAsignatura = model.FindEntityType(typeof(ProgramaAsignatura));
        Assert.Equal("PROGRAMA_ASIGNATURA", programaAsignatura?.GetTableName());
        Assert.Equal(
            new[] { nameof(ProgramaAsignatura.ProgramaId), nameof(ProgramaAsignatura.AsignaturaId) },
            programaAsignatura?.FindPrimaryKey()?.Properties.Select(property => property.Name));

        var bloqueEnHorario = model.FindEntityType(typeof(BloqueEnHorario));
        Assert.Equal("BLOQUE_EN_HORARIO", bloqueEnHorario?.GetTableName());
        Assert.Equal(
            new[]
            {
                nameof(BloqueEnHorario.HorarioId),
                nameof(BloqueEnHorario.BloqueId),
                nameof(BloqueEnHorario.Fecha)
            },
            bloqueEnHorario?.FindPrimaryKey()?.Properties.Select(property => property.Name));

        Assert.Equal(
            "HORARIO",
            model.FindEntityType(typeof(Horario))?.GetTableName());
        Assert.Equal(
            "PROGRAMA_ACADEMICO",
            model.FindEntityType(typeof(ProgramaAcademico))?.GetTableName());

        var esquemaLlave = model.FindEntityType(typeof(EsquemaLlaveRecord));
        Assert.Equal("ESQUEMA_LLAVE", esquemaLlave?.GetTableName());
        Assert.Equal(9, esquemaLlave?.FindPrimaryKey()?.Properties.Count);
    }
}
