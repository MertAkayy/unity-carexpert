using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class VehicleCoolantReservoir : VehiclePart, IInteractable, IVehicleCoolantReservoir, IReadable
{
    [Header("Coolant Reservoir Properties")]
    public int CoolantCapacity { get; set; }
    public int CoolantLevel { get; set; }
    public bool IsCracked { get; set; }

    [Header("Settings")]
    [SerializeField] private float minAcceptableCoolantLevel = 0.3f; // 30%

    private void Awake() { }

    /// <summary>
    /// Initializes the reservoir with a clean normal baseline.
    /// Issues assigned afterwards drive condition flags and abnormal values.
    /// </summary>
    public void InitializeCoolantReservoir()
    {
        // Capacity: 10–15 units (100ml each = 1.0–1.5 litres)
        CoolantCapacity = Random.Range(10, 16);

        // Start at a healthy level (70–100% of capacity)
        CoolantLevel = (int)Math.Round(Random.Range(0.7f, 1.0f) * CoolantCapacity);

        IsCracked = false;

        GameLogger.Log($"[VehicleCoolantReservoir] Initialized: CoolantLevel={CoolantLevel}/{CoolantCapacity}");
    }

    public override void AssignIssue(Issue issue)
    {
        base.AssignIssue(issue);

        switch (issue.FailureName)
        {
            case "Low_Coolant_Level":
                CoolantLevel = (int)Math.Round(Random.Range(0.05f, 0.28f) * CoolantCapacity);
                GameLogger.Log($"[VehicleCoolantReservoir] Low_Coolant_Level assigned — Level={CoolantLevel}/{CoolantCapacity} ({GetCoolantLevelPercentage() * 100f:F0}%)");
                break;

            case "Coolant_Reservoir_Crack":
                IsCracked = true;
                CoolantLevel = (int)Math.Round(Random.Range(0.3f, 0.6f) * CoolantCapacity);
                GameLogger.Log($"[VehicleCoolantReservoir] Coolant_Reservoir_Crack assigned — IsCracked=true, Level={CoolantLevel}/{CoolantCapacity}");
                break;
        }
    }

    public float GetCoolantLevelPercentage()
    {
        return (float)CoolantLevel / CoolantCapacity;
    }

    public bool IsCoolantLevelLow()
    {
        return GetCoolantLevelPercentage() < minAcceptableCoolantLevel;
    }

    public void Interact()
    {
        GameLogger.Log("[VehicleCoolantReservoir] Interacting with coolant reservoir");
    }

    public void Read()
    {
        string info = GetReservoirInfoString();
        GameLogger.Log($"[VehicleCoolantReservoir] Reading: {info}");
        DetectIssuesFromRead();
        ShowReadResult(info);
    }

    private void DetectIssuesFromRead()
    {
        VehicleManager vehicleManager = FindObjectOfType<VehicleManager>();
        if (vehicleManager?.IssueDatabase == null) return;

        if (IsCoolantLevelLow())
        {
            Issue issue = vehicleManager.IssueDatabase.GetByName("Low_Coolant_Level");
            if (issue != null && !predictedIssues.Contains(issue))
            {
                predictedIssues.Add(issue);
                GameLogger.Log($"[VehicleCoolantReservoir] 'Low_Coolant_Level' added to predicted issues ({GetCoolantLevelPercentage() * 100f:F0}% coolant)");
            }
        }
    }

    private string GetReservoirInfoString()
    {
        float pct = GetCoolantLevelPercentage() * 100f;
        return $"COOLANT RESERVOIR\n" +
               $"Level: {CoolantLevel}/{CoolantCapacity} ({pct:F0}%)\n" +
               $"Status: {(IsCoolantLevelLow() ? "LOW" : "OK")}\n" +
               $"{(IsCracked ? "[RESERVOIR CRACKED]" : "")}";
    }
}
