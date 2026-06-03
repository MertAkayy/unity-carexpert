using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using PlayerScripts;
using UnityEngine;

/// <summary>
/// Interface for the FaultGenerator system.
/// </summary>
public interface IFaultGenerator : ISystem
{
    void AssignRandomIssues(Vehicle vehicle, int issueCount, int playerLevel);
    void AssignRandomIssues(Vehicle vehicle, int playerLevel);
    int GetIssueCount(int playerLevel);
    void CalculateIssuePossibilityWeights(Vehicle vehicle, int playerLevel);
    void AssignRepairIssuesFromAccidents(Vehicle vehicle, int playerLevel);
    void GenerateFaultsForVehicle(Vehicle vehicle, int playerLevel);
}

/// <summary>
/// Fault generation service responsible for assigning issues to vehicle parts.
///
/// Key behaviours:
/// - Preserves original ScriptableObject weights (never mutates PossibilityWeight).
/// - Computes runtime weights in a local dictionary per vehicle.
/// - Guarantees every newly unlocked issue appears at least once before being
///   treated as a regular pool member.
/// - Persistent cosmetic issues (scratches, cracks, corrosion, etc.) receive a
///   recurring weight boost so they appear naturally on most vehicles.
/// - Accident report system is untouched.
/// </summary>
public class FaultGenerator : IFaultGenerator
{
    private readonly IssueDataBase _issueDatabase;

    // ── New-issue guarantee tracking ──────────────────────────────
    private int _lastTrackedLevel = -1;
    private readonly HashSet<string> _unseenNewIssues = new HashSet<string>();

