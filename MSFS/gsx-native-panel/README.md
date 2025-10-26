GSX Native Panel

This is an MSFS in-game toolbar panel that loads GSX-style JSON data and exposes basic actions.

Contents
- html_ui/InGamePanels/gsx-native-panel: Panel assets (index.html, css, js, panel.xml)
- assets/airports: Sample JSON profiles (e.g., eham.sample.json)

Build & Package
- Use the MSFS SDK FSPackagetool to generate layout.json and package the folder for the Community directory.
- Alternatively, for quick dev, you can copy the folder as-is into Community, but FSPackagetool is recommended for proper layout.json.

Notes
- Buttons call a local bridge at http://localhost:8787 (stub for now). If the bridge is not running, actions are no-ops.
- Next steps: auto-detect ICAO via bridge; if no profile, query sim parking data and generate a transient profile.
