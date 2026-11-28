using Microsoft.EntityFrameworkCore;
using Timeblock.Application.Aulas;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Aulas;

namespace Timeblock.Infrastructure.Persistence;

public sealed class AulaRepository(TimeblockDbContext db) : IAulaRepository
{
    public async Task AgregarAsync(Aula aula, CancellationToken ct)
    {
        db.Aulas.Add(new AulaRecord
        {
            IdAula = aula.Id,
            Edificio = aula.Edificio,
            Capacidad = aula.Capacidad,
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task<Aula?> ObtenerAsync(Guid aulaId, CancellationToken ct)
    {
        AulaRecord? registro = await db.Aulas
            .AsNoTracking()
            .SingleOrDefaultAsync(aula => aula.IdAula == aulaId, ct);

        return registro is null ? null : ADominio(registro);
    }

    public async Task<PagedResult<Aula>> ListarAsync(
        string? edificio,
        PageRequest page,
        CancellationToken ct)
    {
        IQueryable<AulaRecord> consulta = db.Aulas.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(edificio))
        {
            string patron = $"%{EscaparLike(edificio.Trim())}%";
            // El escape se indica de forma explícita: así "%" y "_" del texto
            // buscado se tratan como caracteres literales y no como comodines.
            consulta = consulta.Where(aula =>
                EF.Functions.ILike(aula.Edificio, patron, "\\"));
        }

        // El identificador desempata para que la paginación sea estable.
        PagedResult<AulaRecord> pagina = await consulta
            .OrderBy(aula => aula.Edificio)
            .ThenBy(aula => aula.Capacidad)
            .ThenBy(aula => aula.IdAula)
            .ToPagedResultAsync(page, ct);

        return pagina.Map(ADominio);
    }

    public async Task<bool> ActualizarAsync(Aula aula, CancellationToken ct)
    {
        int filas = await db.Aulas
            .Where(registro => registro.IdAula == aula.Id)
            .ExecuteUpdateAsync(
                cambios => cambios
                    .SetProperty(registro => registro.Edificio, aula.Edificio)
                    .SetProperty(registro => registro.Capacidad, aula.Capacidad),
                ct);

        return filas > 0;
    }

    private static Aula ADominio(AulaRecord registro) =>
        Aula.Reconstituir(registro.IdAula, registro.Edificio, registro.Capacidad);

    private static string EscaparLike(string valor) =>
        valor.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
