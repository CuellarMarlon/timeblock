using System.Text.Json;
using System.Text.Json.Serialization;

namespace Timeblock.Api.Configuration;


/// Escribe las marcas de tiempo en ISO 8601 UTC con el sufijo <c>Z</c>
/// (por ejemplo, <c>2026-10-12T14:30:00Z</c>), como indica el contrato.

public sealed class UtcDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) =>
        reader.GetDateTimeOffset().ToUniversalTime();

    public override void Write(
        Utf8JsonWriter writer,
        DateTimeOffset value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(value.UtcDateTime);
}
