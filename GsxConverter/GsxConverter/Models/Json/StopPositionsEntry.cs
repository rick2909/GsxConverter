using System.Text.Json;
using System.Text.Json.Serialization;

namespace GsxConverter.Models.Json;

[JsonConverter(typeof(StopPositionsEntryConverter))]
public class StopPositionsEntry
{
    // If set, emit as a single number value
    public double? Single { get; set; }

    // If set, emit as an object with variant keys (including optional "default")
    public Dictionary<string, double>? Variants { get; set; }

    public bool HasVariants => Variants != null && Variants.Count > 0;
}

public class StopPositionsEntryConverter : JsonConverter<StopPositionsEntry>
{
    public override StopPositionsEntry? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Reading is not required for this tool; implement minimal support
        if (reader.TokenType == JsonTokenType.Number)
        {
            var val = reader.GetDouble();
            return new StopPositionsEntry { Single = val };
        }
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            var entry = new StopPositionsEntry { Variants = new Dictionary<string, double>() };
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject) break;
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    var prop = reader.GetString() ?? string.Empty;
                    reader.Read();
                    if (reader.TokenType == JsonTokenType.Number)
                    {
                        entry.Variants![prop] = reader.GetDouble();
                    }
                    else
                    {
                        reader.Skip();
                    }
                }
            }
            return entry;
        }
        reader.Skip();
        return null;
    }

    public override void Write(Utf8JsonWriter writer, StopPositionsEntry value, JsonSerializerOptions options)
    {
        if (!value.HasVariants)
        {
            writer.WriteNumberValue(value.Single ?? 0);
            return;
        }
        writer.WriteStartObject();
        if (value.Variants != null)
        {
            foreach (var kv in value.Variants.OrderBy(x => x.Key == "default" ? "zzz" : x.Key))
            {
                writer.WritePropertyName(kv.Key);
                writer.WriteNumberValue(kv.Value);
            }
        }
        writer.WriteEndObject();
    }
}
