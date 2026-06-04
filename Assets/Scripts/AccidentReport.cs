using System;
using System.Collections.Generic;
using UnityEngine;


public enum AccidentRegion
{
    Front,
    Rear,
    Left,
    Right
}
public class AccidentReport
{
    public DateTime AccidentDate { get; set; }
    public List<ExteriorPartPosition> DamagedParts { get; set; } = new();
    public float RepairCost = 0;
    private static readonly Dictionary<ExteriorPartPosition, float> PartRepairCosts = new Dictionary<ExteriorPartPosition, float>
    {
        {ExteriorPartPosition.FrontBumper,100f},
        {ExteriorPartPosition.RearBumper,120},
        {ExteriorPartPosition.FrontLeftDoor,200f},
        {ExteriorPartPosition.FrontRightDoor,200f},
        {ExteriorPartPosition.RearLeftDoor,220f},
        {ExteriorPartPosition.RearRightDoor,220f},
        {ExteriorPartPosition.FrontLeftFender,150f},
        {ExteriorPartPosition.FrontRightFender,150f},
        {ExteriorPartPosition.RearLeftFender,150f},
        {ExteriorPartPosition.RearRightFender,150f},
        {ExteriorPartPosition.Hood,500f},
        {ExteriorPartPosition.Trunk,300f},
        {ExteriorPartPosition.Roof,400f},
    };

    private static readonly Dictionary<AccidentRegion, List<ExteriorPartPosition>> AccidentRegionParts =
        new Dictionary<AccidentRegion, List<ExteriorPartPosition>>
        {
            {AccidentRegion.Front, new List<ExteriorPartPosition> { ExteriorPartPosition.FrontBumper ,ExteriorPartPosition.Hood,ExteriorPartPosition.FrontLeftFender,ExteriorPartPosition.FrontRightFender}}, 
            {AccidentRegion.Left ,new List<ExteriorPartPosition> { ExteriorPartPosition.FrontLeftFender ,ExteriorPartPosition.FrontLeftDoor,ExteriorPartPosition.RearLeftDoor,ExteriorPartPosition.RearLeftFender}},
            {AccidentRegion.Right ,new List<ExteriorPartPosition> { ExteriorPartPosition.FrontRightFender ,ExteriorPartPosition.FrontRightDoor,ExteriorPartPosition.RearRightDoor,ExteriorPartPosition.RearRightFender}},
            { AccidentRegion.Rear ,new List<ExteriorPartPosition> { ExteriorPartPosition.RearLeftFender ,ExteriorPartPosition.RearBumper,ExteriorPartPosition.Trunk,ExteriorPartPosition.RearRightFender}}
        };

public AccidentReport(DateTime registrationDate, DateTime modelDateTime)
{
    AccidentDate = DetermineAccidentDate(registrationDate);
    AccidentRegion accidentRegion = GetRandomAccidentRegion();
    List<ExteriorPartPosition> possibleParts = new List<ExteriorPartPosition>(AccidentRegionParts[accidentRegion]);

    SelectDamagedParts(possibleParts);
    float costMultiplier = CalculateCostMultiplier(modelDateTime);
    RepairCost = CalculateRepairCost(DamagedParts, costMultiplier);
}
private DateTime DetermineAccidentDate(DateTime registrationDate)
{
    DateTime endDate = DateTime.Now.AddDays(-7);
    if (registrationDate > endDate)
    {
        GameLogger.Log("Start date is later than 7 days ago. Using registration date as accident date.");
        return registrationDate;
    }

    TimeSpan timeSpan = endDate - registrationDate;
    int randomDays = UnityEngine.Random.Range(0, (int)timeSpan.TotalDays + 1);
    return registrationDate.AddDays(randomDays);
}
private AccidentRegion GetRandomAccidentRegion()
{
    return (AccidentRegion)UnityEngine.Random.Range(0, Enum.GetValues(typeof(AccidentRegion)).Length);
}
private void SelectDamagedParts(List<ExteriorPartPosition> possibleParts)
{
    int damagedCount = UnityEngine.Random.Range(1, Math.Min(4, possibleParts.Count + 1));
    if (damagedCount > 0)
    {
        int startIndex = UnityEngine.Random.Range(0, possibleParts.Count - damagedCount + 1);
        for (int i = 0; i < damagedCount; i++)
        {
            ExteriorPartPosition part = possibleParts[startIndex + i];
            DamagedParts.Add(part);
        }
        possibleParts.RemoveRange(startIndex, damagedCount);
    }
}
private float CalculateCostMultiplier(DateTime modelDateTime)
{
    int vehicleAge = DateTime.Now.Year - modelDateTime.Year;
    if (vehicleAge <= 2) return 1.5f; // Yeni araç
    if (vehicleAge <= 5) return 1.2f; // 3-5 yaş
    return 1.0f; // 6+ yaş
}
private float CalculateRepairCost(IEnumerable<ExteriorPartPosition> damagedParts, float costMultiplier)
{
    float totalCost = 0f;
    foreach (var part in damagedParts)
    {
        totalCost += PartRepairCosts.ContainsKey(part) ? PartRepairCosts[part] : 400f;
    }
    return totalCost * costMultiplier;
}
}