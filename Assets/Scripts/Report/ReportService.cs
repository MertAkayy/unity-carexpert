using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using Inspection;
using UnityEngine;

namespace Report
{
    /// <summary>
    /// Interface for the Report Service.
    /// Provides access to report generation and history functionality.
    /// </summary>
    public interface IReportService : ISystem
    {
        /// <summary>History of all generated reports.</summary>
        IReadOnlyList<InspectionReport> ReportHistory { get; }

        /// <summary>Event fired when a new report is generated.</summary>
        event Action<InspectionReport> OnReportGenerated;

        /// <summary>Generates a final inspection report for the current session.</summary>
        InspectionReport GenerateReport(Vehicle vehicle, List<InspectionResult> inspectionResults);

        /// <summary>Generates a report by comparing assigned vs predicted issues directly.</summary>
        InspectionReport GenerateReportFromVehicle(Vehicle vehicle);

        /// <summary>Gets the most recent report.</summary>
        InspectionReport GetLatestReport();

        /// <summary>Gets reports within a date range.</summary>
        List<InspectionReport> GetReportsByDateRange(DateTime startDate, DateTime endDate);

        /// <summary>Gets reports for a specific vehicle.</summary>
        List<InspectionReport> GetReportsForVehicle(string vehicleId);

        /// <summary>Calculates accuracy by comparing assigned issues vs predicted issues.</summary>
        float CalculateAccuracy(Vehicle vehicle);

        /// <summary>Estimates the vehicle condition based on assigned issues.</summary>
        VehicleConditionRating EstimateCondition(Vehicle vehicle);

        /// <summary>Estimates the vehicle value modifier based on condition.</summary>
        float EstimateValueModifier(Vehicle vehicle);

        /// <summary>Estimates total repair cost for all issues on a vehicle.</summary>
        float EstimateTotalRepairCost(Vehicle vehicle);

        /// <summary>Clears all report history.</summary>
        void ClearHistory();

        /// <summary>Removes a specific report from history.</summary>
        void RemoveReport(Guid reportId);

        /// <summary>Saves report history to persistent storage.</summary>
        void SaveHistory();

        /// <summary>Loads report history from persistent storage.</summary>
        void LoadHistory();
    }

    /// <summary>
    /// Service for generating inspection reports and maintaining report history.
    /// Compares player-detected issues with actual vehicle issues to calculate accuracy.
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly List<InspectionReport> _reportHistory = new List<InspectionReport>();
        private IInspectionService _inspectionService;
        private DateTime _inspectionStartTime;

        /// <inheritdoc/>
        public int Priority => 110; // After InspectionService

        /// <inheritdoc/>
        public IReadOnlyList<InspectionReport> ReportHistory => _reportHistory.AsReadOnly();

        /// <inheritdoc/>
        public event Action<InspectionReport> OnReportGenerated;

        /// <inheritdoc/>
        public void OnRegistered()
        {
            GameLogger.Log("[ReportService] Registered with ServiceLocator");

            // Subscribe to inspection events
            if (ServiceLocator.TryGet<IInspectionService>(out var inspectionService))
            {
                _inspectionService = inspectionService;
                _inspectionService.OnInspectionStarted += HandleInspectionStarted;
                _inspectionService.OnInspectionEnded += HandleInspectionEnded;
            }
        }

        /// <inheritdoc/>
        public void Initialize()
        {
            LoadHistory();
            GameLogger.Log("[ReportService] Initialized");
        }

        /// <inheritdoc/>
        public void Shutdown()
        {
            SaveHistory();

            if (_inspectionService != null)
            {
                _inspectionService.OnInspectionStarted -= HandleInspectionStarted;
                _inspectionService.OnInspectionEnded -= HandleInspectionEnded;
            }

            GameLogger.Log("[ReportService] Shutdown complete");
        }

        private void HandleInspectionStarted(Vehicle vehicle)
        {
            _inspectionStartTime = DateTime.Now;
            GameLogger.Log($"[ReportService] Inspection started at {_inspectionStartTime}");
        }

        private void HandleInspectionEnded(Vehicle vehicle)
        {
            // Auto-generate report when inspection ends
            if (vehicle != null)
            {
                GameLogger.Log("[ReportService] Auto-generating report for completed inspection");
                var report = GenerateReportFromVehicle(vehicle);
                AddReportToHistory(report);
            }
        }

