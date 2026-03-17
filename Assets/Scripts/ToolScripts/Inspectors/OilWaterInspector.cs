using PlayerScripts;
using UnityEngine;
using UnityEngine.InputSystem;
using ToolScripts.Base;
using ToolScripts.UI;

namespace ToolScripts.Inspectors
{
    /// <summary>
    /// Oil/Water/Fluid inspector tool handler.
    /// Visual inspection for engine oil, coolant, and fluid levels.
    ///
    /// Features:
    /// - Oil level check (dipstick visualization)
    /// - Oil leaks detection
    /// - Coolant level check
    /// - Radiator leaks
    /// - Instant visual check (no measurement duration)
    /// </summary>
    public class OilWaterInspector : ToolHandlerBase
    {
        [Header("Inspection Settings")]
        [SerializeField] private float minAcceptableOilLevel = 0.5f; // 50%
        [SerializeField] private float minAcceptableCoolantLevel = 0.3f; // 30%

        private VehicleEngine _targetEngine;
        private VehicleRadiator _targetRadiator;
        private VehiclePart _currentTarget;

        protected override void Awake()
        {
            base.Awake();
            toolType = Tool.Handle; // Uses hand/tool for inspection
            toolName = "Fluid Inspector";
            inspectionDuration = 0.5f; // Quick visual check
            compatiblePartInterfaces = new string[] { "IVehicleEngine", "IVehicleRadiotor" };
        }

