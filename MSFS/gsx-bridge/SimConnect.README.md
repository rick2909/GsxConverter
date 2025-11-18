# SimConnect Integration Plan

This bridge hosts HTTP endpoints for the in-game panel and will integrate with SimConnect for live data and ground actions.

## Options
- SimConnect SDK (native): Microsoft Flight Simulator SDK ships headers and libs for C++/WASM.
- .NET bindings: Community packages exist but may lag. Prefer official C++/WASM for deepest integration; use .NET interop or a tested wrapper when available.

## Roadmap
1) Implement a SimConnectDataProvider that implements ISimDataProvider
   - GetCurrentIcaoAsync: read current ICAO via simvars
   - GetParkingAsync: enumerate parking spots, jetway flags
   - ExecuteActionAsync: pushback, fuel, catering (as supported)

2) Feature-flag it
   - Add a compile symbol (e.g., USE_SIMCONNECT) to switch from StubSimDataProvider

3) Packaging/permissions
   - If using external app, ensure MSFS SimConnect server is reachable
   - If moving to WASM, create a module project and expose interop

## Notes
- The MVP UI will continue to function with the stub provider if SimConnect is not available.
