import { Gate } from '../types/GateData';
import './GateSelection.css';

interface GateSelectionProps {
  gate: Gate;
  onBack: () => void;
  onConfirm: () => void;
}

export default function GateSelection({ gate, onBack, onConfirm }: GateSelectionProps) {
  const handleFollowMe = () => {
    console.log("Request Follow Me to", gate.gate_id);
    // Send message to EFB
    window.parent.postMessage({
      type: 'GROUND_SERVICE',
      action: 'FOLLOW_ME',
      gateId: gate.gate_id
    }, '*');
  };

  const handleShowSpot = () => {
    console.log("Show spot for", gate.gate_id);
    window.parent.postMessage({
      type: 'GROUND_SERVICE',
      action: 'SHOW_SPOT',
      gateId: gate.gate_id
    }, '*');
  };

  const handleWarp = () => {
    console.log("Warp to", gate.gate_id);
    window.parent.postMessage({
      type: 'GROUND_SERVICE',
      action: 'WARP',
      gateId: gate.gate_id
    }, '*');
  };

  return (
    <div className="gate-selection">
      <div className="selection-header">
        <button className="back-button" onClick={onBack}>
          ← Back
        </button>
        <div className="header-content">
          <h1>{gate.gate_id}</h1>
          <p className="gate-name">{gate.ui_name}</p>
        </div>
      </div>

      <div className="selection-content">
        {/* Gate Information Summary */}
        <div className="gate-summary">
          <div className="summary-item">
            <span className="label">Type:</span>
            <span className="value">
              {gate.has_jetway ? 'Jetway' : gate.no_passenger_bus ? 'Walk' : 'Bus'}
            </span>
          </div>
          {gate.airline_codes && (
            <div className="summary-item">
              <span className="label">Airlines:</span>
              <span className="value">{gate.airline_codes}</span>
            </div>
          )}
          {gate.max_wingspan && (
            <div className="summary-item">
              <span className="label">Max Wingspan:</span>
              <span className="value">{gate.max_wingspan}m</span>
            </div>
          )}
        </div>

        {/* Action Buttons */}
        <div className="action-section">
          <h2>Options</h2>
          <div className="action-buttons">
            <button className="action-btn" onClick={handleFollowMe}>
              <span className="icon">🚗</span>
              <span className="text">Request Follow Me</span>
            </button>

            <button className="action-btn" onClick={handleShowSpot}>
              <span className="icon">📍</span>
              <span className="text">Show Me This Spot</span>
            </button>

            <button className="action-btn" onClick={handleWarp}>
              <span className="icon">✈️</span>
              <span className="text">Warp Me There</span>
            </button>
          </div>
        </div>

        {/* Confirm/Cancel Section */}
        <div className="confirm-section">
          <button className="cancel-btn" onClick={onBack}>
            Cancel
          </button>
          <button className="confirm-btn" onClick={onConfirm}>
            Confirm Selection
          </button>
        </div>
      </div>
    </div>
  );
}
