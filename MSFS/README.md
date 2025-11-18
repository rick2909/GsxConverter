GSX Native Plugin (MSFS 2024) – Workspace

This folder contains two parts:

- gsx-native-panel: An in-game toolbar panel (HTML/JS/CSS) to visualize gates/services and later trigger ground actions.
- gsx-bridge: A .NET web app skeleton intended to host SimConnect logic and an HTTP/WebSocket API the panel can call. This enables running without a profile by reading sim data when available.

Notes
- Initial scope: UI loads a sample JSON airport profile (placeholder) and shows structure. Deep integration is planned via the bridge or a future WASM module.
- Taxi-line extraction is explicitly out-of-scope for now (as requested).

Quick Start
1) Panel
   - Package `gsx-native-panel` using the MSFS SDK FSPackagetool to generate `layout.json` and copy the resulting package to your Community folder.
   - The panel will appear in the in-game toolbar. It reads `assets/airports/eham.sample.json` by default.

2) Bridge (optional for MVP)
   - Build and run `gsx-bridge` (.NET 9). It hosts a local HTTP endpoint for the panel at http://localhost:8787.
   - SimConnect integration is stubbed; wire in the SDK once available.

Packaging (MSFS SDK)
- Use FSPackagetool (from MSFS SDK) to generate `layout.json` in `gsx-native-panel`.
- Place the resulting package into your Community folder (varies by store/Steam install).

Roadmap
- Auto-detect current airport by ICAO and load matching JSON when present.
- If no profile, query MSFS (via SimConnect/WASM) for stands/gates and generate a dynamic profile.
- Trigger ground actions natively (pushback, fuel, catering) via SimConnect/WASM.
