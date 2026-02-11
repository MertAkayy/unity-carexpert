using System;
using UnityEngine;

public class VehicleLight : VehiclePart,IInteractable,IVehicleLight,IReadable
{
    public LightPosition Position { get; set; }
    public bool IsWorking { get; set; }
    public DateTime ProductionDate { get; set; }
    public void Interact()
    {
      //  throw new System.NotImplementedException();
    }

    public void Read()
    {
        throw new NotImplementedException();
    }
}

public enum LightPosition
{
    FrontLeft,
    FrontRight,
    RearLeft,
    RearRight 
}
