import { useState, useEffect } from 'react';
import { AirportData, Gate } from './types/GateData';
import GateList from '@components/GateList';
import GateSelection from '@components/GateSelection';
import GateOperations from '@components/GateOperations';
import DeicingView from '@components/DeicingView';
import MetadataView from '@components/MetadataView';
import ehamGatesData from './data/eham-gates.json';
import './App.css';

// Use real EHAM data for development
const mockAirportData: AirportData = ehamGatesData as unknown as AirportData;


function App() {
  const [airportData, setAirportData] = useState<AirportData>(mockAirportData);
  const [currentView, setCurrentView] = useState<'gateList' | 'gateSelection' | 'gateOperations' | 'deicing' | 'metadata'>('gateList');
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

  const handleNavigate = (view: 'gateList' | 'gateSelection' | 'gateOperations' | 'deicing' | 'metadata') => {
    // If navigating to gate list, deicing, or metadata, clear selected gate
    if (view === 'gateList' || view === 'deicing' || view === 'metadata') {
      setSelectedGate(null);
    }
    
    // Special handling for gateSelection from GateList
    if (view === 'gateSelection') {
      const gate = (window as any).selectedGate;
      if (gate) {
        setSelectedGate(gate);
      }
    }
    
    setCurrentView(view);
  };

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
          onNavigate={handleNavigate}
        />
      )}
      {currentView === 'deicing' && (
        <DeicingView
          airportData={airportData}
          onNavigate={handleNavigate}
        />
      )}
      {currentView === 'metadata' && (
        <MetadataView
          airportData={airportData}
          onNavigate={handleNavigate}
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
