using System.Text.Json.Serialization;

namespace GsxConverter.Models.Json;

// De-Ice definition model - stored at top level in the config
public class DeIceDefinition
{
    // section name from INI (e.g., "DeIce_01" or "deice_eham")
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    // UI display name for the deice area
    [JsonPropertyName("ui_name")]
    public string? UiName { get; set; }

    // optional canonical type field if present (e.g., "deice")
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    // Whether this is a deice area
    [JsonPropertyName("is_deice_area")]
    public bool? IsDeiceArea { get; set; }

    // Position of this deice area
    [JsonPropertyName("position")]
    public Position? Position { get; set; }

    // Parking system for this deice area
    [JsonPropertyName("parking_system")]
    public string? ParkingSystem { get; set; }

    // Radius of the deice area
    [JsonPropertyName("radius")]
    public double? Radius { get; set; }

    // Stop position for parking system
    [JsonPropertyName("parking_system_stop_position")]
    public Position? ParkingSystemStopPosition { get; set; }

    // Object position for parking system
    [JsonPropertyName("parking_system_object_position")]
    public ParkingSystemObjectPosition? ParkingSystemObjectPosition { get; set; }

    // Whether this area is user customized
    [JsonPropertyName("user_customized")]
    public bool? UserCustomized { get; set; }

    // numeric or string properties from the INI section preserved here
    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; } = new();
}