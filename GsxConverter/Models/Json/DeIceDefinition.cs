using System.Text.Json.Serialization;

namespace GsxConverter.Models.Json;

// De-Ice definition model - stored at top level in the config
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