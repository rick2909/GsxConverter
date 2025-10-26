using System.Text.Json.Serialization;

namespace GsxConverter.Models.Json;

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
    
    // Optional: groupings defined in the INI (GateGroups etc)
    [JsonPropertyName("gate_groups")]
    public List<GateGroup> GateGroups { get; set; } = new();
}