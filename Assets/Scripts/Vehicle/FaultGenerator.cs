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
/// Extracts fault assignment logic from Vehicle.cs for better separation of concerns.
/// </summary>
public class FaultGenerator : IFaultGenerator
{
    private readonly IssueDataBase _issueDatabase;

    public int Priority => 35;

    /// <summary>
    /// Creates a new FaultGenerator instance.
    /// </summary>
    /// <param name="issueDatabase">The database of available issues</param>
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

        // Calculate weights for issue selection
        CalculateIssuePossibilityWeights(vehicle, playerLevel);

        // Get total issue count based on player level
        int totalIssueCount = GetIssueCount(playerLevel);

        // Create accident reports (which also assigns repair issues)
        int accidentCount = vehicle.AccidentReports?.Count ?? 0;

        // Assign random issues (subtracting accident-assigned issues)
        int randomIssueCount = totalIssueCount - accidentCount;
        AssignRandomIssues(vehicle, randomIssueCount, playerLevel);

        GameLogger.Log($"[FaultGenerator] Generated {totalIssueCount} faults for vehicle (accidents: {accidentCount}, random: {randomIssueCount})");
    }

    /// <summary>
    /// Assigns random issues to a vehicle based on player level.
    /// Uses weighted random selection from available issues.
    /// </summary>
    public void AssignRandomIssues(Vehicle vehicle, int playerLevel)
    {
        int issueCount = GetIssueCount(playerLevel);
        int accidentCount = vehicle.AccidentReports?.Count ?? 0;
        AssignRandomIssues(vehicle, issueCount - accidentCount, playerLevel);
    }

    /// <summary>
    /// Assigns a specific number of random issues to appropriate vehicle parts.
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

        // Gather all vehicle parts into a single list
        List<VehiclePart> allParts = GetAllVehicleParts(vehicle);

        if (allParts.Count == 0)
        {
            GameLogger.LogWarning("[FaultGenerator] No vehicle parts are defined.");
            return;
        }

        // Get available issues for the player's level
        List<Issue> availableIssues = new List<Issue>(_issueDatabase.GetAvailableForLevel(playerLevel));

        // Limit issue count to available issues
        int maxIssues = Mathf.Min(issueCount, availableIssues.Count);
        List<Issue> selectedIssues = new List<Issue>();

        // Select issues using weighted random
        for (int i = 0; i < maxIssues; i++)
        {
            if (availableIssues.Count == 0)
            {
                GameLogger.LogWarning("[FaultGenerator] There are not enough faults left.");
                break;
            }

            var selectedIssue = Utilities.WeightedRandom(availableIssues, issue => issue.PossibilityWeight);
            if (selectedIssue != null)
            {
                selectedIssues.Add(selectedIssue);
                availableIssues.Remove(selectedIssue);
            }
        }

        // Assign each selected issue to a valid part
        foreach (var issue in selectedIssues)
        {
            // Find parts that are valid for this issue
            var validParts = allParts.Where(part => issue.IsValidFor(part)).ToList();

            if (validParts.Count > 0)
            {
                // Select a random valid part
                VehiclePart selectedPart = validParts[UnityEngine.Random.Range(0, validParts.Count)];
                selectedPart.AssignIssue(issue);
                GameLogger.LogWarning($"[FaultGenerator] The {issue.FailureName} fault has been assigned to the '{selectedPart.name}' component.");
            }
            else
            {
                GameLogger.LogWarning($"[FaultGenerator] No suitable part was found for the '{issue.FailureName}' fault.");
            }
        }

        // Log if fewer issues were assigned than requested
        if (selectedIssues.Count < issueCount)
        {
            GameLogger.LogWarning($"[FaultGenerator] Not enough faults could be assigned. Only {selectedIssues.Count} fault(s) were assigned.");
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
            // Level 1-10: More easy vehicles with fewer issues
            if (random < 0.50f) return 3; // 50%
            if (random < 0.80f) return 4; // 30%
            if (random < 0.90f) return 5; // 10%
            if (random < 0.97f) return 6; // 7%
            return 7;                      // 3%
        }
        else if (level <= 20)
        {
            // Level 11-20: Medium difficulty
            if (random < 0.10f) return 3; // 10%
            if (random < 0.60f) return 4; // 50%
            if (random < 0.90f) return 5; // 30%
            if (random < 0.97f) return 6; // 7%
            return 7;                      // 3%
        }
        else if (level <= 30)
        {
            // Level 21-30: Harder vehicles
            if (random < 0.05f) return 3; // 5%
            if (random < 0.15f) return 4; // 10%
            if (random < 0.65f) return 5; // 50%
            if (random < 0.95f) return 6; // 30%
            return 7;                      // 5%
        }
        else
        {
            // Level 31+: Expert level
            if (random < 0.05f) return 3; // 5%
            if (random < 0.10f) return 4; // 5%
            if (random < 0.20f) return 5; // 10%
            if (random < 0.70f) return 6; // 50%
            return 7;                      // 30%
        }
    }

    /// <summary>
    /// Calculates possibility weights for issues based on player level and vehicle state.
    /// Higher weights make issues more likely to be selected.
    /// </summary>
    public void CalculateIssuePossibilityWeights(Vehicle vehicle, int playerLevel)
    {
        if (_issueDatabase == null) return;

        var availableIssues = _issueDatabase.GetAvailableForLevel(playerLevel);

        foreach (var issue in availableIssues)
        {
            issue.PossibilityWeight = 0;

            // Boost newly unlocked issues
            if (issue.AvailableLevel == playerLevel)
            {
                issue.PossibilityWeight += 30;
            }

            // Boost common exterior/glass/light issues if vehicle has those parts
            if (vehicle.exteriorParts != null && vehicle.exteriorParts.Count > 0 &&
                vehicle.glasses != null && vehicle.glasses.Count > 0 &&
                vehicle.lights != null && vehicle.lights.Count > 0)
            {
                if (issue.IsValidFor(vehicle.exteriorParts[0]) ||
                    issue.IsValidFor(vehicle.glasses[0]) ||
                    issue.IsValidFor(vehicle.lights[0]))
                {
                    issue.PossibilityWeight += 10;
                }
            }

            // Boost exterior issues if vehicle has accident history
            if (vehicle.AccidentReports != null && vehicle.AccidentReports.Count > 0 &&
                vehicle.exteriorParts != null && vehicle.exteriorParts.Count > 0)
            {
                if (issue.IsValidFor(vehicle.exteriorParts[0]))
                {
                    issue.PossibilityWeight += 25;
                }
            }
        }
    }

    /// <summary>
    /// Assigns repair-related issues to parts that were damaged in accidents.
    /// </summary>
    public void AssignRepairIssuesFromAccidents(Vehicle vehicle, int playerLevel)
    {
        if (vehicle == null || vehicle.AccidentReports == null || _issueDatabase == null) return;

        foreach (var report in vehicle.AccidentReports)
        {
            foreach (var damagedPart in report.DamagedParts)
            {
                foreach (var vehiclePart in vehicle.exteriorParts)
                {
                    if (vehiclePart.partPosition == damagedPart)
                    {
                        int randomSign;

                        // Check if Dent_Repaired issue is available for player level
                        Issue dentRepaired = _issueDatabase.GetByName("Dent_Repaired");
                        if (dentRepaired != null && playerLevel < dentRepaired.AvailableLevel)
                        {
                            randomSign = UnityEngine.Random.Range(0, 2);
                        }
                        else
                        {
                            randomSign = UnityEngine.Random.Range(0, 3);
                        }

                        // Assign appropriate repair issue
                        if (randomSign == 0)
                        {
                            AssignIssueToPart(vehiclePart, "Painted_Part");
                        }
                        else if (randomSign == 1)
                        {
                            AssignIssueToPart(vehiclePart, "Replaced_Part");
                        }
                        else if (randomSign == 2)
                        {
                            AssignIssueToPart(vehiclePart, "Dent_Repaired");
                        }
                    }
                }
            }
        }
    }

    #endregion

    #region Private Helpers

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

    #endregion
}
