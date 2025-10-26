using System;
using System.Collections.Generic;
using System.Linq;
using GsxConverter.Models.Json;

namespace GsxConverter.Parsers
{
    /// <summary>
    /// Helper to merge a base GroundServiceConfig (from INI) with an override config (from Python).
    /// The Python file is expected to only contain deltas/overrides — the INI is authoritative for basic data.
    /// </summary>
    public static class ConfigMerger
    {
        public static GroundServiceConfig Merge(GroundServiceConfig baseCfg, GroundServiceConfig overrides)
        {
            if (baseCfg == null) throw new ArgumentNullException(nameof(baseCfg));
            if (overrides == null) return baseCfg;

            // Merge top-level metadata and simple lists
            foreach (var kv in overrides.Metadata)
            {
                baseCfg.Metadata[kv.Key] = kv.Value;
            }

            // Jetway heights: override or add
            foreach (var kv in overrides.JetwayRootfloorHeights)
            {
                baseCfg.JetwayRootfloorHeights[kv.Key] = kv.Value;
            }

            // Merge DeIces: append any that don't conflict by Id
            foreach (var d in overrides.DeIces)
            {
                if (!baseCfg.DeIces.Any(x => string.Equals(x.Id, d.Id, StringComparison.OrdinalIgnoreCase)))
                    baseCfg.DeIces.Add(d);
            }

            // Merge gate groups: append if id not present
            foreach (var g in overrides.GateGroups)
            {
                if (!baseCfg.GateGroups.Any(x => string.Equals(x.Id, g.Id, StringComparison.OrdinalIgnoreCase)))
                    baseCfg.GateGroups.Add(g);
            }

            // Merge gates: find matching gate by GateId and apply overrides
            foreach (var oGate in overrides.Gates)
            {
                var match = baseCfg.Gates.FirstOrDefault(g => string.Equals(g.GateId, oGate.GateId, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    // new gate introduced by Python -> add
                    baseCfg.Gates.Add(oGate);
                    continue;
                }

                MergeGate(match, oGate);
            }

            return baseCfg;
        }

        private static void MergeGate(GateDefinition target, GateDefinition source)
        {
            // Positions: if source has non-zero coordinates, overwrite
            if (source.Position != null)
            {
                if (source.Position.Latitude != 0 || source.Position.Longitude != 0)
                {
                    target.Position.Latitude = source.Position.Latitude;
                    target.Position.Longitude = source.Position.Longitude;
                }
                if (source.Position.Heading != 0)
                    target.Position.Heading = source.Position.Heading;
            }

            // 3D passenger enter pos
            if (source.PassengerEnterGatePos != null)
            {
                target.PassengerEnterGatePos = source.PassengerEnterGatePos;
            }

            // Pushback / parking system stop positions
            if (source.ParkingSystemStopPosition != null)
                target.ParkingSystemStopPosition = source.ParkingSystemStopPosition;
            if (source.ParkingSystemObjectPosition != null)
                target.ParkingSystemObjectPosition = source.ParkingSystemObjectPosition;
            if (source.PushbackConfig != null)
                target.PushbackConfig = source.PushbackConfig;
            if (source.Pushback.HasValue)
                target.Pushback = source.Pushback;
            if (source.PushbackAddPos != null && source.PushbackAddPos.Count > 0)
                target.PushbackAddPos = source.PushbackAddPos;

            // Waypoints: if present on source, replace
            if (source.WalkerWaypoints != null)
                target.WalkerWaypoints = source.WalkerWaypoints;
            if (source.PassengerWaypoints != null)
                target.PassengerWaypoints = source.PassengerWaypoints;

            // Services: merge by type, append new
            if (source.Services != null && source.Services.Count > 0)
            {
                foreach (var s in source.Services)
                {
                    if (!target.Services.Any(t => string.Equals(t.Type, s.Type, StringComparison.OrdinalIgnoreCase)))
                        target.Services.Add(s);
                }
            }

            // Aircraft stop positions: override or add per key
            if (source.AircraftStopPositions != null && source.AircraftStopPositions.Count > 0)
            {
                if (target.AircraftStopPositions == null)
                    target.AircraftStopPositions = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in source.AircraftStopPositions)
                {
                    target.AircraftStopPositions[kv.Key] = kv.Value;
                }
            }

            // Primitive fields: override if source has a value
            if (source.HasJetway.HasValue) target.HasJetway = source.HasJetway;
            if (source.MaxWingspan.HasValue) target.MaxWingspan = source.MaxWingspan;
            if (source.RadiusLeft.HasValue) target.RadiusLeft = source.RadiusLeft;
            if (source.RadiusRight.HasValue) target.RadiusRight = source.RadiusRight;
            if (source.GateDistanceThreshold.HasValue) target.GateDistanceThreshold = source.GateDistanceThreshold;

            if (!string.IsNullOrEmpty(source.ParkingSystem)) target.ParkingSystem = source.ParkingSystem;
            if (!string.IsNullOrEmpty(source.LoaderType)) target.LoaderType = source.LoaderType;
            if (!string.IsNullOrEmpty(source.AirlineCodes)) target.AirlineCodes = source.AirlineCodes;
            if (source.HandlingTexture != null && source.HandlingTexture.Count > 0) target.HandlingTexture = source.HandlingTexture;
            if (source.CateringTexture != null && source.CateringTexture.Count > 0) target.CateringTexture = source.CateringTexture;

            // Merge properties (source overrides)
            foreach (var kv in source.Properties)
            {
                target.Properties[kv.Key] = kv.Value;
            }
        }
    }
}
