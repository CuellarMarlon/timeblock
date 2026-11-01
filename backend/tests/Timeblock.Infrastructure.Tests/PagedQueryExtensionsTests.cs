using Microsoft.EntityFrameworkCore;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Catalogo;
using Timeblock.Infrastructure.Persistence;

namespace Timeblock.Infrastructure.Tests;

public sealed class PagedQueryExtensionsTests
{
    [PostgreSqlFact]
    public async Task ToPagedResultAsync_DevuelveLaPaginaSolicitadaYElTotal()
    {
        string connectionString = Environment.GetEnvironmentVariable(
            "TIMEBLOCK_TEST_CONNECTION")!;

        var options = new DbContextOptionsBuilder<TimeblockDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new TimeblockDbContext(options);
        await context.Database.MigrateAsync();
        await using var transaction = await context.Database.BeginTransactionAsync();

        for (int numero = 1; numero <= 5; numero++)
        {
            context.Asignaturas.Add(
                Asignatura.Crear(Guid.NewGuid(), $"Paginación {numero:00}"));
        }

        await context.SaveChangesAsync();

        IOrderedQueryable<Asignatura> consulta = context.Asignaturas
            .Where(asignatura => asignatura.Nombre.StartsWith("Paginación "))
            .OrderBy(asignatura => asignatura.Nombre);

        PagedResult<Asignatura> pagina =
            await consulta.ToPagedResultAsync(PageRequest.Create(2, 2));

        Assert.Equal(5, pagina.TotalItems);
        Assert.Equal(2, pagina.Page);
        Assert.Equal(2, pagina.PageSize);
        Assert.Equal(
            new[] { "Paginación 03", "Paginación 04" },
            pagina.Items.Select(asignatura => asignatura.Nombre));
    }
}
