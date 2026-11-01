using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Timeblock.Api.Tests.Support;

namespace Timeblock.Api.Tests;

public sealed class IntegracionConApplicationTests(TimeblockApiFactory factory)
    : IClassFixture<TimeblockApiFactory>
{
    [Fact]
    public void Api_ResuelveLosServiciosComunesDeApplication()
    {
        using IServiceScope scope = factory.Services.CreateScope();

        TimeProvider reloj = scope.ServiceProvider.GetRequiredService<TimeProvider>();

        Assert.Same(TimeProvider.System, reloj);
    }

    [Fact]
    public async Task Api_InvocaUnCasoDeUsoRegistradoPorEscaneo()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage respuesta =
            await client.GetAsync("/api/_tests/caso-de-uso");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        JsonElement json = await respuesta.LeerJsonAsync();
        Assert.Equal("eco: hola", json.GetProperty("mensaje").GetString());
    }
}
