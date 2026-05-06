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
    FrontRightLight,
    FrontLeftLight,
    RearRightLight,
    RearLeftLight,
}