        /// <inheritdoc/>
        public InspectionReport GenerateReport(Vehicle vehicle, List<InspectionResult> inspectionResults)
        {
            if (vehicle == null)
            {
                GameLogger.LogWarning("[ReportService] Cannot generate report: vehicle is null");
                return null;
            }

            var report = InspectionReport.CreateForVehicle(vehicle);
            report.InspectionDate = DateTime.Now;

            // Calculate inspection duration
            if (_inspectionStartTime != default)
            {
                report.InspectionDurationSeconds = (float)(DateTime.Now - _inspectionStartTime).TotalSeconds;
            }

            // Get all vehicle parts
            var allParts = GetAllVehicleParts(vehicle);

            // Compare assigned issues with predicted issues
            ProcessIssues(report, allParts);

            // Calculate summary
            int partsInspected = CountInspectedParts(allParts);
            int totalParts = allParts.Count;
            report.CalculateSummary(partsInspected, totalParts);

            report.IsComplete = true;

            GameLogger.Log($"[ReportService] Generated report: {report}");
            return report;
        }

        /// <inheritdoc/>
        public InspectionReport GenerateReportFromVehicle(Vehicle vehicle)
        {
            if (vehicle == null)
            {
                GameLogger.LogWarning("[ReportService] Cannot generate report: vehicle is null");
                return null;
            }

            var report = InspectionReport.CreateForVehicle(vehicle);
            report.InspectionDate = DateTime.Now;

            // Calculate inspection duration
            if (_inspectionStartTime != default)
            {
                report.InspectionDurationSeconds = (float)(DateTime.Now - _inspectionStartTime).TotalSeconds;
            }

            // Get all vehicle parts
            var allParts = GetAllVehicleParts(vehicle);

            // Process issues by comparing assigned vs predicted
            ProcessIssues(report, allParts);

            // Calculate summary
            int partsInspected = CountPartsWithPredictions(allParts);
            int totalParts = allParts.Count;
            report.CalculateSummary(partsInspected, totalParts);

            report.IsComplete = true;

            GameLogger.Log($"[ReportService] Generated report from vehicle: {report}");

            GameLogger.Log($"[ReportService] Report ready - waiting for CustomerManager to show with rewards");

            OnReportGenerated?.Invoke(report);
            return report;
        }

        /// <summary>
        /// Processes issues by comparing assigned (actual) vs predicted (player-detected) issues.
        /// </summary>
        private void ProcessIssues(InspectionReport report, List<VehiclePart> allParts)
        {
            int totalAssigned = 0;
            int totalPredicted = 0;

            GameLogger.Log("========== INSPECTION REPORT DEBUG ==========");
            GameLogger.Log($"Total parts found: {allParts.Count}");
            int nullCount = allParts.Count(p => p == null);
            if (nullCount > 0) GameLogger.Log($"  WARNING: {nullCount} null parts!");

            GameLogger.Log("--- ASSIGNED ISSUES (Ground Truth) ---");
            foreach (var part in allParts)
            {
                if (part == null) continue;
                int count = part.assignedIssues?.Count ?? 0;
                totalAssigned += count;
                if (count > 0)
                {
                    foreach (var issue in part.assignedIssues)
                        GameLogger.Log($"  [ASSIGNED] {part.name} -> {issue.FailureName} (Level: {issue.AvailableLevel}, Tool: {issue.RequiredTool})");
                }
            }
            if (totalAssigned == 0) GameLogger.Log("  (NONE - no issues assigned to any part!)");

            GameLogger.Log("--- PREDICTED ISSUES (Player Detected) ---");
            foreach (var part in allParts)
            {
                if (part == null) continue;
                int count = part.predictedIssues?.Count ?? 0;
                totalPredicted += count;
                if (count > 0)
                {
                    foreach (var issue in part.predictedIssues)
                        GameLogger.Log($"  [PREDICTED] {part.name} -> {issue.FailureName}");
                }
            }
            if (totalPredicted == 0) GameLogger.Log("  (NONE - no issues detected by player)");
            GameLogger.Log("=============================================");

            foreach (var part in allParts)
            {
                if (part == null) continue;

                var assignedIssues = part.assignedIssues ?? new List<Issue>();
                var predictedIssues = part.predictedIssues ?? new List<Issue>();

                // Find correctly identified issues (intersection)
                foreach (var assigned in assignedIssues)
                {
                    bool wasFound = predictedIssues.Any(p => p.FailureName == assigned.FailureName ||
                                                             p.IssueId == assigned.IssueId);

                    if (wasFound)
                    {
                        report.AddFoundIssue(assigned, part);
                    }
                    else
                    {
                        report.AddMissedIssue(assigned, part);
                    }
                }

                // Find false positives (predicted but not assigned)
                foreach (var predicted in predictedIssues)
                {
                    bool isActual = assignedIssues.Any(a => a.FailureName == predicted.FailureName ||
                                                            a.IssueId == predicted.IssueId);

                    if (!isActual)
                    {
                        report.AddFalsePositive(predicted, part);
                    }
                }
            }

            GameLogger.Log("========== REPORT RESULTS ==========");
            GameLogger.Log($"  Total Assigned:     {totalAssigned}");
            GameLogger.Log($"  Total Predicted:    {totalPredicted}");
            GameLogger.Log($"  Found (correct):    {report.FoundIssues.Count}");
            GameLogger.Log($"  Missed:             {report.MissedIssues.Count}");
            GameLogger.Log($"  False Positives:    {report.FalsePositives.Count}");
            float accuracy = totalAssigned > 0 ? (float)report.FoundIssues.Count / totalAssigned * 100f : 100f;
            GameLogger.Log($"  Accuracy:           {accuracy:F1}%");
            GameLogger.Log("=====================================");
        }

