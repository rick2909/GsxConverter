import { TTButton, GamepadUiView, RequiredProps, TVNode, UiViewProps } from '@efb/efb-api';
import { FSComponent } from '@microsoft/msfs-sdk';
import './GateOperations.scss';
import { Gate } from '../types/GateData';

interface GateOperationsProps extends RequiredProps<UiViewProps, 'appViewService'> {
}

/**
 * GateOperations component - Shows service options when at a gate
 * Services: Deboarding, Catering, Refueling, Boarding, Pushback
 * Additional: Jetway/Stairs toggle, Reposition, Customize, Change gate
 */
export class GateOperations extends GamepadUiView<HTMLDivElement, GateOperationsProps> {
  public readonly tabName = GateOperations.name;

  /**
   * Renders the gate operations page
   */
  public render(): TVNode {
    // Get the selected gate from window (passed from GateSelection)
    const gate: Gate = (window as any).selectedGate;

    if (!gate) {
      return (
        <div ref={this.gamepadUiViewRef} class="gate-operations">
          <div class="error-message">No gate selected</div>
        </div>
      );
    }

    return (
      <div ref={this.gamepadUiViewRef} class="gate-operations">
        <div class="operations-header">
          <TTButton
            key="Back"
            type="secondary"
            callback={(): void => {
              this.props.appViewService.goBack();
            }}
          />
          <div class="header-content">
            <h1>{gate.gate_id}</h1>
            <p class="gate-name">{gate.ui_name}</p>
          </div>
        </div>

        <div class="operations-content">
          {/* Services Section */}
          <div class="section services-section">
            <h2>
              <span class="icon">🛎️</span>
              Services
            </h2>
            <div class="service-grid">
              <button class="service-btn" onClick={(): void => {
                console.log("Deboarding service");
                // TODO: Implement deboarding logic
              }}>
                <span class="icon">🚶</span>
                <span class="text">Deboarding</span>
              </button>

              <button class="service-btn" onClick={(): void => {
                console.log("Catering service");
                // TODO: Implement catering logic
              }}>
                <span class="icon">🍱</span>
                <span class="text">Catering</span>
              </button>

              <button class="service-btn" onClick={(): void => {
                console.log("Refueling service");
                // TODO: Implement refueling logic
              }}>
                <span class="icon">⛽</span>
                <span class="text">Refueling</span>
              </button>

              <button class="service-btn" onClick={(): void => {
                console.log("Boarding service");
                // TODO: Implement boarding logic
              }}>
                <span class="icon">🧳</span>
                <span class="text">Boarding</span>
              </button>

              <button class="service-btn pushback" onClick={(): void => {
                console.log("Pushback and start");
                // TODO: Implement pushback logic
              }}>
                <span class="icon">🚀</span>
                <span class="text">Pushback & Start</span>
              </button>
            </div>
          </div>

          {/* Jetway/Stairs Section */}
          <div class="section jetway-section">
            <h2>
              <span class="icon">🔧</span>
              Additional Services
            </h2>
            <div class="jetway-controls">
              {gate.has_jetway && (
                <button class="control-btn jetway" onClick={(): void => {
                  console.log("Toggle jetway");
                  // TODO: Implement jetway toggle
                }}>
                  <span class="icon">🌉</span>
                  <span class="text">Toggle Jetway</span>
                </button>
              )}

              {!gate.no_passenger_stairs && (
                <button class="control-btn stairs" onClick={(): void => {
                  console.log("Toggle stairs");
                  // TODO: Implement stairs toggle
                }}>
                  <span class="icon">🪜</span>
                  <span class="text">Toggle Stairs</span>
                </button>
              )}
            </div>
          </div>

          {/* Reposition Section */}
          <div class="section reposition-section">
            <h2>
              <span class="icon">📍</span>
              Aircraft Position
            </h2>
            <div class="reposition-controls">
              <button class="reposition-btn" onClick={(): void => {
                console.log("Reposition aircraft at current gate");
                // TODO: Implement reposition logic
              }}>
                <span class="icon">🔄</span>
                <span class="text">Reposition Aircraft</span>
                <span class="subtitle">Reset to current gate position</span>
              </button>

              <button class="reposition-btn" onClick={(): void => {
                console.log("Customize position");
                // TODO: Implement customize position logic
              }}>
                <span class="icon">✏️</span>
                <span class="text">Customize Position</span>
                <span class="subtitle">Adjust aircraft placement</span>
              </button>

              <button class="reposition-btn change-gate" onClick={(): void => {
                // Go back to gate list
                this.props.appViewService.open('GateList');
              }}>
                <span class="icon">🔀</span>
                <span class="text">Change Gate Selection</span>
                <span class="subtitle">Return to gate list</span>
              </button>
            </div>
          </div>
        </div>
      </div>
    );
  }
}