    // ── Persistent cosmetic issues that keep recurring after unlock ─
    private static readonly HashSet<string> PersistentCosmetics = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        "Glass_Crack",
        "Glass_Scratch",
        "Window_Regulator_Failure",
        "Surface_Scratch",
        "Surface_Corrosion",
        "Painted_Part",
        "Surface_Sun_Damage",
        "Glass_Tint_Peel_Off",
        "Non_OEM_Part",
        "Replaced_Part"
    };

    public int Priority => 35;

    public FaultGenerator(IssueDataBase issueDatabase)
    {
        _issueDatabase = issueDatabase;
    }

    #region ISystem Implementation

    public void OnRegistered()
    {
        GameLogger.Log("[FaultGenerator] Registered with ServiceLocator");
    }

    public void Initialize()
    {
        if (_issueDatabase == null)
        {
            GameLogger.LogWarning("[FaultGenerator] IssueDatabase is null, fault generation may not work correctly");
        }
        GameLogger.Log("[FaultGenerator] Initialized");
    }

    public void Shutdown()
    {
        GameLogger.Log("[FaultGenerator] Shutdown complete");
    }

    #endregion

    #region IFaultGenerator Implementation

    /// <summary>
    /// Main entry point for generating faults on a vehicle.
    /// Combines accident-based issues with random issues.
    /// </summary>
    public void GenerateFaultsForVehicle(Vehicle vehicle, int playerLevel)
    {
        if (vehicle == null)
        {
            GameLogger.LogError("[FaultGenerator] Cannot generate faults: vehicle is null");
            return;
        }

        if (_issueDatabase == null || _issueDatabase.issues == null || _issueDatabase.issues.Count == 0)
        {
            GameLogger.LogWarning("[FaultGenerator] IssueDatabase not defined or empty.");
            return;
        }

        // Update new-issue tracker when level changes
        UpdateNewIssueTracker(playerLevel);

        // Get total issue count based on player level
        int totalIssueCount = GetIssueCount(playerLevel);

        // Accident-assigned issues count
        int accidentCount = vehicle.AccidentReports?.Count ?? 0;

        // Assign random issues (subtracting accident-assigned issues)
        int randomIssueCount = Mathf.Max(0, totalIssueCount - accidentCount);
        AssignRandomIssues(vehicle, randomIssueCount, playerLevel);

        GameLogger.Log($"[FaultGenerator] Generated {totalIssueCount} faults for vehicle " +
                        $"(accidents: {accidentCount}, random: {randomIssueCount}, " +
                        $"unseen new issues remaining: {_unseenNewIssues.Count})");
    }

    /// <summary>
    /// Assigns random issues to a vehicle based on player level.
    /// </summary>
    public void AssignRandomIssues(Vehicle vehicle, int playerLevel)
    {
        int issueCount = GetIssueCount(playerLevel);
        int accidentCount = vehicle.AccidentReports?.Count ?? 0;
        AssignRandomIssues(vehicle, Mathf.Max(0, issueCount - accidentCount), playerLevel);
    }

    /// <summary>
    /// Assigns a specific number of random issues to appropriate vehicle parts.
    /// Guarantees unseen new issues appear first, then fills with weighted random.
    /// </summary>
    public void AssignRandomIssues(Vehicle vehicle, int issueCount, int playerLevel)
    {
        if (_issueDatabase == null || _issueDatabase.issues == null || _issueDatabase.issues.Count == 0)
        {
            GameLogger.LogWarning("[FaultGenerator] IssueDatabase not defined or empty.");
            return;
        }

        if (vehicle == null)
        {
            GameLogger.LogError("[FaultGenerator] Vehicle is null, cannot assign issues.");
            return;
        }

        List<VehiclePart> allParts = GetAllVehicleParts(vehicle);
        if (allParts.Count == 0)
        {
            GameLogger.LogWarning("[FaultGenerator] No vehicle parts are defined.");
            return;
        }

        // Get available issues for the player's level
        List<Issue> availableIssues = new List<Issue>(_issueDatabase.GetAvailableForLevel(playerLevel));

        // Compute runtime weights (never touches ScriptableObject data)
        Dictionary<string, int> computedWeights = ComputeWeights(availableIssues, vehicle, playerLevel);

        // Log available issues with computed weights
        string issueNames = string.Join(", ", availableIssues.ConvertAll(i =>
            $"{i.FailureName}(w:{GetWeight(computedWeights, i.FailureName)})"));
        GameLogger.Log($"[FaultGenerator] Available issues ({availableIssues.Count}): {issueNames}");

        int maxIssues = Mathf.Min(issueCount, availableIssues.Count);
        List<Issue> selectedIssues = new List<Issue>();

        // ── Phase 1: Force unseen new issues ──────────────────────
        if (_unseenNewIssues.Count > 0)
        {
            // Pick up to maxIssues unseen new issues that have valid parts on this vehicle
            var unseenCandidates = availableIssues
                .Where(i => _unseenNewIssues.Contains(i.FailureName))
                .Where(i => allParts.Any(p => i.IsValidFor(p)))
                .ToList();

            // Shuffle so we don't always pick the same ones first
            ShuffleList(unseenCandidates);

            foreach (var issue in unseenCandidates)
            {
                if (selectedIssues.Count >= maxIssues) break;

                selectedIssues.Add(issue);
                availableIssues.Remove(issue);
                _unseenNewIssues.Remove(issue.FailureName);

                GameLogger.Log($"[FaultGenerator] Guaranteed new issue: {issue.FailureName}");
            }
        }

        // ── Phase 2: Fill remaining slots with weighted random ────
        int remainingSlots = maxIssues - selectedIssues.Count;
        for (int i = 0; i < remainingSlots; i++)
        {
            if (availableIssues.Count == 0)
            {
                GameLogger.LogWarning("[FaultGenerator] There are not enough faults left.");
                break;
            }

            var selectedIssue = Utilities.WeightedRandom(
                availableIssues,
                issue => GetWeight(computedWeights, issue.FailureName));

            if (selectedIssue != null)
            {
                selectedIssues.Add(selectedIssue);
                availableIssues.Remove(selectedIssue);
            }
        }

        // ── Assign each selected issue to a valid part ────────────
        foreach (var issue in selectedIssues)
        {
            var validParts = allParts.Where(part => issue.IsValidFor(part)).ToList();

            if (validParts.Count > 0)
            {
                VehiclePart selectedPart = validParts[UnityEngine.Random.Range(0, validParts.Count)];
                selectedPart.AssignIssue(issue);
                GameLogger.Log($"[FaultGenerator] Assigned {issue.FailureName} to '{selectedPart.name}'");
            }
            else
            {
                GameLogger.LogWarning($"[FaultGenerator] No suitable part for '{issue.FailureName}'");
            }
        }

        if (selectedIssues.Count < issueCount)
        {
            GameLogger.LogWarning($"[FaultGenerator] Only {selectedIssues.Count}/{issueCount} fault(s) were assigned.");
        }
    }

    /// <summary>
    /// Calculates the number of issues based on player level.
    /// Uses probability distributions that vary by level.
    /// </summary>
    public int GetIssueCount(int level)
    {
        float random = UnityEngine.Random.value;

        if (level <= 10)
        {
            if (random < 0.50f) return 3;
            if (random < 0.80f) return 4;
            if (random < 0.90f) return 5;
            if (random < 0.97f) return 6;
            return 7;
        }
        else if (level <= 20)
        {
            if (random < 0.10f) return 3;
            if (random < 0.60f) return 4;
            if (random < 0.90f) return 5;
            if (random < 0.97f) return 6;
            return 7;
        }
        else if (level <= 30)
        {
            if (random < 0.05f) return 3;
            if (random < 0.15f) return 4;
            if (random < 0.65f) return 5;
            if (random < 0.95f) return 6;
            return 7;
        }
        else
        {
            if (random < 0.05f) return 3;
            if (random < 0.10f) return 4;
            if (random < 0.20f) return 5;
            if (random < 0.70f) return 6;
            return 7;
        }
    }

    /// <summary>
    /// Legacy method kept for interface compatibility.
    /// Weight calculation is now done inside AssignRandomIssues via ComputeWeights().
    /// This no longer mutates ScriptableObject data.
    /// </summary>
    public void CalculateIssuePossibilityWeights(Vehicle vehicle, int playerLevel)
    {
        // Intentionally empty — weights are now computed locally in ComputeWeights()
        // without modifying ScriptableObject data. This method exists only for
        // interface compatibility.
    }

    /// <summary>
    /// Assigns repair-related issues to parts that were damaged in accidents.
    /// This system is unchanged — accident reports continue to work as before.
    /// </summary>
    public void AssignRepairIssuesFromAccidents(Vehicle vehicle, int playerLevel)
    {
        if (vehicle == null || vehicle.AccidentReports == null || _issueDatabase == null) return;

        // Build list of repair issues available at the player's current level
        var repairCandidates = new List<string>();

        Issue replacedPart = _issueDatabase.GetByName("Replaced_Part");
        if (replacedPart != null && playerLevel >= replacedPart.AvailableLevel)
            repairCandidates.Add("Replaced_Part");

        Issue paintedPart = _issueDatabase.GetByName("Painted_Part");
        if (paintedPart != null && playerLevel >= paintedPart.AvailableLevel)
            repairCandidates.Add("Painted_Part");

        Issue dentRepaired = _issueDatabase.GetByName("Dent_Repaired");
        if (dentRepaired != null && playerLevel >= dentRepaired.AvailableLevel)
            repairCandidates.Add("Dent_Repaired");

        if (repairCandidates.Count == 0)
        {
            GameLogger.Log("[FaultGenerator] No repair issues available at current level — skipping accident repairs.");
            return;
        }

        foreach (var report in vehicle.AccidentReports)
        {
            foreach (var damagedPart in report.DamagedParts)
            {
                foreach (var vehiclePart in vehicle.exteriorParts)
                {
                    if (vehiclePart.partPosition == damagedPart)
                    {
                        string selected = repairCandidates[UnityEngine.Random.Range(0, repairCandidates.Count)];
                        AssignIssueToPart(vehiclePart, selected);
                    }
                }
            }
        }
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Computes runtime weights for issue selection WITHOUT modifying ScriptableObject data.
    /// Weight formula:
    ///   base = original PossibilityWeight from ScriptableObject (preserved)
    ///   + 30 if the issue is newly unlocked at this level
    ///   + 15 if the issue is a persistent cosmetic (scratches, cracks, corrosion, etc.)
    ///   + 10 if the issue was unlocked in the last 3 levels (recent historical)
    ///   + 10 if the issue targets exterior/glass/light and vehicle has those parts
    ///   + 25 if the vehicle has accident history and issue targets exterior
    ///   minimum 1 for any available issue (so nothing is completely dead)
    /// </summary>
    private Dictionary<string, int> ComputeWeights(List<Issue> availableIssues, Vehicle vehicle, int playerLevel)
    {
        var weights = new Dictionary<string, int>();

        bool hasExterior = vehicle.exteriorParts != null && vehicle.exteriorParts.Count > 0;
        bool hasGlass = vehicle.glasses != null && vehicle.glasses.Count > 0;
        bool hasLights = vehicle.lights != null && vehicle.lights.Count > 0;
        bool hasAccidents = vehicle.AccidentReports != null && vehicle.AccidentReports.Count > 0;

        foreach (var issue in availableIssues)
        {
            // Start with the ORIGINAL weight from ScriptableObject (not overwritten)
            int weight = issue.PossibilityWeight;

            // Boost newly unlocked issues (highest priority)
            if (issue.AvailableLevel == playerLevel)
            {
                weight += 30;
            }

            // Boost persistent cosmetic issues (always recurring)
            if (PersistentCosmetics.Contains(issue.FailureName))
            {
                weight += 15;
            }

            // Boost recent historical issues (unlocked in last 3 levels)
            int levelDiff = playerLevel - issue.AvailableLevel;
            if (levelDiff > 0 && levelDiff <= 3)
            {
                weight += 10;
            }

            // Boost exterior/glass/light issues if vehicle has those parts
            if ((hasExterior && issue.IsValidFor(vehicle.exteriorParts[0])) ||
                (hasGlass && issue.IsValidFor(vehicle.glasses[0])) ||
                (hasLights && issue.IsValidFor(vehicle.lights[0])))
            {
                weight += 10;
            }

            // Boost exterior issues if vehicle has accident history
            if (hasAccidents && hasExterior && issue.IsValidFor(vehicle.exteriorParts[0]))
            {
                weight += 25;
            }

            // Ensure every available issue has at least weight 1
            // so nothing is completely impossible to appear
            weights[issue.FailureName] = Mathf.Max(1, weight);
        }

        return weights;
    }

    /// <summary>
    /// Tracks which new issues have been seen when the player level changes.
    /// When the player reaches a new level, all issues unlocked at that level
    /// are added to the unseen set and must appear on cars before being optional.
    /// </summary>
    private void UpdateNewIssueTracker(int playerLevel)
    {
        if (playerLevel == _lastTrackedLevel) return;

        _lastTrackedLevel = playerLevel;
        _unseenNewIssues.Clear();

        if (_issueDatabase == null) return;

        // Find all issues that unlock at exactly this level
        foreach (var issue in _issueDatabase.issues)
        {
            if (issue.AvailableLevel == playerLevel)
            {
                _unseenNewIssues.Add(issue.FailureName);
            }
        }

        if (_unseenNewIssues.Count > 0)
        {
            GameLogger.Log($"[FaultGenerator] Level {playerLevel}: {_unseenNewIssues.Count} new issues to guarantee: " +
                           string.Join(", ", _unseenNewIssues));
        }
    }

    /// <summary>
    /// Gets all vehicle parts from the vehicle into a single list.
    /// </summary>
    private List<VehiclePart> GetAllVehicleParts(Vehicle vehicle)
    {
        List<VehiclePart> allParts = new List<VehiclePart>();

        if (vehicle.exteriorParts != null) allParts.AddRange(vehicle.exteriorParts);
        if (vehicle.wheels != null) allParts.AddRange(vehicle.wheels);
        if (vehicle.glasses != null) allParts.AddRange(vehicle.glasses);
        if (vehicle.lights != null) allParts.AddRange(vehicle.lights);

        if (vehicle.battery != null) allParts.Add(vehicle.battery);
        if (vehicle.engine != null) allParts.Add(vehicle.engine);
        if (vehicle.radiator != null) allParts.Add(vehicle.radiator);
        if (vehicle.exhaust != null) allParts.Add(vehicle.exhaust);
        if (vehicle.coolantReservoir != null) allParts.Add(vehicle.coolantReservoir);

        return allParts;
    }

    /// <summary>
    /// Assigns an issue to a part by name lookup.
    /// </summary>
    private void AssignIssueToPart(VehiclePart part, string issueName)
    {
        if (part == null || _issueDatabase == null) return;

        Issue issue = _issueDatabase.GetByName(issueName);
        if (issue != null)
        {
            part.AssignIssue(issue);
            GameLogger.Log($"[FaultGenerator] Assigned {issueName} to {part.name}");
        }
        else
        {
            GameLogger.LogWarning($"[FaultGenerator] Issue '{issueName}' not found in database");
        }
    }

    /// <summary>
    /// Safe dictionary lookup with default value (compatible with .NET Standard 2.0).
    /// </summary>
    private static int GetWeight(Dictionary<string, int> dict, string key)
    {
        return dict.TryGetValue(key, out int value) ? value : 1;
    }

    /// <summary>
    /// Fisher-Yates shuffle for randomizing lists.
    /// </summary>
    private void ShuffleList<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    #endregion
}
