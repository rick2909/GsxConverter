using System.Text.Json;
using GsxConverter.Models.Json;
using GsxConverter.Parsers;

namespace GsxConverter;

internal class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            Console.WriteLine("GSX Converter - Unified parser for GSX INI and Python configuration files");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  GsxConverter --input <file> --output <out.json>");
            Console.WriteLine("  GsxConverter --test                              # Run comprehensive tests");
            Console.WriteLine();
            Console.WriteLine("Supported input formats:");
            Console.WriteLine("  .ini files - GSX INI configuration format");
            Console.WriteLine("  .py files  - GSX Python configuration format");
            return 1;
        }

        string? input = null;
        string? output = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--input" && i + 1 < args.Length) input = args[++i];
            if (args[i] == "--output" && i + 1 < args.Length) output = args[++i];
        }

        if (input == null)
        {
            Console.Error.WriteLine("Missing --input argument");
            return 2;
        }
        if (output == null)
        {
            output = Path.ChangeExtension(input, ".canonical.json");
        }

        try
        {
            Console.WriteLine($"Parsing input: {input}");
            
            GroundServiceConfig config;
            var extension = Path.GetExtension(input).ToLowerInvariant();
            
            switch (extension)
            {
                case ".ini":
                    var iniParser = new IniGsxParser();
                    config = iniParser.ParseFile(input);
                    break;
                    
                case ".py":
                    // For Python inputs, require a sibling INI file as base (same filename, .ini extension).
                    var pyParser = new PythonGsxParser();
                    var pyCfg = pyParser.ParseFile(input);

                    var baseIni = Path.ChangeExtension(input, ".ini");
                    if (!File.Exists(baseIni))
                    {
                        Console.Error.WriteLine($"Missing base INI file required for Python override: {baseIni}");
                        return 5;
                    }

                    var baseCfg = new IniGsxParser().ParseFile(baseIni);
                    config = ConfigMerger.Merge(baseCfg, pyCfg);
                    break;
                    
                default:
                    Console.Error.WriteLine($"Unsupported file format: {extension}");
                    Console.Error.WriteLine("Supported formats: .ini, .py");
                    return 4;
            }

            Console.WriteLine($"Serializing canonical JSON to: {output}");
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            string json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(output, json);

            // Print summary
            Console.WriteLine();
            Console.WriteLine("Conversion Summary:");
            Console.WriteLine($"  Airport: {config.AirportIcao}");
            Console.WriteLine($"  Gates: {config.Gates.Count}");
            Console.WriteLine($"  DeIce Areas: {config.DeIces.Count}");
            Console.WriteLine($"  Jetway Heights: {config.JetwayRootfloorHeights.Count}");
            Console.WriteLine($"  Gate Groups: {config.GateGroups.Count}");
            Console.WriteLine("Done.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (args.Contains("--verbose"))
            {
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            return 3;
        }
    }
}