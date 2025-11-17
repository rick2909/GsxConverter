import { useState, useMemo } from 'react';
import { AirportData, Gate, GateGroup } from '../types/GateData';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { Icons } from '../fontawesome';
import './GateList.css';

interface GateListProps {
  airportData: AirportData;
  onNavigate: (view: 'gateList' | 'gateSelection' | 'gateOperations' | 'deicing' | 'metadata') => void;
}

export default function GateList({ airportData, onNavigate }: GateListProps) {
  const [filterText, setFilterText] = useState('');
  const [selectedGroupId, setSelectedGroupId] = useState<string | null>(null);

  const getGateType = (gate: Gate): 'jetway' | 'bus' | 'walk' => {
    if (gate.has_jetway) return 'jetway';
    if (!gate.no_passenger_bus) return 'bus';
    return 'walk';
  };

  const filteredGates = useMemo(() => {
    let gates = airportData.gates;

    // Filter by selected group
    if (selectedGroupId && airportData.gate_groups) {
      const selectedGroup = airportData.gate_groups.find(g => g.id === selectedGroupId);
      if (selectedGroup) {
        gates = gates.filter(gate => selectedGroup.members.includes(gate.gate_id));
      }
    }

    // Apply search filter
    if (filterText) {
      const searchTerm = filterText.toLowerCase();
      gates = gates.filter(gate =>
        gate.gate_id.toLowerCase().includes(searchTerm) ||
        gate.ui_name.toLowerCase().includes(searchTerm) ||
        gate.airline_codes.toLowerCase().includes(searchTerm)
      );
    }

    return gates;
  }, [airportData, filterText, selectedGroupId]);

  const hasGroups = !!(airportData.gate_groups && airportData.gate_groups.length > 0);
  const showGroupedView = hasGroups && !selectedGroupId && !filterText;

  const getGatesByGroup = (groupId: string): Gate[] => {
    const group = airportData.gate_groups?.find(g => g.id === groupId);
    if (!group) return [];
    return airportData.gates.filter(gate => group.members.includes(gate.gate_id));
  };

  const handleGateClick = (gate: Gate) => {
    console.log('Gate clicked:', gate.gate_id);
    // Store selected gate for next view
    (window as any).selectedGate = gate;
    onNavigate('gateSelection');
  };

  const handleBackToGroups = () => {
    setSelectedGroupId(null);
    setFilterText('');
  };

  return (
    <div className="gate-list">
      <div className="header">
        <div className="header-left">
          <h1>{airportData.airport}</h1>
          <div className="header-info">
            <span className="gate-count">{filteredGates.length} gates</span>
            <span className="version">Version: {airportData.version}</span>
          </div>
        </div>
        <div className="header-right">
          <button className="nav-button active" onClick={() => onNavigate('gateList')}>
            Gate List
          </button>
          <button className="nav-button" onClick={() => onNavigate('deicing')}>
            De-Icing
          </button>
          <button className="nav-button" onClick={() => onNavigate('metadata')}>
            Metadata
          </button>
        </div>
      </div>

      {/* Controls row with back button and search */}
      <div className="controls-row">
        {/* Back to Groups button */}
        {hasGroups && selectedGroupId ? (
          <button className="back-button" onClick={handleBackToGroups}>
            <FontAwesomeIcon icon={Icons.BACK} /> Back to Groups
          </button>
        ) : (
          <div className="spacer"></div>
        )}

        {/* Search bar */}
        <div className="search-bar">
          <input
            type="text"
            placeholder="Search by gate ID, name, or airline..."
            value={filterText}
            onChange={(e) => setFilterText(e.target.value)}
          />
        </div>
      </div>

      {/* GROUPED VIEW */}
      {showGroupedView && airportData.gate_groups && (
        <div className="gates-container">
          <div className="groups-grid">
            {airportData.gate_groups.map((group: GateGroup) => {
              const groupGates = getGatesByGroup(group.id);
              const previewGates = groupGates.slice(0, 10);

              return (
                <div key={group.id} className="group-card">
                  <div className="group-card-header">
                    <div className="group-title-section">
                      <h3>{group.id}</h3>
                      <span className="member-count">{group.members.length} gates</span>
                    </div>
                    <button
                      className="view-all-button"
                      onClick={() => {
                        console.log('View All clicked for group:', group.id);
                        setSelectedGroupId(group.id);
                      }}
                    >
                      View All →
                    </button>
                  </div>

                  <div className="group-gates-preview">
                    {previewGates.map((gate: Gate) => {
                      const gateType = getGateType(gate);
                      return (
                        <div
                          key={gate.gate_id}
                          className="mini-gate-card"
                          onClick={() => handleGateClick(gate)}
                        >
                          <div className="mini-gate-id">{gate.gate_id.toUpperCase()}</div>
                          <div className="mini-gate-badges">
                            <span className={`badge badge-${gateType}`}>
                              <FontAwesomeIcon icon={gateType === 'jetway' ? Icons.JET : gateType === 'bus' ? Icons.BUS : Icons.WALK} />{' '}
                              {gateType === 'jetway' ? 'Jetway' : gateType === 'bus' ? 'Bus' : 'Walk'}
                            </span>
                            <span className="badge badge-wingspan">{gate.max_wingspan}m</span>
                          </div>
                        </div>
                      );
                    })}
                    {groupGates.length > 10 && (
                      <div className="more-gates-indicator">
                        +{groupGates.length - 10} more
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      )}

      {/* GATE VIEW */}
      {!showGroupedView && (
        <div className="gates-container">
          <div className="gates-grid-compact">
            {filteredGates.map((gate: Gate) => {
              const gateType = getGateType(gate);
              return (
                <div
                  key={gate.gate_id}
                  className="compact-gate-card"
                  onClick={() => handleGateClick(gate)}
                >
                  <div className="compact-gate-header">
                    <h4>{gate.gate_id.toUpperCase()}</h4>
                  </div>

                  <div className="compact-gate-badges">
                    <span className={`badge badge-${gateType}`}>
                      <FontAwesomeIcon icon={gateType === 'jetway' ? Icons.JET : gateType === 'bus' ? Icons.BUS : Icons.WALK} />{' '}
                      {gateType === 'jetway' ? 'Jetway' : gateType === 'bus' ? 'Bus' : 'Walk'}
                    </span>
                    <span className="badge badge-wingspan">{gate.max_wingspan}m</span>
                  </div>

                  {gate.airline_codes && (
                    <div className="compact-gate-airlines">{gate.airline_codes}</div>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      )}
    </div>
  );
}
