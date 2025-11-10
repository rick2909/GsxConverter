import { TTButton, GamepadUiView, RequiredProps, TVNode, UiViewProps } from "@efb/efb-api";
import { FSComponent } from "@microsoft/msfs-sdk";
import { AirportData, Gate } from "../types/GateData";
import "./GateList.scss";

interface GateListProps extends RequiredProps<UiViewProps, "appViewService"> {
  /** The airport data */
  airportData: AirportData;
}

export class GateList extends GamepadUiView<HTMLDivElement, GateListProps> {
  public readonly tabName = GateList.name;

  private filterText = "";

  private getFilteredGates(): Gate[] {
    if (!this.filterText) {
      return this.props.airportData.gates;
    }
    
    const searchTerm = this.filterText.toLowerCase();
    return this.props.airportData.gates.filter(gate => 
      gate.gate_id.toLowerCase().includes(searchTerm) ||
      gate.ui_name.toLowerCase().includes(searchTerm) ||
      gate.airline_codes.toLowerCase().includes(searchTerm)
    );
  }

  public render(): TVNode<HTMLDivElement> {
    const filteredGates = this.getFilteredGates();
    const isLoading = this.props.airportData.airport === "Loading..." || this.props.airportData.gates.length === 0;
    
    return (
      <div ref={this.gamepadUiViewRef} class="gate-list">
        <div class="header">
          <h1>{this.props.airportData.airport}</h1>
          <div class="header-info">
            <span class="gate-count">{filteredGates.length} gates</span>
            <span class="version">Version: {this.props.airportData.version}</span>
          </div>
        </div>

        {isLoading && (
          <div class="loading-message">
            <p>Loading gate data...</p>
          </div>
        )}

        <div class="search-bar">
          <input
            type="text"
            placeholder="Search by gate ID, name, or airline..."
            onInput={(e: any): void => {
              this.filterText = (e.target as HTMLInputElement).value;
              // Force re-render by reopening current view
              this.props.appViewService.open("GateList");
            }}
          />
        </div>

        {!isLoading && (
          <div class="gates-container">
            <div class="gates-grid">
              {filteredGates.map((gate) => (
              <div
                class="gate-card"
                onClick={(): void => {
                  // Store gate data for detail view - we'll pass it via a service or state management
                  (window as any).selectedGate = gate;
                  this.props.appViewService.open("GateDetail");
                }}
              >
                <div class="gate-header">
                  <h3>{gate.gate_id.toUpperCase()}</h3>
                  {gate.has_jetway && <span class="jetway-badge">Jetway</span>}
                </div>
                
                <div class="gate-info">
                  <div class="info-row">
                    <span class="label">Type:</span>
                    <span class="value">Gate Type {gate.gate_type}</span>
                  </div>
                  
                  <div class="info-row">
                    <span class="label">Max Wingspan:</span>
                    <span class="value">{gate.max_wingspan}m</span>
                  </div>
                  
                  {gate.airline_codes && (
                    <div class="info-row">
                      <span class="label">Airlines:</span>
                      <span class="value airline-codes">{gate.airline_codes}</span>
                    </div>
                  )}
                  
                  <div class="info-row">
                    <span class="label">Parking System:</span>
                    <span class="value">{gate.parking_system}</span>
                  </div>
                </div>

                <div class="gate-features">
                  {gate.underground_refueling && <span class="feature">Underground Fuel</span>}
                  {gate.pushback > 0 && <span class="feature">Pushback</span>}
                  {!gate.no_passenger_bus && <span class="feature">Bus Service</span>}
                </div>
              </div>
              ))}
            </div>
          </div>
        )}
      </div>
    );
  }
}
