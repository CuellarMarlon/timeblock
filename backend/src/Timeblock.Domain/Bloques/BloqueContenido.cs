using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Common;

namespace Timeblock.Domain.Bloques;

/// <summary>
/// Bloque de contenido de una asignatura creado por un coordinador y asociado a
/// los programas donde puede utilizarse.
/// </summary>
/// <remarks>
/// El dominio solo valida con los datos que recibe. Que existan la asignatura,
/// el coordinador (con perfil de coordinador) y los programas, y que no haya
/// otro bloque con la misma <see cref="ClaveDuplicidad"/>, se comprueba en
/// Application y persistencia.
/// </remarks>
public sealed class BloqueContenido
{
    private readonly List<BloquePrograma> _programas = [];

    public Guid Id { get; private set; }

    /// <summary>Descripción sin espacios sobrantes; conserva mayúsculas y acentos.</summary>
    public string Descripcion { get; private set; }

    /// <summary>
    /// Descripción en la forma usada para detectar duplicados. Es el valor
    /// sobre el que debe construirse la restricción de unicidad.
    /// </summary>
    public string DescripcionNormalizada { get; private set; }

    public TipoActividad TipoActividad { get; private set; }

    public NivelComplejidad NivelComplejidad { get; private set; }

    public Guid AsignaturaId { get; private set; }

    /// <summary>
    /// Coordinador que creó el bloque. Solo sirve de atribución: no limita
    /// quién puede utilizarlo.
    /// </summary>
    public Guid CoordinadorCreadorId { get; private set; }

    public IReadOnlyCollection<BloquePrograma> Programas => _programas.AsReadOnly();

    public ClaveDuplicidad ClaveDuplicidad =>
        new(AsignaturaId, TipoActividad, DescripcionNormalizada);

    private BloqueContenido(
        Guid id,
        string descripcion,
        string descripcionNormalizada,
        TipoActividad tipoActividad,
        NivelComplejidad nivelComplejidad,
        Guid asignaturaId,
        Guid coordinadorCreadorId)
    {
        Id = id;
        Descripcion = descripcion;
        DescripcionNormalizada = descripcionNormalizada;
        TipoActividad = tipoActividad;
        NivelComplejidad = nivelComplejidad;
        AsignaturaId = asignaturaId;
        CoordinadorCreadorId = coordinadorCreadorId;
    }

    public static BloqueContenido Crear(
        string descripcion,
        TipoActividad tipoActividad,
        NivelComplejidad nivelComplejidad,
        Guid asignaturaId,
        Guid coordinadorCreadorId) =>
        Crear(
            Guid.NewGuid(),
            descripcion,
            tipoActividad,
            nivelComplejidad,
            asignaturaId,
            coordinadorCreadorId);

    public static BloqueContenido Crear(
        Guid id,
        string descripcion,
        TipoActividad tipoActividad,
        NivelComplejidad nivelComplejidad,
        Guid asignaturaId,
        Guid coordinadorCreadorId)
    {
        string limpia = DescripcionBloque.Limpiar(descripcion);

        return new BloqueContenido(
            Guard.IdValido(id, "bloqueId"),
            limpia,
            limpia.ToLowerInvariant(),
            Guard.EnumValido(tipoActividad, "tipoActividad"),
            Guard.EnumValido(nivelComplejidad, "nivelComplejidad"),
            Guard.IdValido(asignaturaId, "asignaturaId"),
            Guard.IdValido(coordinadorCreadorId, "coordinadorCreadorId"));
    }

    /// <summary>
    /// Indica si dos bloques distintos comparten asignatura, tipo de actividad y
    /// descripción normalizada.
    /// </summary>
    public bool EsDuplicadoDe(BloqueContenido otro)
    {
        ArgumentNullException.ThrowIfNull(otro);

        return Id != otro.Id && ClaveDuplicidad == otro.ClaveDuplicidad;
    }

    /// <summary>
    /// Actualiza descripción, tipo de actividad y complejidad. Si algún dato no
    /// es válido, el bloque no cambia. Puede modificar la
    /// <see cref="ClaveDuplicidad"/>: Application debe volver a comprobarla.
    /// </summary>
    public void ActualizarDatos(
        string descripcion,
        TipoActividad tipoActividad,
        NivelComplejidad nivelComplejidad)
    {
        string limpia = DescripcionBloque.Limpiar(descripcion);
        TipoActividad tipoValido = Guard.EnumValido(tipoActividad, "tipoActividad");
        NivelComplejidad nivelValido = Guard.EnumValido(nivelComplejidad, "nivelComplejidad");

        Descripcion = limpia;
        DescripcionNormalizada = limpia.ToLowerInvariant();
        TipoActividad = tipoValido;
        NivelComplejidad = nivelValido;
    }

    /// <summary>
    /// Asocia el bloque con un programa (RII03: el programa debe incluir la
    /// asignatura del bloque). Es idempotente: asociar dos veces el mismo
    /// programa no tiene efecto.
    /// </summary>
    public void AsociarPrograma(ProgramaAcademico programa)
    {
        ArgumentNullException.ThrowIfNull(programa);

        if (!programa.IncluyeAsignatura(AsignaturaId))
        {
            throw new DomainException(
                "El bloque solo puede asociarse a un programa que incluya su asignatura.");
        }

        if (EstaAsociadoAPrograma(programa.Id))
        {
            return;
        }

        _programas.Add(BloquePrograma.Crear(Id, programa.Id));
    }

    public bool EstaAsociadoAPrograma(Guid programaId) =>
        _programas.Any(asociacion => asociacion.ProgramaId == programaId);

    /// <summary>
    /// Un bloque puede utilizarse en un programa cuando está asociado a él,
    /// con independencia de qué coordinador lo creó.
    /// </summary>
    public bool PuedeUsarseEn(Guid programaId) => EstaAsociadoAPrograma(programaId);

    /// <summary>
    /// Cambia la asignatura del bloque si todas sus asociaciones con programas
    /// siguen siendo válidas (RII03). Application debe pasar los programas
    /// asociados actuales del bloque; si falta alguno no se puede comprobar y
    /// se rechaza. Si alguna asociación dejaría de ser válida, el bloque no
    /// cambia. Modifica la <see cref="ClaveDuplicidad"/>: Application debe
    /// volver a comprobarla.
    /// </summary>
    public void CambiarAsignatura(
        Guid nuevaAsignaturaId,
        IEnumerable<ProgramaAcademico> programasAsociados)
    {
        ArgumentNullException.ThrowIfNull(programasAsociados);

        Guid asignaturaValida = Guard.IdValido(nuevaAsignaturaId, "asignaturaId");
        Dictionary<Guid, ProgramaAcademico> programas =
            programasAsociados.ToDictionary(programa => programa.Id);

        foreach (BloquePrograma asociacion in _programas)
        {
            if (!programas.TryGetValue(asociacion.ProgramaId, out ProgramaAcademico? programa))
            {
                throw new ArgumentException(
                    "Faltan programas asociados al bloque para comprobar la nueva asignatura.",
                    nameof(programasAsociados));
            }

            if (!programa.IncluyeAsignatura(asignaturaValida))
            {
                throw new DomainException(
                    "La nueva asignatura no pertenece a todos los programas asociados al bloque.");
            }
        }

        AsignaturaId = asignaturaValida;
    }
}
