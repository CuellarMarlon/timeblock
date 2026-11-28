using Microsoft.EntityFrameworkCore;
using Timeblock.Application.Aulas;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Common.Pagination;
using Timeblock.Domain.Aulas;
using Timeblock.Infrastructure.Persistence;

namespace Timeblock.Infrastructure.Tests;

public sealed class AulaRepositoryIntegrationTests
{
    private static async Task<TimeblockDbContext> AbrirAsync()
    {
        DbContextOptions<TimeblockDbContext> options =
            new DbContextOptionsBuilder<TimeblockDbContext>()
                .UseNpgsql(Environment.GetEnvironmentVariable("TIMEBLOCK_TEST_CONNECTION")!)
                .Options;

        var db = new TimeblockDbContext(options);
        await db.Database.MigrateAsync();
        return db;
    }

    [PostgreSqlFact]
    public async Task CrearObtenerYActualizar_PersistenLosDatos()
    {
        await using TimeblockDbContext db = await AbrirAsync();
        await using var transaccion = await db.Database.BeginTransactionAsync();
        var servicio = new AulasService(new AulaRepository(db));
        CancellationToken ct = CancellationToken.None;

        Aula creada = await servicio.CrearAsync($"  Edificio {Guid.NewGuid():N} ", 30, ct);
        Aula leida = await servicio.ObtenerAsync(creada.Id, ct);

        Assert.Equal(creada.Id, leida.Id);
        Assert.Equal(creada.Edificio, leida.Edificio);
        Assert.Equal(30, leida.Capacidad);

        string nuevoEdificio = $"Anexo {Guid.NewGuid():N}";
        await servicio.ActualizarAsync(creada.Id, nuevoEdificio, 45, ct);
        Aula actualizada = await servicio.ObtenerAsync(creada.Id, ct);

        Assert.Equal(creada.Id, actualizada.Id);
        Assert.Equal(nuevoEdificio, actualizada.Edificio);
        Assert.Equal(45, actualizada.Capacidad);

        await transaccion.RollbackAsync(ct);
    }

    [PostgreSqlFact]
    public async Task Actualizar_AulaInexistente_LanzaNoEncontrado()
    {
        await using TimeblockDbContext db = await AbrirAsync();
        await using var transaccion = await db.Database.BeginTransactionAsync();
        var repositorio = new AulaRepository(db);
        var servicio = new AulasService(repositorio);
        CancellationToken ct = CancellationToken.None;

        Assert.False(await repositorio.ActualizarAsync(
            Aula.Crear(Guid.NewGuid(), "Edificio A", 30), ct));
        await Assert.ThrowsAsync<ResourceNotFoundException>(() =>
            servicio.ActualizarAsync(Guid.NewGuid(), "Edificio A", 30, ct));

        await transaccion.RollbackAsync(ct);
    }

    [PostgreSqlFact]
    public async Task Obtener_AulaInexistente_DevuelveNull()
    {
        await using TimeblockDbContext db = await AbrirAsync();
        var repositorio = new AulaRepository(db);

        Assert.Null(await repositorio.ObtenerAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [PostgreSqlFact]
    public async Task Listar_OrdenaPorEdificioCapacidadEIdYPagina()
    {
        await using TimeblockDbContext db = await AbrirAsync();
        await using var transaccion = await db.Database.BeginTransactionAsync();
        var servicio = new AulasService(new AulaRepository(db));
        CancellationToken ct = CancellationToken.None;

        // La marca aísla estas aulas de las que ya haya en la base de pruebas.
        string marca = Guid.NewGuid().ToString("N");
        await servicio.CrearAsync($"{marca} B", 20, ct);
        await servicio.CrearAsync($"{marca} A", 45, ct);
        await servicio.CrearAsync($"{marca} A", 30, ct);

        PagedResult<Aula> primera = await servicio.ListarAsync(marca, 1, 2, ct);
        PagedResult<Aula> segunda = await servicio.ListarAsync(marca, 2, 2, ct);

        Assert.Equal(3, primera.TotalItems);
        Assert.Equal(
            new[] { $"{marca} A:30", $"{marca} A:45" },
            primera.Items.Select(aula => $"{aula.Edificio}:{aula.Capacidad}").ToArray());
        Aula ultima = Assert.Single(segunda.Items);
        Assert.Equal($"{marca} B", ultima.Edificio);
        Assert.Equal(20, ultima.Capacidad);

        await transaccion.RollbackAsync(ct);
    }

    [PostgreSqlFact]
    public async Task Listar_FiltroNoDistingueMayusculasYEscapaLosComodines()
    {
        await using TimeblockDbContext db = await AbrirAsync();
        await using var transaccion = await db.Database.BeginTransactionAsync();
        var servicio = new AulasService(new AulaRepository(db));
        CancellationToken ct = CancellationToken.None;

        string marca = Guid.NewGuid().ToString("N");
        await servicio.CrearAsync($"{marca}_A", 10, ct);
        await servicio.CrearAsync($"{marca}-A", 10, ct);

        PagedResult<Aula> sinMayusculas = await servicio.ListarAsync(
            marca.ToUpperInvariant(), null, null, ct);
        // "_" es un comodín de LIKE: debe tratarse como un carácter literal.
        PagedResult<Aula> conGuionBajo = await servicio.ListarAsync(
            $"{marca}_", null, null, ct);
        PagedResult<Aula> conPorcentaje = await servicio.ListarAsync(
            $"{marca}%", null, null, ct);

        Assert.Equal(2, sinMayusculas.TotalItems);
        Assert.Equal($"{marca}_A", Assert.Single(conGuionBajo.Items).Edificio);
        Assert.Empty(conPorcentaje.Items);

        await transaccion.RollbackAsync(ct);
    }

    [PostgreSqlFact]
    public async Task LaRestriccionDeCapacidadPositivaSigueVigenteEnLaBase()
    {
        await using TimeblockDbContext db = await AbrirAsync();
        await using var transaccion = await db.Database.BeginTransactionAsync();

        db.Aulas.Add(new AulaRecord
        {
            IdAula = Guid.NewGuid(),
            Edificio = "Edificio A",
            Capacidad = 0,
        });

        await Assert.ThrowsAsync<DbUpdateException>(
            () => db.SaveChangesAsync(CancellationToken.None));

        await transaccion.RollbackAsync(CancellationToken.None);
    }
}
