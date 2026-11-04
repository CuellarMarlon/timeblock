using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Common;
using Timeblock.Domain.Matriculas;

namespace Timeblock.Application.Matriculas;

public sealed class MatriculasService(IMatriculaRepository matriculas) : ICasoDeUso
{
    public async Task<Matricula> MatricularAsync(Guid programaId, Guid estudianteId, CancellationToken ct)
    {
        Matricula matricula;
        try { matricula = new Matricula(estudianteId, programaId); }
        catch (DomainException ex) { throw new InvalidRequestException(ex.Message, ex); }

        await AsegurarEstudianteAsync(estudianteId, ct);
        await AsegurarProgramaAsync(programaId, ct);

        if (await matriculas.ExisteAsync(estudianteId, programaId, ct))
            throw new ResourceConflictException("El estudiante ya está matriculado en ese programa.");

        await matriculas.AgregarAsync(matricula, ct);
        return matricula;
    }

    public async Task<PagedResult<Matricula>> ListarPorProgramaAsync(
        Guid programaId, int? page, int? pageSize, CancellationToken ct)
    {
        PageRequest pagina = CrearPagina(page, pageSize);
        await AsegurarProgramaAsync(programaId, ct);
        return await matriculas.ListarPorProgramaAsync(programaId, pagina, ct);
    }

    public async Task<PagedResult<Matricula>> ListarPorEstudianteAsync(
        Guid estudianteId, int? page, int? pageSize, CancellationToken ct)
    {
        PageRequest pagina = CrearPagina(page, pageSize);
        await AsegurarEstudianteAsync(estudianteId, ct);
        return await matriculas.ListarPorEstudianteAsync(estudianteId, pagina, ct);
    }

    private async Task AsegurarEstudianteAsync(Guid estudianteId, CancellationToken ct)
    {
        if (await matriculas.ExisteEstudianteAsync(estudianteId, ct))
            return;

        throw await matriculas.ExistePersonaAsync(estudianteId, ct)
            ? new ResourceNotFoundException($"La persona '{estudianteId}' no tiene perfil de estudiante.")
            : new ResourceNotFoundException($"No existe la persona '{estudianteId}'.");
    }

    private async Task AsegurarProgramaAsync(Guid programaId, CancellationToken ct)
    {
        if (!await matriculas.ExisteProgramaAsync(programaId, ct))
            throw new ResourceNotFoundException($"No existe el programa '{programaId}'.");
    }

    private static PageRequest CrearPagina(int? page, int? pageSize)
    {
        try { return PageRequest.Create(page, pageSize); }
        catch (DomainException ex) { throw new InvalidRequestException(ex.Message, ex); }
    }
}
