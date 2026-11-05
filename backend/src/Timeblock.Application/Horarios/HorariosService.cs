using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Common;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Personas;

namespace Timeblock.Application.Horarios;


public sealed class HorariosService(IHorarioRepository horarios, TimeProvider reloj)
    : ICasoDeUso
{
    public async Task<HorarioDetalle> CrearAsync(
        CrearHorarioCommand command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Validaciones de formato (400), sin consultar nada.
        ValidarId(command.ProgramaId, "programaId");
        ValidarId(command.CoordinadorId, "coordinadorId");
        if (string.IsNullOrWhiteSpace(command.PeriodoAcademico))
        {
            throw new InvalidRequestException("periodoAcademico es obligatorio.");
        }

        IReadOnlyCollection<BloqueEnHorarioCommand> solicitados = command.Bloques ?? [];
        if (solicitados.Count == 0)
        {
            throw new InvalidRequestException("bloques debe contener al menos un elemento.");
        }

        if (solicitados.Any(solicitado => solicitado.BloqueId == Guid.Empty))
        {
            throw new InvalidRequestException("bloques contiene un identificador de bloque no válido.");
        }

        // Referencias (404), en el orden del contrato.
        if (!await horarios.ExisteProgramaAsync(command.ProgramaId, ct))
        {
            throw new ResourceNotFoundException($"No existe el programa '{command.ProgramaId}'.");
        }

        if (!await horarios.ExisteCoordinadorAsync(command.CoordinadorId, ct))
        {
            throw new ResourceNotFoundException($"No existe el coordinador '{command.CoordinadorId}'.");
        }

        // Autorización vigente con permiso de generación (403).
        DateTimeOffset ahora = reloj.GetUtcNow();
        AutorizacionCoordinador autorizacion = await ResolverAutorizacionAsync(
            command.CoordinadorId,
            command.ProgramaId,
            DateOnly.FromDateTime(ahora.UtcDateTime),
            ct);

        Dictionary<Guid, BloqueContenido> bloques = await ObtenerBloquesPorIdAsync(
            solicitados.Select(solicitado => solicitado.BloqueId).Distinct().ToArray(),
            ct);

        // Reglas del dominio: el bloque debe poder usarse en el programa y no
        // repetirse en la misma fecha (409). Nada se guarda hasta que todo
        // el horario es válido.
        Horario horario = Horario.Crear(
            Guid.NewGuid(),
            ahora,
            command.PeriodoAcademico,
            command.ProgramaId,
            command.CoordinadorId,
            autorizacion);

        foreach (BloqueEnHorarioCommand solicitado in solicitados)
        {
            horario.AgregarBloque(bloques[solicitado.BloqueId], solicitado.Fecha);
        }

        await horarios.AgregarAsync(horario, ct);
        return Detalle(horario, bloques);
    }

    public async Task<HorarioDetalle> ObtenerAsync(Guid horarioId, CancellationToken ct)
    {
        ValidarId(horarioId, "horarioId");

        Horario horario = await horarios.ObtenerAsync(horarioId, ct)
            ?? throw new ResourceNotFoundException($"No existe el horario '{horarioId}'.");

        return (await DetallarAsync([horario], ct))[0];
    }

    public async Task<PagedResult<HorarioDetalle>> ListarAsync(
        HorarioFilter filter,
        int? page,
        int? pageSize,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filter);

        PageRequest pagina = PageRequest.Create(page, pageSize);
        if (filter.Estado is { } estado && !Enum.IsDefined(estado))
        {
            throw new InvalidRequestException("estado no es válido.");
        }

        HorarioFilter normalizado = filter with
        {
            PeriodoAcademico = string.IsNullOrWhiteSpace(filter.PeriodoAcademico)
                ? null
                : filter.PeriodoAcademico.Trim(),
        };

        PagedResult<Horario> resultado = await horarios.ListarAsync(normalizado, pagina, ct);
        IReadOnlyList<HorarioDetalle> detalles = await DetallarAsync(resultado.Items, ct);

        return new PagedResult<HorarioDetalle>(
            detalles,
            resultado.Page,
            resultado.PageSize,
            resultado.TotalItems);
    }

    /// <summary>
    /// Busca la autorización del coordinador que está vigente hoy y permite
    /// generar; si hay varias, la de inicio más reciente. La vigencia la decide
    /// el dominio.
    /// </summary>
    private async Task<AutorizacionCoordinador> ResolverAutorizacionAsync(
        Guid coordinadorId,
        Guid programaId,
        DateOnly hoy,
        CancellationToken ct)
    {
        IReadOnlyList<AutorizacionCoordinador> autorizaciones =
            await horarios.ObtenerAutorizacionesAsync(coordinadorId, programaId, ct);

        return autorizaciones
                .Where(autorizacion => autorizacion.PermiteGenerarEn(hoy))
                .OrderByDescending(autorizacion => autorizacion.FechaInicio)
                .FirstOrDefault()
            ?? throw new DomainException(
                "El coordinador no tiene una autorización vigente con permiso para generar horarios en el programa.",
                TipoErrorDominio.AutorizacionNegocio);
    }

    private async Task<Dictionary<Guid, BloqueContenido>> ObtenerBloquesPorIdAsync(
        Guid[] bloquesIds,
        CancellationToken ct)
    {
        IReadOnlyList<BloqueContenido> bloques = await horarios.ObtenerBloquesAsync(bloquesIds, ct);
        Dictionary<Guid, BloqueContenido> porId = bloques.ToDictionary(bloque => bloque.Id);

        foreach (Guid bloqueId in bloquesIds)
        {
            if (!porId.ContainsKey(bloqueId))
            {
                throw new ResourceNotFoundException($"No existe el bloque '{bloqueId}'.");
            }
        }

        return porId;
    }

    /// <summary>Completa varios horarios con una sola consulta de bloques.</summary>
    private async Task<IReadOnlyList<HorarioDetalle>> DetallarAsync(
        IReadOnlyCollection<Horario> lista,
        CancellationToken ct)
    {
        Guid[] bloquesIds = lista
            .SelectMany(horario => horario.Bloques)
            .Select(asociacion => asociacion.BloqueId)
            .Distinct()
            .ToArray();

        Dictionary<Guid, BloqueContenido> bloques = bloquesIds.Length == 0
            ? []
            : await ObtenerBloquesPorIdAsync(bloquesIds, ct);

        return lista.Select(horario => Detalle(horario, bloques)).ToList();
    }

    private static HorarioDetalle Detalle(
        Horario horario,
        IReadOnlyDictionary<Guid, BloqueContenido> bloques) =>
        new(
            horario,
            horario.Bloques
                .Select(asociacion =>
                    new BloqueEnHorarioDetalle(asociacion, bloques[asociacion.BloqueId]))
                .OrderBy(detalle => detalle.Asociacion.Fecha)
                .ThenBy(detalle => detalle.Bloque.DescripcionNormalizada, StringComparer.Ordinal)
                .ThenBy(detalle => detalle.Bloque.Id)
                .ToList());

    private static void ValidarId(Guid id, string nombre)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidRequestException($"{nombre} no es válido.");
        }
    }
}
