using System.Text.Json;
using System.Text.Json.Serialization;

namespace AStarDev.WallpaperScraper;

internal sealed class NumericBooleanJsonConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt32(out int value) && value is 0 or 1 => value == 1,
            _ => throw new JsonException("Expected a boolean or numeric 0/1 value.")
        };

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
        => writer.WriteBooleanValue(value);
}
