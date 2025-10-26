using System.Text.Json.Serialization;

namespace GsxConverter.Models.Json;

public class GroundService
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // e.g., pushback, marshaller, catering

    [JsonPropertyName("offset")]
    public double? OffsetMeters { get; set; }

    [JsonPropertyName("spawn_coords")]
    public Position? SpawnCoords { get; set; }

    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; } = new();
}