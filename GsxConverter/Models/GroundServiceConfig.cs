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

    // Top-level metadata preserved
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    // Preserve a typed representation of the INI section [jetway_rootfloor_heights]
    // key => numeric value (e.g. "ft_eham_jetway" => 4.99)
    [JsonPropertyName("jetway_rootfloor_heights")]
    public Dictionary<string, double> JetwayRootfloorHeights { get; set; } = new();

    // De-ice configurations as separate top-level objects (not embedded per-gate)
    [JsonPropertyName("deices")]
    public List<DeIceDefinition> DeIces { get; set; } = new();
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
        
    // preserve arbitrary keys found in the section
    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; } = new();
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

// New De-Ice definition model - stored at top level in the config
public class DeIceDefinition
{
    // section name from INI (e.g., "DeIce_01" or "deice_eham")
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    // optional canonical type field if present (e.g., "deice")
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    // numeric or string properties from the INI section preserved here
    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; } = new();
}
