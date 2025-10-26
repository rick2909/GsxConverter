using System.Text.Json.Serialization;

namespace GsxConverter.Models.Json;

public class Position
{
    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    [JsonPropertyName("lon")]
    public double Longitude { get; set; }

    [JsonPropertyName("heading")]
    public double Heading { get; set; }
}