using System;
using UnityEngine;

public class VehicleBattery : VehiclePart,IInteractable,IVehicleBattery,IReadable
{
    public double chargeLevel;
    public int voltage;
    public bool isWorking = true;

    public void Interact()
    {
        // throw new System.NotImplementedException();
    }

    public void Read()
    {
        throw new System.NotImplementedException();
    }
}
