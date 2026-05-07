using UnityEngine;
using UnityEngine.InputSystem;
using ToolScripts.Base;
using System.Collections.Generic;
using System.Linq;
using PlayerScripts;
using ToolScripts.UI;

namespace ToolScripts
{
    /// <summary>
    /// OBD-II scanner tool handler.
    /// Scans all vehicle parts for issues with OBD codes.
    ///
    /// Features:
    /// - Scan all vehicle parts for assigned issues with OBD codes
    /// - Display raw codes only (PXXXX, CXXXX, BXXXX, UXXXX)
    /// - Player must use reference book to decode (no decode in scanner)
    /// - 3 second scan duration
    ///
    /// OBD Code Categories:
    /// - Pxxxx: Powertrain (engine, transmission)
    /// - Cxxxx: Chassis (brakes, suspension)
    /// - Bxxxx: Body (HVAC, seats)
    /// - Uxxxx: Network (communication)
    /// - 0xxxx/1xxxx: Manufacturer specific
    /// </summary>
    public class OBDScannerHandler : ToolHandlerBase
    {
        [Header("OBD Scanner Settings")]
        [SerializeField] private float scanRange = 10f; // Range to detect vehicle
        [SerializeField] private LayerMask vehicleLayerMask = -1;

        private Vehicle _targetVehicle;

        protected override void Awake()
        {
            base.Awake();
            toolType = Tool.ObdScanner;
            toolName = "OBD-II Scanner";
            inspectionDuration = 3f;
            // OBD scanner works on the whole vehicle, not specific parts
            compatiblePartInterfaces = new string[0];
        }

        protected override VehiclePart GetTargetPart()
        {
            // OBD scanner finds the entire vehicle
            Player player = FindObjectOfType<Player>();
            if (player == null) return null;

            PlayerCamera playerCamera = FindObjectOfType<PlayerCamera>();
            if (playerCamera == null) return null;

            // Raycast to find any vehicle component
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxInspectionDistance, vehicleLayerMask))
            {
                Vehicle vehicle = hit.collider.GetComponentInParent<Vehicle>();
                if (vehicle != null)
                {
                    _targetVehicle = vehicle;
                    // Return any part from the vehicle as the target
                    if (vehicle.exteriorParts != null && vehicle.exteriorParts.Count > 0)
                        return vehicle.exteriorParts[0];
                    if (vehicle.wheels != null && vehicle.wheels.Count > 0)
                        return vehicle.wheels[0];
                }
            }

