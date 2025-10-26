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

public class Position3D : Position
{
    [JsonPropertyName("height")]
    public double? Height { get; set; }
}

public class Waypoint
{
    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    [JsonPropertyName("lon")]
    public double Longitude { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }
}

public class WaypointPath
{
    [JsonPropertyName("waypoints")]
    public List<Waypoint> Waypoints { get; set; } = new();

    [JsonPropertyName("thickness")]
    public double? Thickness { get; set; }
}