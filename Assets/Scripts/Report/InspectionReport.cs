using System;
using System.Collections.Generic;
using UnityEngine;

namespace Report
{
    /// <summary>
    /// Enum representing the overall condition rating of a vehicle.
    /// </summary>
    public enum VehicleConditionRating
    {
        /// <summary>Excellent condition - no issues found.</summary>
        Excellent = 5,

        /// <summary>Good condition - minor issues only.</summary>
        Good = 4,

        /// <summary>Fair condition - some issues requiring attention.</summary>
        Fair = 3,

        /// <summary>Poor condition - significant issues found.</summary>
        Poor = 2,

        /// <summary>Very Poor condition - major problems detected.</summary>
        VeryPoor = 1,

        /// <summary>Uninspected or unknown condition.</summary>
        Unknown = 0
    }

    /// <summary>
    /// Represents a single issue entry in an inspection report.
    /// </summary>
    [Serializable]
    public class ReportIssueEntry
    {
        public string IssueName { get; set; }
        public string Description { get; set; }
        public AffectedPartType PartType { get; set; }
        public string PartName { get; set; }
        public VehiclePartUniqueType PartUniqueType { get; set; }
        public Tool RequiredTool { get; set; }
        public int SeverityLevel { get; set; }
        public float EstimatedRepairCost { get; set; }
        public string ObdCode { get; set; }

        /// <summary>
        /// Creates a ReportIssueEntry from an Issue instance.
        /// </summary>
        public static ReportIssueEntry FromIssue(Issue issue, VehiclePart part)
        {
            return new ReportIssueEntry
            {
                IssueName = issue.FailureName,
                Description = issue.Description,
                PartType = issue.AffectedPartType,
                PartName = part?.name ?? "Unknown",
                PartUniqueType = part?.partUniqueType ?? VehiclePartUniqueType.Engine,
                RequiredTool = issue.RequiredTool,
                SeverityLevel = issue.AvailableLevel,
                EstimatedRepairCost = EstimateRepairCost(issue, part),
                ObdCode = issue.ObdCode
            };
        }

        /// <summary>
        /// Estimates repair cost based on issue type and part.
        /// </summary>
        private static float EstimateRepairCost(Issue issue, VehiclePart part)
        {
            // Base cost estimation logic
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

            // Adjust based on severity level
            baseCost *= (1 + (issue.AvailableLevel * 0.1f));

            return Mathf.Round(baseCost * 100f) / 100f;
        }
    }

    /// <summary>
    /// Summary statistics for an inspection report.
    /// </summary>
    [Serializable]
    public class InspectionSummary
    {
        public int TotalIssuesAssigned { get; set; }
        public int TotalIssuesFound { get; set; }
        public int IssuesMissed { get; set; }
        public int FalsePositives { get; set; }
        public int PartsInspected { get; set; }
        public int TotalParts { get; set; }
        public float AccuracyPercentage { get; set; }
        public float CompletionPercentage { get; set; }
        public VehicleConditionRating ConditionRating { get; set; }
        public float TotalEstimatedRepairCost { get; set; }
        public float EstimatedVehicleValueModifier { get; set; }

        /// <summary>
        /// Returns a human-readable summary string.
        /// </summary>
        public override string ToString()
        {
            return $"[Summary] Accuracy: {AccuracyPercentage:F1}% | Found: {TotalIssuesFound}/{TotalIssuesAssigned} | Condition: {ConditionRating}";
        }
    }

    /// <summary>
    /// Formal inspection report data structure.
    /// Contains all information about a completed vehicle inspection.
    /// </summary>
    [Serializable]
    public class InspectionReport
    {
        /// <summary>
        /// Unique identifier for this report.
        /// </summary>
        public Guid ReportId { get; private set; }

        /// <summary>
        /// Reference to the inspected vehicle.
        /// </summary>
        public Vehicle InspectedVehicle { get; private set; }

        /// <summary>
        /// Vehicle ID for serialization purposes.
        /// </summary>
        public string VehicleId { get; private set; }

