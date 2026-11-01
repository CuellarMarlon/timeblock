using Microsoft.EntityFrameworkCore;
using Timeblock.Api.Configuration;
using Timeblock.Api.Errors;
using Timeblock.Application;
using Timeblock.Application.Personas;
using Timeblock.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

string connectionString = builder.Configuration.GetConnectionString("Timeblock")
    ?? throw new InvalidOperationException(
        "No se configuró la cadena de conexión ConnectionStrings:Timeblock.");

builder.Services.AddDbContext<TimeblockDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<IPersonaRepository, PersonaRepository>();
builder.Services.AddScoped<PersonasService>();

builder.Services.AddApplication();
builder.Services.AddControllers();
builder.Services.AddAuthorization();

builder.Services.AddTimeblockJson();
builder.Services.AddTimeblockProblemDetails();
builder.Services.AddTimeblockCors(builder.Configuration);
builder.Services.AddTimeblockOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors(CorsConfiguration.PolicyName);
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;