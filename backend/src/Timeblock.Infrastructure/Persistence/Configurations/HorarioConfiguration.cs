using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Personas;

namespace Timeblock.Infrastructure.Persistence.Configurations;

public sealed class HorarioConfiguration : IEntityTypeConfiguration<Horario>
{
    public void Configure(EntityTypeBuilder<Horario> builder)
    {
        builder.ToTable("HORARIO", table =>
        {
            table.HasCheckConstraint(
                "CK_HORARIO_Estado",
                "\"estadoHorario\" IN ('GENERADO', 'EN_REVISION', 'APROBADO', 'REQUIERE_REGENERACION', 'PENDIENTE_VALIDACION', 'VALIDADO', 'RECHAZADO', 'CANCELADO')");
            table.HasCheckConstraint(
                "CK_HORARIO_Proporciones",
                "(\"propTeorica\" IS NULL AND \"propPractica\" IS NULL AND \"propLaboratorio\" IS NULL) OR (\"propTeorica\" IS NOT NULL AND \"propPractica\" IS NOT NULL AND \"propLaboratorio\" IS NOT NULL AND \"propTeorica\" BETWEEN 0 AND 1 AND \"propPractica\" BETWEEN 0 AND 1 AND \"propLaboratorio\" BETWEEN 0 AND 1 AND \"propTeorica\" + \"propPractica\" + \"propLaboratorio\" = 1)");
            table.HasCheckConstraint(
                "CK_HORARIO_CoberturaNoNegativa",
                "\"coberturaAsignatura\" IS NULL OR \"coberturaAsignatura\" >= 0");
        });

        builder.HasKey(horario => horario.Id);

        builder.Property(horario => horario.Id)
            .HasColumnName("idHorario")
            .ValueGeneratedNever();
        builder.Property(horario => horario.FechaCreacion)
            .HasColumnName("fechaCreacion")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(horario => horario.PeriodoAcademico)
            .HasColumnName("periodoAcademico")
            .IsRequired();
        builder.Property(horario => horario.Estado)
            .HasColumnName("estadoHorario")
            .HasConversion(
                estado => EstadoHorarioToDatabase(estado),
                valor => EstadoHorarioFromDatabase(valor))
            .IsRequired();
        builder.Property(horario => horario.ProgramaId)
            .HasColumnName("idPrograma")
            .IsRequired();
        builder.Property(horario => horario.CoordinadorId)
            .HasColumnName("idCoordinadorGen")
            .IsRequired();
        builder.Property(horario => horario.FechaInicioAutorizacion)
            .HasColumnName("fechaInicioGen")
            .IsRequired();

        // Parámetros de generación que M4 no usa: quedan nulos hasta un hito posterior.
        builder.Property<string?>("nivelComplejidadHorario")
            .HasColumnName("nivelComplejidadHorario");
        builder.Property<decimal?>("propTeorica")
            .HasColumnName("propTeorica")
            .HasColumnType("numeric");
        builder.Property<decimal?>("propPractica")
            .HasColumnName("propPractica")
            .HasColumnType("numeric");
        builder.Property<decimal?>("propLaboratorio")
            .HasColumnName("propLaboratorio")
            .HasColumnType("numeric");
        builder.Property<decimal?>("coberturaAsignatura")
            .HasColumnName("coberturaAsignatura")
            .HasColumnType("numeric");
        builder.Property<Guid?>("idRevisionOrigen")
            .HasColumnName("idRevisionOrigen");

        builder.HasOne<ProgramaAcademico>()
            .WithMany()
            .HasForeignKey(horario => horario.ProgramaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AutorizacionCoordinador>()
            .WithMany()
            .HasForeignKey(horario => new
            {
                horario.CoordinadorId,
                horario.ProgramaId,
                horario.FechaInicioAutorizacion
            })
            .HasPrincipalKey(autorizacion => new
            {
                autorizacion.CoordinadorId,
                autorizacion.ProgramaId,
                autorizacion.FechaInicio
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(horario => horario.Bloques)
            .HasField("_bloques")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static string EstadoHorarioToDatabase(EstadoHorario estado) =>
        estado == EstadoHorario.Generado
            ? "GENERADO"
            : throw new ArgumentOutOfRangeException(nameof(estado));

    private static EstadoHorario EstadoHorarioFromDatabase(string valor) =>
        valor == "GENERADO"
            ? EstadoHorario.Generado
            : throw new ArgumentOutOfRangeException(nameof(valor));
}