        /// <summary>
        /// Vehicle registration info for the report.
        /// </summary>
        public VehicleRegistration Registration { get; private set; }

        /// <summary>
        /// Date and time when the inspection was performed.
        /// </summary>
        public DateTime InspectionDate { get; set; }

        /// <summary>
        /// Duration of the inspection in seconds.
        /// </summary>
        public float InspectionDurationSeconds { get; set; }

        /// <summary>
        /// Issues correctly identified by the player.
        /// </summary>
        public List<ReportIssueEntry> FoundIssues { get; private set; }

        /// <summary>
        /// Issues that exist but were not detected by the player.
        /// </summary>
        public List<ReportIssueEntry> MissedIssues { get; private set; }

        /// <summary>
        /// Issues incorrectly reported by the player (false positives).
        /// </summary>
        public List<ReportIssueEntry> FalsePositives { get; private set; }

        /// <summary>
        /// All issues assigned to the vehicle (ground truth).
        /// </summary>
        public List<ReportIssueEntry> AllAssignedIssues { get; private set; }

        /// <summary>
        /// Summary statistics for this report.
        /// </summary>
        public InspectionSummary Summary { get; private set; }

        /// <summary>
        /// The player level at the time of inspection.
        /// </summary>
        public int PlayerLevel { get; set; }

        /// <summary>
        /// Experience points earned from this inspection.
        /// </summary>
        public int ExperienceEarned { get; set; }

        /// <summary>
        /// Money earned from this inspection.
        /// </summary>
        public float MoneyEarned { get; set; }

        /// <summary>
        /// Whether this report represents a completed inspection.
        /// </summary>
        public bool IsComplete { get; set; }

        /// <summary>
        /// Additional notes or comments about the inspection.
        /// </summary>
        public string Notes { get; set; }

        #region Convenience Properties (access Summary data)

        /// <summary>
        /// Accuracy percentage from the summary.
        /// </summary>
        public float AccuracyPercentage => Summary?.AccuracyPercentage ?? 0f;

        /// <summary>
        /// Condition rating from the summary.
        /// </summary>
        public VehicleConditionRating ConditionRating => Summary?.ConditionRating ?? VehicleConditionRating.Unknown;

        /// <summary>
        /// Number of issues found.
        /// </summary>
        public int FoundIssuesCount => Summary?.TotalIssuesFound ?? 0;

        /// <summary>
        /// Number of issues missed.
        /// </summary>
        public int MissedIssuesCount => Summary?.IssuesMissed ?? 0;

        /// <summary>
        /// Number of false positives.
        /// </summary>
        public int FalsePositivesCount => Summary?.FalsePositives ?? 0;

        /// <summary>
        /// Total estimated repair cost.
        /// </summary>
        public float EstimatedRepairCost => Summary?.TotalEstimatedRepairCost ?? 0f;

        #endregion

        /// <summary>
        /// Creates a new empty inspection report.
        /// </summary>
        public InspectionReport()
        {
            ReportId = Guid.NewGuid();
            InspectionDate = DateTime.Now;
            FoundIssues = new List<ReportIssueEntry>();
            MissedIssues = new List<ReportIssueEntry>();
            FalsePositives = new List<ReportIssueEntry>();
            AllAssignedIssues = new List<ReportIssueEntry>();
            Summary = new InspectionSummary();
            Notes = string.Empty;
        }

        /// <summary>
        /// Creates a new inspection report for the specified vehicle.
        /// </summary>
        /// <param name="vehicle">The vehicle that was inspected.</param>
        /// <returns>A new InspectionReport instance.</returns>
        public static InspectionReport CreateForVehicle(Vehicle vehicle)
        {
            var report = new InspectionReport
            {
                InspectedVehicle = vehicle,
                VehicleId = vehicle?.VehicleId.ToString() ?? Guid.Empty.ToString(),
                Registration = vehicle?.Registration
            };

            return report;
        }

