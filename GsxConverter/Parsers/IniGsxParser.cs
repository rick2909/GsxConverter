using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GsxConverter.Models;

namespace GsxConverter.Parsers;

/// <summary>
/// Extended INI parser for GSX files.
/// Maps common GSX sections and keys into canonical DTOs.
/// Preserves unknown keys into properties and metadata.
/// Adds:
///  - typed parsing for [jetway_rootfloor_heights]
///  - top-level DeIce objects for DeIce* sections
///  - more robust gate section name detection (supports "Gate_", "Gate ", "Gate-")
/// </summary>
public class IniGsxParser
{
    private static readonly string[] ServiceSectionPrefixes = { "GndService", "Service" };
    private static readonly Regex GateRegex = new Regex(@"^(Gate|Stand|Parking)[\s_\-:]*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex DeIceRegex = new Regex(@"^de[-_]?ice", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex GateGroupRegex = new Regex(@"^GateGroup[\s_\-:]*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

        // Position fields
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

        // tags/categories
        if (TryGet(keys, "tags", out var tags)) gate.Tags.AddRange(SplitList(tags));
        if (TryGet(keys, "category", out var cat) && !string.IsNullOrWhiteSpace(cat)) gate.Tags.Add(cat);

        // allowed aircraft lists
        if (TryGet(keys, "allowed_aircraft", out var allowed) || TryGet(keys, "allowedaircraft", out allowed))
        {
            foreach (var a in SplitList(allowed)) gate.AllowedAircraft.Add(a);
        }

        // services referenced inline
        var services = new List<GroundService>();
        foreach (var k in keys)
        {
            if (k.KeyName.StartsWith("service_", StringComparison.OrdinalIgnoreCase))
            {
                var svcRef = k.Value.Trim();
                if (string.IsNullOrEmpty(svcRef)) continue;
                if (globalServices.TryGetValue(svcRef, out var gs))
                    services.Add(CloneService(gs));
                else
                    services.Add(new GroundService { Type = svcRef });
            }
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

    private static bool IsParsedKey(string key)
    {
        string[] parsedPrefixes = {
            "lat", "lon", "heading", "spawn_lat", "spawn_lon", "spawn_heading",
            "service_", "serviceType", "offset", "offset_meters", "tags", "category",
            "pushback", "marshaller", "catering", "fuel", "allowed_aircraft", "members"
        };

        return parsedPrefixes.Any(p => key.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}