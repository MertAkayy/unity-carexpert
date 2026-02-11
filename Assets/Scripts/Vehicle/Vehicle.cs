using System;
using System.Collections.Generic;
using System.Linq;
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
    [SerializeField] public VehicleBattery battery=new VehicleBattery();
    [SerializeField] public VehicleEngine engine = new VehicleEngine();
    [SerializeField] public VehicleRadiator radiator = new VehicleRadiator();
    public List<AccidentReport> AccidentReports = new();
    [SerializeField] private Transform liftPlatform;
    public readonly VehicleRegistration Registration=new VehicleRegistration();
    public int milage=250;
    public Guid VehicleId = Guid.NewGuid();
    private int _totalIssueCount;
    private void AssignRandomPaintThickness()
    {
        foreach (var part in exteriorParts)
        {
            part.paintThickness=UnityEngine.Random.Range(60, 100);
        } 
    }

    private void Start()
    {
        transform.SetParent(liftPlatform);
        AssignRandomPaintThickness();
        CalculateVehicleMillage();
        _totalIssueCount = GetIssueCount(PlayerDataManager.Instance.playerData.level);
        CreateAccidentReports();
        CalculateIssuePossibilityWeights();
        AssignRandomIssues(_totalIssueCount-AccidentReports.Count);
        
        GameLogger.Log("Accident Reports : "+ AccidentReports.Count);
        List<VehiclePart> allParts = new List<VehiclePart>();
        allParts.AddRange(exteriorParts);
        allParts.AddRange(wheels);
        allParts.AddRange(glasses);
        allParts.AddRange(lights);
        allParts.Add(battery);
        allParts.Add(engine);
        allParts.Add(radiator);
        foreach (var carPart in allParts)
        {
            foreach (var printedissue in carPart.assignedIssues)
            {
                GameLogger.Log(carPart.name+" --- "+printedissue.FailureName +" ---- "+printedissue.AvailableLevel);
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
        AssignRepairIssuesFromAccidents();
    }
    private void AssignRepairIssuesFromAccidents()
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
    public void AssignRandomIssues(int issueCount)
    {
        if (issuePool == null || issuePool.issues == null || issuePool.issues.Count == 0)
        {
            GameLogger.LogWarning("IssueDatabase not defined or empty.");
            return;
        }
        // Tüm araç parçalarını tek bir listede topla
        List<VehiclePart> allParts = new List<VehiclePart>();
        allParts.AddRange(exteriorParts);
        allParts.AddRange(wheels);
        allParts.AddRange(glasses);
        allParts.AddRange(lights);
        allParts.Add(battery);
        allParts.Add(engine);
        allParts.Add(radiator);
        if (allParts.Count == 0)
        {
            GameLogger.LogWarning("No vehicle parts are defined.");
            return;
        }
        // Rastgele issueCount (örn. 5) adet arıza seç
        List<Issue> selectedIssues = new List<Issue>();
        List<Issue> availableIssues = new List<Issue>(issuePool.GetAvailableForLevel(PlayerDataManager.Instance.playerData.level)); 
        int maxIssues = Mathf.Min(issueCount, availableIssues.Count); // Mevcut arıza sayısını aşmamak için
        for (int i = 0; i < maxIssues; i++)
        {
            if (availableIssues.Count == 0)
            {
                GameLogger.LogWarning("There are not enough faults left.");
                break;
            }

            var selectedIssue = Utilities.WeightedRandom(availableIssues,i=>i.PossibilityWeight); // GetWeightedRandomIndex(availableIssues);
            selectedIssues.Add(selectedIssue);
            availableIssues.Remove(selectedIssue);
        }
        // Seçilen her arızayı uygun bir parçaya ata
        foreach (var issue in selectedIssues)
        {
            // Arızaya uygun parçaları bul
            var validParts = allParts.Where(part => issue.IsValidFor(part)).ToList();
            if (validParts.Count > 0)
            {
                // Rastgele bir uygun parça seç
                VehiclePart selectedPart = validParts[Random.Range(0, validParts.Count)];
                selectedPart.AssignIssue(issue);
                GameLogger.LogWarning($" The {issue.FailureName} fault has been assigned to the '{selectedPart.name}' component.");
            }
            else
            {
                GameLogger.LogWarning($"No suitable part was found for the '{issue.FailureName}' fault.");
            }
        }
        // Eğer seçilen arıza sayısı istenenden azsa, logla
        if (selectedIssues.Count < issueCount)
        {
            GameLogger.LogWarning($"Not enough faults could be assigned. Only {selectedIssues.Count} fault(s) were assigned.");
        }
    }
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
}