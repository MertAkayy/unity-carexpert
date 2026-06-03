using UnityEngine;
using System;

public class VehicleRadiator : VehiclePart, IVehicleRadiotor, IReadable, IInteractable
{
    [Header("Radiator Properties")]
    public float CoolantLevel { get; set; } // 0-1 percentage
    public bool HasLeak { get; set; }
    public bool IsDamaged { get; set; }

    [Header("Settings")]
    [SerializeField] private float minAcceptableCoolantLevel = 0.3f; // 30%
    [SerializeField] private float maxCoolantCapacity = 8.0f; // Liters

    private void Awake()
    {
        // Initialize on awake for proper setup
    }

    /// <summary>
    /// Initializes the radiator with random realistic values
    /// </summary>
    public void InitializeRadiator()
    {
        // Coolant level: 30-100% (0.3-1.0)
        CoolantLevel = UnityEngine.Random.Range(0.3f, 1.0f);

        // Leak and damage chances
        HasLeak = UnityEngine.Random.value < 0.1f; // 10% chance
        IsDamaged = UnityEngine.Random.value < 0.05f; // 5% chance

        GameLogger.Log($"[VehicleRadiator] Initialized: {CoolantLevel * 100:F0}% coolant, " +
                       $"{(HasLeak ? "Leaking" : "No Leak")}, " +
                       $"{(IsDamaged ? "Damaged" : "Good")}");
    }

    /// <summary>
    /// Gets coolant level in liters
    /// </summary>
    public float GetCoolantVolume()
    {
        return CoolantLevel * maxCoolantCapacity;
    }

    /// <summary>
    /// Checks if coolant level is low
    /// </summary>
    public bool IsCoolantLow()
    {
        return CoolantLevel < minAcceptableCoolantLevel;
    }

    /// <summary>
    /// Checks if radiator needs service
    /// </summary>
    public bool NeedsService()
    {
        return HasLeak || IsDamaged || IsCoolantLow();
    }

    public void Interact()
    {
        // Interact functionality - could be used to open cap for inspection
        GameLogger.Log("Interacting with radiator");
    }

    public void Read()
    {
        string radiatorInfo = GetRadiatorInfoString();
        GameLogger.Log($"[VehicleRadiator] Reading: {radiatorInfo}");
        ShowReadResult(radiatorInfo);
    }

    private string GetRadiatorInfoString()
    {
        float coolantVolume = GetCoolantVolume();
        return $"RADIATOR\n" +
               $"Coolant Level: {coolantVolume:F1}L / {maxCoolantCapacity:F1}L ({CoolantLevel * 100:F0}%)\n" +
               $"Status: {(IsCoolantLow() ? "LOW" : "Good")}\n" +
               $"Condition: {(IsDamaged ? "DAMAGED" : "Good")}\n" +
               $"{(HasLeak ? "[COOLANT LEAK DETECTED]" : "")}\n" +
               $"{(IsDamaged ? "[RADIATOR DAMAGED]" : "")}";
    }

    /// <summary>
    /// Simulates coolant loss due to leak
    /// </summary>
    public void LoseCoolant(float amount)
    {
        CoolantLevel = Mathf.Max(0f, CoolantLevel - amount);
        if (IsCoolantLow())
        {
            GameLogger.LogWarning("[VehicleRadiator] Coolant level is low!");
        }
    }

    /// <summary>
    /// Simulates adding coolant
    /// </summary>
    public void AddCoolant(float amount)
    {
        CoolantLevel = Mathf.Min(1f, CoolantLevel + amount);
    }

    /// <summary>
    /// Simulates time-based coolant loss if leaking
    /// </summary>
    public void UpdateLeakState()
    {
        if (HasLeak)
        {
            // Lose coolant over time
            LoseCoolant(0.01f * Time.deltaTime);
        }
    }
}
