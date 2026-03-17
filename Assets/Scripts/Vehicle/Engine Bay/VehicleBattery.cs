using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class VehicleBattery : VehiclePart, IInteractable, IVehicleBattery, IReadable
{
    [Header("Battery Properties")]
    public double chargeLevel;
    public int voltage;
    public bool isWorking = true;

    [Header("Condition")]
    public bool HasCorrosion { get; set; }
    public DateTime InstallationDate { get; set; }

    [Header("Settings")]
    [SerializeField] private float normalVoltage = 12.6f; // Fully charged battery voltage
    [SerializeField] private float minVoltage = 11.5f; // Minimum acceptable voltage
    [SerializeField] private int minWorkingCharge = 20; // Minimum charge percentage

    private void Awake()
    {
        // Initialize on awake for proper setup
    }

    /// <summary>
    /// Initializes the battery with random realistic values
    /// </summary>
    public void InitializeBattery()
    {
        // Charge level: 10-100% (most batteries should be reasonably charged)
        chargeLevel = Math.Round(Random.Range(10f, 100f), 1);

        // Voltage varies with charge level (rough approximation)
        // 12.6V = 100%, 12.0V = 50%, 11.5V = 20%
        voltage = CalculateVoltage(chargeLevel);

        // Corrosion: 15% chance
        HasCorrosion = Random.value < 0.15f;

        // Installation date: Within last 5 years
        int yearsAgo = Random.Range(0, 6);
        InstallationDate = DateTime.Now.AddYears(-yearsAgo).AddDays(Random.Range(0, 365));

        // Battery is working if charge is above minimum
        isWorking = chargeLevel >= minWorkingCharge;

        GameLogger.Log($"[VehicleBattery] Initialized: {chargeLevel}% charge, {voltage}V, {(HasCorrosion ? "Has corrosion" : "Clean")}");
    }

    /// <summary>
    /// Calculates battery voltage based on charge level
    /// </summary>
    private int CalculateVoltage(double chargePercent)
    {
        // Rough approximation of lead-acid battery voltage curve
        float baseVoltage = 11.5f; // Voltage at 0%
        float voltageRange = normalVoltage - baseVoltage;
        float calculatedVoltage = baseVoltage + (voltageRange * (float)(chargePercent / 100.0));

        // Add some random variance
        calculatedVoltage += Random.Range(-0.2f, 0.2f);

        return Mathf.RoundToInt(calculatedVoltage * 10) / 10; // Round to 1 decimal
    }

    /// <summary>
    /// Gets the battery age in years
    /// </summary>
    public float GetBatteryAge()
    {
        return (float)((DateTime.Now - InstallationDate).TotalDays / 365.25);
    }

    /// <summary>
    /// Checks if the battery is old (more than 3 years)
    /// </summary>
    public bool IsOld()
    {
        return GetBatteryAge() > 3f;
    }

    /// <summary>
    /// Checks if the battery charge is low
    /// </summary>
    public bool IsLowCharge()
    {
        return chargeLevel < minWorkingCharge;
    }

    public void Interact()
    {
        // Interact functionality - could be used to disconnect/reconnect battery
        GameLogger.Log("Interacting with battery");
    }

    public void Read()
    {
        string batteryInfo = GetBatteryInfoString();
        GameLogger.Log($"[VehicleBattery] Reading: {batteryInfo}");
        DebugToScreen.ShowMessage(batteryInfo, 5f);
    }

    private string GetBatteryInfoString()
    {
        return $"Battery\n" +
               $"Charge: {chargeLevel:F1}%\n" +
               $"Voltage: {voltage}V\n" +
               $"Status: {(isWorking ? "Good" : "Low Charge")}\n" +
               $"Age: {GetBatteryAge():F1} years\n" +
               $"Installed: {InstallationDate:yyyy-MM}\n" +
               $"Terminals: {(HasCorrosion ? "CORRODED" : "Clean")}";
    }

    /// <summary>
    /// Simulates battery drain
    /// </summary>
    public void DrainCharge(float amount)
    {
        chargeLevel = Math.Max(0, chargeLevel - amount);
        voltage = CalculateVoltage(chargeLevel);
        isWorking = chargeLevel >= minWorkingCharge;
    }

    /// <summary>
    /// Simulates battery charging
    /// </summary>
    public void Charge(float amount)
    {
        chargeLevel = Math.Min(100, chargeLevel + amount);
        voltage = CalculateVoltage(chargeLevel);
        isWorking = chargeLevel >= minWorkingCharge;
    }
}
