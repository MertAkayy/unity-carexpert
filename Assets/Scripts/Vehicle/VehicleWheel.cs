using System;
using UnityEngine;

public class VehicleWheel : VehiclePart,IInteractable,IVehicleWheel,IReadable
{
    public WheelPosition Position { get; set; }
    public double TreadDepthMm { get; set; }
    public bool IsAlloy { get; set; }
    public bool IsDamaged { get; set; }
    public bool IsPunctured { get; set; }
    public WheelSeasonType SeasonType { get; set; }
    public DateTime ProductionDate { get; set; }
    public void Interact()
    {
        throw new NotImplementedException();
    }

    public void Read()
    {
        throw new System.NotImplementedException();
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
