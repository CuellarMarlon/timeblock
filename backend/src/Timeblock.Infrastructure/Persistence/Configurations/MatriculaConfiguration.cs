using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Matriculas;

namespace Timeblock.Infrastructure.Persistence.Configurations;

public sealed class MatriculaConfiguration : IEntityTypeConfiguration<Matricula>
{
    public void Configure(EntityTypeBuilder<Matricula> builder)
    {
        builder.ToTable("MATRICULA");
        builder.HasKey(matricula => new
        {
            matricula.EstudianteId,
            matricula.ProgramaId
        });

        builder.Property(matricula => matricula.EstudianteId)
            .HasColumnName("idEstudiante")
            .ValueGeneratedNever();
        builder.Property(matricula => matricula.ProgramaId)
            .HasColumnName("idPrograma")
            .ValueGeneratedNever();

        builder.HasOne<EstudiantePerfilRecord>()
            .WithMany()
            .HasForeignKey(matricula => matricula.EstudianteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProgramaAcademico>()
            .WithMany()
            .HasForeignKey(matricula => matricula.ProgramaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
