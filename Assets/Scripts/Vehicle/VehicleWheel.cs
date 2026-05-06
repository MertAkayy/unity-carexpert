using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class VehicleWheel : VehiclePart, IInteractable, IVehicleWheel, IReadable
{
    [Header("Wheel Properties")]
    public WheelPosition Position { get; set; }
    public double TreadDepthMm { get; set; }
    public bool IsAlloy { get; set; }
    public bool IsDamaged { get; set; }
    public bool IsPunctured { get; set; }
    public WheelSeasonType SeasonType { get; set; }
    public DateTime ProductionDate { get; set; }

    [Header("Tire Properties")]
    public float Pressure { get; set; } // PSI
    public string TireBrand { get; set; }

    [Header("Settings")]
    [SerializeField] private string[] possibleTireBrands = new string[]
    {
        "Michelin",
        "Bridgestone",
        "Continental",
        "Goodyear",
        "Pirelli",
        "Dunlop",
        "Hankook",
        "Yokohama"
    };

    private void Awake()
    {
        // Initialize on awake for proper setup
    }

    /// <summary>
    /// Initializes the wheel with random realistic values
    /// </summary>
    public void InitializeWheel()
    {
        // Tread depth: New tires are ~8mm, legal minimum is 1.6mm
        TreadDepthMm = Math.Round(Random.Range(1.5f, 8.0f), 2);

        // Pressure: Normal range is 28-35 PSI
        Pressure =(float) Math.Round(Random.Range(28f, 35f), 1);

        // Season type: Random selection
        SeasonType = (WheelSeasonType)Random.Range(0, 3);

        // Production date: Within last 10 years
        int yearsAgo = Random.Range(0, 11);
        ProductionDate = DateTime.Now.AddYears(-yearsAgo).AddDays(Random.Range(0, 365));

        // Damage and puncture chances
        IsPunctured = Random.value < 0.1f; // 10% chance
        IsDamaged = Random.value < 0.15f; // 15% chance
        IsAlloy = Random.value < 0.3f; // 30% chance of alloy wheels

        // Select random tire brand
        if (possibleTireBrands != null && possibleTireBrands.Length > 0)
        {
            TireBrand = possibleTireBrands[Random.Range(0, possibleTireBrands.Length)];
        }
        else
        {
            TireBrand = "Generic";
        }

        GameLogger.Log($"[VehicleWheel] Initialized {Position}: {TreadDepthMm}mm tread, {Pressure} PSI, {SeasonType} tire");
    }

    /// <summary>
    /// Gets the tire age in years
    /// </summary>
    public float GetTireAge()
    {
        return (float)((DateTime.Now - ProductionDate).TotalDays / 365.25);
    }

    /// <summary>
    /// Checks if the tire is expired (older than 5 years)
    /// </summary>
    public bool IsExpired()
    {
        return GetTireAge() > 5f;
    }

    /// <summary>
    /// Checks if the tire season is appropriate for current month
    /// </summary>
    public bool IsSeasonAppropriate()
    {
        int currentMonth = DateTime.Now.Month;

        switch (SeasonType)
        {
            case WheelSeasonType.Winter:
                // Winter tires are appropriate November-March
                return currentMonth >= 11 || currentMonth <= 3;

            case WheelSeasonType.Summer:
                // Summer tires are appropriate April-October
                return currentMonth >= 4 && currentMonth <= 10;

            case WheelSeasonType.AllSeason:
                // All season is always appropriate
                return true;

            default:
                return true;
        }
    }

    public void Interact()
    {
        // Interact functionality - could be used to remove wheel for closer inspection
        GameLogger.Log($"Interacting with {Position} wheel");
    }

    public void Read()
    {
        Debug.Log("read");
        string tireInfo = GetTireInfoString();
        GameLogger.Log($"[VehicleWheel] Reading: {tireInfo}");
        DebugToScreen.ShowMessage(tireInfo, 5f);
        DetectTireIssuesFromLabel();
    }

    private void DetectTireIssuesFromLabel()
    {
        VehicleManager vehicleManager = FindObjectOfType<VehicleManager>();
        if (vehicleManager?.IssueDatabase == null) return;

        if (IsExpired())
        {
            Issue issue = vehicleManager.IssueDatabase.GetByName("Expired_Tire");
            if (issue != null && !predictedIssues.Contains(issue))
            {
                predictedIssues.Add(issue);
                GameLogger.Log($"[VehicleWheel] 'Expired_Tire' added to predicted issues on {name} (age: {GetTireAge():F1} years)");
                DebugToScreen.ShowMessage("Expired Tire Detected!", 3f);
            }
        }

        if (!IsSeasonAppropriate())
        {
            Issue issue = vehicleManager.IssueDatabase.GetByName("Wrong_Season_Tire");
            if (issue != null && !predictedIssues.Contains(issue))
            {
                predictedIssues.Add(issue);
                GameLogger.Log($"[VehicleWheel] 'Wrong_Season_Tire' added to predicted issues on {name} (type: {SeasonType})");
                DebugToScreen.ShowMessage("Wrong Season Tire Detected!", 3f);
            }
        }
    }

    private string GetTireInfoString()
    {
        return $"Tire: {Position}\n" +
               $"Brand: {TireBrand}\n" +
               $"Type: {SeasonType}\n" +
               $"Age: {GetTireAge():F1} years\n" +
               $"Produced: {ProductionDate:yyyy-MM}\n" +
               $"Wheel: {(IsAlloy ? "Alloy" : "Steel")}\n" +
               $"{(IsPunctured ? "[PUNCTURED]" : "")}\n" +
               $"{(IsDamaged ? "[DAMAGED]" : "")}";
    }

    /// <summary>
    /// Simulates air being pumped into the tire
    /// </summary>
    /// <param name="amount">PSI to add</param>
    /// <returns>Actual pressure increase (0 if punctured)</returns>
    public float PumpAir(float amount)
    {
        if (IsPunctured)
        {
            // Punctured tires won't hold air
            return 0f;
        }

        Pressure = Math.Min(Pressure + amount, 50f); // Max 50 PSI
        return amount;
    }

    /// <summary>
    /// Simulates air being released from the tire
    /// </summary>
    /// <param name="amount">PSI to remove</param>
    public void ReleaseAir(float amount)
    {
        Pressure = Math.Max(Pressure - amount, 0f);
    }
}

public enum WheelPosition
{
    Spare,
    FrontLeft,
    FrontRight,
    RearLeft,
    RearRight
}

public enum WheelSeasonType
{
    Summer,
    Winter,
    AllSeason
}
