using GsxConverter.Models.Json;
using System.Collections.ObjectModel;
using System.Text.Json;
using GsxConverter.Parsers;

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
            DeIceCollection.ItemsSource = new ObservableCollection<DeIceDefinition>();
        }

        async void OnOpenGsxFileClicked(object? sender, EventArgs e)
        {
            try
            {
                var customFileType = new FilePickerFileType(
                    new Dictionary<DevicePlatform, IEnumerable<string>>
                    {
                        { DevicePlatform.WinUI, new[] { ".ini", ".py" } },
                        { DevicePlatform.macOS, new[] { "ini", "py" } },
                    });

                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Select GSX Configuration File",
                    FileTypes = customFileType,
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

                // Update file type display
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                FileTypeLabel.Text = extension switch
                {
                    ".ini" => "GSX INI Format",
                    ".py" => "GSX Python Format", 
                    _ => "Unknown Format"
                };

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
                var cfg = await Task.Run(() => ParseGsxFile(_inputTempPath));
                var opt = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                var json = JsonSerializer.Serialize(cfg, opt);

                // Ensure directory exists
                var dir = Path.GetDirectoryName(output) ?? Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                await File.WriteAllTextAsync(output, json);
                JsonPreviewEditor.Text = json;
                StatusLabel.Text = $"Saved to {output} (Airport: {cfg.AirportIcao}, Gates: {cfg.Gates.Count})";
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "Convert error: " + ex.Message;
            }
        }

        GroundServiceConfig ParseGsxFile(string path)
        {
            var extension = Path.GetExtension(path).ToLowerInvariant();

            if (extension == ".ini")
                return new IniGsxParser().ParseFile(path);

            if (extension == ".py")
            {
                // Python config must have a sibling INI file with the same basename
                var iniPath = Path.ChangeExtension(path, ".ini");
                if (!File.Exists(iniPath))
                    throw new FileNotFoundException($"Base INI file not found for Python override: {iniPath}");

                var baseCfg = new IniGsxParser().ParseFile(iniPath);
                var pyCfg = new PythonGsxParser().ParseFile(path);
                return ConfigMerger.Merge(baseCfg, pyCfg);
            }

            throw new NotSupportedException($"Unsupported file format: {extension}");
        }

        void ParseAndDisplay(string path)
        {
            try
            {
                var cfg = ParseGsxFile(path);

                _gates.Clear();
                foreach (var g in cfg.Gates.Take(500))
                    _gates.Add(g);

                var opt = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                };
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

                // Populate DeIce list
                var deiceList = DeIceCollection.ItemsSource as ObservableCollection<DeIceDefinition>;
                if (deiceList != null)
                {
                    deiceList.Clear();
                    foreach (var d in cfg.DeIces) deiceList.Add(d);
                }

                // Show comprehensive status with enhanced model features
                var extension = Path.GetExtension(path).ToLowerInvariant();
                var formatName = extension switch
                {
                    ".ini" => "INI",
                    ".py" => "Python",
                    _ => "Unknown"
                };

                var hasWaypoints = cfg.Gates.Any(g => g.WalkerWaypoints != null || g.PassengerWaypoints != null);
                var waypointInfo = hasWaypoints ? " (with waypoints)" : "";
                
                StatusLabel.Text = $"Parsed {formatName}: {cfg.Gates.Count} gates, {cfg.DeIces.Count} deice areas{waypointInfo}";
            }
            catch (Exception ex)
            {
                StatusLabel.Text = "Parse error: " + ex.Message;
                JsonPreviewEditor.Text = ex.ToString();
                ConfigEditor.Text = string.Empty;
            }
        }

        void OnGateSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is GateDefinition selectedGate)
            {
                // Show detailed gate information in JSON preview
                var options = new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var gateJson = JsonSerializer.Serialize(selectedGate, options);
                JsonPreviewEditor.Text = $"Selected Gate: {selectedGate.GateId}\n\n{gateJson}";
                
                // Update status with enhanced features
                var features = new List<string>();
                if (selectedGate.HasJetway == true) features.Add("Jetway");
                if (selectedGate.WalkerWaypoints?.Waypoints.Count > 0) features.Add($"Walker waypoints ({selectedGate.WalkerWaypoints.Waypoints.Count})");
                if (selectedGate.PassengerWaypoints?.Waypoints.Count > 0) features.Add($"Passenger waypoints ({selectedGate.PassengerWaypoints.Waypoints.Count})");
                if (selectedGate.PassengerEnterGatePos != null) features.Add("3D passenger entry");
                if (selectedGate.BaggagePositions != null) features.Add("Baggage positions");
                if (selectedGate.PushbackConfig != null) features.Add("Pushback config");
                
                var featureText = features.Count > 0 ? $" - Features: {string.Join(", ", features)}" : "";
                StatusLabel.Text = $"Gate {selectedGate.GateId}: {selectedGate.Services.Count} services{featureText}";
            }
        }
    }
}