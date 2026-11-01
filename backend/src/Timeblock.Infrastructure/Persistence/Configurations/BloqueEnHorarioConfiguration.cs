using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Horarios;

namespace Timeblock.Infrastructure.Persistence.Configurations;

public sealed class BloqueEnHorarioConfiguration
    : IEntityTypeConfiguration<BloqueEnHorario>
{
    public void Configure(EntityTypeBuilder<BloqueEnHorario> builder)
    {
        builder.ToTable("BLOQUE_EN_HORARIO", table =>
            table.HasCheckConstraint(
                "CK_BLOQUE_EN_HORARIO_ImparticionCompleta",
                "(\"horaInicio\" IS NULL AND \"horaFin\" IS NULL AND \"periodoImp\" IS NULL AND \"idAula\" IS NULL) OR (\"horaInicio\" IS NOT NULL AND \"horaFin\" IS NOT NULL AND \"periodoImp\" IS NOT NULL AND btrim(\"periodoImp\") <> '' AND \"idAula\" IS NOT NULL AND \"horaInicio\" < \"horaFin\")"));

        builder.HasKey(asociacion => new
        {
            asociacion.HorarioId,
            asociacion.BloqueId,
            asociacion.Fecha
        });

        builder.Property(asociacion => asociacion.HorarioId)
            .HasColumnName("idHorario")
            .ValueGeneratedNever();
        builder.Property(asociacion => asociacion.BloqueId)
            .HasColumnName("idBloque")
            .ValueGeneratedNever();
        builder.Property(asociacion => asociacion.Fecha)
            .HasColumnName("fecha")
            .ValueGeneratedNever();
        builder.Property(asociacion => asociacion.HoraInicio)
            .HasColumnName("horaInicio");
        builder.Property(asociacion => asociacion.HoraFin)
            .HasColumnName("horaFin");
        builder.Property(asociacion => asociacion.PeriodoImp)
            .HasColumnName("periodoImp");
        builder.Property(asociacion => asociacion.AulaId)
            .HasColumnName("idAula");

        builder.HasOne<Horario>()
            .WithMany(horario => horario.Bloques)
            .HasForeignKey(asociacion => asociacion.HorarioId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<BloqueContenido>()
            .WithMany()
            .HasForeignKey(asociacion => asociacion.BloqueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<FechaRecord>()
            .WithMany()
            .HasForeignKey(asociacion => asociacion.Fecha)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AulaRecord>()
            .WithMany()
            .HasForeignKey(asociacion => asociacion.AulaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(asociacion => new
        {
            asociacion.AulaId,
            asociacion.Fecha,
            asociacion.HoraInicio
        })
            .IsUnique()
            .HasFilter("\"idAula\" IS NOT NULL AND \"horaInicio\" IS NOT NULL")
            .HasDatabaseName("UX_BLOQUE_EN_HORARIO_AulaFechaHoraInicio");
    }
}
