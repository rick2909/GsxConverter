using System.Text.Json.Serialization;

namespace GsxConverter.Models.Json;

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
    
    // allowed aircraft templates (if present)
    [JsonPropertyName("allowed_aircraft")]
    public List<string> AllowedAircraft { get; set; } = new();

    // GSX-specific gate properties
    [JsonPropertyName("gate_type")]
    public int? GateType { get; set; }

    [JsonPropertyName("max_wingspan")]
    public double? MaxWingspan { get; set; }

    [JsonPropertyName("radius_left")]
    public double? RadiusLeft { get; set; }

    [JsonPropertyName("radius_right")]
    public double? RadiusRight { get; set; }

    [JsonPropertyName("gate_distance_threshold")]
    public double? GateDistanceThreshold { get; set; }

    [JsonPropertyName("has_jetway")]
    public bool? HasJetway { get; set; }

    [JsonPropertyName("parking_system")]
    public string? ParkingSystem { get; set; }

    [JsonPropertyName("underground_refueling")]
    public bool? UndergroundRefueling { get; set; }

    [JsonPropertyName("no_passenger_stairs")]
    public bool? NoPassengerStairs { get; set; }

    [JsonPropertyName("no_passenger_bus")]
    public bool? NoPassengerBus { get; set; }

    [JsonPropertyName("no_passenger_bus_deboarding")]
    public bool? NoPassengerBusDebording { get; set; }

    [JsonPropertyName("ignore_icao_prefixes")]
    public bool? IgnoreIcaoPrefixes { get; set; }

    [JsonPropertyName("ignore_preferred_exit")]
    public bool? IgnorePreferredExit { get; set; }

    [JsonPropertyName("dont_create_jetways")]
    public bool? DontCreateJetways { get; set; }

    [JsonPropertyName("disable_pax_barriers")]
    public bool? DisablePaxBarriers { get; set; }

    [JsonPropertyName("disable_pax_barriers_deboarding")]
    public bool? DisablePaxBarriersDebording { get; set; }

    [JsonPropertyName("user_customized")]
    public bool? UserCustomized { get; set; }

    [JsonPropertyName("loader_type")]
    public string? LoaderType { get; set; }

    [JsonPropertyName("airline_codes")]
    public string? AirlineCodes { get; set; }

    [JsonPropertyName("handling_texture")]
    public List<string>? HandlingTexture { get; set; }

    [JsonPropertyName("catering_texture")]
    public List<string>? CateringTexture { get; set; }

    [JsonPropertyName("walker_type")]
    public string? WalkerType { get; set; }

    [JsonPropertyName("walker_path_thickness")]
    public double? WalkerPathThickness { get; set; }

    [JsonPropertyName("walker_loop_start")]
    public int? WalkerLoopStart { get; set; }

    [JsonPropertyName("passenger_path_thickness")]
    public double? PassengerPathThickness { get; set; }

    [JsonPropertyName("passenger_path_thickness_deboarding")]
    public double? PassengerPathThicknessDebording { get; set; }

    // Pushback configuration
    [JsonPropertyName("pushback_config")]
    public PushbackConfig? PushbackConfig { get; set; }

    // Parking system positions
    [JsonPropertyName("parking_system_stop_position")]
    public Position? ParkingSystemStopPosition { get; set; }

    [JsonPropertyName("parking_system_object_position")]
    public ParkingSystemObjectPosition? ParkingSystemObjectPosition { get; set; }

    // Equipment positions
    [JsonPropertyName("baggage_positions")]
    public BaggagePositions? BaggagePositions { get; set; }

    [JsonPropertyName("stairs_positions")]
    public StairsPositions? StairsPositions { get; set; }

    // preserve arbitrary keys not mapped above
    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; } = new();
}

public class PushbackConfig
{
    [JsonPropertyName("pushback_type")]
    public int? PushbackType { get; set; }

    [JsonPropertyName("pushback_labels")]
    public List<string>? PushbackLabels { get; set; }

    [JsonPropertyName("snap_left_pushback_pos")]
    public bool? SnapLeftPushbackPos { get; set; }

    [JsonPropertyName("snap_right_pushback_pos")]
    public bool? SnapRightPushbackPos { get; set; }

    [JsonPropertyName("pushback_left_pos")]
    public Position? PushbackLeftPos { get; set; }

    [JsonPropertyName("pushback_right_pos")]
    public Position? PushbackRightPos { get; set; }

    [JsonPropertyName("pushback_left_approach_pos")]
    public Position? PushbackLeftApproachPos { get; set; }

    [JsonPropertyName("pushback_right_approach_pos")]
    public Position? PushbackRightApproachPos { get; set; }

    [JsonPropertyName("pushback_left_approach_pos2")]
    public Position? PushbackLeftApproachPos2 { get; set; }

    [JsonPropertyName("pushback_right_approach_pos2")]
    public Position? PushbackRightApproachPos2 { get; set; }

    [JsonPropertyName("pushback_pos")]
    public Position? PushbackPos { get; set; }

    [JsonPropertyName("wingwalkers_left_pushback")]
    public int? WingwalkersLeftPushback { get; set; }

    [JsonPropertyName("wingwalkers_right_pushback")]
    public int? WingwalkersRightPushback { get; set; }

    [JsonPropertyName("wingwalkers_quick_pushback")]
    public int? WingwalkersQuickPushback { get; set; }

    [JsonPropertyName("start_engines_left_pushback")]
    public double? StartEnginesLeftPushback { get; set; }

    [JsonPropertyName("start_engines_right_pushback")]
    public double? StartEnginesRightPushback { get; set; }

    [JsonPropertyName("start_engines_quick_pushback")]
    public double? StartEnginesQuickPushback { get; set; }
}

public class ParkingSystemObjectPosition
{
    [JsonPropertyName("lat")]
    public double Latitude { get; set; }

    [JsonPropertyName("lon")]
    public double Longitude { get; set; }

    [JsonPropertyName("heading")]
    public double Heading { get; set; }

    [JsonPropertyName("height")]
    public double? Height { get; set; }
}

public class BaggagePositions
{
    [JsonPropertyName("baggage_loader_front_pos")]
    public Position? BaggageLoaderFrontPos { get; set; }

    [JsonPropertyName("baggage_loader_rear_pos")]
    public Position? BaggageLoaderRearPos { get; set; }

    [JsonPropertyName("baggage_loader_main_pos")]
    public Position? BaggageLoaderMainPos { get; set; }

    [JsonPropertyName("baggage_train_front_pos")]
    public Position? BaggageTrainFrontPos { get; set; }

    [JsonPropertyName("baggage_train_rear_pos")]
    public Position? BaggageTrainRearPos { get; set; }

    [JsonPropertyName("baggage_train_main_pos")]
    public Position? BaggageTrainMainPos { get; set; }
}

public class StairsPositions
{
    [JsonPropertyName("stairs_front_pos")]
    public Position? StairsFrontPos { get; set; }

    [JsonPropertyName("stairs_middle_pos")]
    public Position? StairsMiddlePos { get; set; }

    [JsonPropertyName("stairs_rear_pos")]
    public Position? StairsRearPos { get; set; }
}