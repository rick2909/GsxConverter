using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GsxConverter.Models.Json;
using System.Reflection;

namespace GsxConverter.Parsers;

/// <summary>
/// Extended INI parser for GSX files.
/// Maps common GSX sections and keys into canonical DTOs.
/// Preserves unknown keys into properties and metadata.
/// Adds:
///  - typed parsing for [jetway_rootfloor_heights]
///  - top-level DeIce objects for DeIce* sections
///  - more robust gate section name detection (supports "Gate_", "Gate ", "Gate-")
///  - per-service indexed keys (service_1, service_1_offset, service_1_spawn_lat etc.)
///  - stricter mapping so only genuinely unknown keys are preserved
/// </summary>
public class IniGsxParser
{
    private static readonly string[] ServiceSectionPrefixes = { "GndService", "Service" };
    private static readonly Regex GateRegex = new Regex(@"^(Gate|Stand|Parking)[\s_\-:]*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DeIceRegex = new Regex(@"^de[-_]?ice", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex GateGroupRegex = new Regex(@"^GateGroup[\s_\-:]*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ServiceIndexRegex = new Regex(@"^service[_\s\-]?(\d+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ServiceIndexedKeyRegex = new Regex(@"^service[_\s\-]?(\d+)[_\s\-:](.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public GroundServiceConfig ParseFile(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException(path);

        // Read file and parse into lightweight SectionData objects to avoid requiring IniParser package
        var lines = File.ReadAllLines(path);
        var sections = new List<SectionData>();
        SectionData? current = null;

        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (line.StartsWith(";") || line.StartsWith("#")) continue; // comment

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                var sect = line.Substring(1, line.Length - 2).Trim();
                current = new SectionData(sect, new KeyCollection());
                sections.Add(current);
                continue;
            }

            // key = value
            var eq = line.IndexOf('=');
            if (eq > 0 && current != null)
            {
                var key = line.Substring(0, eq).Trim();
                var val = line.Substring(eq + 1).Trim();
                // strip inline comments
                var commentIdx = val.IndexOf(';');
                if (commentIdx >= 0) val = val.Substring(0, commentIdx).Trim();
                current.Keys.Add(key, val);
            }
            else
            {
                // lines outside sections -> store under a root section name
                if (current == null)
                {
                    current = new SectionData(string.Empty, new KeyCollection());
                    sections.Add(current);
                }
                if (eq > 0)
                {
                    var key = line.Substring(0, eq).Trim();
                    var val = line.Substring(eq + 1).Trim();
                    current.Keys.Add(key, val);
                }
            }
        }

        var cfg = new GroundServiceConfig
        {
            AirportIcao = Path.GetFileNameWithoutExtension(path).ToUpperInvariant()
        };

        // Collect global service definitions (GndService_* sections)
        var globalServices = new Dictionary<string, GroundService>(StringComparer.OrdinalIgnoreCase);

        // First pass: handle service sections, jetway_rootfloor_heights, gate groups and deice sections
        foreach (var section in sections)
        {
            string name = section.SectionName.Trim();

            // Jetway config section
            if (string.Equals(name, "jetway_rootfloor_heights", StringComparison.OrdinalIgnoreCase))
            {
                ParseJetwayRootfloorSection(section.Keys, cfg);
                continue;
            }

            // DeIce sections -> top-level DeIces list
            if (DeIceRegex.IsMatch(name))
            {
                var deice = ParseDeIceSection(name, section.Keys);
                cfg.DeIces.Add(deice);
                continue;
            }

            // Gate groups
            if (GateGroupRegex.IsMatch(name))
            {
                var gid = GateGroupRegex.Match(name).Groups[1].Value.Trim();
                cfg.GateGroups.Add(ParseGateGroupSection(gid, section.Keys));
                continue;
            }

            if (IsServiceSection(name))
            {
                var svcId = name;
                var svc = ParseServiceSection(svcId, section.Keys);
                globalServices[svcId] = svc;
                // Also store as metadata for traceability
                cfg.Metadata[$"service.{svcId}.type"] = svc.Type;
            }
        }

        // Second pass: handle gates and remaining sections
        foreach (var section in sections)
        {
            string name = section.SectionName.Trim();

            // Handle gate sections
            if (IsGateSection(name))
            {
                var gate = ParseGateSection(name, section.Keys, globalServices);
                cfg.Gates.Add(gate);
                continue;
            }

            // Skip sections already processed above (services, jetway config, deice)
            if (IsServiceSection(name) || string.Equals(name, "jetway_rootfloor_heights", StringComparison.OrdinalIgnoreCase) || DeIceRegex.IsMatch(name))
            {
                continue;
            }

            // unknown or top-level section -> flatten keys to metadata
            foreach (var k in section.Keys)
            {
                cfg.Metadata[$"{name}.{k.KeyName}"] = k.Value;
            }
        }

        return cfg;
    }

    // Lightweight in-file types to avoid external IniParser dependency
    private sealed class SectionData
    {
        public string SectionName { get; }
        public KeyCollection Keys { get; }
        public SectionData(string name, KeyCollection keys) { SectionName = name; Keys = keys; }
    }

    private sealed class KeyCollection : IEnumerable<KeyItem>
    {
        private readonly Dictionary<string, string> _dict = new(StringComparer.OrdinalIgnoreCase);
        public void Add(string k, string v) => _dict[k] = v;
        public bool ContainsKey(string k) => _dict.ContainsKey(k);
        public string this[string k] { get => _dict.TryGetValue(k, out var v) ? v : string.Empty; set => _dict[k] = value; }
        public IEnumerator<KeyItem> GetEnumerator() => _dict.Select(kvp => new KeyItem(kvp.Key, kvp.Value)).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public int Count => _dict.Count;
    }

    private sealed class KeyItem
    {
        public string KeyName { get; }
        public string Value { get; }
        public KeyItem(string name, string value) { KeyName = name; Value = value; }
    }

    private bool IsGateSection(string sectionName) => GateRegex.IsMatch(sectionName);

    private bool IsServiceSection(string sectionName)
    {
        return ServiceSectionPrefixes.Any(prefix => sectionName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private void ParseJetwayRootfloorSection(KeyCollection keys, GroundServiceConfig cfg)
    {
        foreach (var k in keys)
        {
            var key = k.KeyName.Trim();
            var val = k.Value.Trim();

            if (TryParseInvariant(val, out var d))
            {
                cfg.JetwayRootfloorHeights[key] = d;
            }
            else
            {
                cfg.Metadata[$"jetway_rootfloor_heights.{key}"] = val;
            }
        }
    }

    private DeIceDefinition ParseDeIceSection(string sectionName, KeyCollection keys)
    {
        var d = new DeIceDefinition { Id = sectionName };

        // Parse structured deice area properties
        if (TryGet(keys, "uiname", out var uiName))
            d.UiName = uiName;

        if (TryGet(keys, "is_deicearea", out var isDeiceS))
            d.IsDeiceArea = ParseGsxBoolean(isDeiceS);

        if (TryGet(keys, "this_parking_pos", out var posS))
            d.Position = ParsePosition(posS);

        if (TryGet(keys, "parkingsystem", out var parkingSystem))
            d.ParkingSystem = parkingSystem;

        if (TryGet(keys, "radius", out var radiusS) && TryParseInvariant(radiusS, out var radius))
            d.Radius = radius;

        if (TryGet(keys, "parkingsystem_stopposition", out var stopPosS))
            d.ParkingSystemStopPosition = ParsePosition(stopPosS);

        if (TryGet(keys, "parkingsystem_objectposition", out var objPosS))
            d.ParkingSystemObjectPosition = ParseParkingSystemObjectPosition(objPosS);

        if (TryGet(keys, "usercustomized", out var userCustomS))
            d.UserCustomized = ParseGsxBoolean(userCustomS);

        foreach (var k in keys)
        {
            var key = k.KeyName.Trim();
            var val = k.Value.Trim();
            if (string.Equals(key, "type", StringComparison.OrdinalIgnoreCase))
                d.Type = val;
            d.Properties[key] = val;
        }
        return d;
    }

    private GateGroup ParseGateGroupSection(string id, KeyCollection keys)
    {
        var g = new GateGroup { Id = id };
        foreach (var k in keys)
        {
            var key = k.KeyName.Trim();
            var val = k.Value.Trim();
            if (string.Equals(key, "members", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var m in SplitList(val)) g.Members.Add(m);
            }
            else
            {
                g.Properties[key] = val;
            }
        }
        return g;
    }

    private GateDefinition ParseGateSection(string sectionName, KeyCollection keys, Dictionary<string, GroundService> globalServices)
    {
        var match = GateRegex.Match(sectionName);
        var gateId = match.Success ? match.Groups[2].Value.Trim() : sectionName;

        var gate = new GateDefinition { GateId = gateId };

        // Position fields - first try this_parking_pos (GSX format: "lat lon heading")
        if (TryGet(keys, "this_parking_pos", out var parkingPos))
        {
            var parts = parkingPos.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                if (TryParseInvariant(parts[0], out var lat)) gate.Position.Latitude = lat;
                if (TryParseInvariant(parts[1], out var lon)) gate.Position.Longitude = lon;
                if (TryParseInvariant(parts[2], out var heading)) gate.Position.Heading = heading;
            }
        }
        else
        {
            // Fallback to individual lat/lon/heading fields
            if (TryGet(keys, "lat", out var latS) && TryParseInvariant(latS, out var lat)) gate.Position.Latitude = lat;
            if (TryGet(keys, "lon", out var lonS) && TryParseInvariant(lonS, out var lon)) gate.Position.Longitude = lon;
            if (TryGet(keys, "heading", out var hS) && TryParseInvariant(hS, out var heading)) gate.Position.Heading = heading;

            // fallback spawn coords if lat/lon missing
            if ((gate.Position.Latitude == 0 && gate.Position.Longitude == 0) &&
                (TryGet(keys, "spawn_lat", out var sLat) || TryGet(keys, "spawn_latitude", out sLat)))
            {
                if (TryParseInvariant(sLat, out var lat2)) gate.Position.Latitude = lat2;
                if (TryGet(keys, "spawn_lon", out var sLon) && TryParseInvariant(sLon, out var lon2)) gate.Position.Longitude = lon2;
                if (TryGet(keys, "spawn_heading", out var sH) && TryParseInvariant(sH, out var h2)) gate.Position.Heading = h2;
            }
        }

        // tags/categories
        if (TryGet(keys, "tags", out var tags)) gate.Tags.AddRange(SplitList(tags));
        if (TryGet(keys, "category", out var cat) && !string.IsNullOrWhiteSpace(cat)) gate.Tags.Add(cat);
        
        // GSX-specific type mapping to tags and direct property
        if (TryGet(keys, "type", out var gateType) && !string.IsNullOrWhiteSpace(gateType))
        {
            gate.Tags.Add($"type_{gateType}");
            if (int.TryParse(gateType, out var typeNum)) gate.GateType = typeNum;
        }

        // Map GSX gate properties to structured fields
        if (TryGet(keys, "maxwingspan", out var wingspanS) && TryParseInvariant(wingspanS, out var wingspan))
            gate.MaxWingspan = wingspan;
        
        if (TryGet(keys, "radiusleft", out var radiusLeftS) && TryParseInvariant(radiusLeftS, out var radiusLeft))
            gate.RadiusLeft = radiusLeft;
        
        if (TryGet(keys, "radiusright", out var radiusRightS) && TryParseInvariant(radiusRightS, out var radiusRight))
            gate.RadiusRight = radiusRight;
        
        if (TryGet(keys, "gatedistancethreshold", out var gateDistS) && TryParseInvariant(gateDistS, out var gateDist))
            gate.GateDistanceThreshold = gateDist;
        
        if (TryGet(keys, "hasjetway", out var hasJetwayS))
            gate.HasJetway = ParseGsxBoolean(hasJetwayS);
        
        if (TryGet(keys, "parkingsystem", out var parkingSystemS))
            gate.ParkingSystem = parkingSystemS;
        
        if (TryGet(keys, "undergroundrefueling", out var undergroundRefS))
            gate.UndergroundRefueling = ParseGsxBoolean(undergroundRefS);
        
        if (TryGet(keys, "nopassengerstairs", out var noStairsS))
            gate.NoPassengerStairs = ParseGsxBoolean(noStairsS);
        
        if (TryGet(keys, "nopassengerbus", out var noBusS))
            gate.NoPassengerBus = ParseGsxBoolean(noBusS);
        
        if (TryGet(keys, "nopassengerbus_deboarding", out var noBusDebS))
            gate.NoPassengerBusDebording = ParseGsxBoolean(noBusDebS);
        
        if (TryGet(keys, "ignoreicaoprefixes", out var ignoreIcaoS))
            gate.IgnoreIcaoPrefixes = ParseGsxBoolean(ignoreIcaoS);
        
        if (TryGet(keys, "ignorepreferredexit", out var ignorePrefExitS))
            gate.IgnorePreferredExit = ParseGsxBoolean(ignorePrefExitS);
        
        if (TryGet(keys, "dontcreatejetways", out var dontCreateJetwaysS))
            gate.DontCreateJetways = ParseGsxBoolean(dontCreateJetwaysS);
        
        if (TryGet(keys, "disablepaxbarriers", out var disablePaxS))
            gate.DisablePaxBarriers = ParseGsxBoolean(disablePaxS);
        
        if (TryGet(keys, "disablepaxbarriers_deboarding", out var disablePaxDebS))
            gate.DisablePaxBarriersDebording = ParseGsxBoolean(disablePaxDebS);
        
        if (TryGet(keys, "usercustomized", out var userCustomS))
            gate.UserCustomized = ParseGsxBoolean(userCustomS);
        
        if (TryGet(keys, "loadertype", out var loaderTypeS))
            gate.LoaderType = loaderTypeS;
        
        if (TryGet(keys, "airlinecodes", out var airlineCodesS))
            gate.AirlineCodes = airlineCodesS;
        
        if (TryGet(keys, "handlingtexture", out var handlingTextureS))
            gate.HandlingTexture = SplitList(handlingTextureS).ToList();
        
        if (TryGet(keys, "cateringtexture", out var cateringTextureS))
            gate.CateringTexture = SplitList(cateringTextureS).ToList();
        
        if (TryGet(keys, "walkertype", out var walkerTypeS))
            gate.WalkerType = walkerTypeS;
        
        if (TryGet(keys, "walkerpaththickness", out var walkerPathS) && TryParseInvariant(walkerPathS, out var walkerPath))
            gate.WalkerPathThickness = walkerPath;
        
        if (TryGet(keys, "walkerloopstart", out var walkerLoopS) && int.TryParse(walkerLoopS, out var walkerLoop))
            gate.WalkerLoopStart = walkerLoop;
        
        if (TryGet(keys, "passengerpaththickness", out var passengerPathS) && TryParseInvariant(passengerPathS, out var passengerPath))
            gate.PassengerPathThickness = passengerPath;
        
        if (TryGet(keys, "passengerpaththickness_deboarding", out var passengerPathDebS) && TryParseInvariant(passengerPathDebS, out var passengerPathDeb))
            gate.PassengerPathThicknessDebording = passengerPathDeb;

        // Parse pushback configuration
        gate.PushbackConfig = ParsePushbackConfig(keys);

        // Parse parking system positions
        if (TryGet(keys, "parkingsystem_stopposition", out var stopPosS))
            gate.ParkingSystemStopPosition = ParsePosition(stopPosS);
        
        if (TryGet(keys, "parkingsystem_objectposition", out var objPosS))
            gate.ParkingSystemObjectPosition = ParseParkingSystemObjectPosition(objPosS);

        // Parse equipment positions
        gate.BaggagePositions = ParseBaggagePositions(keys);
        gate.StairsPositions = ParseStairsPositions(keys);

        // Parse waypoints and paths
        gate.WalkerWaypoints = ParseWaypointPath(keys, "walkerwaypoints", "walkerpaththickness");
        gate.PassengerWaypoints = ParseWaypointPath(keys, "passengerwaypoints", "passengerpaththickness");

        // Parse passenger enter gate position
        if (TryGet(keys, "passengerentergatepos", out var passengerEnterGateS))
            gate.PassengerEnterGatePos = ParsePosition3D(passengerEnterGateS);

        // Parse texture configurations
        if (TryGet(keys, "paxbarrierstexture", out var paxBarriersTextureS))
            gate.PaxBarriersTexture = paxBarriersTextureS;

        // Parse additional pushback configuration
        if (TryGet(keys, "pushback", out var pushbackS) && int.TryParse(pushbackS, out var pushback))
            gate.Pushback = pushback;

        if (TryGet(keys, "pushbackaddpos", out var pushbackAddPosS))
            gate.PushbackAddPos = ParsePositionArray(pushbackAddPosS);

        // Parse UI name
        if (TryGet(keys, "uiname", out var uiNameS))
            gate.UiName = uiNameS;

        // services referenced inline or via indexed keys
        var services = new List<GroundService>();
        // Collect indexed service references (service_1 = GndService_Pushback)
        var indexedServiceRefs = new Dictionary<int, string>();
        var indexedServiceOverrides = new Dictionary<int, Dictionary<string,string>>();
        foreach (var k in keys)
        {
            var m = ServiceIndexRegex.Match(k.KeyName);
            if (m.Success)
            {
                if (int.TryParse(m.Groups[1].Value, out var idx))
                {
                    indexedServiceRefs[idx] = k.Value.Trim();
                }
                continue;
            }
            var m2 = ServiceIndexedKeyRegex.Match(k.KeyName);
            if (m2.Success)
            {
                if (int.TryParse(m2.Groups[1].Value, out var idx))
                {
                    var sub = m2.Groups[2].Value.Trim();
                    if (!indexedServiceOverrides.TryGetValue(idx, out var d)) { d = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase); indexedServiceOverrides[idx] = d; }
                    d[sub] = k.Value.Trim();
                }
                continue;
            }
        }

        // Build service objects from indexed refs and overrides
        foreach (var kv in indexedServiceRefs.OrderBy(k => k.Key))
        {
            var idx = kv.Key;
            var refName = kv.Value;
            GroundService svc;
            if (!string.IsNullOrEmpty(refName) && globalServices.TryGetValue(refName, out var gs)) svc = CloneService(gs);
            else svc = new GroundService { Type = refName };

            if (indexedServiceOverrides.TryGetValue(idx, out var ov))
            {
                if (ov.TryGetValue("offset", out var offS) && TryParseInvariant(offS, out var off)) svc.OffsetMeters = off;
                if (ov.TryGetValue("offset_meters", out var offm) && TryParseInvariant(offm, out off)) svc.OffsetMeters = off;
                var spawn = new Position(); var hasSpawn = false;
                if (ov.TryGetValue("spawn_lat", out var sl) && TryParseInvariant(sl, out var parsedLat)) { spawn.Latitude = parsedLat; hasSpawn = true; }
                if (ov.TryGetValue("spawn_lon", out var slon) && TryParseInvariant(slon, out var parsedLon)) { spawn.Longitude = parsedLon; hasSpawn = true; }
                if (ov.TryGetValue("spawn_heading", out var sh) && TryParseInvariant(sh, out var parsedHeading)) { spawn.Heading = parsedHeading; hasSpawn = true; }
                if (hasSpawn) svc.SpawnCoords = spawn;
                // copy any remaining override props
                foreach (var p in ov) svc.Properties[p.Key] = p.Value;
            }

            services.Add(svc);
        }

        // explicit per-section service fields (serviceType, offset, spawn_lat etc.)
        if (TryGet(keys, "serviceType", out var st))
        {
            var svc = new GroundService { Type = st };
            if (TryGet(keys, "offset", out var offS) && TryParseInvariant(offS, out var off)) svc.OffsetMeters = off;
            if (TryGet(keys, "offset_meters", out var offm) && TryParseInvariant(offm, out off)) svc.OffsetMeters = off;
            var spawn = new Position();
            var hasSpawn = false;
            if (TryGet(keys, "spawn_lat", out var spl) && TryParseInvariant(spl, out var spln)) { spawn.Latitude = spln; hasSpawn = true; }
            if (TryGet(keys, "spawn_lon", out var splon) && TryParseInvariant(splon, out var splonv)) { spawn.Longitude = splonv; hasSpawn = true; }
            if (TryGet(keys, "spawn_heading", out var sph) && TryParseInvariant(sph, out var sphv)) { spawn.Heading = sphv; hasSpawn = true; }
            if (hasSpawn) svc.SpawnCoords = spawn;
            // collect extra svc.* keys
            foreach (var k in keys)
            {
                if (k.KeyName.StartsWith("service.", StringComparison.OrdinalIgnoreCase) || k.KeyName.StartsWith("svc_", StringComparison.OrdinalIgnoreCase))
                    svc.Properties[k.KeyName] = k.Value;
            }
            services.Add(svc);
        }

        // infer services from boolean flags (pushback=1, marshaller=1 etc.)
        services.AddRange(InferServicesFromFlags(keys));

        gate.Services = services;

        // remaining keys -> gate properties (preserve)
        foreach (var k in keys)
        {
            var keyName = k.KeyName;
            if (IsParsedKey(keyName)) continue;
            gate.Properties[keyName] = k.Value;
        }

        return gate;
    }

    private GroundService ParseServiceSection(string sectionName, KeyCollection keys)
    {
        var svc = new GroundService();
        if (TryGet(keys, "type", out var t)) svc.Type = t;
        else if (sectionName.IndexOf("push", StringComparison.OrdinalIgnoreCase) >= 0) svc.Type = "pushback";
        else if (sectionName.IndexOf("marshall", StringComparison.OrdinalIgnoreCase) >= 0 || sectionName.IndexOf("gear", StringComparison.OrdinalIgnoreCase) >= 0) svc.Type = "marshaller";

        if (TryGet(keys, "offset", out var offS) && TryParseInvariant(offS, out var off)) svc.OffsetMeters = off;
        if (TryGet(keys, "offset_meters", out var offm) && TryParseInvariant(offm, out off)) svc.OffsetMeters = off;

        var spawn = new Position();
        var hasSpawn = false;
        if (TryGet(keys, "spawn_lat", out var sl) && TryParseInvariant(sl, out var lat)) { spawn.Latitude = lat; hasSpawn = true; }
        if (TryGet(keys, "spawn_lon", out var slon) && TryParseInvariant(slon, out var lon)) { spawn.Longitude = lon; hasSpawn = true; }
        if (TryGet(keys, "spawn_heading", out var sh) && TryParseInvariant(sh, out var h)) { spawn.Heading = h; hasSpawn = true; }
        if (hasSpawn) svc.SpawnCoords = spawn;

        // copy all keys into properties for traceability
        foreach (var k in keys)
        {
            svc.Properties[k.KeyName] = k.Value;
        }

        return svc;
    }

    private List<GroundService> InferServicesFromFlags(KeyCollection keys)
    {
        var list = new List<GroundService>();
        var flags = new[] { "pushback", "marshaller", "catering", "baggage", "fuel" };
        foreach (var f in flags)
        {
            if (TryGet(keys, f, out var v) && IsTrueValue(v))
                list.Add(new GroundService { Type = f });
        }

        // look for pushback_offset style keys
        foreach (var k in keys)
        {
            if (k.KeyName.IndexOf("pushback", StringComparison.OrdinalIgnoreCase) >= 0 &&
                k.KeyName.IndexOf("offset", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var svc = new GroundService { Type = "pushback" };
                if (TryParseInvariant(k.Value, out var off)) svc.OffsetMeters = off;
                list.Add(svc);
            }
        }

        return list;
    }

    private bool TryGet(KeyCollection keys, string keyName, out string value)
    {
        value = string.Empty;
        if (keys.ContainsKey(keyName))
        {
            value = keys[keyName].Trim();
            return true;
        }
        var found = keys.FirstOrDefault(k => string.Equals(k.KeyName, keyName, StringComparison.OrdinalIgnoreCase));
        if (found != null && !string.IsNullOrEmpty(found.Value))
        {
            value = found.Value.Trim();
            return true;
        }
        return false;
    }

    private static bool TryParseInvariant(string s, out double result)
    {
        return double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result)
            || double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out result);
    }

