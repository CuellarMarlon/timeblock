using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Timeblock.Infrastructure.Persistence.Configurations;

public sealed class FechaRecordConfiguration : IEntityTypeConfiguration<FechaRecord>
{
    public void Configure(EntityTypeBuilder<FechaRecord> builder)
    {
        builder.ToTable("FECHA");
        builder.HasKey(fecha => fecha.Fecha);
        builder.Property(fecha => fecha.Fecha)
            .HasColumnName("fecha")
            .ValueGeneratedNever();
    }
}

public sealed class AulaRecordConfiguration : IEntityTypeConfiguration<AulaRecord>
{
    public void Configure(EntityTypeBuilder<AulaRecord> builder)
    {
        builder.ToTable("AULA", table =>
            table.HasCheckConstraint("CK_AULA_CapacidadPositiva", "\"capacidad\" > 0"));

        builder.HasKey(aula => aula.IdAula);
        builder.Property(aula => aula.IdAula)
            .HasColumnName("idAula")
            .ValueGeneratedNever();
        builder.Property(aula => aula.Edificio)
            .HasColumnName("edificio")
            .IsRequired();
        builder.Property(aula => aula.Capacidad)
            .HasColumnName("capacidad")
            .IsRequired();
    }
}
