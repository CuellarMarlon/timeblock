using Timeblock.Domain.Personas;

namespace Timeblock.Infrastructure.Persistence;

/// <summary>
/// Representación relacional de una persona y exactamente uno de sus perfiles.
/// </summary>
public sealed record PersonaPersistenceRows(
    PersonaRecord Persona,
    EstudiantePerfilRecord? Estudiante,
    CoordinadorPerfilRecord? Coordinador,
    JefeDepartamentoPerfilRecord? JefeDepartamento);

/// <summary>
/// Traduce el agregado de dominio Persona al esquema normalizado, que almacena
/// cada tipo de perfil en su propia tabla.
/// </summary>
public static class PersonaPersistenceMapper
{
    public static PersonaPersistenceRows ToRows(Persona persona)
    {
        ArgumentNullException.ThrowIfNull(persona);

        var personaRecord = new PersonaRecord
        {
            IdPersona = persona.Id,
            NombrePersona = persona.Nombre
        };

        return persona.Perfil switch
        {
            PerfilEstudiante estudiante => new PersonaPersistenceRows(
                personaRecord,
                new EstudiantePerfilRecord
                {
                    IdEstudiante = persona.Id,
                    Edad = estudiante.Edad,
                    Grupo = estudiante.Grupo,
                    CoordinadorAsignadoId = estudiante.CoordinadorId
                },
                null,
                null),
            PerfilCoordinador coordinador => new PersonaPersistenceRows(
                personaRecord,
                null,
                new CoordinadorPerfilRecord
                {
                    IdCoordinador = persona.Id,
                    Especialidad = coordinador.Especialidad
                },
                null),
            PerfilJefeDepartamento => new PersonaPersistenceRows(
                personaRecord,
                null,
                null,
                new JefeDepartamentoPerfilRecord
                {
                    IdJefeDepartamento = persona.Id
                }),
            _ => throw new InvalidOperationException(
                $"El tipo de perfil {persona.Perfil.GetType().Name} no está soportado.")
        };
    }

    public static Persona ToDomain(PersonaPersistenceRows rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        int profilesPresent =
            (rows.Estudiante is null ? 0 : 1) +
            (rows.Coordinador is null ? 0 : 1) +
            (rows.JefeDepartamento is null ? 0 : 1);

        if (profilesPresent != 1)
        {
            throw new InvalidOperationException(
                "La persona persistida debe tener exactamente un registro de perfil.");
        }

        if ((rows.Estudiante?.IdEstudiante ?? rows.Persona.IdPersona) != rows.Persona.IdPersona ||
            (rows.Coordinador?.IdCoordinador ?? rows.Persona.IdPersona) != rows.Persona.IdPersona ||
            (rows.JefeDepartamento?.IdJefeDepartamento ?? rows.Persona.IdPersona) != rows.Persona.IdPersona)
        {
            throw new InvalidOperationException(
                "El identificador del perfil persistido no coincide con el de la persona.");
        }

        Perfil perfil = rows.Estudiante is { } estudiante
            ? new PerfilEstudiante(
                estudiante.Edad,
                estudiante.Grupo,
                estudiante.CoordinadorAsignadoId)
            : rows.Coordinador is { } coordinador
                ? new PerfilCoordinador(coordinador.Especialidad)
                : new PerfilJefeDepartamento();

        return Persona.Crear(rows.Persona.IdPersona, rows.Persona.NombrePersona, perfil);
    }
}
