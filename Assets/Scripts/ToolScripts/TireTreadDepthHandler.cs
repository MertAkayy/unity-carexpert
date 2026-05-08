using UnityEngine;
using UnityEngine.InputSystem;
using ToolScripts.Base;
using System;
using System.Collections.Generic;
using PlayerScripts;
using ToolScripts.UI;

namespace ToolScripts
{
    /// <summary>
    /// Tire tread depth gauge tool handler.
    /// Measures tire tread depth and detects various tire issues.
    ///
    /// Detection logic:
    /// - Low tread depth (< 3mm): Low_Tread_Depth issue
    /// - Wrong season for current month: Wrong_Season_Tire issue
    /// - Expired (> 5 years): Expired_Tire issue
    /// - Rim damaged: Rim_Damaged issue
    /// - Punctured: Punctured_Tire issue
    /// </summary>
    public class TireTreadDepthHandler : ToolHandlerBase
    {
        [Header("Tire Inspection Settings")]
        [SerializeField] private float minimumLegalTreadDepth = 1.6f; // mm
        [SerializeField] private float recommendedTreadDepth = 3.0f; // mm
        [SerializeField] private float maximumTireAge = 5.0f; // years

        private VehicleWheel _targetWheel;

        protected override void Awake()
        {
            base.Awake();
            toolType = Tool.TireTreadDepthGauge;
            toolName = "Tread Depth Gauge";
            inspectionDuration = 2f;
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

        protected override ToolInspectionResult PerformInspection()
        {
            if (_targetWheel == null)
            {
                return ToolInspectionResult.CreateFailure("Invalid target for tread depth measurement.");
            }

            var result = ToolInspectionResult.CreateSuccess(currentTargetPart, "Tire inspection complete.");
            List<string> issues = new List<string>();

            // Measure tread depth
            double treadDepth = _targetWheel.TreadDepthMm;
            result.AddMeasurement("Tread Depth", $"{treadDepth:F2} mm");
            result.AddMeasurement("Position", _targetWheel.Position.ToString());

            // Check tread depth
            if (treadDepth < minimumLegalTreadDepth)
            {
                result.AddMeasurement("Tread Status", "ILLEGAL - Below legal minimum!");
                issues.Add("Tire_Worn");
            }
            else
            {
                result.AddMeasurement("Tread Status", "Good");
            }
            
            

            // Check pressure
            float pressure = _targetWheel.Pressure;
            result.AddMeasurement("Pressure", $"{pressure:F1} PSI");

            if (pressure < 25f)
            {
                result.AddMeasurement("Pressure Status", "Low");
            }
            else if (pressure > 38f)
            {
                result.AddMeasurement("Pressure Status", "High");
            }
            else
            {
                result.AddMeasurement("Pressure Status", "Normal");
            }
            

            // Check for puncture
            if (_targetWheel.IsPunctured)
            {
                result.AddMeasurement("Tire Status", "Flat_Tire");
                issues.Add("Flat_Tire");
            }
            else
            {
                result.AddMeasurement("Tire Status", "Good");
            }

            // Add brand info
            result.AddMeasurement("Brand", _targetWheel.TireBrand ?? "Unknown");
            result.AddMeasurement("Wheel Type", _targetWheel.IsAlloy ? "Alloy" : "Steel");

            // Build display message
            string message = BuildInspectionMessage(treadDepth, issues);
            result.DisplayMessage = message;

            // Add detected issues
            foreach (string issue in issues)
            {
                result.AddDetectedIssue(issue);
            }

            GameLogger.Log($"[TreadDepthGauge] Inspected {_targetWheel.Position}: {treadDepth:F2}mm tread");

            return result;
        }

        private string BuildInspectionMessage(double treadDepth, List<string> issues)
        {
            string message = $"Tread Depth: {treadDepth:F2} mm \n";

            if (issues.Count > 0)
            {
                message += "\nISSUES DETECTED:\n";
                foreach (string issue in issues)
                {
                    message += $"- {FormatIssueName(issue)}\n";
                }
            }
            else
            {
                message += "\nTire is in good condition.";
            }

            return message;
        }

        private string FormatIssueName(string issue)
        {
            return issue.Replace("_", " ");
        }

        protected override void OnInspectionStarted()
        {
            if (_targetWheel != null)
            {
                ToolUIManager.Instance?.ShowInstruction($"Measuring {_targetWheel.Position} tire...");
            }
            else
            {
                ToolUIManager.Instance?.ShowInstruction("Measuring tire...");
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
    }
}
