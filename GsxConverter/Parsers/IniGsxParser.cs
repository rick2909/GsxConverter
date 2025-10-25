using GsxConverter.Models;
using IniParser;
using IniParser.Model;

namespace GsxConverter.Parsers;

    /// <summary>
    /// Skeleton INI parser for GSX files.
    /// - Uses IniParser (NuGet) to read sections and keys.
    /// - Preserves unknown keys by copying them into Metadata or Properties.
    /// - This class should be extended to handle GSX-specific keys, arrays, and Python mapping extraction.
    /// </summary>
    public class IniGsxParser
    {
        public GroundServiceConfig ParseFile(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException(path);

            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(path);

            var cfg = new GroundServiceConfig();
            // Try to derive airport ICAO from filename or a section key
            cfg.AirportIcao = Path.GetFileNameWithoutExtension(path).ToUpperInvariant();

            // Copy top-level unknown keys into metadata (example)
            foreach (var s in data.Sections)
            {
                ParseSectionIntoConfig(s.SectionName, s.Keys, cfg);
            }

            return cfg;
        }

        private void ParseSectionIntoConfig(string sectionName, KeyDataCollection keys, GroundServiceConfig cfg)
        {
            // GSX typically contains sections like [Gate_<id>] or [GndService_<id>]
            // This is a very small extractor — extend to support full GSX spec.

            if (sectionName.StartsWith("Gate_", StringComparison.OrdinalIgnoreCase))
            {
                var gate = new GateDefinition { GateId = sectionName.Substring(5) };

                if (keys.ContainsKey("lat") && keys.ContainsKey("lon"))
                {
                    if (double.TryParse(keys["lat"], out var lat)) gate.Position.Latitude = lat;
                    if (double.TryParse(keys["lon"], out var lon)) gate.Position.Longitude = lon;
                    if (keys.ContainsKey("heading") && double.TryParse(keys["heading"], out var h)) gate.Position.Heading = h;
                }

                // collect service keys if present
                var services = new List<GroundService>();
                foreach (var k in keys)
                {
                    if (k.KeyName.StartsWith("service_", StringComparison.OrdinalIgnoreCase))
                    {
                        var svc = new GroundService { Type = k.Value };
                        services.Add(svc);
                    }
                    else
                    {
                        // unknown keys -> store as tag/metadata on gate
                        gate.Tags.Add($"{k.KeyName}={k.Value}");
                    }
                }

                gate.Services = services;
                cfg.Gates.Add(gate);
            }
            else
            {
                // top-level or other sections -> store to metadata
                foreach (var k in keys)
                {
                    cfg.Metadata[$"{sectionName}.{k.KeyName}"] = k.Value;
                }
            }
        }
    }