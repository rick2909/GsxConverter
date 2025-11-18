import { AirportData, DeIceArea } from '../types/GateData';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { Icons } from '../fontawesome';
import './DeicingView.css';

interface DeicingViewProps {
  airportData: AirportData;
  onNavigate: (view: 'gateList' | 'gateSelection' | 'gateOperations' | 'deicing' | 'metadata') => void;
}

export default function DeicingView({ airportData, onNavigate }: DeicingViewProps) {
  const deiceAreas = airportData.deices || [];

  const handleDeiceClick = (deice: DeIceArea) => {
    console.log('Deice area clicked:', deice.id);
    // Send message to EFB for navigation/warp
    window.parent.postMessage({
      type: 'GROUND_SERVICE',
      action: 'SHOW_DEICE_SPOT',
      deiceId: deice.id
    }, '*');
  };

  const handleWarpToDeice = (deice: DeIceArea, event: React.MouseEvent) => {
    event.stopPropagation();
    console.log('Warp to deice area:', deice.id);
    window.parent.postMessage({
      type: 'GROUND_SERVICE',
      action: 'WARP_TO_DEICE',
      deiceId: deice.id
    }, '*');
  };

  return (
    <div className="deicing-view">
      <div className="header">
        <div className="header-left">
          <h1>De-Icing Areas</h1>
          <div className="header-info">
            <span className="deice-count">{deiceAreas.length} areas</span>
          </div>
        </div>
        <div className="header-right">
          <button className="nav-button" onClick={() => onNavigate('gateList')}>
            Gate List
          </button>
          <button className="nav-button active" onClick={() => onNavigate('deicing')}>
            De-Icing
          </button>
          <button className="nav-button" onClick={() => onNavigate('metadata')}>
            Metadata
          </button>
        </div>
      </div>

      <div className="deicing-container">
        {deiceAreas.length === 0 ? (
          <div className="no-deicing">
            <p>No de-icing areas configured for this airport.</p>
          </div>
        ) : (
          <div className="deicing-grid">
            {deiceAreas.map((deice: DeIceArea) => (
              <div
                key={deice.id}
                className="deice-card"
                onClick={() => handleDeiceClick(deice)}
              >
                <div className="deice-header">
                  <h3>{deice.ui_name || deice.id}</h3>
                  {deice.user_customized && (
                    <span className="badge badge-customized">Customized</span>
                  )}
                </div>

                <div className="deice-info">
                  <div className="info-item">
                    <span className="label">ID:</span>
                    <span className="value">{deice.id}</span>
                  </div>
                  <div className="info-item">
                    <span className="label">Radius:</span>
                    <span className="value">{deice.radius}m</span>
                  </div>
                  <div className="info-item">
                    <span className="label">Parking:</span>
                    <span className="value">{deice.parking_system || 'None'}</span>
                  </div>
                  <div className="info-item">
                    <span className="label">Position:</span>
                    <span className="value coords">
                      {deice.position.lat.toFixed(6)}, {deice.position.lon.toFixed(6)}
                    </span>
                  </div>
                </div>

                <div className="deice-actions">
                  <button
                    className="action-btn show-spot"
                    onClick={(e) => {
                      e.stopPropagation();
                      handleDeiceClick(deice);
                    }}
                  >
                    <FontAwesomeIcon icon={Icons.LOCATION} /> Show Location
                  </button>
                  <button
                    className="action-btn warp"
                    onClick={(e) => handleWarpToDeice(deice, e)}
                  >
                    <FontAwesomeIcon icon={Icons.PLANE} /> Warp
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
