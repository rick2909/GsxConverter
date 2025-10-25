using System.Text.Json.Serialization;

namespace GsxConverter.Models;

public class GroundServiceConfig
{
    [JsonPropertyName("airport")]
    public string AirportIcao { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("gates")]
    public List<GateDefinition> Gates { get; set; } = new();

    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class GateDefinition
{
    [JsonPropertyName("gate_id")]
    public string GateId { get; set; } = string.Empty;

    [JsonPropertyName("position")]
    public Position Position { get; set; } = new();

    [JsonPropertyName("services")]
    public List<GroundService> Services { get; set; } = new();

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new();
}

public class Position
{
    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    [JsonPropertyName("lon")]
    public double Longitude { get; set; }

    [JsonPropertyName("heading")]
    public double Heading { get; set; }
}

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