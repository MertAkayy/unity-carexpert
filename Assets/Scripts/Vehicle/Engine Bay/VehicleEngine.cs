using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class VehicleEngine : VehiclePart, IInteractable, IVehicleEngine, IReadable
{
    [Header("Engine Specifications")]
    public FuelType fuelType;
    public double EngineCapacity { get; set; }
    public int MaxHorsePower { get; set; }
    public int Performance { get; set; }
    public int OilCapacity { get; set; }
    public int OilLevel { get; set; }
    public string SerialNumber { get; set; }
    public bool isWorking = true;

    [Header("Engine Condition")]
    public bool HasOilLeak { get; set; }
    public bool HasCracks { get; set; }
    public bool BeltsAged { get; set; }
    public bool CoolantReservoirLow { get; set; }
    public bool VacuumTestFailed { get; set; }

    [Header("Settings")]
    [SerializeField] private float minAcceptableOilLevel = 0.5f; // 50%

    public VehicleEngine()
    {
    }

    private void Awake()
    {
        // Initialize on awake for proper setup
    }

    /// <summary>
    /// Initializes the engine with a clean normal baseline.
    /// Issues assigned afterwards drive condition flags and abnormal values.
    /// </summary>
    public void InitializeEngine()
    {
        // Oil capacity: 4.0–6.0 litres stored in 100ml units (40–60)
        OilCapacity = Random.Range(40, 61);

        // Start with a full/healthy oil level (80–100% of capacity)
        OilLevel = (int)Math.Round(Random.Range(0.8f, 1.0f) * OilCapacity);

        // All condition flags start clean — issues will set them
        HasOilLeak = false;
        HasCracks = false;
        BeltsAged = false;
        CoolantReservoirLow = false;
        VacuumTestFailed = false;
        isWorking = true;

        if (string.IsNullOrEmpty(SerialNumber))
            SerialNumber = GenerateSerialNumber();

        GameLogger.Log($"[VehicleEngine] Initialized: OilLevel={OilLevel}/{OilCapacity}, Serial={SerialNumber}");
    }

    public override void AssignIssue(Issue issue)
    {
        base.AssignIssue(issue);

        if (issue.FailureName == "Low_Oil_Level")
        {
            // Set oil level below the recommended threshold (< 50% of capacity)
            OilLevel = (int)Math.Round(Random.Range(0.05f, 0.44f) * OilCapacity);
            isWorking = OilLevel >= minAcceptableOilLevel * OilCapacity && !HasCracks;
            GameLogger.Log($"[VehicleEngine] Low_Oil_Level assigned — OilLevel={OilLevel}/{OilCapacity} ({GetOilLevelPercentage() * 100f:F0}%)");
        }
    }

    private string GenerateSerialNumber()
    {
        // Generate a realistic engine serial number
        return $"ENG{UnityEngine.Random.Range(100000, 999999)}";
    }

    /// <summary>
    /// Gets oil level as a percentage (0-1)
    /// </summary>
    public float GetOilLevelPercentage()
    {
        return (float)OilLevel / OilCapacity;
    }

    /// <summary>
    /// Checks if oil level is low
    /// </summary>
    public bool IsOilLevelLow()
    {
        return GetOilLevelPercentage() < minAcceptableOilLevel;
    }

    public void Interact()
    {
        // Interact functionality - could be used to open hood for inspection
        GameLogger.Log("Interacting with engine");
    }

    public void Read()
    {
        string engineInfo = GetEngineInfoString();
        GameLogger.Log($"[VehicleEngine] Reading: {engineInfo}");
        DebugToScreen.ShowMessage(engineInfo, 5f);
        DetectIssuesFromRead();
    }

    private void DetectIssuesFromRead()
    {
        VehicleManager vehicleManager = FindObjectOfType<VehicleManager>();
        if (vehicleManager?.IssueDatabase == null) return;

        if (IsOilLevelLow())
        {
            Issue issue = vehicleManager.IssueDatabase.GetByName("Low_Oil_Level");
            if (issue != null && !predictedIssues.Contains(issue))
            {
                predictedIssues.Add(issue);
                GameLogger.Log($"[VehicleEngine] 'Low_Oil_Level' added to predicted issues ({GetOilLevelPercentage() * 100f:F0}% oil)");
                DebugToScreen.ShowMessage("Low Oil Level Detected!", 3f);
            }
        }
    }

    private string GetEngineInfoString()
    {
        float oilPercent = GetOilLevelPercentage() * 100f;
        return $"ENGINE\n" +
               $"Serial: {SerialNumber}\n" +
               $"Type: {fuelType} | Capacity: {EngineCapacity}L\n" +
               $"Power: {MaxHorsePower} HP\n" +
               $"Oil Level: {OilLevel}/{OilCapacity} ({oilPercent:F0}%)\n" +
               $"Status: {(isWorking ? "Running" : "Not Running")}\n" +
               $"{(HasOilLeak ? "[OIL LEAK DETECTED]" : "")}\n" +
               $"{(HasCracks ? "[CRACKS DETECTED]" : "")}\n" +
               $"{(BeltsAged ? "[AGED BELTS]" : "")}\n" +
               $"{(CoolantReservoirLow ? "[LOW COOLANT]" : "")}";
    }

    /// <summary>
    /// Simulates oil consumption
    /// </summary>
    public void ConsumeOil(int amount)
    {
        OilLevel = Mathf.Max(0, OilLevel - amount);
        if (IsOilLevelLow())
        {
            GameLogger.LogWarning("[VehicleEngine] Oil level is low!");
        }
    }

    /// <summary>
    /// Simulates adding oil
    /// </summary>
    public void AddOil(int amount)
    {
        OilLevel = Mathf.Min(OilCapacity, OilLevel + amount);
    }
}

public enum FuelType
{
    Petrol,
    Diesel,
    Electric,
    Hybrid,
    LPG
}
