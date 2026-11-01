using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Timeblock.Infrastructure.Persistence.Configurations;

public sealed class PersonaConfiguration : IEntityTypeConfiguration<PersonaRecord>
{
    public void Configure(EntityTypeBuilder<PersonaRecord> builder)
    {
        builder.ToTable("PERSONA");
        builder.HasKey(persona => persona.IdPersona);

        builder.Property(persona => persona.IdPersona)
            .HasColumnName("idPersona")
            .ValueGeneratedNever();

        builder.Property(persona => persona.NombrePersona)
            .HasColumnName("nombrePersona")
            .IsRequired();
    }
}
