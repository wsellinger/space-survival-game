using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceSurvivalGame.Configuration;

/// <summary>Like FloatRange, but for integer counts — e.g. "SparkCountRange": [8, 14].</summary>
[JsonConverter(typeof(IntRangeJsonConverter))]
public readonly struct IntRange
{
    public int Min { get; }
    public int Max { get; }

    public IntRange(int min, int max)
    {
        Min = min;
        Max = max;
    }
}

public class IntRangeJsonConverter : JsonConverter<IntRange>
{
    public override IntRange Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected a [min, max] array.");

        reader.Read();
        var min = reader.GetInt32();
        reader.Read();
        var max = reader.GetInt32();
        reader.Read(); // consume EndArray

        return new IntRange(min, max);
    }

    public override void Write(Utf8JsonWriter writer, IntRange value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Min);
        writer.WriteNumberValue(value.Max);
        writer.WriteEndArray();
    }
}
