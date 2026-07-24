using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceSurvivalGame.Configuration;

/// <summary>
/// A [min, max] pair — e.g. a randomized size/speed/lifetime range — that serializes as a
/// 2-element JSON array ("LengthPixelsRange": [6, 16]) instead of separate Min/Max properties.
/// </summary>
[JsonConverter(typeof(FloatRangeJsonConverter))]
public readonly struct FloatRange
{
    public float Min { get; }
    public float Max { get; }

    public FloatRange(float min, float max)
    {
        Min = min;
        Max = max;
    }
}

public class FloatRangeJsonConverter : JsonConverter<FloatRange>
{
    public override FloatRange Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Expected a [min, max] array.");

        reader.Read();
        var min = reader.GetSingle();
        reader.Read();
        var max = reader.GetSingle();
        reader.Read(); // consume EndArray

        return new FloatRange(min, max);
    }

    public override void Write(Utf8JsonWriter writer, FloatRange value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Min);
        writer.WriteNumberValue(value.Max);
        writer.WriteEndArray();
    }
}
