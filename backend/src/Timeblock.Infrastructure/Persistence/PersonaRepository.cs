using Microsoft.EntityFrameworkCore;
using Npgsql;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Personas;
using Timeblock.Domain.Personas;

namespace Timeblock.Infrastructure.Persistence;

public sealed class PersonaRepository(TimeblockDbContext db) : IPersonaRepository
{
    public Task<bool> ExisteCoordinadorAsync(Guid id, CancellationToken ct) =>
        db.Coordinadores.AnyAsync(x => x.IdCoordinador == id, ct);

    public Task<bool> ExisteProgramaAsync(Guid id, CancellationToken ct) =>
        db.Programas.AnyAsync(x => x.Id == id, ct);

    public async Task AgregarAsync(Persona persona, CancellationToken ct)
    {
        PersonaPersistenceRows rows = PersonaPersistenceMapper.ToRows(persona);
        db.Personas.Add(rows.Persona);
        AddProfile(rows);
        await SaveWithConflictTranslationAsync(ct);
    }

    public async Task<Persona?> ObtenerAsync(Guid id, CancellationToken ct)
    {
        PersonaRecord? row = await db.Personas.AsNoTracking().SingleOrDefaultAsync(x => x.IdPersona == id, ct);
        return row is null ? null : await ToDomainAsync(row, ct);
    }