        /// <summary>
        /// Adds a found issue to the report.
        /// </summary>
        public void AddFoundIssue(Issue issue, VehiclePart part)
        {
            var entry = ReportIssueEntry.FromIssue(issue, part);
            FoundIssues.Add(entry);
            AllAssignedIssues.Add(entry);
        }

        /// <summary>
        /// Adds a missed issue to the report.
        /// </summary>
        public void AddMissedIssue(Issue issue, VehiclePart part)
        {
            var entry = ReportIssueEntry.FromIssue(issue, part);
            MissedIssues.Add(entry);
            AllAssignedIssues.Add(entry);
        }

        /// <summary>
        /// Adds a false positive to the report.
        /// </summary>
        public void AddFalsePositive(Issue issue, VehiclePart part)
        {
            var entry = ReportIssueEntry.FromIssue(issue, part);
            FalsePositives.Add(entry);
        }

        /// <summary>
        /// Calculates the summary statistics for this report.
        /// </summary>
        public void CalculateSummary(int partsInspected, int totalParts)
        {
            Summary.TotalIssuesAssigned = AllAssignedIssues.Count;
            Summary.TotalIssuesFound = FoundIssues.Count;
            Summary.IssuesMissed = MissedIssues.Count;
            Summary.FalsePositives = FalsePositives.Count;
            Summary.PartsInspected = partsInspected;
            Summary.TotalParts = totalParts;

            // Calculate accuracy percentage
            if (Summary.TotalIssuesAssigned > 0)
            {
                Summary.AccuracyPercentage = (float)Summary.TotalIssuesFound / Summary.TotalIssuesAssigned * 100f;
            }
            else
            {
                Summary.AccuracyPercentage = 100f; // No issues = perfect score
            }

            // Calculate completion percentage
            if (Summary.TotalParts > 0)
            {
                Summary.CompletionPercentage = (float)Summary.PartsInspected / Summary.TotalParts * 100f;
            }

            // Calculate total estimated repair cost
            Summary.TotalEstimatedRepairCost = 0f;
            foreach (var issue in FoundIssues)
            {
                Summary.TotalEstimatedRepairCost += issue.EstimatedRepairCost;
            }

            // Determine condition rating
            Summary.ConditionRating = DetermineConditionRating();

            // Calculate vehicle value modifier based on condition
            Summary.EstimatedVehicleValueModifier = CalculateValueModifier();

            // Calculate rewards
            CalculateRewards();
        }

        /// <summary>
        /// Determines the vehicle condition rating based on found issues.
        /// </summary>
        private VehicleConditionRating DetermineConditionRating()
        {
            int issueCount = Summary.TotalIssuesFound;

            if (issueCount == 0 && Summary.TotalIssuesAssigned == 0)
                return VehicleConditionRating.Excellent;

            if (issueCount == 0)
                return VehicleConditionRating.Good;

            if (issueCount <= 2)
                return VehicleConditionRating.Good;

            if (issueCount <= 4)
                return VehicleConditionRating.Fair;

            if (issueCount <= 6)
                return VehicleConditionRating.Poor;

            return VehicleConditionRating.VeryPoor;
        }

        /// <summary>
        /// Calculates the vehicle value modifier based on condition.
        /// </summary>
        private float CalculateValueModifier()
        {
            return Summary.ConditionRating switch
            {
                VehicleConditionRating.Excellent => 1.0f,
                VehicleConditionRating.Good => 0.85f,
                VehicleConditionRating.Fair => 0.7f,
                VehicleConditionRating.Poor => 0.5f,
                VehicleConditionRating.VeryPoor => 0.3f,
                _ => 0.5f
            };
        }

        /// <summary>
        /// Calculates the rewards (XP and money) for this inspection.
        /// </summary>
        private void CalculateRewards()
        {
            // Base XP for completing an inspection
            int baseXP = 50;

            // Bonus XP for accuracy
            float accuracyBonus = Summary.AccuracyPercentage / 100f * 100f;

            // Bonus XP for completion
            float completionBonus = Summary.CompletionPercentage / 100f * 50f;

            // Penalty for false positives
            float falsePositivePenalty = Summary.FalsePositives * 10f;

            ExperienceEarned = Mathf.RoundToInt(baseXP + accuracyBonus + completionBonus - falsePositivePenalty);
            ExperienceEarned = Mathf.Max(0, ExperienceEarned);

            // Base money reward
            float baseMoney = 100f;

            // Bonus for issues found
            float issueBonus = Summary.TotalIssuesFound * 25f;

            // Penalty for missed issues
            float missedPenalty = Summary.IssuesMissed * 15f;

            MoneyEarned = Mathf.Max(0, baseMoney + issueBonus - missedPenalty);
        }

