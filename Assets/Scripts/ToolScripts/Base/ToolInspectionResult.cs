using System;
using System.Collections.Generic;
using UnityEngine;

namespace ToolScripts.Base
{
    /// <summary>
    /// Result data structure for tool inspections
    /// </summary>
    [Serializable]
    public class ToolInspectionResult
    {
        public bool Success { get; set; }
        public string DisplayMessage { get; set; }
        public Dictionary<string, string> Measurements { get; set; }
        public List<string> DetectedIssues { get; set; }
        public object RawData { get; set; }
        public VehiclePart TargetPart { get; set; }

        public ToolInspectionResult()
        {
            Success = false;
            DisplayMessage = string.Empty;
            Measurements = new Dictionary<string, string>();
            DetectedIssues = new List<string>();
            RawData = null;
            TargetPart = null;
        }

        public static ToolInspectionResult CreateFailure(string message)
        {
            return new ToolInspectionResult
            {
                Success = false,
                DisplayMessage = message
            };
        }

        public static ToolInspectionResult CreateSuccess(VehiclePart target, string message, Dictionary<string, string> measurements = null)
        {
            return new ToolInspectionResult
            {
                Success = true,
                TargetPart = target,
                DisplayMessage = message,
                Measurements = measurements ?? new Dictionary<string, string>()
            };
        }

        public void AddMeasurement(string key, string value)
        {
            if (Measurements == null)
                Measurements = new Dictionary<string, string>();
            Measurements[key] = value;
        }

        public void AddDetectedIssue(string issueName)
        {
            if (DetectedIssues == null)
                DetectedIssues = new List<string>();
            if (!DetectedIssues.Contains(issueName))
                DetectedIssues.Add(issueName);
        }
    }
}
