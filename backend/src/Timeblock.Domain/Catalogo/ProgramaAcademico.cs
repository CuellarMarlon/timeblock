namespace Timeblock.Domain.Catalogo;

public sealed class ProgramaAcademico
{
    private readonly List<ProgramaAsignatura> _asignaturas = [];

    public Guid Id { get; private set; }

    public string Nombre { get; private set; }

    public string PlanEstudio { get; private set; }

    public IReadOnlyCollection<ProgramaAsignatura> Asignaturas => _asignaturas.AsReadOnly();

    private ProgramaAcademico(Guid id, string nombre, string planEstudio)
    {
        Id = id;
        Nombre = nombre;
        PlanEstudio = planEstudio;
    }

    public static ProgramaAcademico Crear(Guid id, string nombre, string planEstudio)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del programa es obligatorio.",
                nameof(id));
        }

        return new ProgramaAcademico(
            id,
            NormalizarTextoObligatorio(nombre, nameof(nombre)),
            NormalizarTextoObligatorio(planEstudio, nameof(planEstudio)));
    }

    public void ActualizarDatos(string nombre, string planEstudio)
    {
        string nombreValidado = NormalizarTextoObligatorio(nombre, nameof(nombre));

        string planEstudioValidado = NormalizarTextoObligatorio(planEstudio, nameof(planEstudio));

        Nombre = nombreValidado;
        PlanEstudio = planEstudioValidado;
    }

    public void AsociarAsignatura(Guid asignaturaId)
    {
        if (asignaturaId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador de la asignatura es obligatorio.",
                nameof(asignaturaId));
        }

        bool yaAsociada = _asignaturas.Any(
            asociacion => asociacion.AsignaturaId == asignaturaId);

        if (yaAsociada)
        {
            throw new InvalidOperationException(
                "La asignatura ya está asociada a este programa.");
        }

        _asignaturas.Add(
            ProgramaAsignatura.Crear(Id, asignaturaId));
    }

    private static string NormalizarTextoObligatorio(string valor, string nombreParametro)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new ArgumentException(
                "El valor es obligatorio.",
                nombreParametro);
        }

        return valor.Trim();
    }
}