using Timeblock.Domain.Common;

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
        return new ProgramaAcademico(
            Guard.IdValido(id, "programaId"),
            Guard.Requerido(nombre, "nombre"),
            Guard.Requerido(planEstudio, "planEstudio"));
    }

    public void ActualizarDatos(string nombre, string planEstudio)
    {
        string nombreValidado = Guard.Requerido(nombre, "nombre");
        string planEstudioValidado = Guard.Requerido(planEstudio, "planEstudio");

        Nombre = nombreValidado;
        PlanEstudio = planEstudioValidado;
    }

    public void AsociarAsignatura(Guid asignaturaId)
    {
        Guard.IdValido(asignaturaId, "asignaturaId");

        bool yaAsociada = _asignaturas.Any(
            asociacion => asociacion.AsignaturaId == asignaturaId);

        if (yaAsociada)
        {
            throw new DomainException(
                "La asignatura ya está asociada a este programa.");
        }

        _asignaturas.Add(
            ProgramaAsignatura.Crear(Id, asignaturaId));
    }

    public bool IncluyeAsignatura(Guid asignaturaId) =>
        _asignaturas.Any(asociacion => asociacion.AsignaturaId == asignaturaId);

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