    public async Task<PagedResult<Persona>> ListarAsync(PersonaFilter filter, CancellationToken ct)
    {
        IQueryable<PersonaRecord> query = db.Personas.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Nombre))
        {
            string name = filter.Nombre.Trim();
            query = query.Where(x => EF.Functions.ILike(x.NombrePersona, $"%{EscapeLike(name)}%"));
        }
        if (filter.TipoPerfil is { } type)
        {
            query = type switch
            {
                TipoPerfil.Estudiante => query.Where(p => db.Estudiantes.Any(x => x.IdEstudiante == p.IdPersona)),
                TipoPerfil.Coordinador => query.Where(p => db.Coordinadores.Any(x => x.IdCoordinador == p.IdPersona)),
                TipoPerfil.JefeDepartamento => query.Where(p => db.JefesDepartamento.Any(x => x.IdJefeDepartamento == p.IdPersona)),
                _ => query.Where(_ => false)
            };
        }

        int total = await query.CountAsync(ct);
        List<PersonaRecord> pageRows = await query
            .OrderBy(x => x.NombrePersona).ThenBy(x => x.IdPersona)
            .Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToListAsync(ct);
        List<Persona> people = await ToDomainsAsync(pageRows, ct);
        return new PagedResult<Persona>(people, filter.Page, filter.PageSize, total);
    }

    public async Task GuardarCambiosAsync(Persona persona, CancellationToken ct)
    {
        PersonaRecord? row = await db.Personas.SingleOrDefaultAsync(x => x.IdPersona == persona.Id, ct);
        if (row is null) throw new ResourceNotFoundException($"No existe la persona '{persona.Id}'.");
        row.NombrePersona = persona.Nombre;

        switch (persona.Perfil)
        {
            case Timeblock.Domain.Personas.PerfilEstudiante perfil:
                var student = await db.Estudiantes.SingleOrDefaultAsync(x => x.IdEstudiante == persona.Id, ct);
                if (student is null) throw new ResourceConflictException("La persona no tiene un perfil de estudiante persistido.");
                student.Edad = perfil.Edad; student.Grupo = perfil.Grupo; student.CoordinadorAsignadoId = perfil.CoordinadorId;
                break;
            case Timeblock.Domain.Personas.PerfilCoordinador perfil:
                var coordinator = await db.Coordinadores.SingleOrDefaultAsync(x => x.IdCoordinador == persona.Id, ct);
                if (coordinator is null) throw new ResourceConflictException("La persona no tiene un perfil de coordinador persistido.");
                coordinator.Especialidad = perfil.Especialidad;
                break;
            case Timeblock.Domain.Personas.PerfilJefeDepartamento:
                if (!await db.JefesDepartamento.AnyAsync(x => x.IdJefeDepartamento == persona.Id, ct))
                    throw new ResourceConflictException("La persona no tiene un perfil de jefe de departamento persistido.");
                break;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<AutorizacionCoordinador>> ListarAutorizacionesAsync(
        Guid coordinatorId, AutorizacionFilter filter, CancellationToken ct)
    {
        IQueryable<AutorizacionCoordinador> query = db.Autorizaciones.AsNoTracking()
            .Where(x => x.CoordinadorId == coordinatorId);
        if (filter.ProgramaId is { } programId) query = query.Where(x => x.ProgramaId == programId);
        if (filter.VigenteEn is { } date)
            query = query.Where(x => x.FechaInicio <= date && (x.FechaFin == null || x.FechaFin >= date));
        return await query.OrderBy(x => x.ProgramaId).ThenBy(x => x.FechaInicio).ToListAsync(ct);
    }

    public async Task AgregarAutorizacionAsync(AutorizacionCoordinador authorization, CancellationToken ct)
    {
        bool duplicate = await db.Autorizaciones.AnyAsync(x =>
            x.CoordinadorId == authorization.CoordinadorId &&
            x.ProgramaId == authorization.ProgramaId &&
            x.FechaInicio == authorization.FechaInicio, ct);
        if (duplicate)
            throw new ResourceConflictException("Ya existe una autorización con esa clave.");

        db.Autorizaciones.Add(authorization);
        await SaveWithConflictTranslationAsync(ct);
    }

    public Task<AutorizacionCoordinador?> ObtenerAutorizacionAsync(
        Guid coordinatorId, Guid programId, DateOnly start, CancellationToken ct) =>
        db.Autorizaciones.SingleOrDefaultAsync(x => x.CoordinadorId == coordinatorId &&
            x.ProgramaId == programId && x.FechaInicio == start, ct);

    public Task GuardarAutorizacionAsync(AutorizacionCoordinador authorization, CancellationToken ct) =>
        db.SaveChangesAsync(ct);

    private async Task<Persona> ToDomainAsync(PersonaRecord row, CancellationToken ct)
    {
        var rows = new PersonaPersistenceRows(
            row,
            await db.Estudiantes.AsNoTracking().SingleOrDefaultAsync(x => x.IdEstudiante == row.IdPersona, ct),
            await db.Coordinadores.AsNoTracking().SingleOrDefaultAsync(x => x.IdCoordinador == row.IdPersona, ct),
            await db.JefesDepartamento.AsNoTracking().SingleOrDefaultAsync(x => x.IdJefeDepartamento == row.IdPersona, ct));
        return PersonaPersistenceMapper.ToDomain(rows);
    }

    private async Task<List<Persona>> ToDomainsAsync(IReadOnlyCollection<PersonaRecord> records, CancellationToken ct)
    {
        if (records.Count == 0) return [];
        Guid[] ids = records.Select(x => x.IdPersona).ToArray();
        var students = await db.Estudiantes.AsNoTracking().Where(x => ids.Contains(x.IdEstudiante)).ToDictionaryAsync(x => x.IdEstudiante, ct);
        var coordinators = await db.Coordinadores.AsNoTracking().Where(x => ids.Contains(x.IdCoordinador)).ToDictionaryAsync(x => x.IdCoordinador, ct);
        var departmentHeads = await db.JefesDepartamento.AsNoTracking().Where(x => ids.Contains(x.IdJefeDepartamento)).ToDictionaryAsync(x => x.IdJefeDepartamento, ct);

        return records.Select(row => PersonaPersistenceMapper.ToDomain(new PersonaPersistenceRows(
            row,
            students.GetValueOrDefault(row.IdPersona),
            coordinators.GetValueOrDefault(row.IdPersona),
            departmentHeads.GetValueOrDefault(row.IdPersona)))).ToList();
    }

    private void AddProfile(PersonaPersistenceRows rows)
    {
        if (rows.Estudiante is not null) db.Estudiantes.Add(rows.Estudiante);
        if (rows.Coordinador is not null) db.Coordinadores.Add(rows.Coordinador);
        if (rows.JefeDepartamento is not null) db.JefesDepartamento.Add(rows.JefeDepartamento);
    }

    private async Task SaveWithConflictTranslationAsync(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ResourceConflictException("El recurso ya existe o la clave indicada está duplicada.", ex);
        }
    }

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
