using PlayerScripts;
using UnityEngine;
using UnityEngine.InputSystem;
using ToolScripts.Base;
using ToolScripts.UI;

namespace ToolScripts
{
    /// <summary>
    /// Tire pumper/inflator tool handler.
    /// Simulates pumping air into tires and detects punctures.
    ///
    /// Features:
    /// - Increases tire pressure during pumping (up to 35 PSI recommended, max 50 PSI)
    /// - If pressure won't rise → detect Punctured_Tire issue
    /// - Shows current PSI during pumping
    /// - Audio feedback during pumping
    /// </summary>
    public class TirePumperHandler : ToolHandlerBase
    {
        [Header("Pumper Settings")]
        [SerializeField] private float pumpingRate = 2.0f; // PSI per second while holding
        [SerializeField] private float minRecommendedPressure = 28f;
        [SerializeField] private float maxRecommendedPressure = 35f;
        [SerializeField] private float maxSafePressure = 50f;
        [SerializeField] private float punctureDetectThreshold = 0.2f; // PSI increase threshold to confirm puncture

        [Header("Audio")]
        [SerializeField] private AudioClip pumpingSound;
        [SerializeField] private float pumpInterval = 0.5f; // Seconds between pump sounds

        private VehicleWheel _targetWheel;
        private float _startPressure;
        private float _currentPressure;
        private float _pressureIncrease;
        private float _lastPumpSoundTime;
        private bool _isPuncturedDetected = false;

        protected override void Awake()
        {
            base.Awake();
            toolType = Tool.TirePumper;
            toolName = "Tire Pumper";
            inspectionDuration = 5f; // Longer duration for pumping
            compatiblePartInterfaces = new string[] { "IVehicleWheel" };
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

            _targetWheel = currentTargetPart as VehicleWheel;

            if (_targetWheel == null)
            {
                _targetWheel = currentTargetPart.GetComponent<VehicleWheel>();
            }

            return _targetWheel != null;
        }

        protected override void BeginInspection()
        {
            base.BeginInspection();

            // Initialize pumping state
            _startPressure = _targetWheel.Pressure;
            _currentPressure = _startPressure;
            _pressureIncrease = 0f;
            _isPuncturedDetected = false;

            ToolUIManager.Instance?.ShowInstruction($"Pumping {_targetWheel.Position} tire... Hold to inflate");
        }

        protected override void UpdateInspection()
        {
            base.UpdateInspection();

            // Calculate pressure increase based on pumping rate
            float pressureToAdd = pumpingRate * Time.deltaTime;

            // Check if tire is punctured by attempting to add pressure
            float actualIncrease = _targetWheel.PumpAir(pressureToAdd);

            _pressureIncrease += actualIncrease;
            _currentPressure = _targetWheel.Pressure;

            // Update progress based on pressure target
            float pressureProgress = Mathf.Clamp01(
                (_currentPressure - minRecommendedPressure) /
                (maxRecommendedPressure - minRecommendedPressure)
            );
            ToolUIManager.Instance?.UpdateProgress(pressureProgress);

            // Update display with current pressure
            ToolUIManager.Instance?.ShowInstruction(
                $"Pumping {_targetWheel.Position}: {_currentPressure:F1} PSI | Target: {minRecommendedPressure}-{maxRecommendedPressure} PSI"
            );

            // Play pumping sound periodically
            if (pumpingSound != null && _audioSource != null)
            {
                _lastPumpSoundTime += Time.deltaTime;
                if (_lastPumpSoundTime >= pumpInterval)
                {
                    _audioSource.PlayOneShot(pumpingSound);
                    _lastPumpSoundTime = 0f;
                }
            }

            // Check if we've reached target pressure
            if (_currentPressure >= maxRecommendedPressure)
            {
                CompleteInspection();
            }
            // Or if max time reached
            else if (inspectionProgress >= inspectionDuration)
            {
                CompleteInspection();
            }
        }

        protected override ToolInspectionResult PerformInspection()
        {
            if (_targetWheel == null)
            {
                return ToolInspectionResult.CreateFailure("Invalid target for tire inflation.");
            }

            var result = ToolInspectionResult.CreateSuccess(currentTargetPart, "Tire inflation complete.");
            result.AddMeasurement("Starting Pressure", $"{_startPressure:F1} PSI");
            result.AddMeasurement("Final Pressure", $"{_currentPressure:F1} PSI");
            result.AddMeasurement("Pressure Added", $"{_pressureIncrease:F1} PSI");
            result.AddMeasurement("Position", _targetWheel.Position.ToString());

            // Check for puncture
            if (_targetWheel.IsPunctured || _pressureIncrease < punctureDetectThreshold)
            {
                _isPuncturedDetected = true;
                result.AddMeasurement("Status", "PUNCTURED - Tire won't hold air!");
                result.AddDetectedIssue("Punctured_Tire");
                result.DisplayMessage = $"TIRE PUNCTURED! Pressure only increased by {_pressureIncrease:F1} PSI. Tire needs repair.";
            }
            // Check current pressure status
            else if (_currentPressure < minRecommendedPressure)
            {
                result.AddMeasurement("Status", "Low - Keep pumping");
                result.DisplayMessage = $"Pressure is low ({_currentPressure:F1} PSI). Continue pumping to reach {minRecommendedPressure}-{maxRecommendedPressure} PSI.";
            }
            else if (_currentPressure > maxSafePressure)
            {
                result.AddMeasurement("Status", "OVERINFLATED - Release air!");
                result.DisplayMessage = $"WARNING: Pressure is too high ({_currentPressure:F1} PSI). Recommended max is {maxSafePressure} PSI.";
            }
            else
            {
                result.AddMeasurement("Status", "Good");
                result.DisplayMessage = $"Tire inflated to {_currentPressure:F1} PSI. Pressure is within recommended range.";
            }

            GameLogger.Log($"[TirePumper] {_targetWheel.Position}: {_startPressure:F1} -> {_currentPressure:F1} PSI ({_pressureIncrease:F1} added)");

            return result;
        }

        protected override void OnInspectionComplete(ToolInspectionResult result)
        {
            ToolUIManager.Instance?.ClearInstruction();

            if (_isPuncturedDetected)
            {
                GameLogger.Log("[TirePumper] Puncture detected - tire won't hold air");
            }
        }

        protected override void OnInspectionCancelled()
        {
            ToolUIManager.Instance?.ClearInstruction();
            base.OnInspectionCancelled();

            if (_targetWheel != null)
            {
                GameLogger.Log($"[TirePumper] Cancelled. Final pressure: {_targetWheel.Pressure:F1} PSI");
            }
        }

        protected override void OnTargetInvalid()
        {
            ToolUIManager.Instance?.ShowMessage("Aim at a tire to inflate", 2f);
        }
    }
}
