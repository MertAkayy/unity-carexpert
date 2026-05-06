using PlayerScripts;
using UnityEngine;
using UnityEngine.InputSystem;
using ToolScripts.Base;
using ToolScripts.UI;

namespace ToolScripts
{
    /// <summary>
    /// Exhaust gas analyzer tool handler.
    /// Measures vehicle emissions and detects emission faults.
    ///
    /// Features:
    /// - Shows CO (%), HC (ppm), NOx (ppm) readings
    /// - 5 second test duration (emission test takes time)
    /// - Emission limits:
    ///   - CO < 0.5%
    ///   - HC < 100 ppm
    ///   - NOx < 1000 ppm
    /// - Adds Emission_Fault issue if any limit exceeded
    /// - Visual display of emission levels with color coding
    /// - Smoke detection visual feedback
    /// </summary>
    public class ExhaustGasAnalyserHandler : ToolHandlerBase
    {
        [Header("Emission Limits")]
        [SerializeField] private float maxCO = 0.5f; // Percentage
        [SerializeField] private float maxHC = 100f; // ppm
        [SerializeField] private float maxNOx = 1000f; // ppm

        [Header("Settings")]
        [SerializeField] private float maxDetectionDistance = 5f; // Can test from further away

        private VehicleExhaust _targetExhaust;

        protected override void Awake()
        {
            base.Awake();
            toolType = Tool.ExhaustGasAnalyser;
            toolName = "Exhaust Gas Analyzer";
            inspectionDuration = 5f; // Emission test takes time
            maxInspectionDistance = maxDetectionDistance;
        }

        protected override VehiclePart GetTargetPart()
        {
            Player player = FindObjectOfType<Player>();
            if (player == null) return null;

            PlayerCamera playerCamera = FindObjectOfType<PlayerCamera>();
            if (playerCamera == null) return null;

            // Raycast to find exhaust or any vehicle component
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxInspectionDistance, targetLayerMask))
            {
                // First try to find VehicleExhaust
                VehicleExhaust exhaust = hit.collider.GetComponentInParent<VehicleExhaust>();
                if (exhaust != null)
                {
                    _targetExhaust = exhaust;
                    return exhaust;
                }

                // If no exhaust, try to find vehicle and get its exhaust
                Vehicle vehicle = hit.collider.GetComponentInParent<Vehicle>();
                if (vehicle != null)
                {
                    // Look for exhaust in vehicle children
                    VehicleExhaust foundExhaust = vehicle.GetComponentInChildren<VehicleExhaust>();
                    if (foundExhaust != null)
                    {
                        _targetExhaust = foundExhaust;
                        return foundExhaust;
                    }
                }
            }

            return null;
        }

        protected override bool ValidateTarget()
        {
            if (currentTargetPart == null)
            {
                return false;
            }

            _targetExhaust = currentTargetPart as VehicleExhaust;

            if (_targetExhaust == null)
            {
                _targetExhaust = currentTargetPart.GetComponent<VehicleExhaust>();
            }

            return _targetExhaust != null;
        }

        protected override ToolInspectionResult PerformInspection()
        {
            if (_targetExhaust == null)
            {
                return ToolInspectionResult.CreateFailure("No exhaust system in range for emission test.");
            }

            var result = ToolInspectionResult.CreateSuccess(_targetExhaust, "Emission test complete.");

            // Get emission readings
            float co = _targetExhaust.CO_Emission;
            float hc = _targetExhaust.HC_Emission;
            float nox = _targetExhaust.NOx_Emission;

            // Add measurements
            result.AddMeasurement("CO", $"{co:F2}%");
            result.AddMeasurement("HC", $"{hc:F0} ppm");
            result.AddMeasurement("NOx", $"{nox:F0} ppm");

            // Check each emission against limits
            bool coPass = co <= maxCO;
            bool hcPass = hc <= maxHC;
            bool noxPass = nox <= maxNOx;

            result.AddMeasurement("CO Status", coPass ? "PASS" : "FAIL");
            result.AddMeasurement("HC Status", hcPass ? "PASS" : "FAIL");
            result.AddMeasurement("NOx Status", noxPass ? "PASS" : "FAIL");

            // Check for smoke
            if (_targetExhaust.HasSmoke)
            {
                result.AddMeasurement("Visual", "SMOKE DETECTED");
            }

            // Check if broken
            if (_targetExhaust.IsBroken)
                result.AddMeasurement("Exhaust", "DAMAGED");

            // Overall result
            bool overallPass = coPass && hcPass && noxPass && !_targetExhaust.IsBroken;
            result.AddMeasurement("Overall", overallPass ? "PASS" : "FAIL");

            // Detect specific issues from the emission signature and add to result
            if (!overallPass)
            {
                foreach (string issueName in _targetExhaust.GetDetectedIssueNames())
                    result.AddDetectedIssue(issueName);
            }

            // Build display message
            string message = BuildEmissionMessage(co, hc, nox, coPass, hcPass, noxPass, overallPass);
            result.DisplayMessage = message;

            GameLogger.Log($"[ExhaustAnalyzer] CO: {co:F2}%, HC: {hc:F0}ppm, NOx: {nox:F0}ppm - {(overallPass ? "PASS" : "FAIL")}");

            return result;
        }

        private string BuildEmissionMessage(float co, float hc, float nox, bool coPass, bool hcPass, bool noxPass, bool overallPass)
        {
            string message = "EMISSION TEST RESULTS\n";
            message += "======================\n\n";

            message += $"Carbon Monoxide (CO): {co:F2}%\n";
            message += $"  Limit: {maxCO:F2}% - {(coPass ? "PASS" : "FAIL")}\n\n";

            message += $"Hydrocarbons (HC): {hc:F0} ppm\n";
            message += $"  Limit: {maxHC:F0} ppm - {(hcPass ? "PASS" : "FAIL")}\n\n";

            message += $"Nitrogen Oxides (NOx): {nox:F0} ppm\n";
            message += $"  Limit: {maxNOx:F0} ppm - {(noxPass ? "PASS" : "FAIL")}\n\n";

            message += "======================\n";
            message += $"OVERALL: {(overallPass ? "PASS" : "FAIL")}\n";

            if (_targetExhaust.HasSmoke)
            {
                message += "\nWARNING: Visible smoke detected!\n";
            }

            if (_targetExhaust.IsBroken)
            {
                message += "\nCRITICAL: Exhaust system damaged!\n";
            }

            return message;
        }

        protected override void OnInspectionStarted()
        {
            ToolUIManager.Instance?.ShowInstruction("Testing emissions... Probe in exhaust pipe...");
        }

        protected override void OnInspectionComplete(ToolInspectionResult result)
        {
            ToolUIManager.Instance?.ClearInstruction();

            if (_targetExhaust != null && _targetExhaust.HasEmissionFault())
            {
                GameLogger.Log("[ExhaustAnalyzer] Emission fault detected!");
            }
        }

        protected override void OnInspectionCancelled()
        {
            ToolUIManager.Instance?.ClearInstruction();
            base.OnInspectionCancelled();
        }

        protected override void OnTargetInvalid()
        {
            ToolUIManager.Instance?.ShowMessage("Aaim at exhaust pipe for emission test", 2f);
        }
    }
}