            return null;
        }

        protected override bool ValidateTarget()
        {
            if (_targetVehicle == null)
            {
                return false;
            }

            // Check if we're within scan range
            Player player = FindObjectOfType<Player>();
            if (player == null) return false;

            PlayerCamera playerCamera = FindObjectOfType<PlayerCamera>();
            if (playerCamera == null) return false;

            float distance = Vector3.Distance(playerCamera.transform.position, _targetVehicle.transform.position);
            return distance <= scanRange;
        }

        protected override ToolInspectionResult PerformInspection()
        {
            if (_targetVehicle == null)
            {
                return ToolInspectionResult.CreateFailure("No vehicle in range for OBD scan.");
            }

            var result = ToolInspectionResult.CreateSuccess(null, "OBD scan complete.");

            // Collect all parts from the vehicle
            List<VehiclePart> allParts = new List<VehiclePart>();
            if (_targetVehicle.exteriorParts != null) allParts.AddRange(_targetVehicle.exteriorParts);
            if (_targetVehicle.wheels != null) allParts.AddRange(_targetVehicle.wheels);
            if (_targetVehicle.glasses != null) allParts.AddRange(_targetVehicle.glasses);
            if (_targetVehicle.lights != null) allParts.AddRange(_targetVehicle.lights);
            if (_targetVehicle.battery != null) allParts.Add(_targetVehicle.battery);
            if (_targetVehicle.engine != null) allParts.Add(_targetVehicle.engine);
            if (_targetVehicle.radiator != null) allParts.Add(_targetVehicle.radiator);

            // Collect all OBD codes from assigned issues
            List<string> obdCodes = new List<string>();
            Dictionary<string, string> codeToPartMap = new Dictionary<string, string>();

            foreach (var part in allParts)
            {
                if (part == null) continue;

                foreach (var issue in part.assignedIssues)
                {
                    if (issue == null) continue;

                    string code = issue.ObdCode;
                    if (!string.IsNullOrEmpty(code) && code != "PXXXX")
                    {
                        obdCodes.Add(code);
                        codeToPartMap[code] = part.name;
                        result.AddDetectedIssue(issue.FailureName);
                    }
                }
            }

            // Remove duplicates and sort
            obdCodes = obdCodes.Distinct().OrderBy(c => c).ToList();

            // Add measurements for codes
            int codeIndex = 1;
            foreach (string code in obdCodes)
            {
                result.AddMeasurement($"Code {codeIndex}", code);
                if (codeToPartMap.ContainsKey(code))
                {
                    result.AddMeasurement($"  Location", codeToPartMap[code]);
                }
                codeIndex++;
            }

            // Build display message
            string message = BuildOBDMessage(obdCodes);
            result.DisplayMessage = message;

            GameLogger.Log($"[OBDScanner] Found {obdCodes.Count} trouble code(s) — issues will be added to predictedIssues via AddDetectedIssuesToPart");

            return result;
        }

        private string BuildOBDMessage(List<string> codes)
        {
            if (codes.Count == 0)
            {
                return "OBD Scan Complete: No trouble codes found.\nVehicle systems are operating normally.";
            }

            string message = $"OBD Scan Complete: Found {codes.Count} trouble code(s)\n\n";
            message += "RAW CODES DETECTED:\n";

            foreach (string code in codes)
            {
                message += $"  {code}\n";
            }

            message += "\nUse Reference Book (Tab key) to decode codes.";

            return message;
        }

        protected override void OnInspectionStarted()
        {
            ToolUIManager.Instance?.ShowInstruction("Scanning vehicle ECU... Reading trouble codes...");
        }

        protected override void OnInspectionComplete(ToolInspectionResult result)
        {
            ToolUIManager.Instance?.ClearInstruction();

            if (_targetVehicle != null)
            {
                GameLogger.Log($"[OBDScanner] Scan complete for vehicle {_targetVehicle.VehicleId}");
            }
        }

        protected override void OnInspectionCancelled()
        {
            ToolUIManager.Instance?.ClearInstruction();
            base.OnInspectionCancelled();
        }

        protected override void OnTargetInvalid()
        {
            ToolUIManager.Instance?.ShowMessage("No vehicle in range for OBD scan", 2f);
        }

        protected override void AddDetectedIssuesToPart(ToolInspectionResult result)
        {
            if (_targetVehicle == null || result.DetectedIssues == null || result.DetectedIssues.Count == 0) return;

            VehicleManager vehicleManager = FindObjectOfType<VehicleManager>();
            IssueDataBase issueDatabase = vehicleManager != null ? vehicleManager.IssueDatabase : null;
            if (issueDatabase == null)
            {
                GameLogger.LogWarning("[OBDScanner] IssueDataBase not found.");
                return;
            }

            List<VehiclePart> allParts = new List<VehiclePart>();
            if (_targetVehicle.exteriorParts != null) allParts.AddRange(_targetVehicle.exteriorParts);
            if (_targetVehicle.wheels != null) allParts.AddRange(_targetVehicle.wheels);
            if (_targetVehicle.glasses != null) allParts.AddRange(_targetVehicle.glasses);
            if (_targetVehicle.lights != null) allParts.AddRange(_targetVehicle.lights);
            if (_targetVehicle.battery != null) allParts.Add(_targetVehicle.battery);
            if (_targetVehicle.engine != null) allParts.Add(_targetVehicle.engine);
            if (_targetVehicle.radiator != null) allParts.Add(_targetVehicle.radiator);

            int addedCount = 0;
            foreach (string issueName in result.DetectedIssues)
            {
                Issue issue = issueDatabase.GetByName(issueName);
                if (issue == null) continue;

                // Find the part that has this issue assigned
                foreach (var part in allParts)
                {
                    if (part == null) continue;
                    if (!part.assignedIssues.Contains(issue)) continue;

                    if (!part.predictedIssues.Contains(issue))
                    {
                        part.predictedIssues.Add(issue);
                        addedCount++;
                        GameLogger.Log($"[OBDScanner] '{issueName}' added to predictedIssues on '{part.name}'");
                    }
                    else
                    {
                        GameLogger.Log($"[OBDScanner] '{issueName}' already in predictedIssues on '{part.name}'");
                    }
                }
            }

            GameLogger.Log($"[OBDScanner] {addedCount} issue(s) added to predictedIssues across all parts.");
        }
    }
}
