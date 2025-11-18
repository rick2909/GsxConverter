GSX Bridge (Local Service)

Purpose
- Provide a local HTTP/WebSocket surface the in-game panel can call to trigger actions and read MSFS data.
- Later: integrate SimConnect/WASM to control ground services and query live airport/parking data.

Run
- Prereq: .NET 9 SDK
- Start: dotnet run --project ./Gsx.Bridge.csproj
- Health: GET http://localhost:8787/api/health

Endpoints (MVP)
- POST /api/action { action: string, payload?: any }
  - Stubbed; logs the action and returns accepted: true

Next
- Add SimConnect client and wrap common actions (pushback, fuel, catering)
- Add GET for current ICAO and current parking spot
- Add WebSocket for real-time events
