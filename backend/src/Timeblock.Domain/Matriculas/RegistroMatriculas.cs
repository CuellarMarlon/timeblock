using Timeblock.Domain.Common;

namespace Timeblock.Domain.Matriculas;

/// <summary>
/// Conjunto de matrículas en el que no se repite la pareja estudiante +
/// programa. En persistencia la misma regla la garantiza la clave primaria.
/// </summary>
public sealed class RegistroMatriculas
{
    private readonly Dictionary<(Guid EstudianteId, Guid ProgramaId), Matricula> _matriculas = new();

    public RegistroMatriculas()
    {
    }

    public RegistroMatriculas(IEnumerable<Matricula> existentes)
    {
        ArgumentNullException.ThrowIfNull(existentes);

        foreach (var matricula in existentes)
        {
            Agregar(matricula);
        }
    }

    public IReadOnlyCollection<Matricula> Matriculas => _matriculas.Values;

    public Matricula Matricular(Guid estudianteId, Guid programaId)
    {
        var matricula = new Matricula(estudianteId, programaId);
        Agregar(matricula);
        return matricula;
    }

    public bool EstaMatriculado(Guid estudianteId, Guid programaId) =>
        _matriculas.ContainsKey((estudianteId, programaId));

    public IReadOnlyList<Matricula> DeEstudiante(Guid estudianteId) =>
        _matriculas.Values.Where(m => m.EstudianteId == estudianteId).ToList();

    public IReadOnlyList<Matricula> DePrograma(Guid programaId) =>
        _matriculas.Values.Where(m => m.ProgramaId == programaId).ToList();

    private void Agregar(Matricula matricula)
    {
        ArgumentNullException.ThrowIfNull(matricula);

        if (!_matriculas.TryAdd((matricula.EstudianteId, matricula.ProgramaId), matricula))
        {
            throw new DomainException(
                "El estudiante ya está matriculado en este programa.");
        }
    }
}
