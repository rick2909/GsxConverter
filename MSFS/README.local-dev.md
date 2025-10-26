Local Dev Guide

Prereqs
- MSFS 2024 + SDK installed (for FSPackagetool)
- .NET 9 SDK

Run the bridge
- dotnet run --project .\MSFS\gsx-bridge\Gsx.Bridge.csproj

Panel quick test (unpackaged)
- Copy .\MSFS\gsx-native-panel to your Community folder
- Launch MSFS; the GSX panel should appear in the toolbar

Proper packaging
- Use FSPackagetool to create layout.json and build a proper package inside gsx-native-panel

Wiring your EHAM canonical JSON
- Copy your file from `GsxConverter\sample-files\eham-flytampa.canonical.json` to `MSFS\gsx-native-panel\assets\airports\eham.canonical.json`
- Update index.html select to include it or implement auto-detect via the bridge

Next steps
- Implement bridge endpoints for: current ICAO, current parking, list parking spots
- Add SimConnect logic to trigger ground actions (pushback, fuel, catering)
- Optional: Transition heavy control logic to a WASM module for deeper/native integration
