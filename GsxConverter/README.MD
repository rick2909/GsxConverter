# GSX -> Canonical JSON Converter

This repository contains a C# console app to convert GSX INI + mapping sources into a canonical JSON format suitable for the in-sim ground-service module.

Quick start (Rider)
1. Ensure .NET SDK 7.0 (or later) is installed.
2. Open Rider and choose "Open" -> select this folder (or open the `.sln` if you create one).
3. From Rider's terminal or external shell:
    - dotnet restore
    - dotnet build
    - dotnet run --project GsxConverter -- --input path/to/sample_gate.ini --output out.json

Important packages used
- System.Text.Json
- IniParser (for robust INI parsing)

What to implement next
- Implement full INI mapping rules for GSX files (preserve unknown keys & comments).
- Implement Python gate-mapping (.py) extractor to interpret custom mapping functions.
- Add JSON Schema validation step to the pipeline (e.g., NJsonSchema or manual validation).
- Add unit tests for INI -> DTO and .py -> DTO extraction (xUnit).

Rider tips
- Use Run Configurations to pass arguments to the console app.
- Add a solution file via `dotnet new sln` and `dotnet sln add GsxConverter/GsxConverter.csproj` if you prefer a solution.