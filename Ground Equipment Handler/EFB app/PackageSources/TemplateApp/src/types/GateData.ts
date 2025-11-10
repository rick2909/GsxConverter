/**
 * Type definitions for GSX Gate configuration data
 */

export interface Position {
  lat: number;
  lon: number;
  heading: number;
  height?: number;
}

export interface Waypoint {
  lat: number;
  lon: number;
  height: number;
}

export interface WaypointCollection {
  waypoints: Waypoint[] | null;
  thickness: number;
}

export interface BaggagePositions {
  baggage_loader_front_pos: Position | null;
  baggage_loader_rear_pos: Position | null;
  baggage_loader_main_pos: Position | null;
  baggage_train_front_pos: Position | null;
  baggage_train_rear_pos: Position | null;
  baggage_train_main_pos: Position | null;
}

export interface StairsPositions {
  stairs_front_pos: Position | null;
  stairs_middle_pos: Position | null;
  stairs_rear_pos: Position | null;
}

export interface PushbackConfig {
  pushback_type: number;
  pushback_labels: string[];
  snap_left_pushback_pos: boolean;
  snap_right_pushback_pos: boolean;
  pushback_left_pos: Position | null;
  pushback_right_pos: Position | null;
  pushback_left_approach_pos: Position | null;
  pushback_right_approach_pos: Position | null;
  pushback_left_approach_pos2: Position | null;
  pushback_right_approach_pos2: Position | null;
  pushback_pos: Position;
  wingwalkers_left_pushback: any | null;
  wingwalkers_right_pushback: any | null;
  wingwalkers_quick_pushback: any | null;
  start_engines_left_pushback: any | null;
  start_engines_right_pushback: any | null;
  start_engines_quick_pushback: any | null;
}

export interface Service {
  type: string;
  offset: any | null;
  spawn_coords: any | null;
  properties: Record<string, any>;
}

export interface Gate {
  gate_id: string;
  position: Position;
  services: Service[];
  tags: string[];
  aircraft_stop_positions: Record<string, number>;
  gate_type: number;
  max_wingspan: number;
  radius_left: number;
  radius_right: number;
  gate_distance_threshold: number;
  has_jetway: boolean;
  parking_system: string;
  underground_refueling: boolean;
  no_passenger_stairs: boolean;
  no_passenger_bus: boolean;
  no_passenger_bus_deboarding: boolean;
  ignore_icao_prefixes: boolean;
  ignore_preferred_exit: boolean;
  dont_create_jetways: boolean;
  disable_pax_barriers: boolean;
  disable_pax_barriers_deboarding: boolean;
  user_customized: boolean;
  loader_type: string;
  airline_codes: string;
  handling_texture: string[];
  catering_texture: string[];
  walker_type: string;
  walker_path_thickness: number;
  walker_loop_start: number;
  passenger_path_thickness: number;
  passenger_path_thickness_deboarding: number;
  pushback_config: PushbackConfig;
  parking_system_stop_position: Position;
  parking_system_object_position: Position;
  baggage_positions: BaggagePositions;
  stairs_positions: StairsPositions;
  walker_waypoints: WaypointCollection | null;
  passenger_waypoints: WaypointCollection | null;
  passenger_enter_gate_pos: Position;
  pax_barriers_texture: string | null;
  pushback: number;
  pushback_add_pos: any[];
  ui_name: string;
  properties: Record<string, any>;
}

export interface AirportData {
  airport: string;
  version: string;
  gates: Gate[];
}
