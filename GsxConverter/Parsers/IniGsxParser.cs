using System.Globalization;
using GsxConverter.Models;
using IniParser;
using IniParser.Model;

namespace GsxConverter.Parsers;

/// <summary>
/// Extended INI parser for GSX files.
/// Maps common GSX sections and keys into canonical DTOs.
/// Preserves unknown keys into properties and metadata.
/// </summary>
public class IniGsxParser
{
    private static readonly string[] GateSectionPrefixes = { "Gate_", "Stand_", "Parking_" };
    private static readonly string[] ServiceSectionPrefixes = { "GndService_", "Service_" };

    public GroundServiceConfig ParseFile(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException(path);

        var parser = new FileIniDataParser();
        IniData data = parser.ReadFile(path);

        var cfg = new GroundServiceConfig
        {
            AirportIcao = Path.GetFileNameWithoutExtension(path).ToUpperInvariant()
        };

        // Collect global service definitions (GndService_* sections)
        var globalServices = new Dictionary<string, GroundService>(StringComparer.OrdinalIgnoreCase);

        foreach (var section in data.Sections)
        {
            string name = section.SectionName.Trim();

            if (IsServiceSection(name))
            {
                var svcId = name;
                var svc = ParseServiceSection(svcId, section.Keys);
                globalServices[svcId] = svc;
                // Also store as metadata for traceability
                cfg.Metadata[$"service.{svcId}.type"] = svc.Type;
            }
        }

        foreach (var section in data.Sections)
        {
            string name = section.SectionName.Trim();

            if (IsGateSection(name))
            {
                var gate = ParseGateSection(name, section.Keys, globalServices);
                cfg.Gates.Add(gate);
            }
            else if (IsServiceSection(name))
            {
                // already processed above
                continue;
            }
            else
            {
                // unknown or top-level section -> flatten keys to metadata
                foreach (var k in section.Keys)
                {
                    cfg.Metadata[$"{name}.{k.KeyName}"] = k.Value;
                }
            }
        }

        return cfg;
    }

