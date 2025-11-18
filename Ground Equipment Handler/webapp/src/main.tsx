import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import './fontawesome'; // Initialize Font Awesome icon library
import App from './App';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
);