        protected override VehiclePart GetTargetPart()
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                return player.GetTargetPart();
            }
            return base.GetTargetPart();
        }

        protected override bool ValidateTarget()
        {
            if (currentTargetPart == null)
            {
                return false;
            }

            _currentTarget = currentTargetPart;
            _targetEngine = currentTargetPart as VehicleEngine;
            _targetRadiator = currentTargetPart as VehicleRadiator;

            if (_targetEngine == null)
            {
                _targetEngine = currentTargetPart.GetComponent<VehicleEngine>();
            }

            if (_targetRadiator == null)
            {
                _targetRadiator = currentTargetPart.GetComponent<VehicleRadiator>();
            }

            return _targetEngine != null || _targetRadiator != null;
        }

        protected override ToolInspectionResult PerformInspection()
        {
            if (_currentTarget == null)
            {
                return ToolInspectionResult.CreateFailure("Invalid target for fluid inspection.");
            }

            var result = ToolInspectionResult.CreateSuccess(_currentTarget, "Fluid inspection complete.");

            // Inspect engine
            if (_targetEngine != null)
            {
                InspectEngine(result);
            }

            // Inspect radiator
            if (_targetRadiator != null)
            {
                InspectRadiator(result);
            }

            return result;
        }

        private void InspectEngine(ToolInspectionResult result)
        {
            float oilPercent = _targetEngine.GetOilLevelPercentage();
            result.AddMeasurement("Part Type", "Engine");
            result.AddMeasurement("Oil Level", $"{oilPercent * 100:F0}%");
            result.AddMeasurement("Oil Amount", $"{_targetEngine.OilLevel}/{_targetEngine.OilCapacity}");

            // Check oil level
            if (oilPercent < minAcceptableOilLevel)
            {
                result.AddMeasurement("Oil Status", "LOW - Add oil");
                result.AddDetectedIssue("Low_Oil_Level");
            }
            else
            {
                result.AddMeasurement("Oil Status", "Good");
            }

            // Check for oil leaks
            if (_targetEngine.HasOilLeak)
            {
                result.AddMeasurement("Leak Check", "OIL LEAK DETECTED");
                result.AddMeasurement("Leak Location", "Engine area");
            }
            else
            {
                result.AddMeasurement("Leak Check", "No leaks detected");
            }

            // Check other engine conditions
            if (_targetEngine.HasCracks)
            {
                result.AddMeasurement("Block Condition", "CRACKS DETECTED");
            }

            if (_targetEngine.BeltsAged)
            {
                result.AddMeasurement("Belts/Hoses", "Aged - Check condition");
            }

            if (_targetEngine.CoolantReservoirLow)
            {
                result.AddMeasurement("Coolant Reservoir", "LOW");
            }

            // Build message
            string message = $"ENGINE FLUID INSPECTION:\n";
            message += $"Oil Level: {oilPercent * 100:F0}%\n";
            message += $"{(_targetEngine.HasOilLeak ? "WARNING: Oil leak detected!\n" : "")}";
            message += $"{(_targetEngine.HasCracks ? "WARNING: Engine block cracks detected!\n" : "")}";
            message += $"{(_targetEngine.BeltsAged ? "NOTICE: Aged belts/hoses detected.\n" : "")}";
            message += $"{(_targetEngine.CoolantReservoirLow ? "NOTICE: Coolant reservoir low.\n" : "")}";

            if (oilPercent >= minAcceptableOilLevel && !_targetEngine.HasOilLeak && !_targetEngine.HasCracks)
            {
                message += "\nEngine fluids are in good condition.";
            }

            result.DisplayMessage = message;

            GameLogger.Log($"[OilWaterInspector] Engine: Oil {oilPercent * 100:F0}%, Leak: {_targetEngine.HasOilLeak}");
        }

        private void InspectRadiator(ToolInspectionResult result)
        {
            float coolantPercent = _targetRadiator.CoolantLevel;
            float coolantVolume = _targetRadiator.GetCoolantVolume();

            result.AddMeasurement("Part Type", "Radiator");
            result.AddMeasurement("Coolant Level", $"{coolantPercent * 100:F0}%");
            result.AddMeasurement("Coolant Volume", $"{coolantVolume:F1}L");

            // Check coolant level
            if (coolantPercent < minAcceptableCoolantLevel)
            {
                result.AddMeasurement("Coolant Status", "LOW - Add coolant");
                result.AddDetectedIssue("Coolant_Low");
            }
            else
            {
                result.AddMeasurement("Coolant Status", "Good");
            }

            // Check for leaks
            if (_targetRadiator.HasLeak)
            {
                result.AddMeasurement("Leak Check", "COOLANT LEAK DETECTED");
            }
            else
            {
                result.AddMeasurement("Leak Check", "No leaks detected");
            }

            // Check for damage
            if (_targetRadiator.IsDamaged)
            {
                result.AddMeasurement("Condition", "DAMAGED - Service required");
            }
            else
            {
                result.AddMeasurement("Condition", "Good");
            }

            // Build message
            string message = $"RADIATOR INSPECTION:\n";
            message += $"Coolant Level: {coolantPercent * 100:F0}% ({coolantVolume:F1}L)\n";
            message += $"{(_targetRadiator.HasLeak ? "WARNING: Coolant leak detected!\n" : "")}";
            message += $"{(_targetRadiator.IsDamaged ? "WARNING: Radiator damaged!\n" : "")}";

            if (coolantPercent >= minAcceptableCoolantLevel && !_targetRadiator.HasLeak && !_targetRadiator.IsDamaged)
            {
                message += "\nRadiator is in good condition.";
            }

            result.DisplayMessage = message;

            GameLogger.Log($"[OilWaterInspector] Radiator: Coolant {coolantPercent * 100:F0}%, Leak: {_targetRadiator.HasLeak}");
        }

        protected override void OnInspectionStarted()
        {
            if (_targetEngine != null)
            {
                ToolUIManager.Instance?.ShowInstruction("Inspecting engine fluids...");
            }
            else if (_targetRadiator != null)
            {
                ToolUIManager.Instance?.ShowInstruction("Inspecting radiator...");
            }
        }

        protected override void OnInspectionComplete(ToolInspectionResult result)
        {
            ToolUIManager.Instance?.ClearInstruction();
        }

        protected override void OnInspectionCancelled()
        {
            ToolUIManager.Instance?.ClearInstruction();
            base.OnInspectionCancelled();
        }

        protected override void OnTargetInvalid()
        {
            ToolUIManager.Instance?.ShowMessage("Aim at engine or radiator to inspect fluids", 2f);
        }
    }
}
