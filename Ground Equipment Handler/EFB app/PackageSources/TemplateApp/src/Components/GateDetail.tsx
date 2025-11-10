import { TTButton, GamepadUiView, RequiredProps, TVNode, UiViewProps } from "@efb/efb-api";
import { FSComponent } from "@microsoft/msfs-sdk";
import { Gate } from "../types/GateData";
import "./GateDetail.scss";

interface GateDetailProps extends RequiredProps<UiViewProps, "appViewService"> {
}

export class GateDetail extends GamepadUiView<HTMLDivElement, GateDetailProps> {
  public readonly tabName = GateDetail.name;

  private getGate(): Gate | null {
    // Retrieve gate from temporary storage
    return (window as any).selectedGate || null;
  }

  public render(): TVNode<HTMLDivElement> {
    const gate = this.getGate();
    
    if (!gate) {
      return (
        <div ref={this.gamepadUiViewRef} class="gate-detail">
          <div class="header">
            <TTButton
              key="Go back"
              type="secondary"
              callback={(): void => {
                this.props.appViewService.goBack();
              }}
            />
            <h2>No Gate Selected</h2>
          </div>
          <div class="content">
            <p>Please select a gate from the list.</p>
          </div>
        </div>
      );
    }

    return (
      <div ref={this.gamepadUiViewRef} class="gate-detail">
        <div class="header">
          <TTButton
            key="Go back"
            type="secondary"
            callback={(): void => {
              this.props.appViewService.goBack();
            }}
          />
          <div class="header-content">
            <h2>{gate.gate_id.toUpperCase()}</h2>
            {gate.has_jetway && <span class="jetway-badge">Has Jetway</span>}
          </div>
        </div>

        <div class="content">
          <div class="detail-section">
            <h3>General Information</h3>
            <div class="detail-grid">
              <div class="detail-item">
                <span class="label">Gate Type:</span>
                <span class="value">{gate.gate_type}</span>
              </div>
              <div class="detail-item">
                <span class="label">UI Name:</span>
                <span class="value">{gate.ui_name}</span>
              </div>
              <div class="detail-item">
                <span class="label">Max Wingspan:</span>
                <span class="value">{gate.max_wingspan}m</span>
              </div>
              <div class="detail-item">
                <span class="label">Parking System:</span>
                <span class="value">{gate.parking_system}</span>
              </div>
              <div class="detail-item">
                <span class="label">Radius Left:</span>
                <span class="value">{gate.radius_left}m</span>
              </div>
              <div class="detail-item">
                <span class="label">Radius Right:</span>
                <span class="value">{gate.radius_right}m</span>
              </div>
            </div>
          </div>

          <div class="detail-section">
            <h3>Position</h3>
            <div class="detail-grid">
              <div class="detail-item">
                <span class="label">Latitude:</span>
                <span class="value">{gate.position.lat.toFixed(6)}</span>
              </div>
              <div class="detail-item">
                <span class="label">Longitude:</span>
                <span class="value">{gate.position.lon.toFixed(6)}</span>
              </div>
              <div class="detail-item">
                <span class="label">Heading:</span>
                <span class="value">{gate.position.heading.toFixed(2)}°</span>
              </div>
            </div>
          </div>

          {gate.airline_codes && (
            <div class="detail-section">
              <h3>Airlines</h3>
              <div class="airline-tags">
                {gate.airline_codes.split(',').map((code) => (
                  <span class="airline-tag">{code.trim()}</span>
                ))}
              </div>
            </div>
          )}

          <div class="detail-section">
            <h3>Services & Features</h3>
            <div class="features-grid">
              {gate.has_jetway && <div class="feature-badge active">Jetway</div>}
              {gate.underground_refueling && <div class="feature-badge active">Underground Refueling</div>}
              {gate.pushback > 0 && <div class="feature-badge active">Pushback Available</div>}
              {!gate.no_passenger_stairs && <div class="feature-badge active">Passenger Stairs</div>}
              {!gate.no_passenger_bus && <div class="feature-badge active">Bus Service</div>}
              {gate.services.length > 0 && (
                <div class="feature-badge active">
                  {gate.services.length} Service{gate.services.length > 1 ? 's' : ''}
                </div>
              )}
            </div>
          </div>

          {gate.handling_texture && gate.handling_texture.length > 0 && (
            <div class="detail-section">
              <h3>Ground Handling</h3>
              <div class="texture-list">
                {gate.handling_texture.map((texture) => (
                  <span class="texture-item">{texture}</span>
                ))}
              </div>
            </div>
          )}

          {gate.catering_texture && gate.catering_texture.length > 0 && (
            <div class="detail-section">
              <h3>Catering Services</h3>
              <div class="texture-list">
                {gate.catering_texture.map((texture) => (
                  <span class="texture-item">{texture}</span>
                ))}
              </div>
            </div>
          )}

          <div class="detail-section">
            <h3>Aircraft Stop Positions</h3>
            <div class="stop-positions">
              {Object.entries(gate.aircraft_stop_positions).map(([aircraft, position]) => (
                <div class="stop-position-item">
                  <span class="aircraft-type">{aircraft.toUpperCase()}</span>
                  <span class="position-value">{position}m</span>
                </div>
              ))}
            </div>
          </div>

          {gate.pushback_config && gate.pushback_config.pushback_labels && (
            <div class="detail-section">
              <h3>Pushback Options</h3>
              <div class="pushback-list">
                {gate.pushback_config.pushback_labels.map((label, index) => (
                  <div class="pushback-item">
                    <span class="pushback-number">{index + 1}</span>
                    <span class="pushback-label">{label}</span>
                  </div>
                ))}
              </div>
            </div>
          )}

          {gate.properties && Object.keys(gate.properties).length > 0 && (
            <div class="detail-section">
              <h3>Custom Properties</h3>
              <div class="detail-grid">
                {Object.entries(gate.properties).map(([key, value]) => (
                  <div class="detail-item">
                    <span class="label">{key}:</span>
                    <span class="value">{String(value)}</span>
                  </div>
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    );
  }
}
