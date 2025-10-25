using System.Text.Json;
using GsxConverter.Models;
using GsxConverter.Parsers;

namespace GsxConverter;

internal class Program
{
    static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
        {
            Console.WriteLine("Usage: GsxConverter --input <gsx.ini> --output <out.json>");
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
            Console.WriteLine($"Parsing INI: {input}");
            var parser = new IniGsxParser();
            GroundServiceConfig config = parser.ParseFile(input);

            Console.WriteLine($"Serializing canonical JSON to: {output}");
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(output, json);

            Console.WriteLine("Done.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            return 3;
        }
    }
}