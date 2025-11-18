import { AirportData } from '../types/GateData';
import './MetadataView.css';

interface MetadataViewProps {
  airportData: AirportData;
  onNavigate: (view: 'gateList' | 'gateSelection' | 'gateOperations' | 'deicing' | 'metadata') => void;
}

export default function MetadataView({ airportData, onNavigate }: MetadataViewProps) {
  const metadata = airportData.metadata || {};
  const jetwayHeights = airportData.jetway_rootfloor_heights || {};

  const metadataEntries = Object.entries(metadata);
  const jetwayEntries = Object.entries(jetwayHeights);

  return (
    <div className="metadata-view">
      <div className="header">
        <div className="header-left">
          <h1>Airport Metadata</h1>
          <div className="header-info">
            <span className="airport-code">{airportData.airport}</span>
            <span className="version">Version {airportData.version}</span>
          </div>
        </div>
        <div className="header-right">
          <button className="nav-button" onClick={() => onNavigate('gateList')}>
            Gate List
          </button>
          <button className="nav-button" onClick={() => onNavigate('deicing')}>
            De-Icing
          </button>
          <button className="nav-button active" onClick={() => onNavigate('metadata')}>
            Metadata
          </button>
        </div>
      </div>

      <div className="metadata-container">
        {/* General Metadata Section */}
        <div className="section">
          <h2>General Information</h2>
          {metadataEntries.length === 0 ? (
            <div className="no-data">
              <p>No metadata available for this airport.</p>
            </div>
          ) : (
            <div className="metadata-grid">
              {metadataEntries.map(([key, value]) => (
                <div key={key} className="metadata-item">
                  <div className="metadata-key">{key}</div>
                  <div className="metadata-value">{value || 'N/A'}</div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Statistics Section */}
        <div className="section">
          <h2>Airport Statistics</h2>
          <div className="stats-grid">
            <div className="stat-item">
              <div className="stat-label">Total Gates</div>
              <div className="stat-value">{airportData.gates.length}</div>
            </div>
            <div className="stat-item">
              <div className="stat-label">De-icing Areas</div>
              <div className="stat-value">{airportData.deices?.length || 0}</div>
            </div>
            <div className="stat-item">
              <div className="stat-label">Gate Groups</div>
              <div className="stat-value">{airportData.gate_groups?.length || 0}</div>
            </div>
            <div className="stat-item">
              <div className="stat-label">Jetway Gates</div>
              <div className="stat-value">
                {airportData.gates.filter((g) => g.has_jetway).length}
              </div>
            </div>
            <div className="stat-item">
              <div className="stat-label">Bus Gates</div>
              <div className="stat-value">
                {airportData.gates.filter((g) => !g.has_jetway && !g.no_passenger_bus).length}
              </div>
            </div>
            <div className="stat-item">
              <div className="stat-label">Walk Gates</div>
              <div className="stat-value">
                {airportData.gates.filter((g) => !g.has_jetway && g.no_passenger_bus).length}
              </div>
            </div>
          </div>
        </div>

        {/* Gate Groups Section */}
        {airportData.gate_groups && airportData.gate_groups.length > 0 && (
          <div className="section">
            <h2>Gate Groups</h2>
            <div className="groups-list">
              {airportData.gate_groups.map((group) => (
                <div key={group.id} className="group-item">
                  <div className="group-name">{group.id}</div>
                  <div className="group-members">
                    {group.members.length} gates: {group.members.slice(0, 5).join(', ')}
                    {group.members.length > 5 && `, +${group.members.length - 5} more`}
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* Jetway Heights Section */}
        {jetwayEntries.length > 0 && (
          <div className="section">
            <h2>Jetway Root Floor Heights</h2>
            <div className="jetway-grid">
              {jetwayEntries.map(([jetwayId, height]) => (
                <div key={jetwayId} className="jetway-item">
                  <div className="jetway-id">{jetwayId}</div>
                  <div className="jetway-height">{height.toFixed(2)}m</div>
                </div>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
