using Microsoft.EntityFrameworkCore;
using Timeblock.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

string connectionString = builder.Configuration.GetConnectionString("Timeblock")
    ?? throw new InvalidOperationException(
        "No se configuró la cadena de conexión ConnectionStrings:Timeblock.");

builder.Services.AddDbContext<TimeblockDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
