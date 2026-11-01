using Timeblock.Api.Errors;
using Timeblock.Domain.Common;

namespace Timeblock.Api.Tests;

public sealed class ErroresHttpTests
{
    [Theory]
    [InlineData(TipoErrorDominio.Validacion, 400)]
    [InlineData(TipoErrorDominio.AutorizacionNegocio, 403)]
    [InlineData(TipoErrorDominio.NoEncontrado, 404)]
    [InlineData(TipoErrorDominio.Conflicto, 409)]
    public void StatusDe_TraduceCadaTipoAlCodigoDelContrato(
        TipoErrorDominio tipo,
        int esperado)
    {
        Assert.Equal(esperado, ErroresHttp.StatusDe(tipo));
    }

    [Fact]
    public void StatusDe_CubreTodosLosTiposDeError()
    {
        foreach (TipoErrorDominio tipo in Enum.GetValues<TipoErrorDominio>())
        {
            Assert.NotEqual(500, ErroresHttp.StatusDe(tipo));
        }
    }

    [Theory]
    [InlineData(400)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(500)]
    public void TituloDe_TieneTextoParaLosCodigosDelContrato(int status)
    {
        Assert.False(string.IsNullOrWhiteSpace(ErroresHttp.TituloDe(status)));
    }
}
