import { Gate } from '../types/GateData';
import './GateOperations.css';

interface GateOperationsProps {
  gate: Gate;
  onBack: () => void;
  onChangeGate: () => void;
}

export default function GateOperations({ gate, onBack, onChangeGate }: GateOperationsProps) {
  const requestService = (serviceType: string) => {
    console.log(`${serviceType} service requested`);
    window.parent.postMessage({
      type: 'GROUND_SERVICE',
      action: serviceType,
      gateId: gate.gate_id
    }, '*');
  };

  return (
    <div className="gate-operations">
      <div className="operations-header">
        <button className="back-button" onClick={onBack}>
          ← Back
        </button>
        <div className="header-content">
          <h1>{gate.gate_id}</h1>
          <p className="gate-name">{gate.ui_name}</p>
        </div>
      </div>

      <div className="operations-content">
        {/* Services Section */}
        <div className="section services-section">
          <h2>
            <span className="icon">🛎️</span>
            Services
          </h2>
          <div className="service-grid">
            <button className="service-btn" onClick={() => requestService('DEBOARDING')}>
              <span className="icon">🚶</span>
              <span className="text">Deboarding</span>
            </button>

            <button className="service-btn" onClick={() => requestService('CATERING')}>
              <span className="icon">🍱</span>
              <span className="text">Catering</span>
            </button>

            <button className="service-btn" onClick={() => requestService('REFUELING')}>
              <span className="icon">⛽</span>
              <span className="text">Refueling</span>
            </button>

            <button className="service-btn" onClick={() => requestService('BOARDING')}>
              <span className="icon">🧳</span>
              <span className="text">Boarding</span>
            </button>

            <button className="service-btn pushback" onClick={() => requestService('PUSHBACK')}>
              <span className="icon">🚀</span>
              <span className="text">Pushback & Start</span>
            </button>
          </div>
        </div>

        {/* Jetway/Stairs Section */}
        <div className="section jetway-section">
          <h2>
            <span className="icon">🔧</span>
            Additional Services
          </h2>
          <div className="jetway-controls">
            {gate.has_jetway && (
              <button className="control-btn jetway" onClick={() => requestService('TOGGLE_JETWAY')}>
                <span className="icon">🌉</span>
                <span className="text">Toggle Jetway</span>
              </button>
            )}

            {!gate.no_passenger_stairs && (
              <button className="control-btn stairs" onClick={() => requestService('TOGGLE_STAIRS')}>
                <span className="icon">🪜</span>
                <span className="text">Toggle Stairs</span>
              </button>
            )}
          </div>
        </div>

        {/* Reposition Section */}
        <div className="section reposition-section">
          <h2>
            <span className="icon">📍</span>
            Aircraft Position
          </h2>
          <div className="reposition-controls">
            <button className="reposition-btn" onClick={() => requestService('REPOSITION')}>
              <span className="icon">🔄</span>
              <span className="text">Reposition Aircraft</span>
              <span className="subtitle">Reset to current gate position</span>
            </button>

            <button className="reposition-btn" onClick={() => requestService('CUSTOMIZE_POSITION')}>
              <span className="icon">✏️</span>
              <span className="text">Customize Position</span>
              <span className="subtitle">Adjust aircraft placement</span>
            </button>

            <button className="reposition-btn change-gate" onClick={onChangeGate}>
              <span className="icon">🔀</span>
              <span className="text">Change Gate Selection</span>
              <span className="subtitle">Return to gate list</span>
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
