using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Common;

namespace Timeblock.Application.Catalogo;

/// <summary>Casos de uso del catálogo de programas y asignaturas.</summary>
public sealed class CatalogoService(ICatalogoRepository catalogo) : ICasoDeUso
{
    public async Task<ProgramaAcademico> CrearProgramaAsync(
        string nombre, string planEstudio, CancellationToken cancellationToken)
    {
        ProgramaAcademico programa = CrearEntidad(() =>
            ProgramaAcademico.Crear(Guid.NewGuid(), nombre, planEstudio));
        await catalogo.AgregarProgramaAsync(programa, cancellationToken);
        return programa;
    }

    public async Task<PagedResult<ProgramaAcademico>> ListarProgramasAsync(
        string? nombre, int? page, int? pageSize, CancellationToken cancellationToken) =>
        await catalogo.ListarProgramasAsync(nombre, CrearPagina(page, pageSize), cancellationToken);

    public async Task<ProgramaAcademico> ObtenerProgramaAsync(Guid programaId, CancellationToken cancellationToken) =>
        await catalogo.ObtenerProgramaAsync(programaId, cancellationToken)
        ?? throw new ResourceNotFoundException($"No existe el programa '{programaId}'.");

    public async Task<ProgramaAcademico> ActualizarProgramaAsync(
        Guid programaId, string nombre, string planEstudio, CancellationToken cancellationToken)
    {
        ProgramaAcademico programa = await ObtenerProgramaAsync(programaId, cancellationToken);
        EjecutarRegla(() => programa.ActualizarDatos(nombre, planEstudio));
        await catalogo.GuardarProgramaAsync(cancellationToken);
        return programa;
    }

    public async Task<PagedResult<Asignatura>> ListarAsignaturasDeProgramaAsync(
        Guid programaId, int? page, int? pageSize, CancellationToken cancellationToken)
    {
        if (!await catalogo.ExisteProgramaAsync(programaId, cancellationToken))
            throw new ResourceNotFoundException($"No existe el programa '{programaId}'.");
        return await catalogo.ListarAsignaturasDeProgramaAsync(
            programaId, CrearPagina(page, pageSize), cancellationToken);
    }

    public async Task<Asignatura> CrearAsignaturaAsync(string nombre, CancellationToken cancellationToken)
    {
        Asignatura asignatura = CrearEntidad(() => Asignatura.Crear(Guid.NewGuid(), nombre));
        await catalogo.AgregarAsignaturaAsync(asignatura, cancellationToken);
        return asignatura;
    }

    public async Task<PagedResult<Asignatura>> ListarAsignaturasAsync(
        string? nombre, int? page, int? pageSize, CancellationToken cancellationToken) =>
        await catalogo.ListarAsignaturasAsync(nombre, CrearPagina(page, pageSize), cancellationToken);

    public async Task<Asignatura> ObtenerAsignaturaAsync(Guid asignaturaId, CancellationToken cancellationToken) =>
        await catalogo.ObtenerAsignaturaAsync(asignaturaId, cancellationToken)
        ?? throw new ResourceNotFoundException($"No existe la asignatura '{asignaturaId}'.");

    public async Task<Asignatura> ActualizarAsignaturaAsync(
        Guid asignaturaId, string nombre, CancellationToken cancellationToken)
    {
        Asignatura asignatura = await ObtenerAsignaturaAsync(asignaturaId, cancellationToken);
        EjecutarRegla(() => asignatura.CambiarNombre(nombre));
        await catalogo.GuardarAsignaturaAsync(cancellationToken);
        return asignatura;
    }

    public async Task AsociarAsignaturaAsync(
        Guid programaId, Guid asignaturaId, CancellationToken cancellationToken)
    {
        if (!await catalogo.ExisteProgramaAsync(programaId, cancellationToken))
            throw new ResourceNotFoundException($"No existe el programa '{programaId}'.");
        if (!await catalogo.ExisteAsignaturaAsync(asignaturaId, cancellationToken))
            throw new ResourceNotFoundException($"No existe la asignatura '{asignaturaId}'.");

        // PUT idempotente: una relación existente ya satisface la operación.
        if (await catalogo.EstaAsociadaAsync(programaId, asignaturaId, cancellationToken))
            return;

        await catalogo.AsociarAsync(programaId, asignaturaId, cancellationToken);
    }

    private static PageRequest CrearPagina(int? page, int? pageSize)
    {
        try { return PageRequest.Create(page, pageSize); }
        catch (DomainException ex) { throw new InvalidRequestException(ex.Message, ex); }
    }

    private static T CrearEntidad<T>(Func<T> factory)
    {
        try { return factory(); }
        catch (DomainException ex) { throw new InvalidRequestException(ex.Message, ex); }
    }

    private static void EjecutarRegla(Action action)
    {
        try { action(); }
        catch (DomainException ex) { throw new InvalidRequestException(ex.Message, ex); }
    }
}
