using System.Text.Json;
using System.Text.Json.Serialization;

namespace DNBot.Configuration;

public sealed class SnowflakeJsonConverter : JsonConverter<ulong>
{
    public override ulong Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String when ulong.TryParse(reader.GetString(), out var value) => value,
            JsonTokenType.Number when reader.TryGetUInt64(out var value) => value,
            _ => throw new JsonException("Expected a Discord snowflake id.")
        };
    }

    public override void Write(Utf8JsonWriter writer, ulong value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public sealed class NullableSnowflakeJsonConverter : JsonConverter<ulong?>
{
    public override ulong? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
        {
            return null;
        }

        return reader.TokenType switch
        {
            JsonTokenType.String when string.IsNullOrWhiteSpace(reader.GetString()) => null,
            JsonTokenType.String when ulong.TryParse(reader.GetString(), out var value) => value,
            JsonTokenType.Number when reader.TryGetUInt64(out var value) => value,
            _ => throw new JsonException("Expected a Discord snowflake id.")
        };
    }

    public override void Write(Utf8JsonWriter writer, ulong? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString());
    }
}