        /// <summary>
        /// Converts this report to a DTO for serialization.
        /// </summary>
        public InspectionReportDto ToDTO()
        {
            return new InspectionReportDto
            {
                reportId = ReportId.ToString(),
                vehicleId = VehicleId,
                inspectionDate = InspectionDate.ToString("o"),
                inspectionDurationSeconds = InspectionDurationSeconds,
                playerLevel = PlayerLevel,
                experienceEarned = ExperienceEarned,
                moneyEarned = MoneyEarned,
                isComplete = IsComplete,
                notes = Notes,
                summary = new InspectionSummaryDto
                {
                    totalIssuesAssigned = Summary.TotalIssuesAssigned,
                    totalIssuesFound = Summary.TotalIssuesFound,
                    issuesMissed = Summary.IssuesMissed,
                    falsePositives = Summary.FalsePositives,
                    accuracyPercentage = Summary.AccuracyPercentage,
                    conditionRating = (int)Summary.ConditionRating,
                    totalEstimatedRepairCost = Summary.TotalEstimatedRepairCost
                },
                foundIssues = FoundIssues.ConvertAll(i => IssueEntryToDTO(i)),
                missedIssues = MissedIssues.ConvertAll(i => IssueEntryToDTO(i)),
                falsePositives = FalsePositives.ConvertAll(i => IssueEntryToDTO(i))
            };
        }

        private ReportIssueEntryDto IssueEntryToDTO(ReportIssueEntry entry)
        {
            return new ReportIssueEntryDto
            {
                issueName = entry.IssueName,
                description = entry.Description,
                partType = entry.PartType.ToString(),
                partName = entry.PartName,
                requiredTool = entry.RequiredTool.ToString(),
                estimatedRepairCost = entry.EstimatedRepairCost
            };
        }

        /// <summary>
        /// Returns a summary string of this report.
        /// </summary>
        public override string ToString()
        {
            return $"[InspectionReport] {InspectionDate:yyyy-MM-dd HH:mm} | Vehicle: {Registration?.PlateNumber ?? "Unknown"} | Accuracy: {Summary.AccuracyPercentage:F1}% | Rating: {Summary.ConditionRating}";
        }
    }

    /// <summary>
    /// Data Transfer Object for serialization of InspectionReport.
    /// </summary>
    [Serializable]
    public class InspectionReportDto
    {
        public string reportId;
        public string vehicleId;
        public string inspectionDate;
        public float inspectionDurationSeconds;
        public int playerLevel;
        public int experienceEarned;
        public float moneyEarned;
        public bool isComplete;
        public string notes;
        public InspectionSummaryDto summary;
        public List<ReportIssueEntryDto> foundIssues;
        public List<ReportIssueEntryDto> missedIssues;
        public List<ReportIssueEntryDto> falsePositives;
    }

    /// <summary>
    /// Data Transfer Object for InspectionSummary.
    /// </summary>
    [Serializable]
    public class InspectionSummaryDto
    {
        public int totalIssuesAssigned;
        public int totalIssuesFound;
        public int issuesMissed;
        public int falsePositives;
        public float accuracyPercentage;
        public int conditionRating;
        public float totalEstimatedRepairCost;
    }

    /// <summary>
    /// Data Transfer Object for ReportIssueEntry.
    /// </summary>
    [Serializable]
    public class ReportIssueEntryDto
    {
        public string issueName;
        public string description;
        public string partType;
        public string partName;
        public string requiredTool;
        public float estimatedRepairCost;
    }
}
