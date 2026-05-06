using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Create Vehicle Issue", menuName = "Vehicle/Issue")]
public class Issue : ScriptableObject
{
    [SerializeField] public string FailureName;
    [SerializeField] public string Description;
    [SerializeField] public int AvailableLevel;
    [SerializeField] public AffectedPartType AffectedPartType;
    [SerializeField] public int PossibilityWeight;
    [SerializeField] public string ObdCode = "PXXXX";
    [SerializeField] public Tool RequiredTool;
    [SerializeField] public Guid IssueId = Guid.NewGuid(); // Auto-generate on creation
    [SerializeField] public List<Clue> Clues = new List<Clue>(); // Initialize to avoid null

    public bool IsValidFor(VehiclePart vehiclePart)
    {
        if (vehiclePart == null) return false;

        switch (AffectedPartType)
        {
            case AffectedPartType.None:
                return false;
            case AffectedPartType.Battery:
                return vehiclePart is IVehicleBattery;
            case AffectedPartType.Engine:
                return vehiclePart is IVehicleEngine;
            case AffectedPartType.Radiator:
                return vehiclePart is IVehicleRadiotor; // Fixed typo from Radiotor
            case AffectedPartType.Wheel:
                return vehiclePart is IVehicleWheel;
            case AffectedPartType.Light:
                return vehiclePart is IVehicleLight;
            case AffectedPartType.Glass:
                return vehiclePart is IVehicleGlass;
            case AffectedPartType.Exterior:
                return vehiclePart is IExteriorPart;
            case AffectedPartType.Exhaust:
                return vehiclePart is IVehicleExhaust;
            default:
                return false;
        }
    }

    public IssueDto ToDTO()
    {
        return new IssueDto
        {
            failureName = FailureName,
            description = Description,
            availableLevel = AvailableLevel,
            affectedPartType = AffectedPartType,
            possibilityWeight = PossibilityWeight,
            obdCode = ObdCode,
            requiredTool = RequiredTool,
            issueId = IssueId.ToString(),
            clues = Clues?.Select(c => new ClueDto { clueText = c.clueText, isCollected = c.isCollected ,clueGuid = c.ClueGuid.ToString()}).ToList() ?? new List<ClueDto>()
        };
    }
}

public enum AffectedPartType
{
    None,
    Exterior,
    Glass,
    Light,
    Wheel,
    Engine,
    Battery,
    Radiator,
    Exhaust,
}
[Serializable]
public class IssueDto
{
    public string failureName;
    public string description;
    public int availableLevel;
    public AffectedPartType affectedPartType;
    public int possibilityWeight;
    public string obdCode;
    public Tool requiredTool;
    public string issueId;
    public List<ClueDto> clues;
}