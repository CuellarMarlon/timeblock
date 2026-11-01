using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Timeblock.Domain.Catalogo;

namespace Timeblock.Infrastructure.Persistence.Configurations;

public sealed class ProgramaAsignaturaConfiguration
    : IEntityTypeConfiguration<ProgramaAsignatura>
{
    public void Configure(EntityTypeBuilder<ProgramaAsignatura> builder)
    {
        builder.ToTable("PROGRAMA_ASIGNATURA");

        builder.HasKey(asociacion => new
        {
            asociacion.ProgramaId,
            asociacion.AsignaturaId
        });

        builder.Property(asociacion => asociacion.ProgramaId)
            .HasColumnName("idPrograma")
            .ValueGeneratedNever();

        builder.Property(asociacion => asociacion.AsignaturaId)
            .HasColumnName("idAsignatura")
            .ValueGeneratedNever();

        builder.HasOne<ProgramaAcademico>()
            .WithMany(programa => programa.Asignaturas)
            .HasForeignKey(asociacion => asociacion.ProgramaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Asignatura>()
            .WithMany()
            .HasForeignKey(asociacion => asociacion.AsignaturaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}