        /// <inheritdoc/>
        public InspectionReport GetLatestReport()
        {
            if (_reportHistory.Count == 0) return null;
            return _reportHistory[_reportHistory.Count - 1];
        }

        /// <inheritdoc/>
        public List<InspectionReport> GetReportsByDateRange(DateTime startDate, DateTime endDate)
        {
            return _reportHistory
                .Where(r => r.InspectionDate >= startDate && r.InspectionDate <= endDate)
                .OrderBy(r => r.InspectionDate)
                .ToList();
        }

        /// <inheritdoc/>
        public List<InspectionReport> GetReportsForVehicle(string vehicleId)
        {
            return _reportHistory
                .Where(r => r.VehicleId == vehicleId)
                .OrderByDescending(r => r.InspectionDate)
                .ToList();
        }

        /// <inheritdoc/>
        public float CalculateAccuracy(Vehicle vehicle)
        {
            if (vehicle == null) return 0f;

            var allParts = GetAllVehicleParts(vehicle);
            int totalAssigned = 0;
            int totalFound = 0;

            foreach (var part in allParts)
            {
                if (part == null) continue;

                var assigned = part.assignedIssues ?? new List<Issue>();
                var predicted = part.predictedIssues ?? new List<Issue>();

                totalAssigned += assigned.Count;

                foreach (var issue in assigned)
                {
                    bool found = predicted.Any(p => p.FailureName == issue.FailureName ||
                                                    p.IssueId == issue.IssueId);
                    if (found) totalFound++;
                }
            }

            if (totalAssigned == 0) return 100f;
            return (float)totalFound / totalAssigned * 100f;
        }

        /// <inheritdoc/>
        public VehicleConditionRating EstimateCondition(Vehicle vehicle)
        {
            if (vehicle == null) return VehicleConditionRating.Unknown;

            var allParts = GetAllVehicleParts(vehicle);
            int totalIssues = 0;

            foreach (var part in allParts)
            {
                if (part?.assignedIssues != null)
                {
                    totalIssues += part.assignedIssues.Count;
                }
            }

            if (totalIssues == 0) return VehicleConditionRating.Excellent;
            if (totalIssues <= 2) return VehicleConditionRating.Good;
            if (totalIssues <= 4) return VehicleConditionRating.Fair;
            if (totalIssues <= 6) return VehicleConditionRating.Poor;
            return VehicleConditionRating.VeryPoor;
        }

        /// <inheritdoc/>
        public float EstimateValueModifier(Vehicle vehicle)
        {
            var condition = EstimateCondition(vehicle);
            return condition switch
            {
                VehicleConditionRating.Excellent => 1.0f,
                VehicleConditionRating.Good => 0.85f,
                VehicleConditionRating.Fair => 0.7f,
                VehicleConditionRating.Poor => 0.5f,
                VehicleConditionRating.VeryPoor => 0.3f,
                _ => 0.5f
            };
        }

        /// <inheritdoc/>
        public float EstimateTotalRepairCost(Vehicle vehicle)
        {
            if (vehicle == null) return 0f;

            float totalCost = 0f;
            var allParts = GetAllVehicleParts(vehicle);

            foreach (var part in allParts)
            {
                if (part?.assignedIssues == null) continue;

                foreach (var issue in part.assignedIssues)
                {
                    totalCost += EstimateIssueCost(issue, part);
                }
            }

            return totalCost;
        }

