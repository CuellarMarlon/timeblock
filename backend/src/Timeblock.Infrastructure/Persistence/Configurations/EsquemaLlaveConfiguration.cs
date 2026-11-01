using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Timeblock.Domain.Bloques;

namespace Timeblock.Infrastructure.Persistence.Configurations;

public sealed class EsquemaLlaveConfiguration
    : IEntityTypeConfiguration<EsquemaLlaveRecord>
{
    public void Configure(EntityTypeBuilder<EsquemaLlaveRecord> builder)
    {
        builder.ToTable("ESQUEMA_LLAVE");

        builder.HasKey(clave => new
        {
            clave.IdPersona,
            clave.IdCoordinador,
            clave.FechaInicio,
            clave.Fecha,
            clave.FechaAsignacion,
            clave.IdBloque,
            clave.IdValidacion,
            clave.IdRevision,
            clave.IdSolicitud
        });

        builder.Property(clave => clave.IdPersona)
            .HasColumnName("idPersona");
        builder.Property(clave => clave.IdCoordinador)
            .HasColumnName("idCoordinador");
        builder.Property(clave => clave.FechaInicio)
            .HasColumnName("fechaInicio");
        builder.Property(clave => clave.Fecha)
            .HasColumnName("fecha");
        builder.Property(clave => clave.FechaAsignacion)
            .HasColumnName("fechaAsignacion")
            .HasColumnType("timestamp with time zone");
        builder.Property(clave => clave.IdBloque)
            .HasColumnName("idBloque");
        builder.Property(clave => clave.IdValidacion)
            .HasColumnName("idValidacion");
        builder.Property(clave => clave.IdRevision)
            .HasColumnName("idRevision");
        builder.Property(clave => clave.IdSolicitud)
            .HasColumnName("idSolicitud");

        // R16 conserva todas las columnas y la clave compuesta del diseño.
        // Las referencias a VALIDACION, REVISION, SOLICITUD_CAMBIO y
        // HORARIO_ESTUDIANTE no se mapean aquí porque esas relaciones quedan
        // fuera del esquema funcional M4.
        builder.HasOne<PersonaRecord>()
            .WithMany()
            .HasForeignKey(clave => clave.IdPersona)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CoordinadorPerfilRecord>()
            .WithMany()
            .HasForeignKey(clave => clave.IdCoordinador)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FechaRecord>()
            .WithMany()
            .HasForeignKey(clave => clave.FechaInicio)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FechaRecord>()
            .WithMany()
            .HasForeignKey(clave => clave.Fecha)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<BloqueContenido>()
            .WithMany()
            .HasForeignKey(clave => clave.IdBloque)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
