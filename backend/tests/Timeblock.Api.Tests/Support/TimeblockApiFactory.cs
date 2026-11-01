using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Timeblock.Application;

namespace Timeblock.Api.Tests.Support;

/// <summary>
/// Levanta la API en memoria, en entorno Development, con los endpoints de
/// <see cref="PruebasController"/> y sin acceder a PostgreSQL.
/// </summary>
public sealed class TimeblockApiFactory : WebApplicationFactory<Program>
{
    static TimeblockApiFactory()
    {
        // Program exige la cadena de conexión al arrancar. El DbContext no se
        // conecta mientras ninguna prueba lo use.
        if (string.IsNullOrWhiteSpace(
                Environment.GetEnvironmentVariable("ConnectionStrings__Timeblock")))
        {
            Environment.SetEnvironmentVariable(
                "ConnectionStrings__Timeblock",
                "Host=localhost;Database=timeblock_api_tests;Username=test;Password=test");
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            services.AddControllers()
                .AddApplicationPart(typeof(TimeblockApiFactory).Assembly);
            services.AddCasosDeUso(typeof(TimeblockApiFactory).Assembly);
        });
    }
}
