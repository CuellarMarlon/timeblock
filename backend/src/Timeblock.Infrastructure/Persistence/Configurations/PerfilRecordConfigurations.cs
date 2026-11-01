using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Timeblock.Infrastructure.Persistence.Configurations;

public sealed class EstudiantePerfilRecordConfiguration
    : IEntityTypeConfiguration<EstudiantePerfilRecord>
{
    public void Configure(EntityTypeBuilder<EstudiantePerfilRecord> builder)
    {
        builder.ToTable("ESTUDIANTE", table =>
            table.HasCheckConstraint("CK_ESTUDIANTE_EdadPositiva", "\"edad\" > 0"));

        builder.HasKey(estudiante => estudiante.IdEstudiante);

        builder.Property(estudiante => estudiante.IdEstudiante)
            .HasColumnName("idEstudiante")
            .ValueGeneratedNever();
        builder.Property(estudiante => estudiante.Edad)
            .HasColumnName("edad")
            .IsRequired();
        builder.Property(estudiante => estudiante.Grupo)
            .HasColumnName("grupo")
            .IsRequired();
        builder.Property(estudiante => estudiante.CoordinadorAsignadoId)
            .HasColumnName("idCoordinadorAsig")
            .IsRequired();

        builder.HasOne<PersonaRecord>()
            .WithOne()
            .HasForeignKey<EstudiantePerfilRecord>(estudiante => estudiante.IdEstudiante)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CoordinadorPerfilRecord>()
            .WithMany()
            .HasForeignKey(estudiante => estudiante.CoordinadorAsignadoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CoordinadorPerfilRecordConfiguration
    : IEntityTypeConfiguration<CoordinadorPerfilRecord>
{
    public void Configure(EntityTypeBuilder<CoordinadorPerfilRecord> builder)
    {
        builder.ToTable("COORDINADOR");
        builder.HasKey(coordinador => coordinador.IdCoordinador);

        builder.Property(coordinador => coordinador.IdCoordinador)
            .HasColumnName("idCoordinador")
            .ValueGeneratedNever();
        builder.Property(coordinador => coordinador.Especialidad)
            .HasColumnName("especialidad")
            .IsRequired();

        builder.HasOne<PersonaRecord>()
            .WithOne()
            .HasForeignKey<CoordinadorPerfilRecord>(coordinador => coordinador.IdCoordinador)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class JefeDepartamentoPerfilRecordConfiguration
    : IEntityTypeConfiguration<JefeDepartamentoPerfilRecord>
{
    public void Configure(EntityTypeBuilder<JefeDepartamentoPerfilRecord> builder)
    {
        builder.ToTable("JEFE_DEPTO");
        builder.HasKey(jefe => jefe.IdJefeDepartamento);

        builder.Property(jefe => jefe.IdJefeDepartamento)
            .HasColumnName("idJefeDepto")
            .ValueGeneratedNever();

        builder.HasOne<PersonaRecord>()
            .WithOne()
            .HasForeignKey<JefeDepartamentoPerfilRecord>(jefe => jefe.IdJefeDepartamento)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
