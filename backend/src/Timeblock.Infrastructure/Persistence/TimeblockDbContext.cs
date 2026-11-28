using Microsoft.EntityFrameworkCore;
using Npgsql;
using Timeblock.Application.Common.Exceptions;
using Timeblock.Domain.Bloques;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Horarios;
using Timeblock.Domain.Matriculas;
using Timeblock.Domain.Personas;

namespace Timeblock.Infrastructure.Persistence;

public sealed class TimeblockDbContext(
    DbContextOptions<TimeblockDbContext> options) : DbContext(options)
{
    public DbSet<PersonaRecord> Personas => Set<PersonaRecord>();
    public DbSet<ProgramaAcademico> Programas => Set<ProgramaAcademico>();
    public DbSet<Asignatura> Asignaturas => Set<Asignatura>();
    public DbSet<ProgramaAsignatura> ProgramasAsignaturas => Set<ProgramaAsignatura>();
    public DbSet<Matricula> Matriculas => Set<Matricula>();
    public DbSet<AutorizacionCoordinador> Autorizaciones => Set<AutorizacionCoordinador>();
    public DbSet<BloqueContenido> Bloques => Set<BloqueContenido>();
    public DbSet<BloquePrograma> BloquesProgramas => Set<BloquePrograma>();
    public DbSet<Horario> Horarios => Set<Horario>();
    public DbSet<BloqueEnHorario> BloquesEnHorario => Set<BloqueEnHorario>();

    // Filas de infraestructura para el esquema relacional normalizado.
    public DbSet<EstudiantePerfilRecord> Estudiantes => Set<EstudiantePerfilRecord>();
    public DbSet<CoordinadorPerfilRecord> Coordinadores => Set<CoordinadorPerfilRecord>();
    public DbSet<JefeDepartamentoPerfilRecord> JefesDepartamento => Set<JefeDepartamentoPerfilRecord>();
    public DbSet<FechaRecord> Fechas => Set<FechaRecord>();
    public DbSet<AulaRecord> Aulas => Set<AulaRecord>();
    public DbSet<EsquemaLlaveRecord> EsquemaLlave => Set<EsquemaLlaveRecord>();

    /// <summary>
    /// Guarda los cambios dando de alta antes en FECHA las fechas que
    /// referencian. Si no hay una transacción abierta, abre una para que el
    /// alta de las fechas y el resto de los cambios se confirmen o se deshagan
    /// juntos.
    /// </summary>
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        DateOnly[] fechas = GetReferencedDatesNotTracked();
        if (fechas.Length == 0 || Database.CurrentTransaction is not null)
        {
            InsertarFechas(fechas);
            return Guardar(acceptAllChangesOnSuccess);
        }

        using var transaccion = Database.BeginTransaction();
        InsertarFechas(fechas);
        int filas = Guardar(acceptAllChangesOnSuccess);
        transaccion.Commit();
        return filas;
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        DateOnly[] fechas = GetReferencedDatesNotTracked();
        if (fechas.Length == 0 || Database.CurrentTransaction is not null)
        {
            await InsertarFechasAsync(fechas, cancellationToken);
            return await GuardarAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        await using var transaccion = await Database.BeginTransactionAsync(cancellationToken);
        await InsertarFechasAsync(fechas, cancellationToken);
        int filas = await GuardarAsync(acceptAllChangesOnSuccess, cancellationToken);
        await transaccion.CommitAsync(cancellationToken);
        return filas;
    }

    private int Guardar(bool acceptAllChangesOnSuccess)
    {
        try { return base.SaveChanges(acceptAllChangesOnSuccess); }
        catch (DbUpdateException ex) when (EsRegistroInactivo(ex))
        { throw new ResourceConflictException("No se puede usar un registro inactivo para esta operación.", ex); }
    }

    private async Task<int> GuardarAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken)
    {
        try { return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken); }
        catch (DbUpdateException ex) when (EsRegistroInactivo(ex))
        { throw new ResourceConflictException("No se puede usar un registro inactivo para esta operación.", ex); }
    }

    private static bool EsRegistroInactivo(DbUpdateException ex) =>
        ex.InnerException is PostgresException error && error.SqlState == PostgresErrorCodes.CheckViolation &&
        error.MessageText.StartsWith("REGISTRO_INACTIVO:", StringComparison.Ordinal);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Perfil es una jerarquía de objetos del dominio, no una tabla propia.
        // Su almacenamiento normalizado se configurará en tablas de perfiles.
        modelBuilder.Ignore<Persona>();
        modelBuilder.Ignore<Perfil>();

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TimeblockDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Da de alta en FECHA las fechas indicadas con <c>ON CONFLICT DO
    /// NOTHING</c>. Si otra transacción está dando de alta la misma fecha,
    /// PostgreSQL espera a que termine y no falla por la clave primaria, como
    /// ocurría al consultar primero y luego insertar.
    /// </summary>
    private void InsertarFechas(DateOnly[] fechas)
    {
        if (fechas.Length > 0)
        {
            Database.ExecuteSql(InsertarFechasSql(fechas));
        }
    }

    private async Task InsertarFechasAsync(DateOnly[] fechas, CancellationToken cancellationToken)
    {
        if (fechas.Length > 0)
        {
            await Database.ExecuteSqlAsync(InsertarFechasSql(fechas), cancellationToken);
        }
    }

    private static FormattableString InsertarFechasSql(DateOnly[] fechas) =>
        $"""
        INSERT INTO "FECHA" ("fecha")
        SELECT unnest({fechas})
        ON CONFLICT ("fecha") DO NOTHING
        """;

    private DateOnly[] GetReferencedDatesNotTracked()
    {
        var referencedDates = new HashSet<DateOnly>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
            {
                continue;
            }

            switch (entry.Entity)
            {
                case AutorizacionCoordinador autorizacion:
                    referencedDates.Add(autorizacion.FechaInicio);
                    if (autorizacion.FechaFin is DateOnly fechaFin)
                    {
                        referencedDates.Add(fechaFin);
                    }
                    break;

                case BloqueEnHorario asociacion:
                    referencedDates.Add(asociacion.Fecha);
                    break;

                case EsquemaLlaveRecord clave:
                    referencedDates.Add(clave.FechaInicio);
                    referencedDates.Add(clave.Fecha);
                    break;
            }
        }

        HashSet<DateOnly> localDates = Fechas.Local
            .Select(fecha => fecha.Fecha)
            .ToHashSet();

        return referencedDates
            .Where(fecha => !localDates.Contains(fecha))
            .ToArray();
    }
}
