using PlayerScripts;
using UnityEngine;
using UnityEngine.InputSystem;
using ToolScripts.Base;
using ToolScripts.UI;

namespace ToolScripts
{
    /// <summary>
    /// Mechanical paint thickness gauge tool handler.
    /// Measures paint thickness on exterior parts in micrometers (μm).
    ///
    /// Behaves identically to DigitalPaintThicknessHandler but with
    /// 2x inspection duration to represent the slower mechanical process.
    ///
    /// Detection logic:
    /// - 60-100 μm: Normal (factory paint thickness)
    /// - 110-450 μm: Painted_Part (repainted area)
    /// - less than 60 μm: Possible scratch or wear
    /// - greater than 450 μm: Aftermarket part or body filler
    /// </summary>
    public class PaintingGaugeHandler : ToolHandlerBase
    {
        [Header("Paint Thickness Settings")]
        [SerializeField] private int minNormalThickness = 60;
        [SerializeField] private int maxNormalThickness = 100;
        [SerializeField] private int paintedThreshold = 110;
        [SerializeField] private int aftermarketThreshold = 450;

        private ExteriorPart _targetExteriorPart;

        protected override void Awake()
        {
            base.Awake();
            toolType = Tool.MechanicPaintThicknessGauge;
            toolName = "Mechanical Paint Gauge";
            inspectionDuration = 1.5f; // 2x the digital gauge (0.75f)
            compatiblePartInterfaces = new string[] { "IExteriorPart" };
        }

        protected override VehiclePart GetTargetPart()
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                return player.GetTargetPart();
            }

            // Fallback to base implementation
            return base.GetTargetPart();
        }

        protected override bool ValidateTarget()
        {
            if (currentTargetPart == null)
            {
                return false;
            }

            _targetExteriorPart = currentTargetPart as ExteriorPart;

            if (_targetExteriorPart == null)
            {
                _targetExteriorPart = currentTargetPart.GetComponent<ExteriorPart>();
            }

            return _targetExteriorPart != null;
        }

        protected override ToolInspectionResult PerformInspection()
        {
            if (_targetExteriorPart == null)
            {
                return ToolInspectionResult.CreateFailure("Invalid target for paint thickness measurement.");
            }

            int thickness = _targetExteriorPart.paintThickness;
            var result = ToolInspectionResult.CreateSuccess(currentTargetPart, "Paint thickness measured.");

            // Add measurement
            result.AddMeasurement("Thickness", $"{thickness} μm");
            result.AddMeasurement("Location", _targetExteriorPart.partPosition.ToString());

            // Determine issue based on thickness
            string detectedIssue = null;
            string statusMessage = "";

            if (thickness < minNormalThickness)
            {
                detectedIssue = "Scratch"; // Possible scratch or wear
                statusMessage = $"WARNING: Paint thickness below normal ({thickness} μm). Possible scratch or wear.";
                result.AddMeasurement("Status", "Below Normal");
            }
            else if (thickness <= maxNormalThickness)
            {
                statusMessage = $"Paint thickness is normal ({thickness} μm). Original factory paint.";
                result.AddMeasurement("Status", "Normal");
            }
            else if (thickness <= paintedThreshold)
            {
                statusMessage = $"Paint thickness slightly elevated ({thickness} μm). Within acceptable range.";
                result.AddMeasurement("Status", "Slightly High");
            }
            else if (thickness <= aftermarketThreshold)
            {
                detectedIssue = "Painted_Part";
                statusMessage = $"PAINTED PART DETECTED! Thickness is {thickness} μm. This area has been repainted.";
                result.AddMeasurement("Status", "Repainted");
            }
            else
            {
                detectedIssue = "Painted_Part"; // Could also be Replaced_Part
                statusMessage = $"AFTERMARKET PART suspected! Very high thickness ({thickness} μm). Possible body filler.";
                result.AddMeasurement("Status", "Aftermarket");
            }

            result.DisplayMessage = statusMessage;

            if (!string.IsNullOrEmpty(detectedIssue))
            {
                result.AddDetectedIssue(detectedIssue);
            }

            GameLogger.Log($"[MechanicalPaintGauge] Measured {_targetExteriorPart.name}: {thickness} μm - {result.Measurements["Status"]}");

            return result;
        }

        protected override void OnInspectionStarted()
        {
            ToolUIManager.Instance?.ShowInstruction("Hold still... measuring paint thickness...");
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
    }
}
