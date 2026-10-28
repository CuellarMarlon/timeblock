namespace Timeblock.Domain.Common;

internal static class Guard
{
    public static string Requerido(string? valor, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DomainException($"El campo {campo} es obligatorio.");
        }

        return valor.Trim();
    }

    public static Guid IdValido(Guid id, string campo)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException($"El identificador {campo} no es válido.");
        }

        return id;
    }

    public static TEnum EnumValido<TEnum>(TEnum valor, string campo)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(valor))
        {
            throw new DomainException($"El valor de {campo} no es válido.");
        }

        return valor;
    }
}
