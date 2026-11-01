using Timeblock.Application.Common.Exceptions;
using Timeblock.Application.Personas;
using Timeblock.Domain.Personas;

namespace Timeblock.Application.Tests.Personas;

public sealed class PersonasServiceTests
{
    [Fact]
    public async Task Crear_Coordinador_GuardaUnaPersonaConSuPerfil()
    {
        var repository = new FakePersonaRepository();
        var service = new PersonasService(repository);

        Persona result = await service.CrearAsync(
            new CrearPersonaCommand(" Luis García ", new PerfilCoordinadorCrearCommand("Bases de datos")),
            CancellationToken.None);

        Assert.Equal("Luis García", result.Nombre);
        Assert.IsType<PerfilCoordinador>(result.Perfil);
        Assert.Same(result, Assert.Single(repository.Personas.Values));
    }

    [Fact]
    public async Task Crear_EstudianteConCoordinadorInexistente_LanzaNotFound()
    {
        var service = new PersonasService(new FakePersonaRepository());

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => service.CrearAsync(
            new CrearPersonaCommand("Ana", new PerfilEstudianteCrearCommand(20, "C311", Guid.NewGuid())),
            CancellationToken.None));
    }

    [Fact]
    public async Task Crear_EstudianteConEdadInvalida_LanzaBadRequestAntesDeConsultarCoordinador()
    {
        var repository = new FakePersonaRepository();
        var service = new PersonasService(repository);

        await Assert.ThrowsAsync<InvalidRequestException>(() => service.CrearAsync(
            new CrearPersonaCommand("Ana", new PerfilEstudianteCrearCommand(0, "C311", Guid.NewGuid())),
            CancellationToken.None));
        Assert.Equal(0, repository.CoordinatorChecks);
    }

    [Fact]
    public async Task ActualizarPerfil_NoPermiteCambiarElTipo()
    {
        var repository = new FakePersonaRepository();
        var coordinator = Persona.Crear(Guid.NewGuid(), "Luis", new PerfilCoordinador("Sistemas"));
        repository.Personas.Add(coordinator.Id, coordinator);
        var service = new PersonasService(repository);

        await Assert.ThrowsAsync<InvalidRequestException>(() => service.ActualizarPerfilAsync(
            coordinator.Id, new PerfilJefeDepartamentoCrearCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task CrearAutorizacion_RechazaFechaFinAnteriorAInicio()
    {
        var repository = new FakePersonaRepository();
        Guid coordinatorId = Guid.NewGuid();
        Guid programId = Guid.NewGuid();
        repository.Coordinators.Add(coordinatorId);
        repository.Programs.Add(programId);
        var service = new PersonasService(repository);

        await Assert.ThrowsAsync<InvalidRequestException>(() => service.CrearAutorizacionAsync(
            coordinatorId, programId, new DateOnly(2026, 10, 12), new DateOnly(2026, 10, 11),
            true, false, CancellationToken.None));
    }

    [Fact]
    public async Task Listar_RechazaPaginacionFueraDeRango()
    {
        var service = new PersonasService(new FakePersonaRepository());
        await Assert.ThrowsAsync<InvalidRequestException>(() => service.ListarAsync(
            new PersonaFilter(null, null, 1, 101), CancellationToken.None));
    }

    private sealed class FakePersonaRepository : IPersonaRepository
    {
        public Dictionary<Guid, Persona> Personas { get; } = [];
        public HashSet<Guid> Coordinators { get; } = [];
        public HashSet<Guid> Programs { get; } = [];
        public int CoordinatorChecks { get; private set; }

        public Task<bool> ExisteCoordinadorAsync(Guid id, CancellationToken ct)
        {
            CoordinatorChecks++;
            return Task.FromResult(Coordinators.Contains(id));
        }

        public Task<bool> ExisteProgramaAsync(Guid id, CancellationToken ct) => Task.FromResult(Programs.Contains(id));
        public Task AgregarAsync(Persona persona, CancellationToken ct)
        {
            Personas.Add(persona.Id, persona);
            if (persona.Perfil is PerfilCoordinador) Coordinators.Add(persona.Id);
            return Task.CompletedTask;
        }
        public Task<Persona?> ObtenerAsync(Guid id, CancellationToken ct) => Task.FromResult(Personas.GetValueOrDefault(id));
        public Task<PagedResult<Persona>> ListarAsync(PersonaFilter filter, CancellationToken ct) =>
            Task.FromResult(new PagedResult<Persona>(Personas.Values.ToArray(), filter.Page, filter.PageSize, Personas.Count));
        public Task GuardarCambiosAsync(Persona persona, CancellationToken ct) => Task.CompletedTask;
        public Task<IReadOnlyList<AutorizacionCoordinador>> ListarAutorizacionesAsync(Guid id, AutorizacionFilter filter, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<AutorizacionCoordinador>>([]);
        public Task AgregarAutorizacionAsync(AutorizacionCoordinador authorization, CancellationToken ct) => Task.CompletedTask;
        public Task<AutorizacionCoordinador?> ObtenerAutorizacionAsync(Guid coordinatorId, Guid programId, DateOnly start, CancellationToken ct) =>
            Task.FromResult<AutorizacionCoordinador?>(null);
        public Task GuardarAutorizacionAsync(AutorizacionCoordinador authorization, CancellationToken ct) => Task.CompletedTask;
    }
}
