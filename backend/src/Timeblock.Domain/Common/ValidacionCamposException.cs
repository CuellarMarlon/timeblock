namespace Timeblock.Domain.Common;

public sealed class ValidacionCamposException : DomainException
{
    public const string MensajeGeneral = "Revisa los campos indicados.";

    public ValidacionCamposException(
        IReadOnlyDictionary<string, string[]> errores)
        : base(MensajeGeneral, TipoErrorDominio.Validacion)
    {
        ArgumentNullException.ThrowIfNull(errores);

        if (errores.Count == 0)
        {
            throw new ArgumentException(
                "Debe indicarse al menos un campo inválido.",
                nameof(errores));
        }

        Errores = errores;
    }

    //Mensajes de error por nombre de campo (en camelCase).
    public IReadOnlyDictionary<string, string[]> Errores { get; }
}
