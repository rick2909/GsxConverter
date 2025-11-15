import { TTButton, GamepadUiView, RequiredProps, TVNode, UiViewProps } from '@efb/efb-api';
import { FSComponent } from '@microsoft/msfs-sdk';
import './GateSelection.scss';
import { Gate } from '../types/GateData';

interface GateSelectionProps extends RequiredProps<UiViewProps, 'appViewService'> {
}

/**
 * GateSelection component - Shows options before going to a gate
 * Options: Follow Me, Show Spot, Warp, Cancel/Confirm
 */
export class GateSelection extends GamepadUiView<HTMLDivElement, GateSelectionProps> {
  public readonly tabName = GateSelection.name;

  /**
   * Renders the gate selection page
   */
  public render(): TVNode {
    // Get the selected gate from window (passed from GateList)
    const gate: Gate = (window as any).selectedGate;

    if (!gate) {
      return (
        <div ref={this.gamepadUiViewRef} class="gate-selection">
          <div class="error-message">No gate selected</div>
        </div>
      );
    }

    return (
      <div ref={this.gamepadUiViewRef} class="gate-selection">
        <div class="selection-header">
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

        <div class="selection-content">
          {/* Gate Information Summary */}
          <div class="gate-summary">
            <div class="summary-item">
              <span class="label">Type:</span>
              <span class="value">{gate.has_jetway ? 'Jetway' : gate.no_passenger_bus ? 'Walk' : 'Bus'}</span>
            </div>
            {gate.airline_codes && (
              <div class="summary-item">
                <span class="label">Airlines:</span>
                <span class="value">{gate.airline_codes}</span>
              </div>
            )}
            {gate.max_wingspan && (
              <div class="summary-item">
                <span class="label">Max Wingspan:</span>
                <span class="value">{gate.max_wingspan}m</span>
              </div>
            )}
          </div>

          {/* Action Buttons */}
          <div class="action-section">
            <h2>Options</h2>
            <div class="action-buttons">
              <button class="action-btn" onClick={(): void => {
                console.log("Request Follow Me to", gate.gate_id);
                // TODO: Implement follow me logic
              }}>
                <span class="icon">🚗</span>
                <span class="text">Request Follow Me</span>
              </button>

              <button class="action-btn" onClick={(): void => {
                console.log("Show spot for", gate.gate_id);
                // TODO: Implement show spot logic
              }}>
                <span class="icon">📍</span>
                <span class="text">Show Me This Spot</span>
              </button>

              <button class="action-btn" onClick={(): void => {
                console.log("Warp to", gate.gate_id);
                // TODO: Implement warp logic
              }}>
                <span class="icon">✈️</span>
                <span class="text">Warp Me There</span>
              </button>
            </div>
          </div>

          {/* Confirm/Cancel Section */}
          <div class="confirm-section">
            <button 
              class="cancel-btn"
              onClick={(): void => {
                this.props.appViewService.goBack();
              }}
            >
              Cancel
            </button>
            <button 
              class="confirm-btn"
              onClick={(): void => {
                // For now, go directly to gate operations page
                this.props.appViewService.open('GateOperations');
              }}
            >
              Confirm Selection
            </button>
          </div>
        </div>
      </div>
    );
  }
}