    private bool IsGateSection(string sectionName)
    {
        return GateSectionPrefixes.Any(prefix => sectionName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsServiceSection(string sectionName)
    {
        return ServiceSectionPrefixes.Any(prefix => sectionName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private GateDefinition ParseGateSection(string sectionName, KeyDataCollection keys, Dictionary<string, GroundService> globalServices)
    {
        // Gate ID is the part after the prefix if known, otherwise full name
        string gateId = sectionName;
        foreach (var p in GateSectionPrefixes)
        {
            if (sectionName.StartsWith(p, StringComparison.OrdinalIgnoreCase))
            {
                gateId = sectionName.Substring(p.Length);
                break;
            }
        }

        var gate = new GateDefinition { GateId = gateId };

        // Position
        double lat = 0, lon = 0, heading = 0;
        if (TryGet(keys, "lat", out var latS) && TryParseInvariant(latS, out lat)) gate.Position.Latitude = lat;
        if (TryGet(keys, "lon", out var lonS) && TryParseInvariant(lonS, out lon)) gate.Position.Longitude = lon;
        if (TryGet(keys, "heading", out var hS) && TryParseInvariant(hS, out heading)) gate.Position.Heading = heading;

        // Some GSX files use spawn_lat/spawn_lon for the gate; map those if present and position was not set
        if ((gate.Position.Latitude == 0 && gate.Position.Longitude == 0) &&
            (TryGet(keys, "spawn_lat", out var sl) || TryGet(keys, "spawn_latitude", out sl)))
        {
            if (TryParseInvariant(sl, out lat)) gate.Position.Latitude = lat;
            if (TryGet(keys, "spawn_lon", out var slon) && TryParseInvariant(slon, out lon)) gate.Position.Longitude = lon;
            if (TryGet(keys, "spawn_heading", out var sh) && TryParseInvariant(sh, out heading)) gate.Position.Heading = heading;
        }

        // Tags and lists
        if (TryGet(keys, "tags", out var tagsS)) gate.Tags.AddRange(SplitList(tagsS));
        if (TryGet(keys, "category", out var cat) && !string.IsNullOrWhiteSpace(cat)) gate.Tags.Add(cat);

        // Services referenced inline like service_1 = GndService_Pushback or service_1 = pushback
        var services = new List<GroundService>();
        foreach (var key in keys)
        {
            if (key.KeyName.StartsWith("service_", StringComparison.OrdinalIgnoreCase))
            {
                var svcRef = key.Value.Trim();
                if (string.IsNullOrEmpty(svcRef)) continue;

                // If it's a global service section name, use that definition
                if (globalServices.TryGetValue(svcRef, out var gs))
                {
                    // clone gs to avoid cross-gate sharing of dict instances
                    services.Add(CloneService(gs));
                }
                else
                {
                    // inline shorthand: e.g., service_1 = pushback
                    var svc = new GroundService { Type = svcRef };
                    services.Add(svc);
                }
            }
        }

        // Also support explicit keys per service (serviceType, offset, spawn_lat, spawn_lon, spawn_heading)
        // We'll create a single service when keys like serviceType exist
        if (TryGet(keys, "serviceType", out var st))
        {
            var svc = new GroundService { Type = st };
            if (TryGet(keys, "offset", out var offS) && TryParseInvariant(offS, out var off)) svc.OffsetMeters = off;
            if (TryGet(keys, "offset_meters", out var offm) && TryParseInvariant(offm, out off)) svc.OffsetMeters = off;
            var spawn = new Position();
            bool hasSpawn = false;
            if (TryGet(keys, "spawn_lat", out var spLat) && TryParseInvariant(spLat, out var spl)) { spawn.Latitude = spl; hasSpawn = true; }
            if (TryGet(keys, "spawn_lon", out var spLon) && TryParseInvariant(spLon, out var splon)) { spawn.Longitude = splon; hasSpawn = true; }
            if (TryGet(keys, "spawn_heading", out var spH) && TryParseInvariant(spH, out var sph)) { spawn.Heading = sph; hasSpawn = true; }
            if (hasSpawn) svc.SpawnCoords = spawn;

            // collect additional unknown keys for the service
            foreach (var key in keys)
            {
                if (key.KeyName.StartsWith("service.", StringComparison.OrdinalIgnoreCase) ||
                    key.KeyName.StartsWith("svc_", StringComparison.OrdinalIgnoreCase))
                {
                    svc.Properties[key.KeyName] = key.Value;
                }
            }

            services.Add(svc);
        }

        // If service entries were not found, try to infer from keys like pushback=1 or marshaller=1
        var inferred = InferServicesFromFlags(keys);
        services.AddRange(inferred);

        // Save services on gate
        gate.Services = services;

        // Capture remaining keys into properties
        foreach (var k in keys)
        {
            string keyName = k.KeyName;
            // skip parsed keys
            if (IsParsedKey(keyName)) continue;
            gate.Properties[keyName] = k.Value;
        }

        return gate;
    }

    private GroundService ParseServiceSection(string sectionName, KeyDataCollection keys)
    {
        // Create a service DTO from a global GndService_* section
        // sectionName kept as id in metadata elsewhere
        var svc = new GroundService();

        // determine canonical type if present
        if (TryGet(keys, "type", out var t)) svc.Type = t;
        else if (sectionName.IndexOf("push", StringComparison.OrdinalIgnoreCase) >= 0) svc.Type = "pushback";
        else if (sectionName.IndexOf("gear", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 sectionName.IndexOf("marshall", StringComparison.OrdinalIgnoreCase) >= 0) svc.Type = "marshaller";

        // offset / offset_meters
        if (TryGet(keys, "offset", out var offS) && TryParseInvariant(offS, out var off)) svc.OffsetMeters = off;
        if (TryGet(keys, "offset_meters", out var offm) && TryParseInvariant(offm, out off)) svc.OffsetMeters = off;

        // spawn coords (optional)
        var spawn = new Position();
        bool hasSpawn = false;
        if (TryGet(keys, "spawn_lat", out var sl) && TryParseInvariant(sl, out var lat)) { spawn.Latitude = lat; hasSpawn = true; }
        if (TryGet(keys, "spawn_lon", out var slon) && TryParseInvariant(slon, out var lon)) { spawn.Longitude = lon; hasSpawn = true; }
        if (TryGet(keys, "spawn_heading", out var sh) && TryParseInvariant(sh, out var h)) { spawn.Heading = h; hasSpawn = true; }
        if (hasSpawn) svc.SpawnCoords = spawn;

        // arbitrary properties
        foreach (var k in keys)
        {
            // standard keys already processed, but it's fine to copy all into properties too for traceability
            svc.Properties[k.KeyName] = k.Value;
        }

        return svc;
    }

    private List<GroundService> InferServicesFromFlags(KeyDataCollection keys)
    {
        var list = new List<GroundService>();
        // common boolean flags indicating availability of services
        var flags = new[] { "pushback", "marshaller", "catering", "baggage", "fuel" };
        foreach (var f in flags)
        {
            if (TryGet(keys, f, out var v))
            {
                if (IsTrueValue(v))
                {
                    list.Add(new GroundService { Type = f });
                }
            }
        }

        // also detect pushback offset keys like pushback_offset
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

    private bool TryGet(KeyDataCollection keys, string keyName, out string value)
    {
        value = string.Empty;
        if (keys.ContainsKey(keyName))
        {
            value = keys[keyName].Trim();
            return true;
        }

        // try common case-insensitive variants
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
            "pushback", "marshaller", "catering", "fuel"
        };

        return parsedPrefixes.Any(p => key.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }
}