// Font Awesome icon library configuration
// This file is kept separate to allow code-splitting and avoid bloating the main bundle
import { library } from '@fortawesome/fontawesome-svg-core';

// Import only the icons we use
import {
  faArrowLeft,
  faPlane,
  faMapMarkerAlt,
  faCar,
  faUsers,
  faUtensils,
  faGasPump,
  faSuitcase,
  faRocket,
  faBridge,
  faStairs,
  faSync,
  faPencilAlt,
  faRandom,
  faWrench,
  faMapPin,
  faBell,
  faSearch,
  faJetFighterUp,
  faBus,
  faWalking
} from '@fortawesome/free-solid-svg-icons';

// Add icons to library for use with FontAwesomeIcon component
library.add(
  faArrowLeft,
  faPlane,
  faMapMarkerAlt,
  faCar,
  faUsers,
  faUtensils,
  faGasPump,
  faSuitcase,
  faRocket,
  faBridge,
  faStairs,
  faSync,
  faPencilAlt,
  faRandom,
  faWrench,
  faMapPin,
  faBell,
  faSearch,
  faJetFighterUp,
  faBus,
  faWalking
);

// Export icon names for easy reference
export const Icons = {
  BACK: 'arrow-left' as const,
  PLANE: 'plane' as const,
  LOCATION: 'map-marker-alt' as const,
  CAR: 'car' as const,
  DEBOARDING: 'users' as const,
  CATERING: 'utensils' as const,
  REFUELING: 'gas-pump' as const,
  BOARDING: 'suitcase' as const,
  PUSHBACK: 'rocket' as const,
  JETWAY: 'bridge' as const,
  STAIRS: 'stairs' as const,
  REPOSITION: 'sync' as const,
  CUSTOMIZE: 'pencil-alt' as const,
  CHANGE_GATE: 'random' as const,
  SERVICES: 'wrench' as const,
  POSITION: 'map-pin' as const,
  BELL: 'bell' as const,
  SEARCH: 'search' as const,
  JET: 'jet-fighter-up' as const,
  BUS: 'bus' as const,
  WALK: 'walking' as const
};
