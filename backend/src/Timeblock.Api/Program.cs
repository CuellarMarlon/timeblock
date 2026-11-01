using Microsoft.EntityFrameworkCore;
using Timeblock.Api.Configuration;
using Timeblock.Api.Errors;
using Timeblock.Application;
using Timeblock.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string connectionString = builder.Configuration.GetConnectionString("Timeblock")
    ?? throw new InvalidOperationException(
        "No se configuró la cadena de conexión ConnectionStrings:Timeblock.");

builder.Services.AddDbContext<TimeblockDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddApplication();

builder.Services.AddControllers();
builder.Services.AddTimeblockJson();
builder.Services.AddTimeblockProblemDetails();
builder.Services.AddTimeblockCors(builder.Configuration);
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddTimeblockOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors(CorsConfiguration.PolicyName);

app.UseAuthorization();

app.MapControllers();

app.Run();

// Permite que WebApplicationFactory<Program> use esta aplicación en las pruebas.
public partial class Program;
