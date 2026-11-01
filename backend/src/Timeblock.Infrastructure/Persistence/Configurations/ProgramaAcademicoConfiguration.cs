using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Timeblock.Domain.Catalogo;

namespace Timeblock.Infrastructure.Persistence.Configurations;

public sealed class ProgramaAcademicoConfiguration
    : IEntityTypeConfiguration<ProgramaAcademico>
{
    public void Configure(EntityTypeBuilder<ProgramaAcademico> builder)
    {
        builder.ToTable("PROGRAMA_ACADEMICO");

        builder.HasKey(programa => programa.Id);

        builder.Property(programa => programa.Id)
            .HasColumnName("idPrograma")
            .ValueGeneratedNever();

        builder.Property(programa => programa.Nombre)
            .HasColumnName("nombrePrograma")
            .IsRequired();

        builder.Property(programa => programa.PlanEstudio)
            .HasColumnName("planEstudio")
            .IsRequired();

        builder.Navigation(programa => programa.Asignaturas)
            .HasField("_asignaturas")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}