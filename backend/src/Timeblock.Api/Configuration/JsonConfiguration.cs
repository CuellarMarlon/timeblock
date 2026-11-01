using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using Microsoft.AspNetCore.Http.Json;

namespace Timeblock.Api.Configuration;

public static class JsonConfiguration
{
   
    public static IServiceCollection AddTimeblockJson(
        this IServiceCollection services)
    {
        // Los controladores usan las opciones de MVC y los Problem Details y
        // los endpoints mínimos usan las de HTTP; ambas deben coincidir.
        services.Configure<JsonOptions>(
            options => Configure(options.SerializerOptions));
        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(
            options => Configure(options.JsonSerializerOptions));

        return services;
    }

    public static void Configure(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;

        // Mantiene legibles los acentos y escapa caracteres de marcado HTML.
        options.Encoder = JavaScriptEncoder.Create(
            UnicodeRanges.BasicLatin,
            UnicodeRanges.Latin1Supplement,
            UnicodeRanges.LatinExtendedA);

        if (!options.Converters.OfType<JsonStringEnumConverter>().Any())
        {
            options.Converters.Add(new JsonStringEnumConverter(
                JsonNamingPolicy.CamelCase,
                allowIntegerValues: false));
        }

        if (!options.Converters.OfType<UtcDateTimeOffsetConverter>().Any())
        {
            options.Converters.Add(new UtcDateTimeOffsetConverter());
        }
    }
}
