using System.Text.Json;

namespace Timeblock.Api.Tests.Support;

internal static class RespuestaExtensions
{
    public static async Task<JsonElement> LeerJsonAsync(
        this HttpResponseMessage respuesta)
    {
        string contenido = await respuesta.Content.ReadAsStringAsync();
        using JsonDocument documento = JsonDocument.Parse(contenido);
        return documento.RootElement.Clone();
    }

    public static StringContent Json(string contenido) =>
        new(contenido, System.Text.Encoding.UTF8, "application/json");
}
