using Microsoft.EntityFrameworkCore;
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

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        EnsureReferencedDates();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        await EnsureReferencedDatesAsync(cancellationToken);
        return await base.SaveChangesAsync(
            acceptAllChangesOnSuccess,
            cancellationToken);
    }

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

    private void EnsureReferencedDates()
    {
        DateOnly[] missingDates = GetReferencedDatesNotTracked();
        if (missingDates.Length == 0)
        {
            return;
        }

        HashSet<DateOnly> existingDates = Fechas
            .AsNoTracking()
            .Where(fecha => missingDates.Contains(fecha.Fecha))
            .Select(fecha => fecha.Fecha)
            .ToHashSet();

        foreach (DateOnly fecha in missingDates)
        {
            if (!existingDates.Contains(fecha))
            {
                Fechas.Add(new FechaRecord { Fecha = fecha });
            }
        }
    }

    private async Task EnsureReferencedDatesAsync(CancellationToken cancellationToken)
    {
        DateOnly[] missingDates = GetReferencedDatesNotTracked();
        if (missingDates.Length == 0)
        {
            return;
        }

        HashSet<DateOnly> existingDates = (await Fechas
                .AsNoTracking()
                .Where(fecha => missingDates.Contains(fecha.Fecha))
                .Select(fecha => fecha.Fecha)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        foreach (DateOnly fecha in missingDates)
        {
            if (!existingDates.Contains(fecha))
            {
                Fechas.Add(new FechaRecord { Fecha = fecha });
            }
        }
    }

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