    private static IEnumerable<string> SplitList(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) yield break;
        foreach (var part in raw.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var p = part.Trim();
            if (!string.IsNullOrEmpty(p)) yield return p;
        }
    }

    private static bool IsTrueValue(string v)
    {
        if (string.IsNullOrWhiteSpace(v)) return false;
        v = v.Trim();
        return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("yes", StringComparison.OrdinalIgnoreCase) || v.Equals("on", StringComparison.OrdinalIgnoreCase);
    }

    private static GroundService CloneService(GroundService s)
    {
        return new GroundService
        {
            Type = s.Type,
            OffsetMeters = s.OffsetMeters,
            SpawnCoords = s.SpawnCoords == null ? null : new Position { Latitude = s.SpawnCoords.Latitude, Longitude = s.SpawnCoords.Longitude, Heading = s.SpawnCoords.Heading },
            Properties = new Dictionary<string, string>(s.Properties)
        };
    }

    // Helper method to parse position strings (format: "lat lon heading" or "lat lon heading height")
    private static Position? ParsePosition(string positionString)
    {
        if (string.IsNullOrWhiteSpace(positionString)) return null;
        
        var parts = positionString.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) return null;

        var position = new Position();
        if (TryParseInvariant(parts[0], out var lat)) position.Latitude = lat;
        if (TryParseInvariant(parts[1], out var lon)) position.Longitude = lon;
        if (TryParseInvariant(parts[2], out var heading)) position.Heading = heading;
        
        return position;
    }

    // Helper method to parse parking system object position (format: "lat lon heading height")
    private static ParkingSystemObjectPosition? ParseParkingSystemObjectPosition(string positionString)
    {
        if (string.IsNullOrWhiteSpace(positionString)) return null;
        
        var parts = positionString.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 3) return null;

        var position = new ParkingSystemObjectPosition();
        if (TryParseInvariant(parts[0], out var lat)) position.Latitude = lat;
        if (TryParseInvariant(parts[1], out var lon)) position.Longitude = lon;
        if (TryParseInvariant(parts[2], out var heading)) position.Heading = heading;
        if (parts.Length >= 4 && TryParseInvariant(parts[3], out var height)) position.Height = height;
        
        return position;
    }

    // Helper method to parse boolean from various GSX boolean representations
    private static bool? ParseGsxBoolean(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return IsTrueValue(value);
    }

    // Helper method to parse pushback configuration
    private PushbackConfig? ParsePushbackConfig(KeyCollection keys)
    {
        var config = new PushbackConfig();
        bool hasAnyPushbackData = false;

        if (TryGet(keys, "pushbacktype", out var pushbackTypeS) && int.TryParse(pushbackTypeS, out var pushbackType))
        {
            config.PushbackType = pushbackType;
            hasAnyPushbackData = true;
        }

        if (TryGet(keys, "pushbacklabels", out var labelsS))
        {
            config.PushbackLabels = labelsS.Split('|').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToList();
            hasAnyPushbackData = true;
        }

        if (TryGet(keys, "snapleftpushbackpos", out var snapLeftS))
        {
            config.SnapLeftPushbackPos = ParseGsxBoolean(snapLeftS);
            hasAnyPushbackData = true;
        }

        if (TryGet(keys, "snaprightpushbackpos", out var snapRightS))
        {
            config.SnapRightPushbackPos = ParseGsxBoolean(snapRightS);
            hasAnyPushbackData = true;
        }

        if (TryGet(keys, "pushbackleftpos", out var leftPosS))
        {
            config.PushbackLeftPos = ParsePosition(leftPosS);
            hasAnyPushbackData = true;
        }

        if (TryGet(keys, "pushbackrightpos", out var rightPosS))
        {
            config.PushbackRightPos = ParsePosition(rightPosS);
            hasAnyPushbackData = true;
        }

        if (TryGet(keys, "pushbackleftapproachpos", out var leftApproachS))
        {
            config.PushbackLeftApproachPos = ParsePosition(leftApproachS);
            hasAnyPushbackData = true;
        }

        if (TryGet(keys, "pushbackrightapproachpos", out var rightApproachS))
        {
            config.PushbackRightApproachPos = ParsePosition(rightApproachS);
            hasAnyPushbackData = true;
        }

        if (TryGet(keys, "pushbackleftapproachpos2", out var leftApproach2S))
        {
            config.PushbackLeftApproachPos2 = ParsePosition(leftApproach2S);
            hasAnyPushbackData = true;
        }

        if (TryGet(keys, "pushbackrightapproachpos2", out var rightApproach2S))
        {
            config.PushbackRightApproachPos2 = ParsePosition(rightApproach2S);
            hasAnyPushbackData = true;
        }

        if (TryGet(keys, "pushback_pos", out var pushbackPosS))
        {
            config.PushbackPos = ParsePosition(pushbackPosS);
            hasAnyPushbackData = true;
        }

        // Wingwalkers configuration
        if (TryGet(keys, "wingwalkersleftpushback", out var wingLeftS) && int.TryParse(wingLeftS, out var wingLeft))
        {
            config.WingwalkersLeftPushback = wingLeft;
            hasAnyPushbackData = true;
        }

        if (TryGet(keys, "wingwalkersrightpushback", out var wingRightS) && int.TryParse(wingRightS, out var wingRight))
        {
            config.WingwalkersRightPushback = wingRight;
            hasAnyPushbackData = true;
        }

        if (TryGet(keys, "wingwalkersquickpushback", out var wingQuickS) && int.TryParse(wingQuickS, out var wingQuick))
        {
            config.WingwalkersQuickPushback = wingQuick;
            hasAnyPushbackData = true;
        }

        // Engine start configuration
        if (TryGet(keys, "startenginesleftpushback", out var engineLeftS) && TryParseInvariant(engineLeftS, out var engineLeft))
        {
            config.StartEnginesLeftPushback = engineLeft;
            hasAnyPushbackData = true;
        }

        if (TryGet(keys, "startenginesrightpushback", out var engineRightS) && TryParseInvariant(engineRightS, out var engineRight))
        {
            config.StartEnginesRightPushback = engineRight;
            hasAnyPushbackData = true;
        }

        if (TryGet(keys, "startenginesquickpushback", out var engineQuickS) && TryParseInvariant(engineQuickS, out var engineQuick))
        {
            config.StartEnginesQuickPushback = engineQuick;
            hasAnyPushbackData = true;
        }

        return hasAnyPushbackData ? config : null;
    }

    // Helper method to parse baggage positions
    private BaggagePositions? ParseBaggagePositions(KeyCollection keys)
    {
        var positions = new BaggagePositions();
        bool hasAnyPosition = false;

        if (TryGet(keys, "baggage_loader_front_pos", out var frontLoaderS))
        {
            positions.BaggageLoaderFrontPos = ParsePosition(frontLoaderS);
            hasAnyPosition = true;
        }

        if (TryGet(keys, "baggage_loader_rear_pos", out var rearLoaderS))
        {
            positions.BaggageLoaderRearPos = ParsePosition(rearLoaderS);
            hasAnyPosition = true;
        }

        if (TryGet(keys, "baggage_loader_main_pos", out var mainLoaderS))
        {
            positions.BaggageLoaderMainPos = ParsePosition(mainLoaderS);
            hasAnyPosition = true;
        }

        if (TryGet(keys, "baggage_train_front_pos", out var frontTrainS))
        {
            positions.BaggageTrainFrontPos = ParsePosition(frontTrainS);
            hasAnyPosition = true;
        }

        if (TryGet(keys, "baggage_train_rear_pos", out var rearTrainS))
        {
            positions.BaggageTrainRearPos = ParsePosition(rearTrainS);
            hasAnyPosition = true;
        }

        if (TryGet(keys, "baggage_train_main_pos", out var mainTrainS))
        {
            positions.BaggageTrainMainPos = ParsePosition(mainTrainS);
            hasAnyPosition = true;
        }

        return hasAnyPosition ? positions : null;
    }

    // Helper method to parse stairs positions
    private StairsPositions? ParseStairsPositions(KeyCollection keys)
    {
        var positions = new StairsPositions();
        bool hasAnyPosition = false;

        if (TryGet(keys, "stairs_front_pos", out var frontS))
        {
            positions.StairsFrontPos = ParsePosition(frontS);
            hasAnyPosition = true;
        }

        if (TryGet(keys, "stairs_middle_pos", out var middleS))
        {
            positions.StairsMiddlePos = ParsePosition(middleS);
            hasAnyPosition = true;
        }

        if (TryGet(keys, "stairs_rear_pos", out var rearS))
        {
            positions.StairsRearPos = ParsePosition(rearS);
            hasAnyPosition = true;
        }

        return hasAnyPosition ? positions : null;
    }

    // Helper method to parse waypoint paths from Python-style coordinate arrays
    private WaypointPath? ParseWaypointPath(KeyCollection keys, string waypointKey, string thicknessKey)
    {
        if (!TryGet(keys, waypointKey, out var waypointString)) return null;

        var waypoints = ParseWaypointArray(waypointString);
        if (waypoints == null || waypoints.Count == 0) return null;

        var path = new WaypointPath { Waypoints = waypoints };
        
        if (TryGet(keys, thicknessKey, out var thicknessS) && TryParseInvariant(thicknessS, out var thickness))
            path.Thickness = thickness;

        return path;
    }

    // Helper method to parse waypoint arrays from Python-style format: [(lat, lon, height), ...]
    private List<Waypoint>? ParseWaypointArray(string waypointString)
    {
        if (string.IsNullOrWhiteSpace(waypointString)) return null;

        try
        {
            var waypoints = new List<Waypoint>();
            
            // Remove outer brackets and split by coordinate tuples
            var cleaned = waypointString.Trim().TrimStart('[').TrimEnd(']');
            var coordPattern = @"\(\s*([^,]+)\s*,\s*([^,]+)\s*,\s*([^)]+)\s*\)";
            var matches = Regex.Matches(cleaned, coordPattern);

            foreach (Match match in matches)
            {
                if (match.Groups.Count >= 4)
                {
                    var waypoint = new Waypoint();
                    if (TryParseInvariant(match.Groups[1].Value.Trim(), out var lat))
                        waypoint.Latitude = lat;
                    if (TryParseInvariant(match.Groups[2].Value.Trim(), out var lon))
                        waypoint.Longitude = lon;
                    if (TryParseInvariant(match.Groups[3].Value.Trim(), out var height))
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

    // Helper method to parse 3D position with height
    private Position3D? ParsePosition3D(string positionString)
    {
        if (string.IsNullOrWhiteSpace(positionString)) return null;

        try
        {
            // Handle Python tuple format: (lat, lon, height)
            if (positionString.StartsWith("(") && positionString.EndsWith(")"))
            {
                var cleaned = positionString.Trim('(', ')');
                var parts = cleaned.Split(',').Select(p => p.Trim()).ToArray();
                
                if (parts.Length >= 3)
                {
                    var position = new Position3D();
                    if (TryParseInvariant(parts[0], out var lat)) position.Latitude = lat;
                    if (TryParseInvariant(parts[1], out var lon)) position.Longitude = lon;
                    if (TryParseInvariant(parts[2], out var height)) position.Height = height;
                    return position;
                }
            }
            else
            {
                // Handle space-separated format: "lat lon height"
                var parts = positionString.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    var position = new Position3D();
                    if (TryParseInvariant(parts[0], out var lat)) position.Latitude = lat;
                    if (TryParseInvariant(parts[1], out var lon)) position.Longitude = lon;
                    if (TryParseInvariant(parts[2], out var height)) position.Height = height;
                    return position;
                }
            }
        }
        catch
        {
            return null;
        }
        
        return null;
    }

    // Helper method to parse position arrays
    private List<Position>? ParsePositionArray(string positionString)
    {
        if (string.IsNullOrWhiteSpace(positionString)) return null;

        try
        {
            var positions = new List<Position>();
            
            // Handle empty array
            if (positionString.Trim() == "[]") return positions;
            
            // Handle Python list format: [pos1, pos2, ...]
            var cleaned = positionString.Trim().TrimStart('[').TrimEnd(']');
            if (string.IsNullOrWhiteSpace(cleaned)) return positions;
            
            // Split by commas but be careful about nested coordinates
            var parts = cleaned.Split(',').Select(p => p.Trim()).ToArray();
            
            // Simple case: each part should be a position
            for (int i = 0; i < parts.Length; i += 3)
            {
                if (i + 2 < parts.Length)
                {
                    var position = new Position();
                    if (TryParseInvariant(parts[i], out var lat)) position.Latitude = lat;
                    if (TryParseInvariant(parts[i + 1], out var lon)) position.Longitude = lon;
                    if (TryParseInvariant(parts[i + 2], out var heading)) position.Heading = heading;
                    positions.Add(position);
                }
            }

            return positions.Count > 0 ? positions : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsParsedKey(string key)
    {
        string[] parsedPrefixes = {
            "lat", "lon", "heading", "spawn_lat", "spawn_lon", "spawn_heading",
            "service_", "serviceType", "offset", "offset_meters", "tags", "category",
            "pushback", "marshaller", "catering", "fuel", "allowed_aircraft", "members",
            "this_parking_pos", "type", "maxwingspan", "radiusleft", "radiusright",
            "gatedistancethreshold", "hasjetway", "parkingsystem", "undergroundrefueling",
            "nopassengerstairs", "nopassengerbus", "ignoreicaoprefixes", "ignorepreferredexit",
            "dontcreatejetways", "disablepaxbarriers", "usercustomized", "loadertype",
            "airlinecodes", "handlingtexture", "cateringtexture", "walkertype",
            "walkerpaththickness", "walkerloopstart", "passengerpaththickness",
            "pushbacktype", "pushbacklabels", "snapleftpushbackpos", "snaprightpushbackpos",
            "pushbackleftpos", "pushbackrightpos", "pushbackpos", "wingwalkers", "startengines",
            "parkingsystem_stopposition", "parkingsystem_objectposition",
            "baggage_loader_", "baggage_train_", "stairs_", "walkerwaypoints", "passengerwaypoints",
            "passengerentergatepos", "paxbarrierstexture", "pushbackaddpos", "uiname"
        };

        return parsedPrefixes.Any(p => key.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}