namespace Timeblock.Infrastructure.Persistence;

/// <summary>
/// Fila de persistencia de PERSONA. Se separa de Persona porque el dominio
/// representa sus perfiles como una jerarquía, mientras que el esquema usa
/// tablas normalizadas para cada subtipo.
/// </summary>
public sealed class PersonaRecord
{
    public Guid IdPersona { get; set; }

    public string NombrePersona { get; set; } = string.Empty;
}

/// <summary>
/// Fila de persistencia para el subtipo estudiante del esquema relacional.
/// No forma parte del modelo de dominio.
/// </summary>
public sealed class EstudiantePerfilRecord
{
    public Guid IdEstudiante { get; set; }

    public int Edad { get; set; }

    public string Grupo { get; set; } = string.Empty;

    public Guid CoordinadorAsignadoId { get; set; }
}

/// <summary>Fila de persistencia del subtipo coordinador.</summary>
public sealed class CoordinadorPerfilRecord
{
    public Guid IdCoordinador { get; set; }

    public string Especialidad { get; set; } = string.Empty;
}

/// <summary>Fila de persistencia del subtipo jefe de departamento.</summary>
public sealed class JefeDepartamentoPerfilRecord
{
    public Guid IdJefeDepartamento { get; set; }
}

/// <summary>Tabla de fechas usada por las claves foráneas del esquema aprobado.</summary>
public sealed class FechaRecord
{
    public DateOnly Fecha { get; set; }
}

/// <summary>
/// Tabla de aulas del esquema aprobado. La gestión de aulas no forma parte de
/// los casos de uso M4, pero BLOQUE_EN_HORARIO referencia esta relación.
/// </summary>
public sealed class AulaRecord
{
    public Guid IdAula { get; set; }

    public string Edificio { get; set; } = string.Empty;

    public int Capacidad { get; set; }
}

/// <summary>
/// Relación auxiliar exigida por el diseño para cumplir PLJ. No representa un
/// agregado ni se usa en los casos de uso M4.
/// </summary>
public sealed class EsquemaLlaveRecord
{
    public Guid IdPersona { get; set; }

    public Guid IdCoordinador { get; set; }

    public DateOnly FechaInicio { get; set; }

    public DateOnly Fecha { get; set; }

    public DateTimeOffset FechaAsignacion { get; set; }

    public Guid IdBloque { get; set; }

    public Guid IdValidacion { get; set; }

    public Guid IdRevision { get; set; }

    public Guid IdSolicitud { get; set; }
}
