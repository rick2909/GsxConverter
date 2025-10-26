using System.Globalization;
using System.Text.RegularExpressions;
using GsxConverter.Models.Json;

namespace GsxConverter.Parsers;

/// <summary>
/// Parser for GSX Python configuration files.
/// GSX Python files contain procedural code that defines airport gate configurations,
/// including complex waypoints, positioning, and equipment configurations.
/// This parser extracts the structured data and converts it to the unified JSON model.
/// </summary>
public class PythonGsxParser
{
    private static readonly Regex GateDefRegex = new Regex(@"def\s+(\w+)\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PositionRegex = new Regex(@"\(\s*([^,]+)\s*,\s*([^,]+)\s*,\s*([^)]+)\s*\)", RegexOptions.Compiled);
    private static readonly Regex WaypointArrayRegex = new Regex(@"\[\s*(\([^]]+\)(?:\s*,\s*\([^]]+\))*)\s*\]", RegexOptions.Compiled);
    
    public GroundServiceConfig ParseFile(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException(path);

        var lines = File.ReadAllLines(path);
        var cfg = new GroundServiceConfig
        {
            AirportIcao = Path.GetFileNameWithoutExtension(path).ToUpperInvariant()
        };

        var currentContext = new ParseContext();

        foreach (var line in lines)
        {
            ProcessLine(line.Trim(), currentContext, cfg);
        }

        // Finalize any pending gate definition
        FinalizeCurrentGate(currentContext, cfg);

        return cfg;
    }

    private void ProcessLine(string line, ParseContext context, GroundServiceConfig config)
    {
        // Skip empty lines and comments
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) return;

        // Check for function definitions (gate definitions)
        var gateMatch = GateDefRegex.Match(line);
        if (gateMatch.Success)
        {
            // Finalize previous gate if any
            FinalizeCurrentGate(context, config);
            
            // Start new gate
            context.CurrentGate = new GateDefinition
            {
                GateId = ExtractGateIdFromFunction(gateMatch.Groups[1].Value)
            };
            context.InGateDefinition = true;
            return;
        }

        if (!context.InGateDefinition || context.CurrentGate == null) return;

