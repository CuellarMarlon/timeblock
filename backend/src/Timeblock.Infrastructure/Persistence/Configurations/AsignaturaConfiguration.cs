using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Timeblock.Domain.Catalogo;

namespace Timeblock.Infrastructure.Persistence.Configurations;

public sealed class AsignaturaConfiguration
    : IEntityTypeConfiguration<Asignatura>
{
    public void Configure(EntityTypeBuilder<Asignatura> builder)
    {
        builder.ToTable("ASIGNATURA");

        builder.HasKey(asignatura => asignatura.Id);

        builder.Property(asignatura => asignatura.Id)
            .HasColumnName("idAsignatura")
            .ValueGeneratedNever();

        builder.Property(asignatura => asignatura.Nombre)
            .HasColumnName("nombreAsignatura")
            .IsRequired();
    }
}