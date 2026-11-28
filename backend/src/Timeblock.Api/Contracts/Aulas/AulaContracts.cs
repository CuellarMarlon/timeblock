using Timeblock.Domain.Aulas;

namespace Timeblock.Api.Contracts.Aulas;

public sealed record AulaRequest
{
    public string? Edificio { get; init; }

    public int? Capacidad { get; init; }
}

public sealed record AulaResponse(Guid Id, string Edificio, int Capacidad)
{
    public static AulaResponse FromDomain(Aula aula) =>
        new(aula.Id, aula.Edificio, aula.Capacidad);
}
