using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Common;

namespace Timeblock.Application.Bloques;


public sealed class BloquesService(IBloqueRepository bloques) : ICasoDeUso
{
    public async Task<BloqueContenido> CrearAsync(
        CrearBloqueCommand command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);

        // Primero se valida lo que no requiere consultar nada (400).
        ValidarEnumeraciones(command.TipoActividad, command.NivelComplejidad);
        BloqueContenido bloque = ConvertirErrores(() => BloqueContenido.Crear(
            command.Descripcion,
            command.TipoActividad,
            command.NivelComplejidad,
            command.AsignaturaId,
            command.CoordinadorId));
        Guid[] programasIds = ValidarIds(command.ProgramasIds, "programasIds");

        await AsegurarAsignaturaAsync(command.AsignaturaId, ct);
        await AsegurarCoordinadorAsync(command.CoordinadorId, ct);
        IReadOnlyList<ProgramaAcademico> programas =
            await ObtenerProgramasExistentesAsync(programasIds, ct);

        foreach (ProgramaAcademico programa in programas)
        {
            ConvertirErrores(() => bloque.AsociarPrograma(programa));
        }

        await AsegurarSinDuplicadoAsync(bloque.ClaveDuplicidad, null, ct);
        await bloques.AgregarAsync(bloque, ct);
        return bloque;
    }

    public async Task<BloqueContenido> ObtenerAsync(Guid bloqueId, CancellationToken ct)
    {
        ValidarId(bloqueId, "bloqueId");

        return await bloques.ObtenerAsync(bloqueId, ct)
            ?? throw new ResourceNotFoundException($"No existe el bloque '{bloqueId}'.");
    }

    public Task<PagedResult<BloqueContenido>> ListarAsync(
        BloqueFilter filter,
        int? page,
        int? pageSize,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filter);

        PageRequest pagina = ConvertirErrores(() => PageRequest.Create(page, pageSize));
        if (filter.TipoActividad is { } tipo)
        {
            ValidarEnumeracion(tipo, "tipoActividad");
        }

        if (filter.NivelComplejidad is { } nivel)
        {
            ValidarEnumeracion(nivel, "nivelComplejidad");
        }

        return bloques.ListarAsync(filter, pagina, ct);
    }

    public async Task<BloqueContenido> ActualizarAsync(
        Guid bloqueId,
        ActualizarBloqueCommand command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        ValidarId(bloqueId, "bloqueId");

        if (command is { Descripcion: null, TipoActividad: null, NivelComplejidad: null, AsignaturaId: null })
        {
            throw new InvalidRequestException(
                "Indica al menos un dato del bloque para actualizar.");
        }

        if (command.TipoActividad is { } tipoSolicitado)
        {
            ValidarEnumeracion(tipoSolicitado, "tipoActividad");
        }

        if (command.NivelComplejidad is { } nivelSolicitado)
        {
            ValidarEnumeracion(nivelSolicitado, "nivelComplejidad");
        }

        if (command.AsignaturaId is { } asignaturaSolicitada)
        {
            ValidarId(asignaturaSolicitada, "asignaturaId");
        }

        BloqueContenido bloque = await ObtenerAsync(bloqueId, ct);
        ClaveDuplicidad claveAnterior = bloque.ClaveDuplicidad;

        string descripcion = command.Descripcion ?? bloque.Descripcion;
        TipoActividad tipo = command.TipoActividad ?? bloque.TipoActividad;
        NivelComplejidad nivel = command.NivelComplejidad ?? bloque.NivelComplejidad;

        // Se comprueba la descripción antes de modificar nada, para que un
        // dato inválido no deje el bloque a medias.
        ConvertirErrores(() => DescripcionBloque.Limpiar(descripcion));

        if (command.AsignaturaId is { } nuevaAsignaturaId
            && nuevaAsignaturaId != bloque.AsignaturaId)
        {
            await AsegurarAsignaturaAsync(nuevaAsignaturaId, ct);

            Guid[] asociados = bloque.Programas.Select(a => a.ProgramaId).ToArray();
            IReadOnlyList<ProgramaAcademico> programas =
                await ObtenerProgramasExistentesAsync(asociados, ct);

            ConvertirErrores(() => bloque.CambiarAsignatura(nuevaAsignaturaId, programas));
        }

        ConvertirErrores(() => bloque.ActualizarDatos(descripcion, tipo, nivel));

        if (bloque.ClaveDuplicidad != claveAnterior)
        {
            await AsegurarSinDuplicadoAsync(bloque.ClaveDuplicidad, bloque.Id, ct);
        }

        await bloques.GuardarCambiosAsync(ct);
        return bloque;
    }

   
    public async Task AsociarProgramaAsync(Guid bloqueId, Guid programaId, CancellationToken ct)
    {
        ValidarId(bloqueId, "bloqueId");
        ValidarId(programaId, "programaId");

        BloqueContenido bloque = await ObtenerAsync(bloqueId, ct);
        if (bloque.EstaAsociadoAPrograma(programaId))
        {
            return;
        }

        IReadOnlyList<ProgramaAcademico> programas =
            await ObtenerProgramasExistentesAsync([programaId], ct);

        ConvertirErrores(() => bloque.AsociarPrograma(programas[0]));
        await bloques.GuardarCambiosAsync(ct);
    }

    private async Task AsegurarAsignaturaAsync(Guid asignaturaId, CancellationToken ct)
    {
        if (!await bloques.ExisteAsignaturaAsync(asignaturaId, ct))
        {
            throw new ResourceNotFoundException($"No existe la asignatura '{asignaturaId}'.");
        }
    }

    private async Task AsegurarCoordinadorAsync(Guid coordinadorId, CancellationToken ct)
    {
        if (!await bloques.ExisteCoordinadorAsync(coordinadorId, ct))
        {
            throw new ResourceNotFoundException($"No existe el coordinador '{coordinadorId}'.");
        }
    }

    private async Task<IReadOnlyList<ProgramaAcademico>> ObtenerProgramasExistentesAsync(
        Guid[] programasIds,
        CancellationToken ct)
    {
        if (programasIds.Length == 0)
        {
            return [];
        }

        IReadOnlyList<ProgramaAcademico> programas =
            await bloques.ObtenerProgramasAsync(programasIds, ct);

        HashSet<Guid> encontrados = programas.Select(programa => programa.Id).ToHashSet();
        foreach (Guid programaId in programasIds)
        {
            if (!encontrados.Contains(programaId))
            {
                throw new ResourceNotFoundException($"No existe el programa '{programaId}'.");
            }
        }

        return programas;
    }

    private async Task AsegurarSinDuplicadoAsync(
        ClaveDuplicidad clave,
        Guid? excluirBloqueId,
        CancellationToken ct)
    {
        if (await bloques.ExisteDuplicadoAsync(clave, excluirBloqueId, ct))
        {
            throw new ResourceConflictException(
                "Ya existe un bloque con la misma asignatura, tipo de actividad y descripción.");
        }
    }

    private static Guid[] ValidarIds(IReadOnlyCollection<Guid>? ids, string nombre)
    {
        Guid[] distintos = (ids ?? []).Distinct().ToArray();
        if (distintos.Contains(Guid.Empty))
        {
            throw new InvalidRequestException($"{nombre} contiene un identificador no válido.");
        }

        return distintos;
    }

    private static void ValidarId(Guid id, string nombre)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidRequestException($"{nombre} no es válido.");
        }
    }

    private static void ValidarEnumeraciones(TipoActividad tipo, NivelComplejidad nivel)
    {
        ValidarEnumeracion(tipo, "tipoActividad");
        ValidarEnumeracion(nivel, "nivelComplejidad");
    }

    private static void ValidarEnumeracion<TEnum>(TEnum valor, string nombre)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(valor))
        {
            throw new InvalidRequestException($"{nombre} no es válido.");
        }
    }

      private static T ConvertirErrores<T>(Func<T> accion)
    {
        try
        {
            return accion();
        }
        catch (DomainException ex)
        {
            throw new InvalidRequestException(ex.Message, ex);
        }
    }

    private static void ConvertirErrores(Action accion) =>
        ConvertirErrores<object?>(() =>
        {
            accion();
            return null;
        });
}
