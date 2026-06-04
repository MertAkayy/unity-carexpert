using System;
using UnityEngine;

public class VehicleGlass : VehiclePart,IInteractable,IVehicleGlass,IReadable
{
    public GlassPosition Position { get; set; }
    public bool IsDamaged { get; set; }
    public bool HasFilm { get; set; }
    public DateTime? ProductionDate { get; set; }

    private bool IsWindshield()
    {
        return partUniqueType == VehiclePartUniqueType.FrontGlass
            || partUniqueType == VehiclePartUniqueType.RearGlass;
    }

    public override void AssignIssue(Issue issue)
    {
        // Windshields don't have window regulators
        if (IsWindshield() && issue.FailureName == "Window_Regulator_Failure")
            return;

        base.AssignIssue(issue);
    }

    public void Interact()
    {
    }

    public void Read()
    {
        throw new NotImplementedException();
    }
}

public enum GlassPosition
{
    FrontWindshield,
    RearWindshield,
    FrontLeftWindow,
    FrontRightWindow,
    RearLeftWindow,
    RearRightWindow
}
