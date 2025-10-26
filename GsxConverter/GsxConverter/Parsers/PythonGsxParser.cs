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
    // Maps function name to its default stop distance (meters) if found
    private Dictionary<string, double> DefaultStopDistances = new(StringComparer.OrdinalIgnoreCase);
    // Stores all @AlternativeStopPositions function bodies by name
    private Dictionary<string, string> AlternativeStopFunctions = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex GateDefRegex = new Regex(@"def\s+(\w+)\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PositionRegex = new Regex(@"\(\s*([^,]+)\s*,\s*([^,]+)\s*,\s*([^)]+)\s*\)", RegexOptions.Compiled);
    private static readonly Regex WaypointArrayRegex = new Regex(@"\[\s*(\([^]]+\)(?:\s*,\s*\([^]]+\))*)\s*\]", RegexOptions.Compiled);
    private static readonly Regex DictRegex = new Regex(@"\{\s*([^}]*)\s*\}", RegexOptions.Singleline | RegexOptions.Compiled);
    private static readonly Regex DictPairRegex = new Regex(@"(['""])(?<key>[^'""]+)\1\s*:\s*(?<val>-?\d+(?:\.\d+)?)", RegexOptions.Compiled);
    private static readonly Regex ParkingsAssignRegex = new Regex("parkings\\s*=\\s*\\{", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CustomizedNameRegex = new Regex("CustomizedName\\(\\s*['\\\"](?<full>[^'\\\"]+)['\\\"]\\s*\\)", RegexOptions.Compiled);
    private static readonly Regex TupleForParkingRegex = new Regex("^\\s*(?<num>\\d+)\\s*:\\s*\\((?<tuple>[^)]*)\\)\\s*,?\\s*$", RegexOptions.Multiline | RegexOptions.Compiled);
    private static readonly Regex FunctionNameInTupleRegex = new Regex("[,]\\s*(?<fn>[A-Za-z_]\\w*)\\s*(?:[,)]|$)", RegexOptions.Compiled);

    public GroundServiceConfig ParseFile(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException(path);

        var lines = File.ReadAllLines(path);
        var fileText = File.ReadAllText(path);
        var cfg = new GroundServiceConfig
        {
            AirportIcao = Path.GetFileNameWithoutExtension(path).ToUpperInvariant()
        };

        var currentContext = new ParseContext();

        // Scan for all @AlternativeStopPositions functions and store their bodies
        ExtractAlternativeStopFunctions(fileText);
        // Scan all saved function bodies for Distance.fromMeters(x) and build default mapping
        BuildDefaultStopDistances();

        // Parse GSX parkings dict upfront for CustomizedName and stop functions
        TryParseParkings(fileText, cfg);

        foreach (var line in lines)
        {
            ProcessLine(line.Trim(), currentContext, cfg);
        }

        // Finalize any pending gate definition
        FinalizeCurrentGate(currentContext, cfg);

        return cfg;
    }

    // Extracts all @AlternativeStopPositions functions and stores their bodies
    private void ExtractAlternativeStopFunctions(string fileText)
    {
        var funcRegex = new Regex(@"@AlternativeStopPositions\s*def\s+(?<name>\w+)\s*\((?<args>[^)]*)\):", RegexOptions.Multiline);
        var matches = funcRegex.Matches(fileText);
        foreach (Match m in matches)
        {
            var name = m.Groups["name"].Value;
            int start = m.Index;
            int bodyStart = fileText.IndexOf(':', start) + 1;
            int nextFunc = fileText.IndexOf("@AlternativeStopPositions", bodyStart, StringComparison.Ordinal);
            string body = nextFunc > bodyStart ? fileText.Substring(bodyStart, nextFunc - bodyStart) : fileText.Substring(bodyStart);
            AlternativeStopFunctions[name] = body.Trim();
        }
    }

    // Scans all AlternativeStopFunctions for Distance.fromMeters(x) and builds DefaultStopDistances
    private void BuildDefaultStopDistances()
    {
        var distRegex = new Regex(@"Distance\.fromMeters\(\s*(-?\d+(?:\.\d+)?)\s*\)");
        foreach (var kv in AlternativeStopFunctions)
        {
            var body = kv.Value;
            var match = distRegex.Match(body);
            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
            {
                DefaultStopDistances[kv.Key] = d;
            }
        }
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
        ProcessGateConfigLine(line, context, config);
    }

    private void ProcessGateConfigLine(string line, ParseContext context, GroundServiceConfig config)
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

    private void TryParseParkings(string fileText, GroundServiceConfig cfg)
    {
        var m = ParkingsAssignRegex.Match(fileText);
        if (!m.Success) return;

        // Extract the full dict block with balanced braces starting from the first '{'
        int start = fileText.IndexOf('{', m.Index);
        if (start < 0) return;
        int depth = 0;
        int i = start;
        for (; i < fileText.Length; i++)
        {
            char c = fileText[i];
            if (c == '{') depth++;
            else if (c == '}') { depth--; if (depth == 0) { i++; break; } }
        }
        if (depth != 0) return; // unbalanced
        string parkingsBlock = fileText.Substring(start, i - start);

        // Build a lookup for variables assigned via CustomizedName("Group|Name", optionalIndex)
        var customNameVarMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var varRegex = new Regex("^\\s*(?<var>[A-Za-z_]\\w*)\\s*=\\s*CustomizedName\\(\\s*['\\\"](?<text>[^'\\\"]+)['\\\"](?:\\s*,\\s*\\d+)?\\s*\\)\\s*$", RegexOptions.Multiline);
        foreach (Match vm in varRegex.Matches(fileText))
        {
            var varName = vm.Groups["var"].Value;
            var text = vm.Groups["text"].Value;
            customNameVarMap[varName] = text;
        }

        // Parse by group sections to capture group code (e.g., GATE_A)
        var groupStartRegex = new Regex("^\\s*(?<grp>[A-Za-z0-9_]+)\\s*:\\s*\\{", RegexOptions.Multiline);
        var groupMatches = groupStartRegex.Matches(parkingsBlock);
        foreach (Match gm in groupMatches)
        {
            string groupName = gm.Groups["grp"].Value; // e.g., GATE_A
            int gs = parkingsBlock.IndexOf('{', gm.Index);
            if (gs < 0) continue;
            int gdepth = 0; int j = gs; int ge = -1;
            for (; j < parkingsBlock.Length; j++)
            {
                char cc = parkingsBlock[j];
                if (cc == '{') gdepth++;
                else if (cc == '}') { gdepth--; if (gdepth == 0) { ge = j + 1; break; } }
            }
            if (ge == -1) continue;
            string groupBody = parkingsBlock.Substring(gs, ge - gs);

            string groupCode = groupName.Contains('_') ? groupName.Split('_').Last() : groupName;

            foreach (Match pm in TupleForParkingRegex.Matches(groupBody))
            {
                var numStr = pm.Groups["num"].Value;
                var tuple = pm.Groups["tuple"].Value;

                var gate = new GateDefinition { GateId = $"{groupCode.ToLowerInvariant()} {numStr}" };
                gate.Properties["group_code"] = groupCode;

                // Inline CustomizedName or variable reference
                var cm = CustomizedNameRegex.Match(tuple);
                if (cm.Success)
                {
                    var full = cm.Groups["full"].Value;
                    var parts = full.Split('|');
                    if (parts.Length == 2)
                    {
                        gate.Properties["group_name"] = parts[0].Trim();
                        gate.UiName = parts[1].Trim();
                    }
                    else
                    {
                        gate.UiName = full.Trim();
                    }
                }
                else
                {
                    var firstTok = tuple.Split(',').FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(firstTok) && customNameVarMap.TryGetValue(firstTok, out var resolved))
                    {
                        var parts = resolved.Split('|');
                        if (parts.Length == 2)
                        {
                            gate.Properties["group_name"] = parts[0].Trim();
                            gate.UiName = parts[1].Trim();
                        }
                        else
                        {
                            gate.UiName = resolved.Trim();
                        }
                    }
                }

                // Optional stop function name in the tuple
                string? stopFn = null;
                foreach (Match fm in FunctionNameInTupleRegex.Matches(tuple))
                {
                    var fn = fm.Groups["fn"].Value;
                    if (!string.Equals(fn, "CustomizedName", StringComparison.Ordinal))
                    {
                        stopFn = fn;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(stopFn))
                {
                    gate.Properties["custom_stop_function"] = stopFn!;
                    var map = ExtractFunctionStopTables(stopFn!, fileText);
                    if (map != null && map.Count > 0)
                    {
                        // Overwrite any previous AircraftStopPositions for this gate
                        gate.AircraftStopPositions = new Dictionary<string, StopPositionsEntry>(map, StringComparer.OrdinalIgnoreCase);
                    }
                    // Only set default_stop_distance_m if present in DefaultStopDistances
                    if (DefaultStopDistances.TryGetValue(stopFn!, out var defDist))
                    {
                        gate.Properties["default_stop_distance_m"] = defDist.ToString(CultureInfo.InvariantCulture);
                    }
                }

                if (!string.IsNullOrEmpty(gate.UiName) ||
                    (gate.AircraftStopPositions != null && gate.AircraftStopPositions.Count > 0) ||
                    gate.Properties.Count > 0)
                {
                    cfg.Gates.Add(gate);
                }
            }
        }

        // Build GateGroups from parsed gates with group_name property
        var groupMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var gate in cfg.Gates)
        {
            if (gate.Properties.TryGetValue("group_name", out var groupName) && !string.IsNullOrEmpty(groupName))
            {
                if (!groupMap.ContainsKey(groupName))
                {
                    groupMap[groupName] = new List<string>();
                }
                groupMap[groupName].Add(gate.GateId);
            }
        }

        foreach (var kvp in groupMap.OrderBy(g => g.Key))
        {
            cfg.GateGroups.Add(new GateGroup
            {
                Id = kvp.Key,
                Members = kvp.Value
            });
        }
    }

    private Dictionary<string, StopPositionsEntry>? ExtractFunctionStopTables(string funcName, string fileText)
    {
        // Find function body: from "def funcName(" to next "@AlternativeStopPositions" or end
        if (!AlternativeStopFunctions.TryGetValue(funcName, out var body))
        {
            return null;
        }

        // Extract all tables in the function
        var tableAssignRegex = new Regex(@"(?m)^\s*(?<tableName>\w+)\s*=\s*\{(?<body>[^}]*)\}");
        var tables = new Dictionary<string, Dictionary<string, double>>(StringComparer.OrdinalIgnoreCase);
        foreach (Match tm in tableAssignRegex.Matches(body))
        {
            var tableName = tm.Groups["tableName"].Value;
            var tableBody = tm.Groups["body"].Value;
            var table = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
            foreach (Match pair in Regex.Matches(tableBody, @"(?m)^\s*(?<key>\d+)\s*:\s*(?<val>-?\d+(?:\.\d+)?)\s*,?\s*$"))
            {
                var key = pair.Groups["key"].Value.Trim();
                var valS = pair.Groups["val"].Value.Trim();
                if (double.TryParse(valS, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var dv))
                {
                    table[key] = dv;
                }
            }
            tables[tableName] = table;
        }

        // Parse conditional return logic for major/minor IDs and fallback
        var result = new Dictionary<string, StopPositionsEntry>(StringComparer.OrdinalIgnoreCase);

        // Handle cases like: if aircraftData.idMajor == 737: return Distance.fromMeters(table737.get(aircraftData.idMinor, fallback))
        var condRegex = new Regex(@"if\s+aircraftData\.idMajor\s*==\s*(\d+):\s*return\s+Distance\.fromMeters\((\w+)\.get\(aircraftData.idMinor,\s*(-?\d+(?:\.\d+)?)\)\)", RegexOptions.Multiline);
        foreach (Match cm in condRegex.Matches(body))
        {
            var major = cm.Groups[1].Value;
            var tableName = cm.Groups[2].Value;
            var fallbackStr = cm.Groups[3].Value;
            if (tables.TryGetValue(tableName, out var table))
            {
                var entry = new StopPositionsEntry { Variants = new Dictionary<string, double>() };
                foreach (var kv in table)
                {
                    entry.Variants[kv.Key] = kv.Value;
                }
                if (double.TryParse(fallbackStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var fb))
                {
                    entry.Variants["default"] = fb;
                }
                result[major] = entry;
            }
        }

        // Handle elif/else branches for other major types and custom logic
        var elifRegex = new Regex(@"elif\s+aircraftData\.idMajor\s*==\s*(\d+):\s*return\s+Distance\.fromMeters\(((-?\d+(?:\.\d+)?))\)", RegexOptions.Multiline);
        foreach (Match em in elifRegex.Matches(body))
        {
            var major = em.Groups[1].Value;
            var valueStr = em.Groups[2].Value;
            if (double.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var val))
            {
                result[major] = new StopPositionsEntry { Single = val };
            }
        }

        // Handle else: return Distance.fromMeters(table.get(aircraftData.idMajor, fallback))
        var elseRegex = new Regex(@"else:\s*return\s+Distance\.fromMeters\((\w+)\.get\(aircraftData.idMajor,\s*(-?\d+(?:\.\d+)?)\)\)", RegexOptions.Multiline);
        var elseMatch = elseRegex.Match(body);
        if (elseMatch.Success)
        {
            var tableName = elseMatch.Groups[1].Value;
            var fallbackStr = elseMatch.Groups[2].Value;
            if (tables.TryGetValue(tableName, out var table))
            {
                foreach (var kv in table)
                {
                    // Skip if already added as variant entry
                    if (!result.ContainsKey(kv.Key))
                    {
                        result[kv.Key] = new StopPositionsEntry { Single = kv.Value };
                    }
                }
                if (double.TryParse(fallbackStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var fb))
                {
                    result["default"] = new StopPositionsEntry { Single = fb };
                }
            }
        }
        // Only process simple return if no else clause was found
        else
        {
            // Handle simple return: Distance.fromMeters(table.get(aircraftData.idMajor, fallback))
            var simpleRegex = new Regex(@"return\s+Distance\.fromMeters\((\w+)\.get\(aircraftData.idMajor,\s*(-?\d+(?:\.\d+)?)\)\)", RegexOptions.Multiline);
            var simpleMatch = simpleRegex.Match(body);
            if (simpleMatch.Success)
            {
                var tableName = simpleMatch.Groups[1].Value;
                var fallbackStr = simpleMatch.Groups[2].Value;
                if (tables.TryGetValue(tableName, out var table))
                {
                    foreach (var kv in table)
                    {
                        // Skip if already added as variant entry
                        if (!result.ContainsKey(kv.Key))
                        {
                            result[kv.Key] = new StopPositionsEntry { Single = kv.Value };
                        }
                    }
                    if (double.TryParse(fallbackStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var fb))
                    {
                        result["default"] = new StopPositionsEntry { Single = fb };
                    }
                }
            }
        }

        return result.Count > 0 ? result : null;
    }

    private double? ExtractConstantMeters(string funcName, string fileText)
    {
        var defPattern = $"def\\s+{Regex.Escape(funcName)}\\s*\\(";
        var m = Regex.Match(fileText, defPattern);
        if (!m.Success) return null;
        int start = m.Index;
        int next = fileText.IndexOf("\ndef ", start + 1, StringComparison.Ordinal);
        string body = next > start ? fileText.Substring(start, next - start) : fileText.Substring(start);
        var constMatch = Regex.Match(body, @"Distance\.fromMeters\(\s*(-?\d+(?:\.\d+)?)\s*\)");
        if (constMatch.Success && TryParseDouble(constMatch.Groups[1].Value, out var d)) return d;
        return null;
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
            // Aircraft stop positions table (dict): keys are ICAO codes, values are numeric stop distances
            case "functions":
            case "stops":
            case "stop_positions":
            case "aircraft_stop_positions":
            case "custom_stop_positions":
                var dict = ParseStringDoubleDict(value);
                if (dict != null && dict.Count > 0)
                {
                    var converted = new Dictionary<string, StopPositionsEntry>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kv in dict)
                        converted[kv.Key] = new StopPositionsEntry { Single = kv.Value };
                    gate.AircraftStopPositions = converted;
                }
                break;
            default:
                gate.Properties[property] = CleanStringValue(value);
                break;
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

    private Dictionary<string,double>? ParseStringDoubleDict(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var m = DictRegex.Match(value);
        if (!m.Success) return null;

        var body = m.Groups[1].Value;
        var dict = new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);
        foreach (Match pair in DictPairRegex.Matches(body))
        {
            var key = pair.Groups["key"].Value.Trim();
            var valS = pair.Groups["val"].Value.Trim();
            if (TryParseDouble(valS, out var d))
            {
                dict[key] = d;
            }
        }
        return dict.Count > 0 ? dict : null;
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