        // Process gate configuration lines
        ProcessGateConfigLine(line, context);
    }

    private void ProcessGateConfigLine(string line, ParseContext context)
    {
        var gate = context.CurrentGate!;

        // Extract variable assignments and function calls
        if (TryParseAssignment(line, out var varName, out var value))
        {
            ProcessGateProperty(varName, value, gate);
        }
        else if (line.Contains("return"))
        {
            // End of gate definition
            context.InGateDefinition = false;
        }
    }

    private bool TryParseAssignment(string line, out string varName, out string value)
    {
        varName = string.Empty;
        value = string.Empty;

        var eqIndex = line.IndexOf('=');
        if (eqIndex <= 0) return false;

        varName = line.Substring(0, eqIndex).Trim();
        value = line.Substring(eqIndex + 1).Trim();

        // Clean up variable names (remove dots, brackets, etc.)
        if (varName.Contains('.'))
        {
            var parts = varName.Split('.');
            varName = parts.Last();
        }

        return !string.IsNullOrEmpty(varName);
    }

    private void ProcessGateProperty(string property, string value, GateDefinition gate)
    {
        switch (property.ToLowerInvariant())
        {
            case "lat":
            case "latitude":
                if (TryParseDouble(value, out var lat))
                    gate.Position.Latitude = lat;
                break;
                
            case "lon":
            case "longitude":
                if (TryParseDouble(value, out var lon))
                    gate.Position.Longitude = lon;
                break;
                
            case "heading":
                if (TryParseDouble(value, out var heading))
                    gate.Position.Heading = heading;
                break;

            case "jetway":
            case "hasjetway":
                gate.HasJetway = ParsePythonBoolean(value);
                break;

            case "gate_type":
            case "gatetype":
                if (int.TryParse(value, out var gateType))
                    gate.GateType = gateType;
                break;

            case "maxwingspan":
            case "max_wingspan":
                if (TryParseDouble(value, out var wingspan))
                    gate.MaxWingspan = wingspan;
                break;

            case "radiusleft":
            case "radius_left":
                if (TryParseDouble(value, out var radiusLeft))
                    gate.RadiusLeft = radiusLeft;
                break;

            case "radiusright":
            case "radius_right":
                if (TryParseDouble(value, out var radiusRight))
                    gate.RadiusRight = radiusRight;
                break;

            case "parkingsystem":
            case "parking_system":
                gate.ParkingSystem = CleanStringValue(value);
                break;

            case "undergroundrefueling":
            case "underground_refueling":
                gate.UndergroundRefueling = ParsePythonBoolean(value);
                break;

            case "walkerwaypoints":
            case "walker_waypoints":
                gate.WalkerWaypoints = ParseWaypointPathFromValue(value);
                break;

            case "passengerwaypoints":
            case "passenger_waypoints":
                gate.PassengerWaypoints = ParseWaypointPathFromValue(value);
                break;

            case "passengerentergatepos":
            case "passenger_enter_gate_pos":
                gate.PassengerEnterGatePos = ParsePosition3DFromValue(value);
                break;

            default:
                // Store unknown properties
                gate.Properties[property] = CleanStringValue(value);
                break;
        }
    }

    private WaypointPath? ParseWaypointPathFromValue(string value)
    {
        var waypoints = ParseWaypointArrayFromValue(value);
        if (waypoints == null || waypoints.Count == 0) return null;

        return new WaypointPath { Waypoints = waypoints };
    }

    private List<Waypoint>? ParseWaypointArrayFromValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        try
        {
            var waypoints = new List<Waypoint>();
            var matches = PositionRegex.Matches(value);

            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 4)
                {
                    var waypoint = new Waypoint();
                    if (TryParseDouble(match.Groups[1].Value, out var lat))
                        waypoint.Latitude = lat;
                    if (TryParseDouble(match.Groups[2].Value, out var lon))
                        waypoint.Longitude = lon;
                    if (TryParseDouble(match.Groups[3].Value, out var height))
                        waypoint.Height = height;
                    
                    waypoints.Add(waypoint);
                }
            }

            return waypoints.Count > 0 ? waypoints : null;
        }
        catch
        {
            return null;
        }
    }

    private Position3D? ParsePosition3DFromValue(string value)
    {
        var match = PositionRegex.Match(value);
        if (match.Success && match.Groups.Count >= 4)
        {
            var position = new Position3D();
            if (TryParseDouble(match.Groups[1].Value, out var lat))
                position.Latitude = lat;
            if (TryParseDouble(match.Groups[2].Value, out var lon))
                position.Longitude = lon;
            if (TryParseDouble(match.Groups[3].Value, out var height))
                position.Height = height;
            
            return position;
        }

        return null;
    }

    private bool? ParsePythonBoolean(string value)
    {
        var cleaned = CleanStringValue(value).ToLowerInvariant();
        return cleaned switch
        {
            "true" => true,
            "false" => false,
            "1" => true,
            "0" => false,
            _ => null
        };
    }

    private string CleanStringValue(string value)
    {
        return value.Trim().Trim('"', '\'', ';');
    }

    private bool TryParseDouble(string value, out double result)
    {
        var cleaned = CleanStringValue(value);
        return double.TryParse(cleaned, NumberStyles.Float | NumberStyles.AllowThousands, 
            CultureInfo.InvariantCulture, out result) ||
               double.TryParse(cleaned, NumberStyles.Float | NumberStyles.AllowThousands, 
            CultureInfo.CurrentCulture, out result);
    }

    private string ExtractGateIdFromFunction(string functionName)
    {
        // Extract gate ID from function name (e.g., "gate_A12" -> "A12")
        var parts = functionName.Split('_');
        return parts.Length > 1 ? string.Join("_", parts.Skip(1)) : functionName;
    }

    private void FinalizeCurrentGate(ParseContext context, GroundServiceConfig config)
    {
        if (context.CurrentGate != null)
        {
            config.Gates.Add(context.CurrentGate);
            context.CurrentGate = null;
        }
        context.InGateDefinition = false;
    }

    private class ParseContext
    {
        public GateDefinition? CurrentGate { get; set; }
        public bool InGateDefinition { get; set; }
    }
}