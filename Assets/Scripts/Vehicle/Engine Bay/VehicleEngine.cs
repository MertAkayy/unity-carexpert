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
    /// Initializes the engine with random realistic values
    /// </summary>
    public void InitializeEngine()
    {
        // Oil level: 50-100% (0.5-1.0 as percentage of capacity)
        OilLevel = (int)Math.Round(Random.Range(0.5f, 1.0f) * OilCapacity);

        // Random issues based on chance
        HasOilLeak = Random.value < 0.1f; // 10% chance
        HasCracks = Random.value < 0.05f; // 5% chance
        BeltsAged = Random.value < 0.2f; // 20% chance
        CoolantReservoirLow = Random.value < 0.15f; // 15% chance
        VacuumTestFailed = Random.value < 0.1f; // 10% chance

        // Set serial number if not set
        if (string.IsNullOrEmpty(SerialNumber))
        {
            SerialNumber = GenerateSerialNumber();
        }

        // Engine is working if oil level is acceptable and no major damage
        isWorking = OilLevel >= minAcceptableOilLevel * OilCapacity && !HasCracks;

        GameLogger.Log($"[VehicleEngine] Initialized: {OilLevel}/{OilCapacity} oil, " +
                       $"{(HasOilLeak ? "Oil Leak" : "No Leak")}, " +
                       $"{(BeltsAged ? "Aged Belts" : "Good Belts")}");
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
