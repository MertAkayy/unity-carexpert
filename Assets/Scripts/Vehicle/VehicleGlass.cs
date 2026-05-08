using System;
using UnityEngine;

public class VehicleGlass : VehiclePart,IInteractable,IVehicleGlass,IReadable
{
    public GlassPosition Position { get; set; }
    public bool IsDamaged { get; set; }
    public bool HasFilm { get; set; }
    public DateTime? ProductionDate { get; set; }

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
