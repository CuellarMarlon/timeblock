using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;

namespace Timeblock.Infrastructure.Persistence.Configurations;

public sealed class BloqueProgramaConfiguration
    : IEntityTypeConfiguration<BloquePrograma>
{
    public void Configure(EntityTypeBuilder<BloquePrograma> builder)
    {
        builder.ToTable("BLOQUE_PROGRAMA");

        builder.HasKey(asociacion => new
        {
            asociacion.BloqueId,
            asociacion.ProgramaId
        });

        builder.Property(asociacion => asociacion.BloqueId)
            .HasColumnName("idBloque")
            .ValueGeneratedNever();
        builder.Property(asociacion => asociacion.ProgramaId)
            .HasColumnName("idPrograma")
            .ValueGeneratedNever();

        builder.HasOne<BloqueContenido>()
            .WithMany(bloque => bloque.Programas)
            .HasForeignKey(asociacion => asociacion.BloqueId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ProgramaAcademico>()
            .WithMany()
            .HasForeignKey(asociacion => asociacion.ProgramaId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
