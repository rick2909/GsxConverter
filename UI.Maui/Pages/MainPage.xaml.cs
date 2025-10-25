#pragma warning disable CS8019 // Unused using directive
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using GsxConverter.Models;
using GsxConverter.Parsers;
using System.Linq;
#pragma warning restore CS8019

namespace UI.Maui.Pages
{
    public partial class MainPage
    {
        ObservableCollection<GateDefinition> _gates = new();

        string? _inputTempPath;

        public MainPage()
        {
            InitializeComponent();
            GatesCollection.ItemsSource = _gates;
        }

        async void OnOpenIniClicked(object? sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select GSX INI file",
                });

                if (result == null)
                {
                    StatusLabel.Text = "Open cancelled.";
                    return;
                }

                // Copy to a temp path so the parser can read by path
                var fileName = result.FileName;
                var tempPath = Path.Combine(FileSystem.CacheDirectory, fileName);
                using (var stream = await result.OpenReadAsync())
                using (var outStream = File.Create(tempPath))
                {
                    await stream.CopyToAsync(outStream);
                }

                _inputTempPath = tempPath;
                OutputPathEntry.Text = Path.Combine(Path.GetDirectoryName(_inputTempPath) ?? FileSystem.AppDataDirectory, Path.GetFileNameWithoutExtension(_inputTempPath) + ".canonical.json");

                StatusLabel.Text = $"Loaded {fileName}";
                ParseAndDisplay(_inputTempPath);
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "Error opening file: " + ex.Message;
            }
        }

        async void OnConvertClicked(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_inputTempPath) || !File.Exists(_inputTempPath))
            {
                StatusLabel.Text = "No input selected.";
                return;
            }

            var output = OutputPathEntry.Text;
            if (string.IsNullOrWhiteSpace(output))
            {
                StatusLabel.Text = "Please select an output path (edit the Output box).";
                return;
            }

            try
            {
                // Parse on a background thread because parsing can be CPU-bound
                var cfg = await Task.Run(() => new IniGsxParser().ParseFile(_inputTempPath));
                var opt = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(cfg, opt);

                // Ensure directory exists
                var dir = Path.GetDirectoryName(output) ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                await File.WriteAllTextAsync(output, json);
                JsonPreviewEditor.Text = json;
                StatusLabel.Text = $"Saved to {output}";
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "Convert error: " + ex.Message;
            }
        }

        void ParseAndDisplay(string path)
        {
            try
            {
                var parser = new IniGsxParser();
                var cfg = parser.ParseFile(path);

                _gates.Clear();
                foreach (var g in cfg.Gates.Take(500))
                    _gates.Add(g);

                var opt = new JsonSerializerOptions { WriteIndented = true };
                JsonPreviewEditor.Text = JsonSerializer.Serialize(cfg, opt);

                // Build a top-of-UI friendly representation of the jetway_rootfloor_heights section
                if (cfg.JetwayRootfloorHeights.Count > 0)
                {
                    var lines = cfg.JetwayRootfloorHeights
                        .Select(kvp => $"{kvp.Key} = {kvp.Value}")
                        .ToArray();
                    ConfigEditor.Text = string.Join(Environment.NewLine, lines);
                }
                else
                {
                    ConfigEditor.Text = "(no jetway_rootfloor_heights section found)";
                }

                // Show de-ice count in status message for immediate feedback
                var diCount = cfg.DeIces.Count;
                StatusLabel.Text = $"Parsed {cfg.Gates.Count} gates. DeIce entries: {diCount}";
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "Parse error: " + ex.Message;
                JsonPreviewEditor.Text = ex.ToString();
                ConfigEditor.Text = string.Empty;
            }
        }
    }
}