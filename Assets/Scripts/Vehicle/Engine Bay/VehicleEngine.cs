using UnityEngine;

public class VehicleEngine : VehiclePart,IInteractable,IVehicleEngine,IReadable
{
public FuelType fuelType;
public double EngineCapacity { get; set; }
public int MaxHorsePower { get; set; } 
public int Performance { get; set; }
public int OilCapacity { get; set; }
public int OilLevel { get; set; }
public string SerialNumber { get; set; }
public bool isWorking = true;

public VehicleEngine()
{
    
}
public void Interact()
{
}

public void Read()
{
    throw new System.NotImplementedException();
}
}
