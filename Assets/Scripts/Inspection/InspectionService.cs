using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using ToolScripts.Base;
using UnityEngine;

namespace Inspection
{
    /// <summary>
    /// Interface for the Inspection Service.
    /// Provides access to inspection tracking functionality.
    /// </summary>
    public interface IInspectionService : ISystem
    {
        /// <summary>Currently inspected vehicle.</summary>
        Vehicle CurrentVehicle { get; }

        /// <summary>Whether an inspection session is currently active.</summary>
        bool IsInspectionActive { get; }

        /// <summary>All inspection results from the current session.</summary>
        IReadOnlyList<InspectionResult> InspectionResults { get; }

        /// <summary>Event fired when an inspection session starts.</summary>
        event Action<Vehicle> OnInspectionStarted;

        /// <summary>Event fired when an inspection session ends.</summary>
        event Action<Vehicle> OnInspectionEnded;

        /// <summary>Event fired when a new inspection result is recorded.</summary>
        event Action<InspectionResult> OnInspectionResultRecorded;

        /// <summary>Starts a new inspection session for the specified vehicle.</summary>
        void StartInspection(Vehicle vehicle);

        /// <summary>Ends the current inspection session.</summary>
        void EndInspection();

        /// <summary>Records an inspection result from a tool.</summary>
        void RecordInspectionResult(ToolInspectionResult toolResult, Tool usedTool);

        /// <summary>Records a direct inspection result.</summary>
        void RecordInspectionResult(InspectionResult result);

        /// <summary>Gets all inspection results for a specific part.</summary>
        List<InspectionResult> GetResultsForPart(VehiclePart part);

        /// <summary>Checks if a part has been inspected with a specific tool.</summary>
        bool HasInspectedWithTool(VehiclePart part, Tool tool);

        /// <summary>Gets the inspection progress (0-1).</summary>
        float GetInspectionProgress();

        /// <summary>Gets the number of parts that have been inspected at least once.</summary>
        int GetInspectedPartCount();

        /// <summary>Gets the total number of inspectable parts on the current vehicle.</summary>
        int GetTotalPartCount();

        /// <summary>Gets all parts that have been inspected.</summary>
        List<VehiclePart> GetInspectedParts();

        /// <summary>Gets all parts that have not been inspected yet.</summary>
        List<VehiclePart> GetUninspectedParts();

        /// <summary>Clears all inspection results without ending the session.</summary>
        void ClearResults();
    }

    /// <summary>
    /// Central service for coordinating vehicle inspections.
    /// Tracks inspection sessions, results, and progress.
    /// </summary>
    public class InspectionService : IInspectionService
    {
        private Vehicle _currentVehicle;
        private bool _isInspectionActive;
        private readonly List<InspectionResult> _inspectionResults = new List<InspectionResult>();
        private readonly HashSet<VehiclePart> _inspectedParts = new HashSet<VehiclePart>();
        private readonly Dictionary<VehiclePart, HashSet<Tool>> _partToolUsage = new Dictionary<VehiclePart, HashSet<Tool>>();

        /// <inheritdoc/>
        public int Priority => 100;

        /// <inheritdoc/>
        public Vehicle CurrentVehicle => _currentVehicle;

        /// <inheritdoc/>
        public bool IsInspectionActive => _isInspectionActive;

        /// <inheritdoc/>
        public IReadOnlyList<InspectionResult> InspectionResults => _inspectionResults.AsReadOnly();

        /// <inheritdoc/>
        public event Action<Vehicle> OnInspectionStarted;

        /// <inheritdoc/>
        public event Action<Vehicle> OnInspectionEnded;

        /// <inheritdoc/>
        public event Action<InspectionResult> OnInspectionResultRecorded;

        /// <inheritdoc/>
        public void OnRegistered()
        {
            GameLogger.Log("[InspectionService] Registered with ServiceLocator");
        }

        /// <inheritdoc/>
        public void Initialize()
        {
            GameLogger.Log("[InspectionService] Initialized");
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            if (_isInspectionActive)
            {
                EndInspection();
            }
            GameLogger.Log("[InspectionService] Shutdown complete");
        }

        /// <inheritdoc/>
        public void StartInspection(Vehicle vehicle)
        {
            if (vehicle == null)
            {
                GameLogger.LogWarning("[InspectionService] Cannot start inspection: vehicle is null");
                return;
            }

            if (_isInspectionActive)
            {
                GameLogger.LogWarning("[InspectionService] Ending previous inspection session before starting new one");
                EndInspection();
            }

            _currentVehicle = vehicle;
            _isInspectionActive = true;
            _inspectionResults.Clear();
            _inspectedParts.Clear();
            _partToolUsage.Clear();

            GameLogger.Log($"[InspectionService] Started inspection session for vehicle: {vehicle.name}");
            OnInspectionStarted?.Invoke(vehicle);
        }

        /// <inheritdoc/>
        public void EndInspection()
        {
            if (!_isInspectionActive)
            {
                GameLogger.LogWarning("[InspectionService] No active inspection session to end");
                return;
            }

            var previousVehicle = _currentVehicle;
            GameLogger.Log($"[InspectionService] Ended inspection session for vehicle: {previousVehicle?.name}");
            GameLogger.Log($"[InspectionService] Total results recorded: {_inspectionResults.Count}");

            _isInspectionActive = false;
            _currentVehicle = null;

            OnInspectionEnded?.Invoke(previousVehicle);
        }

