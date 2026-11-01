using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Personas;

namespace Timeblock.Infrastructure.Persistence.Configurations;

public sealed class AutorizacionCoordinadorConfiguration
    : IEntityTypeConfiguration<AutorizacionCoordinador>
{
    public void Configure(EntityTypeBuilder<AutorizacionCoordinador> builder)
    {
        builder.ToTable("COORDINADOR_EN_PROGRAMA", table =>
            table.HasCheckConstraint(
                "CK_COORDINADOR_EN_PROGRAMA_Vigencia",
                "\"fechaFin\" IS NULL OR \"fechaFin\" >= \"fechaInicio\""));

        builder.HasKey(autorizacion => new
        {
            autorizacion.CoordinadorId,
            autorizacion.ProgramaId,
            autorizacion.FechaInicio
        });

        builder.Property(autorizacion => autorizacion.CoordinadorId)
            .HasColumnName("idCoordinador")
            .ValueGeneratedNever();
        builder.Property(autorizacion => autorizacion.ProgramaId)
            .HasColumnName("idPrograma")
            .ValueGeneratedNever();
        builder.Property(autorizacion => autorizacion.FechaInicio)
            .HasColumnName("fechaInicio")
            .ValueGeneratedNever();
        builder.Property(autorizacion => autorizacion.FechaFin)
            .HasColumnName("fechaFin");
        builder.Property(autorizacion => autorizacion.PuedeGenerar)
            .HasColumnName("puedeGenerar")
            .IsRequired();
        builder.Property(autorizacion => autorizacion.PuedeValidar)
            .HasColumnName("puedeValidar")
            .IsRequired();

        builder.HasOne<CoordinadorPerfilRecord>()
            .WithMany()
            .HasForeignKey(autorizacion => autorizacion.CoordinadorId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProgramaAcademico>()
            .WithMany()
            .HasForeignKey(autorizacion => autorizacion.ProgramaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FechaRecord>()
            .WithMany()
            .HasForeignKey(autorizacion => autorizacion.FechaInicio)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FechaRecord>()
            .WithMany()
            .HasForeignKey(autorizacion => autorizacion.FechaFin)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
