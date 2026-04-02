using System;
using System.Collections.Generic;
using UnityEngine;
using ToolScripts.Base;

namespace Inspection
{
    /// <summary>
    /// Enhanced result data structure for individual part inspections.
    /// Tracks detailed information about tool usage and issue detection.
    /// </summary>
    [Serializable]
    public class InspectionResult
    {
        /// <summary>
        /// Unique identifier for this inspection result.
        /// </summary>
        public Guid ResultId { get; private set; }

        /// <summary>
        /// The vehicle part that was inspected.
        /// </summary>
        public VehiclePart TargetPart { get; private set; }

        /// <summary>
        /// The unique type identifier of the inspected part.
        /// </summary>
        public VehiclePartUniqueType PartType { get; private set; }

        /// <summary>
        /// Name of the part for display purposes.
        /// </summary>
        public string PartName { get; private set; }

        /// <summary>
        /// The tool that was used for this inspection.
        /// </summary>
        public Tool UsedTool { get; private set; }

        /// <summary>
        /// Timestamp when the inspection was performed.
        /// </summary>
        public DateTime InspectionTime { get; private set; }

        /// <summary>
        /// Whether the inspection was successful.
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Human-readable message describing the inspection outcome.
        /// </summary>
        public string DisplayMessage { get; private set; }

        /// <summary>
        /// Measurements taken during the inspection (e.g., paint thickness, tire tread depth).
        /// </summary>
        public Dictionary<string, string> Measurements { get; private set; }

        /// <summary>
        /// Issues detected by the player during this inspection.
        /// </summary>
        public List<Issue> DetectedIssues { get; private set; }

        /// <summary>
        /// Names of detected issues for serialization.
        /// </summary>
        public List<string> DetectedIssueNames { get; private set; }

        /// <summary>
        /// Raw data from the tool (for advanced processing).
        /// </summary>
        public object RawData { get; private set; }

        /// <summary>
        /// Whether this part has been fully inspected (all possible issues checked).
        /// </summary>
        public bool IsFullyInspected { get; set; }

        /// <summary>
        /// Creates a new empty inspection result.
        /// </summary>
        public InspectionResult()
        {
            ResultId = Guid.NewGuid();
            Measurements = new Dictionary<string, string>();
            DetectedIssues = new List<Issue>();
            DetectedIssueNames = new List<string>();
            InspectionTime = DateTime.Now;
        }

        /// <summary>
        /// Creates an inspection result from a ToolInspectionResult.
        /// </summary>
        /// <param name="toolResult">The tool inspection result to convert.</param>
        /// <param name="usedTool">The tool that was used.</param>
        /// <returns>A new InspectionResult instance.</returns>
        public static InspectionResult FromToolResult(ToolInspectionResult toolResult, Tool usedTool)
        {
            var result = new InspectionResult
            {
                TargetPart = toolResult.TargetPart,
                PartType = toolResult.TargetPart != null ? toolResult.TargetPart.partUniqueType : VehiclePartUniqueType.Engine,
                PartName = toolResult.TargetPart != null ? toolResult.TargetPart.name : "Unknown",
                UsedTool = usedTool,
                Success = toolResult.Success,
                DisplayMessage = toolResult.DisplayMessage,
                Measurements = toolResult.Measurements ?? new Dictionary<string, string>(),
                RawData = toolResult.RawData
            };

            // Extract detected issue names
            if (toolResult.DetectedIssues != null)
            {
                result.DetectedIssueNames = new List<string>(toolResult.DetectedIssues);
            }

            return result;
        }

        /// <summary>
        /// Creates a successful inspection result with the specified parameters.
        /// </summary>
        public static InspectionResult CreateSuccess(VehiclePart part, Tool tool, string message, Dictionary<string, string> measurements = null)
        {
            var result = new InspectionResult
            {
                TargetPart = part,
                PartType = part != null ? part.partUniqueType : VehiclePartUniqueType.Engine,
                PartName = part != null ? part.name : "Unknown",
                UsedTool = tool,
                Success = true,
                DisplayMessage = message,
                Measurements = measurements ?? new Dictionary<string, string>()
            };

            return result;
        }

        /// <summary>
        /// Creates a failed inspection result.
        /// </summary>
        public static InspectionResult CreateFailure(string message, Tool tool = Tool.Null)
        {
            return new InspectionResult
            {
                UsedTool = tool,
                Success = false,
                DisplayMessage = message
            };
        }

        /// <summary>
        /// Adds a detected issue to this result.
        /// </summary>
        /// <param name="issue">The issue that was detected.</param>
        public void AddDetectedIssue(Issue issue)
        {
            if (issue == null) return;

            if (DetectedIssues == null)
                DetectedIssues = new List<Issue>();

            if (DetectedIssueNames == null)
                DetectedIssueNames = new List<string>();

            if (!DetectedIssues.Contains(issue))
            {
                DetectedIssues.Add(issue);
            }

            if (!DetectedIssueNames.Contains(issue.FailureName))
            {
                DetectedIssueNames.Add(issue.FailureName);
            }
        }

        /// <summary>
        /// Adds a measurement to this result.
        /// </summary>
        /// <param name="key">The measurement key.</param>
        /// <param name="value">The measurement value.</param>
        public void AddMeasurement(string key, string value)
        {
            if (Measurements == null)
                Measurements = new Dictionary<string, string>();

            Measurements[key] = value;
        }

        /// <summary>
        /// Gets a measurement value by key.
        /// </summary>
        /// <param name="key">The measurement key.</param>
        /// <returns>The measurement value, or null if not found.</returns>
        public string GetMeasurement(string key)
        {
            if (Measurements != null && Measurements.TryGetValue(key, out string value))
            {
                return value;
            }
            return null;
        }

        /// <summary>
        /// Converts this result to a serializable DTO.
        /// </summary>
        public InspectionResultDto ToDTO()
        {
            return new InspectionResultDto
            {
                resultId = ResultId.ToString(),
                partType = PartType.ToString(),
                partName = PartName,
                usedTool = UsedTool.ToString(),
                inspectionTime = InspectionTime.ToString("o"),
                success = Success,
                displayMessage = DisplayMessage,
                measurements = Measurements,
                detectedIssueNames = DetectedIssueNames
            };
        }

        /// <summary>
        /// Returns a summary string of this inspection result.
        /// </summary>
        public override string ToString()
        {
            string issueCount = DetectedIssueNames?.Count.ToString() ?? "0";
            return $"[InspectionResult] {PartName} ({UsedTool}): {DisplayMessage} - {issueCount} issues detected";
        }
    }

    /// <summary>
    /// Data Transfer Object for serialization of InspectionResult.
    /// </summary>
    [Serializable]
    public class InspectionResultDto
    {
        public string resultId;
        public string partType;
        public string partName;
        public string usedTool;
        public string inspectionTime;
        public bool success;
        public string displayMessage;
        public Dictionary<string, string> measurements;
        public List<string> detectedIssueNames;
    }
}
