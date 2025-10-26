using System.Text.Json.Serialization;

namespace GsxConverter.Models.Json;

public class GateGroup
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("members")]
    public List<string> Members { get; set; } = new();

    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; } = new();
}