        /// <summary>
        /// Estimates the repair cost for a single issue.
        /// </summary>
        private float EstimateIssueCost(Issue issue, VehiclePart part)
        {
            float baseCost = 100f;

            switch (issue.AffectedPartType)
            {
                case AffectedPartType.Engine:
                    baseCost = 500f;
                    break;
                case AffectedPartType.Battery:
                    baseCost = 150f;
                    break;
                case AffectedPartType.Radiator:
                    baseCost = 300f;
                    break;
                case AffectedPartType.Wheel:
                    baseCost = 200f;
                    break;
                case AffectedPartType.Glass:
                    baseCost = 250f;
                    break;
                case AffectedPartType.Light:
                    baseCost = 100f;
                    break;
                case AffectedPartType.Exterior:
                    baseCost = 350f;
                    break;
            }

            baseCost *= (1 + (issue.AvailableLevel * 0.1f));
            return Mathf.Round(baseCost * 100f) / 100f;
        }

        /// <inheritdoc/>
        public void ClearHistory()
        {
            _reportHistory.Clear();
            GameLogger.Log("[ReportService] Report history cleared");
        }

        /// <inheritdoc/>
        public void RemoveReport(Guid reportId)
        {
            int removed = _reportHistory.RemoveAll(r => r.ReportId == reportId);
            if (removed > 0)
            {
                GameLogger.Log($"[ReportService] Removed report: {reportId}");
            }
        }

        /// <inheritdoc/>
        public void SaveHistory()
        {
            try
            {
                var dtoList = _reportHistory.ConvertAll(r => r.ToDTO());
                string json = JsonUtility.ToJson(new ReportHistoryWrapper { reports = dtoList }, true);
                string path = System.IO.Path.Combine(Application.persistentDataPath, "report_history.json");
                System.IO.File.WriteAllText(path, json);
                GameLogger.Log($"[ReportService] Saved {_reportHistory.Count} reports to {path}");
            }
            catch (Exception ex)
            {
                GameLogger.LogWarning($"[ReportService] Failed to save report history: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void LoadHistory()
        {
            try
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, "report_history.json");
                if (System.IO.File.Exists(path))
                {
                    string json = System.IO.File.ReadAllText(path);
                    var wrapper = JsonUtility.FromJson<ReportHistoryWrapper>(json);
                    if (wrapper?.reports != null)
                    {
                        _reportHistory.Clear();
                        // Note: DTOs would need to be converted back to full objects
                        // For now, we just clear on load as full deserialization would require more work
                        GameLogger.Log($"[ReportService] Loaded report history (metadata only)");
                    }
                }
            }
            catch (Exception ex)
            {
                GameLogger.LogWarning($"[ReportService] Failed to load report history: {ex.Message}");
            }
        }

        /// <summary>
        /// Adds a report to the history.
        /// </summary>
        private void AddReportToHistory(InspectionReport report)
        {
            if (report == null) return;

            _reportHistory.Add(report);
            OnReportGenerated?.Invoke(report);
            GameLogger.Log($"[ReportService] Added report to history. Total reports: {_reportHistory.Count}");
        }

        /// <summary>
        /// Gets all vehicle parts from a vehicle.
        /// </summary>
        private List<VehiclePart> GetAllVehicleParts(Vehicle vehicle)
        {
            var allParts = new List<VehiclePart>();

            if (vehicle == null) return allParts;

            if (vehicle.exteriorParts != null)
                allParts.AddRange(vehicle.exteriorParts);

            if (vehicle.wheels != null)
                allParts.AddRange(vehicle.wheels);

            if (vehicle.glasses != null)
                allParts.AddRange(vehicle.glasses);

            if (vehicle.lights != null)
                allParts.AddRange(vehicle.lights);

            if (vehicle.battery != null)
                allParts.Add(vehicle.battery);

            if (vehicle.engine != null)
                allParts.Add(vehicle.engine);

            if (vehicle.radiator != null)
                allParts.Add(vehicle.radiator);

            if (vehicle.exhaust != null)
                allParts.Add(vehicle.exhaust);

            return allParts;
        }

        /// <summary>
        /// Counts parts that have been inspected (have predictions).
        /// </summary>
        private int CountPartsWithPredictions(List<VehiclePart> parts)
        {
            return parts.Count(p => p?.predictedIssues != null && p.predictedIssues.Count > 0);
        }

        /// <summary>
        /// Counts parts that have either predictions or were inspected.
        /// </summary>
        private int CountInspectedParts(List<VehiclePart> parts)
        {
            // For now, count parts with predictions as inspected
            return CountPartsWithPredictions(parts);
        }
    }

    /// <summary>
    /// Wrapper class for JSON serialization of report history.
    /// </summary>
    [Serializable]
    public class ReportHistoryWrapper
    {
        public List<InspectionReportDto> reports;
    }
}
