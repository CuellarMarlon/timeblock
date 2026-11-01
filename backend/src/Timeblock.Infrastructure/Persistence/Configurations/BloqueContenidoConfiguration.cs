using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Timeblock.Domain.Bloques;

namespace Timeblock.Infrastructure.Persistence.Configurations;

internal static class DatabaseEnumConverters
{
    public static readonly ValueConverter<TipoActividad, string> TipoActividadConverter = new(
        value => value == Timeblock.Domain.Bloques.TipoActividad.Teoria
            ? "TEORICA"
            : value == Timeblock.Domain.Bloques.TipoActividad.Practica
                ? "PRACTICA"
                : "LABORATORIO",
        value => value == "TEORICA"
            ? Timeblock.Domain.Bloques.TipoActividad.Teoria
            : value == "PRACTICA"
                ? Timeblock.Domain.Bloques.TipoActividad.Practica
                : Timeblock.Domain.Bloques.TipoActividad.Laboratorio);

    public static readonly ValueConverter<NivelComplejidad, string> NivelComplejidadConverter = new(
        value => value == Timeblock.Domain.Bloques.NivelComplejidad.Bajo
            ? "BAJO"
            : value == Timeblock.Domain.Bloques.NivelComplejidad.Medio
                ? "MEDIO"
                : "ALTO",
        value => value == "BAJO"
            ? Timeblock.Domain.Bloques.NivelComplejidad.Bajo
            : value == "MEDIO"
                ? Timeblock.Domain.Bloques.NivelComplejidad.Medio
                : Timeblock.Domain.Bloques.NivelComplejidad.Alto);
}

public sealed class BloqueContenidoConfiguration
    : IEntityTypeConfiguration<BloqueContenido>
{
    public void Configure(EntityTypeBuilder<BloqueContenido> builder)
    {
        builder.ToTable("BLOQUE_CONTENIDO", table =>
        {
            table.HasCheckConstraint(
                "CK_BLOQUE_CONTENIDO_TipoActividad",
                "\"tipoActividad\" IN ('TEORICA', 'PRACTICA', 'LABORATORIO')");
            table.HasCheckConstraint(
                "CK_BLOQUE_CONTENIDO_NivelComplejidad",
                "\"nivelComplejidad\" IN ('BAJO', 'MEDIO', 'ALTO')");
            table.HasCheckConstraint(
                "CK_BLOQUE_CONTENIDO_DescripcionNormalizada",
                "\"descripcionNormalizada\" = lower(regexp_replace(btrim(\"descripcion\"), '[[:space:]]+', ' ', 'g'))");
        });

        builder.HasKey(bloque => bloque.Id);

        builder.Property(bloque => bloque.Id)
            .HasColumnName("idBloque")
            .ValueGeneratedNever();
        builder.Property(bloque => bloque.Descripcion)
            .HasColumnName("descripcion")
            .IsRequired();
        builder.Property(bloque => bloque.DescripcionNormalizada)
            .HasColumnName("descripcionNormalizada")
            .IsRequired();
        builder.Property(bloque => bloque.TipoActividad)
            .HasColumnName("tipoActividad")
            .HasConversion(DatabaseEnumConverters.TipoActividadConverter)
            .IsRequired();
        builder.Property(bloque => bloque.NivelComplejidad)
            .HasColumnName("nivelComplejidad")
            .HasConversion(DatabaseEnumConverters.NivelComplejidadConverter)
            .IsRequired();
        builder.Property(bloque => bloque.AsignaturaId)
            .HasColumnName("idAsignatura")
            .IsRequired();
        builder.Property(bloque => bloque.CoordinadorCreadorId)
            .HasColumnName("idCoordinadorCreador")
            .IsRequired();

        builder.HasIndex(bloque => new
        {
            bloque.AsignaturaId,
            bloque.TipoActividad,
            bloque.DescripcionNormalizada
        })
            .IsUnique()
            .HasDatabaseName("UX_BLOQUE_CONTENIDO_ClaveDuplicidad");

        builder.HasOne<Timeblock.Domain.Catalogo.Asignatura>()
            .WithMany()
            .HasForeignKey(bloque => bloque.AsignaturaId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CoordinadorPerfilRecord>()
            .WithMany()
            .HasForeignKey(bloque => bloque.CoordinadorCreadorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(bloque => bloque.Programas)
            .HasField("_programas")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