        /// <inheritdoc/>
        public void RecordInspectionResult(ToolInspectionResult toolResult, Tool usedTool)
        {
            if (!_isInspectionActive)
            {
                GameLogger.LogWarning("[InspectionService] Cannot record result: no active inspection session");
                return;
            }

            var result = InspectionResult.FromToolResult(toolResult, usedTool);
            RecordInspectionResult(result);
        }

        /// <inheritdoc/>
        public void RecordInspectionResult(InspectionResult result)
        {
            if (!_isInspectionActive)
            {
                GameLogger.LogWarning("[InspectionService] Cannot record result: no active inspection session");
                return;
            }

            if (result == null)
            {
                GameLogger.LogWarning("[InspectionService] Cannot record null result");
                return;
            }

            _inspectionResults.Add(result);

            // Track inspected parts
            if (result.TargetPart != null)
            {
                _inspectedParts.Add(result.TargetPart);

                // Track tool usage per part
                if (!_partToolUsage.ContainsKey(result.TargetPart))
                {
                    _partToolUsage[result.TargetPart] = new HashSet<Tool>();
                }
                _partToolUsage[result.TargetPart].Add(result.UsedTool);
            }

            GameLogger.Log($"[InspectionService] Recorded inspection result: {result}");
            OnInspectionResultRecorded?.Invoke(result);
        }

        /// <inheritdoc/>
        public List<InspectionResult> GetResultsForPart(VehiclePart part)
        {
            if (part == null) return new List<InspectionResult>();

            return _inspectionResults
                .Where(r => r.TargetPart == part)
                .ToList();
        }

        /// <inheritdoc/>
        public bool HasInspectedWithTool(VehiclePart part, Tool tool)
        {
            if (part == null) return false;

            if (_partToolUsage.TryGetValue(part, out var toolsUsed))
            {
                return toolsUsed.Contains(tool);
            }

            return false;
        }

        /// <inheritdoc/>
        public float GetInspectionProgress()
        {
            int totalParts = GetTotalPartCount();
            if (totalParts == 0) return 0f;

            int inspectedCount = GetInspectedPartCount();
            return (float)inspectedCount / totalParts;
        }

        /// <inheritdoc/>
        public int GetInspectedPartCount()
        {
            return _inspectedParts.Count;
        }

        /// <inheritdoc/>
        public int GetTotalPartCount()
        {
            if (_currentVehicle == null) return 0;

            int count = 0;

            // Count exterior parts
            if (_currentVehicle.exteriorParts != null)
                count += _currentVehicle.exteriorParts.Count;

            // Count wheels
            if (_currentVehicle.wheels != null)
                count += _currentVehicle.wheels.Count;

            // Count glasses
            if (_currentVehicle.glasses != null)
                count += _currentVehicle.glasses.Count;

            // Count lights
            if (_currentVehicle.lights != null)
                count += _currentVehicle.lights.Count;

            // Count individual engine bay parts
            if (_currentVehicle.battery != null) count++;
            if (_currentVehicle.engine != null) count++;
            if (_currentVehicle.radiator != null) count++;
            if (_currentVehicle.exhaust != null) count++;

            return count;
        }

        /// <inheritdoc/>
        public List<VehiclePart> GetInspectedParts()
        {
            return _inspectedParts.ToList();
        }

        /// <inheritdoc/>
        public List<VehiclePart> GetUninspectedParts()
        {
            var allParts = GetAllVehicleParts();
            return allParts.Where(p => !_inspectedParts.Contains(p)).ToList();
        }

        /// <inheritdoc/>
        public void ClearResults()
        {
            _inspectionResults.Clear();
            _inspectedParts.Clear();
            _partToolUsage.Clear();
            GameLogger.Log("[InspectionService] Cleared all inspection results");
        }

        /// <summary>
        /// Gets all vehicle parts from the current vehicle.
        /// </summary>
        private List<VehiclePart> GetAllVehicleParts()
        {
            var allParts = new List<VehiclePart>();

            if (_currentVehicle == null) return allParts;

            if (_currentVehicle.exteriorParts != null)
                allParts.AddRange(_currentVehicle.exteriorParts);

            if (_currentVehicle.wheels != null)
                allParts.AddRange(_currentVehicle.wheels);

            if (_currentVehicle.glasses != null)
                allParts.AddRange(_currentVehicle.glasses);

            if (_currentVehicle.lights != null)
                allParts.AddRange(_currentVehicle.lights);

            if (_currentVehicle.battery != null)
                allParts.Add(_currentVehicle.battery);

            if (_currentVehicle.engine != null)
                allParts.Add(_currentVehicle.engine);

            if (_currentVehicle.radiator != null)
                allParts.Add(_currentVehicle.radiator);

            if (_currentVehicle.exhaust != null)
                allParts.Add(_currentVehicle.exhaust);

            return allParts;
        }
    }
}
