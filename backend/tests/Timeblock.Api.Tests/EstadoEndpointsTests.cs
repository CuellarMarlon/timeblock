using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Timeblock.Api.Tests.Support;
using Timeblock.Application.Catalogo;
using Timeblock.Application.Personas;
using Timeblock.Domain.Catalogo;
using Timeblock.Domain.Personas;

namespace Timeblock.Api.Tests;

public sealed class EstadoEndpointsTests(TimeblockApiFactory factory) : IClassFixture<TimeblockApiFactory>
{
    [Theory]
    [InlineData("personas")]
    [InlineData("programas")]
    [InlineData("asignaturas")]
    public async Task CambiarEstado_DesactivaYReactiva(string recurso)
    {
        Guid id = Guid.NewGuid();
        IPersonaRepository personas = DispatchProxy.Create<IPersonaRepository, PersonaProxy>();
        ((PersonaProxy)personas).Persona = Persona.Crear(id, "Persona", new PerfilJefeDepartamento());
        ICatalogoRepository catalogo = DispatchProxy.Create<ICatalogoRepository, CatalogoProxy>();
        ((CatalogoProxy)catalogo).Programa = ProgramaAcademico.Crear(id, "Programa", "Plan");
        ((CatalogoProxy)catalogo).Asignatura = Asignatura.Crear(id, "Asignatura");
        using var app = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.Replace(ServiceDescriptor.Scoped<IPersonaRepository>(_ => personas));
            services.Replace(ServiceDescriptor.Scoped<ICatalogoRepository>(_ => catalogo));
        }));
        using var cliente = app.CreateClient();
        using var inactivar = await cliente.PutAsync($"/api/{recurso}/{id}/estado", RespuestaExtensions.Json("""{"activo":false}"""));
        Assert.Equal(HttpStatusCode.OK, inactivar.StatusCode);
        Assert.False((await inactivar.LeerJsonAsync()).GetProperty("activo").GetBoolean());
        using var reactivar = await cliente.PutAsync($"/api/{recurso}/{id}/estado", RespuestaExtensions.Json("""{"activo":true}"""));
        Assert.Equal(HttpStatusCode.OK, reactivar.StatusCode);
        Assert.True((await reactivar.LeerJsonAsync()).GetProperty("activo").GetBoolean());
    }

    public class PersonaProxy : DispatchProxy
    {
        public Persona Persona { get; set; } = null!;
        protected override object? Invoke(MethodInfo? metodo, object?[]? args) => metodo?.Name switch
        {
            nameof(IPersonaRepository.ObtenerAsync) => Task.FromResult<Persona?>(Persona),
            nameof(IPersonaRepository.GuardarCambiosAsync) => Task.CompletedTask,
            _ => throw new NotSupportedException(),
        };
    }

    public class CatalogoProxy : DispatchProxy
    {
        public ProgramaAcademico Programa { get; set; } = null!;
        public Asignatura Asignatura { get; set; } = null!;
        protected override object? Invoke(MethodInfo? metodo, object?[]? args) => metodo?.Name switch
        {
            nameof(ICatalogoRepository.ObtenerProgramaAsync) => Task.FromResult<ProgramaAcademico?>(Programa),
            nameof(ICatalogoRepository.ObtenerAsignaturaAsync) => Task.FromResult<Asignatura?>(Asignatura),
            nameof(ICatalogoRepository.GuardarProgramaAsync) or nameof(ICatalogoRepository.GuardarAsignaturaAsync) => Task.CompletedTask,
            _ => throw new NotSupportedException(),
        };
    }
}
