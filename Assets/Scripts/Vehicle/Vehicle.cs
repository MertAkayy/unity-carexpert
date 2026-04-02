using System;
using System.Collections.Generic;
using System.Linq;
using Core;
using PlayerScripts;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class Vehicle : MonoBehaviour
{
    [SerializeField] private IssueDataBase issuePool;
    [SerializeField] public List<ExteriorPart> exteriorParts=new List<ExteriorPart>();
    [SerializeField] public List<VehicleWheel> wheels=new List<VehicleWheel>();
    [SerializeField] public List<VehicleGlass> glasses=new List<VehicleGlass>();
    [SerializeField] public List<VehicleLight> lights=new List<VehicleLight>();
    [SerializeField] public VehicleBattery battery;
    [SerializeField] public VehicleEngine engine;
    [SerializeField] public VehicleRadiator radiator;
    [SerializeField] public VehicleExhaust exhaust;
    public List<AccidentReport> AccidentReports = new();
    [SerializeField] private Transform liftPlatform;
    public readonly VehicleRegistration Registration=new VehicleRegistration();
    public int milage=250;
    public Guid VehicleId = Guid.NewGuid();

    /// <summary>
    /// Reference to the fault generator service.
    /// If null, falls back to legacy behavior.
    /// </summary>
    private IFaultGenerator _faultGenerator;
    private void AssignRandomPaintThickness()
    {
        foreach (var part in exteriorParts)
        {
            part.paintThickness=UnityEngine.Random.Range(60, 100);
        }
    }

    /// <summary>
    /// Initializes all vehicle parts with their default values.
    /// This should be called before assigning issues so that parts have data.
    /// </summary>
    private void InitializeAllParts()
    {
        GameLogger.Log("[Vehicle] Initializing all vehicle parts...");

        // Initialize all wheels
        foreach (var wheel in wheels)
        {
            if (wheel != null)
            {
                wheel.InitializeWheel();
            }
        }

        // Initialize battery
        if (battery != null)
        {
            battery.InitializeBattery();
        }

        // Initialize engine
        if (engine != null)
        {
            engine.InitializeEngine();
        }

        // Initialize radiator
        if (radiator != null)
        {
            radiator.InitializeRadiator();
        }

        // Initialize exhaust (if exists)
        if (exhaust != null)
        {
            exhaust.InitializeExhaust();
        }

        GameLogger.Log("[Vehicle] All vehicle parts initialized.");
    }

    private void Start()
    {
        transform.SetParent(liftPlatform);

        // Initialize all vehicle parts first
        InitializeAllParts();

        // Then set paint thickness (overwrites some initialization)
        AssignRandomPaintThickness();

        // Calculate vehicle mileage
        CalculateVehicleMillage();

        // Create accident reports (generates damage history)
        CreateAccidentReports();

        // Try to get FaultGenerator from ServiceLocator
        if (ServiceLocator.TryGet(out _faultGenerator) && _faultGenerator != null)
        {
            // Use the new FaultGenerator service
            int playerLevel = PlayerDataManager.Instance.playerData.level;
            _faultGenerator.GenerateFaultsForVehicle(this, playerLevel);
        }
        else
        {
            // Fallback to legacy behavior if FaultGenerator is not available
            GameLogger.LogWarning("[Vehicle] FaultGenerator not available, using legacy fault assignment");
            LegacyFaultAssignment();
        }

        // Log all assigned issues for debugging
        LogAssignedIssues();
    }

    /// <summary>
    /// Legacy fault assignment method used when FaultGenerator is not available.
    /// This maintains backward compatibility.
    /// </summary>
    private void LegacyFaultAssignment()
    {
        int totalIssueCount = GetIssueCount(PlayerDataManager.Instance.playerData.level);
        CalculateIssuePossibilityWeights();
        AssignRandomIssues(totalIssueCount - AccidentReports.Count);
    }

    /// <summary>
    /// Logs all assigned issues to the parts for debugging purposes.
    /// </summary>
    private void LogAssignedIssues()
    {
        GameLogger.Log("Accident Reports : " + AccidentReports.Count);

        List<VehiclePart> allParts = new List<VehiclePart>();
        allParts.AddRange(exteriorParts);
        allParts.AddRange(wheels);
        allParts.AddRange(glasses);
        allParts.AddRange(lights);
        if (battery != null) allParts.Add(battery);
        if (engine != null) allParts.Add(engine);
        if (radiator != null) allParts.Add(radiator);
        if (exhaust != null) allParts.Add(exhaust);

        foreach (var carPart in allParts)
        {
            foreach (var printedissue in carPart.assignedIssues)
            {
                GameLogger.Log(carPart.name + " --- " + printedissue.FailureName + " ---- " + printedissue.AvailableLevel);
            }
        }
    }
    private void CreateAccidentReports()
    {
        int randomReportNumber=Random.Range(0, 3);
        for (int i = 0; i < randomReportNumber; i++)
        {
            AccidentReport report = new AccidentReport(Registration.FirstRegistrationDate, Registration.ModelDateTime);
            AccidentReports.Add(report);
        }
        AccidentReports = AccidentReports.OrderBy(report => report.AccidentDate).ToList();

        // Assign repair issues from accidents - try FaultGenerator first, then fallback
        if (_faultGenerator != null)
        {
            _faultGenerator.AssignRepairIssuesFromAccidents(this, PlayerDataManager.Instance.playerData.level);
        }
        else
        {
            AssignRepairIssuesFromAccidentsLegacy();
        }
    }

    /// <summary>
    /// Legacy method for assigning repair issues when FaultGenerator is unavailable.
    /// </summary>
    private void AssignRepairIssuesFromAccidentsLegacy()
    {
        foreach (var report in AccidentReports)
        {
            foreach (var damagedPart in report.DamagedParts)
            {
                foreach (var vehiclePart in exteriorParts)
                {
                    if (vehiclePart.partPosition == damagedPart)
                    {
                        int randomSign;
                        if(PlayerDataManager.Instance.playerData.level<issuePool.GetByName("Dent_Repaired").AvailableLevel)
                            randomSign = UnityEngine.Random.Range(0, 2);
                        else
                            randomSign = UnityEngine.Random.Range(0, 3);

                        if (randomSign == 0)
                            vehiclePart.AssignIssue(issuePool.GetByName("Painted_Part"));
                        else if (randomSign == 1)
                            vehiclePart.AssignIssue(issuePool.GetByName("Replaced_Part"));
                        else if (randomSign == 2)
                            vehiclePart.AssignIssue(issuePool.GetByName("Dent_Repaired"));
                    }
                }
            }
        }
    }
    private void CalculateVehicleMillage()
    {
        int yearDifference=DateTime.Now.Year-Registration.FirstRegistrationDate.Year;
        if (yearDifference <= 0)
        {
            milage = Random.Range(0, 12000);
        }
        else
        {
            milage=Random.Range(yearDifference*12000,yearDifference*25000);
        }
    }

    #region Legacy Methods (Fallback when FaultGenerator is unavailable)

    /// <summary>
    /// Legacy method - Calculates possibility weights for issues.
    /// Used as fallback when FaultGenerator is not available.
    /// </summary>
    private void CalculateIssuePossibilityWeights()
    {
        var list = issuePool.GetAvailableForLevel(PlayerDataManager.Instance.playerData.level);
        foreach (var issue in list)
        {
            issue.PossibilityWeight = 0;
            if (issue.AvailableLevel == PlayerDataManager.Instance.playerData.level)
                issue.PossibilityWeight += 30;
            if (issue.IsValidFor(exteriorParts[0]) || issue.IsValidFor(glasses[0]) || issue.IsValidFor(lights[0]))
                issue.PossibilityWeight += 10;
            if(AccidentReports.Count>0 && issue.IsValidFor(exteriorParts[0]))
                issue.PossibilityWeight += 25;

        }
    }

    /// <summary>
    /// Legacy method - Assigns random issues to vehicle parts.
    /// Used as fallback when FaultGenerator is not available.
    /// </summary>
    public void AssignRandomIssues(int issueCount)
    {
        if (issuePool == null || issuePool.issues == null || issuePool.issues.Count == 0)
        {
            GameLogger.LogWarning("IssueDatabase not defined or empty.");
            return;
        }
        // Tum arac parcalarini tek bir listede topla
        List<VehiclePart> allParts = new List<VehiclePart>();
        allParts.AddRange(exteriorParts);
        allParts.AddRange(wheels);
        allParts.AddRange(glasses);
        allParts.AddRange(lights);
        if (battery != null) allParts.Add(battery);
        if (engine != null) allParts.Add(engine);
        if (radiator != null) allParts.Add(radiator);
        if (exhaust != null) allParts.Add(exhaust);
        if (allParts.Count == 0)
        {
            GameLogger.LogWarning("No vehicle parts are defined.");
            return;
        }
        // Rastgele issueCount (orn. 5) adet ariza sec
        List<Issue> selectedIssues = new List<Issue>();
        List<Issue> availableIssues = new List<Issue>(issuePool.GetAvailableForLevel(PlayerDataManager.Instance.playerData.level));
        int maxIssues = Mathf.Min(issueCount, availableIssues.Count); // Mevcut ariza sayisini asmmak icin
        for (int i = 0; i < maxIssues; i++)
        {
            if (availableIssues.Count == 0)
            {
                GameLogger.LogWarning("There are not enough faults left.");
                break;
            }

            var selectedIssue = Utilities.WeightedRandom(availableIssues,i=>i.PossibilityWeight);
            selectedIssues.Add(selectedIssue);
            availableIssues.Remove(selectedIssue);
        }
        // Secilen her arizayi uygun bir parcaya ata
        foreach (var issue in selectedIssues)
        {
            // Arizaya uygun parcalari bul
            var validParts = allParts.Where(part => issue.IsValidFor(part)).ToList();
            if (validParts.Count > 0)
            {
                // Rastgele bir uygun parca sec
                VehiclePart selectedPart = validParts[Random.Range(0, validParts.Count)];
                selectedPart.AssignIssue(issue);
                GameLogger.LogWarning($" The {issue.FailureName} fault has been assigned to the '{selectedPart.name}' component.");
            }
            else
            {
                GameLogger.LogWarning($"No suitable part was found for the '{issue.FailureName}' fault.");
            }
        }
        // Eger secilen ariza sayisi istenenden azsa, logla
        if (selectedIssues.Count < issueCount)
        {
            GameLogger.LogWarning($"Not enough faults could be assigned. Only {selectedIssues.Count} fault(s) were assigned.");
        }
    }

    /// <summary>
    /// Legacy method - Gets issue count based on player level.
    /// Used as fallback when FaultGenerator is not available.
    /// </summary>
    private int GetIssueCount(int level)
    {
        float random = UnityEngine.Random.value;
        if (level <= 10)
        {
            if (random < 0.50f) return 3; // %50
            if (random < 0.80f) return 4; // %30
            if (random < 0.90f) return 5; // %10
            if (random < 0.97f) return 6; // %7
            return 7;
        }
        else if (level <= 20)
        {
            if (random < 0.10f) return 3; // %10
            if (random < 0.60f) return 4; // %50
            if (random < 0.90f) return 5; // %30
            if (random < 0.97f) return 6; // %7
            return 7;
        }
        else if (level <= 30)
        {
            if (random < 0.05f) return 3; // %5
            if (random < 0.15f) return 4; // %10
            if (random < 0.65f) return 5; // %50
            if (random < 0.95f) return 6; // %30
            return 7;
        }
        else
        {
            if (random < 0.05f) return 3; // %5
            if (random < 0.10f) return 4; // %5
            if (random < 0.20f) return 5; // %10
            if (random < 0.70f) return 6; // %50
            return 7;
        }
    }

    #endregion
}