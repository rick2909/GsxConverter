import { useState, useEffect } from 'react';
import { AirportData, Gate } from './types/GateData';
import GateList from '@components/GateList';
import GateSelection from '@components/GateSelection';
import GateOperations from '@components/GateOperations';
import ehamGatesData from './data/eham-gates.json';
import './App.css';

// Use real EHAM data for development
const mockAirportData: AirportData = ehamGatesData as unknown as AirportData;


function App() {
  const [airportData, setAirportData] = useState<AirportData>(mockAirportData);
  const [currentView, setCurrentView] = useState<'gateList' | 'gateSelection' | 'gateOperations'>('gateList');
  const [selectedGate, setSelectedGate] = useState<Gate | null>(null);

  useEffect(() => {
    // Listen for messages from EFB wrapper
    const handleMessage = (event: MessageEvent) => {
      const message = event.data;
      
      console.log('Webapp received message:', message);

      switch (message.type) {
        case 'AIRPORT_DATA':
          console.log('Setting airport data:', message.data);
          setAirportData(message.data);
          break;
        case 'NAVIGATE':
          setCurrentView(message.view);
          break;
        default:
          console.log('Unknown message type:', message.type);
      }
    };

    window.addEventListener('message', handleMessage);
    
    // Request initial data from EFB (if running in iframe)
    if (window.parent !== window) {
      console.log('Requesting airport data from EFB...');
      window.parent.postMessage({ type: 'REQUEST_AIRPORT_DATA' }, '*');
    } else {
      console.log('Running standalone - using mock data');
    }

    return () => window.removeEventListener('message', handleMessage);
  }, []);

  const handleBack = () => {
    if (currentView === 'gateSelection') {
      setCurrentView('gateList');
      setSelectedGate(null);
    } else if (currentView === 'gateOperations') {
      setCurrentView('gateSelection');
    }
  };

  const handleConfirmGate = () => {
    setCurrentView('gateOperations');
  };

  const handleChangeGate = () => {
    setCurrentView('gateList');
    setSelectedGate(null);
  };

  return (
    <div className="app">
      {currentView === 'gateList' && (
        <GateList 
          airportData={airportData} 
          onNavigate={(view) => {
            if (view === 'gateSelection') {
              // GateList will set window.selectedGate
              const gate = (window as any).selectedGate;
              if (gate) {
                setSelectedGate(gate);
              }
              setCurrentView(view);
            }
          }}
        />
      )}
      {currentView === 'gateSelection' && selectedGate && (
        <GateSelection 
          gate={selectedGate}
          onBack={handleBack}
          onConfirm={handleConfirmGate}
        />
      )}
      {currentView === 'gateOperations' && selectedGate && (
        <GateOperations 
          gate={selectedGate}
          onBack={handleBack}
          onChangeGate={handleChangeGate}
        />
      )}
    </div>
  );
}

export default App;
