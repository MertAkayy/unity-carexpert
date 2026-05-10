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
        // Healthy defaults — faults are applied only via AssignIssue
        chargeLevel = Math.Round(Random.Range(60f, 100f), 1);
        voltage = CalculateVoltage(chargeLevel);
        HasCorrosion = false;

        int yearsAgo = Random.Range(0, 4);
        InstallationDate = DateTime.Now.AddYears(-yearsAgo).AddDays(-Random.Range(0, 365));

        isWorking = true;

        GameLogger.Log($"[VehicleBattery] Initialized: {chargeLevel}% charge, {voltage}V, Clean");
    }

    public override void AssignIssue(Issue issue)
    {
        base.AssignIssue(issue);
        if (issue == null) return;

        if (issue.FailureName == "Low_Battery")
        {
            chargeLevel = Math.Round(Random.Range(2f, 18f), 1);
            voltage = CalculateVoltage(chargeLevel);
            isWorking = false;
            GameLogger.Log($"[VehicleBattery] Low_Battery assigned — charge set to {chargeLevel}%");
        }
        else if (issue.FailureName == "Charging_System_Low_Voltage")
        {
            voltage = Mathf.RoundToInt(Random.Range(10.5f, 11.8f) * 10) / 10;
            GameLogger.Log($"[VehicleBattery] Charging_System_Low_Voltage assigned — voltage set to {voltage}V");
        }
        else if (issue.FailureName == "Battery_Corrosion")
        {
            HasCorrosion = true;
            GameLogger.Log($"[VehicleBattery] Battery_Corrosion assigned — corrosion enabled");
        }
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
        DetectIssuesFromRead();
    }

    private void DetectIssuesFromRead()
    {
        if (!HasCorrosion) return;

        VehicleManager vehicleManager = FindObjectOfType<VehicleManager>();
        if (vehicleManager?.IssueDatabase == null) return;

        Issue issue = vehicleManager.IssueDatabase.GetByName("Battery_Corrosion");
        if (issue != null && assignedIssues.Contains(issue) && !predictedIssues.Contains(issue))
        {
            predictedIssues.Add(issue);
            GameLogger.Log($"[VehicleBattery] 'Battery_Corrosion' detected via Read — added to predictedIssues");
        }
    }

    private string GetBatteryInfoString()
    {
        return $"Battery\n" +
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
