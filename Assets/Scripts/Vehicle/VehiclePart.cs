using System.Collections.Generic;
using UnityEngine;

public class VehiclePart:MonoBehaviour
{
    public List<Issue> assignedIssues = new List<Issue>();
    public List<Issue> predictedIssues = new List<Issue>();
    [SerializeField] public VehiclePartUniqueType partUniqueType;
    public virtual void AssignIssue(Issue issue)
    {
        if (issue.IsValidFor(this) && !assignedIssues.Contains(issue) )
        {
            assignedIssues.Add(issue);
        }
    }

    /// <summary>
    /// Shows a result on the ToolUIManager result panel with a custom title.
    /// Falls back to DebugToScreen if ToolUIManager is not available.
    /// </summary>
    protected void ShowReadResult(string message, string title = "Part Label")
    {
        var result = ToolScripts.Base.ToolInspectionResult.CreateSuccess(this, message);
        result.DisplayMessage = message;
        if (ToolScripts.UI.ToolUIManager.Instance != null)
            ToolScripts.UI.ToolUIManager.Instance.ShowResult(result, title);
        else
            DebugToScreen.ShowMessage(message, 5f);
    }
}

public enum VehiclePartUniqueType
{
    FrontBumper,
    RearBumper,
    FrontLeftDoor,
    FrontRightDoor,
    RearLeftDoor,
    RearRightDoor,
    Hood,  
    Trunk,
    FrontLeftFender,
    FrontRightFender,
    RearLeftFender,
    RearRightFender,
    Roof,
    FrontRightWheel,
    FrontLeftWheel,
    RearLeftWheel,
    RearRightWheel,
    FrontRightGlass,
    FrontLeftGlass,
    RearRightGlass,
    RearLeftGlass,
    FrontGlass,
    RearGlass,
    Engine,
    Radiator,
    Battery,
    Exhaust,
    CoolantReservoir,
    FrontRightLight,
    FrontLeftLight,
    RearRightLight,
    RearLeftLight,
}