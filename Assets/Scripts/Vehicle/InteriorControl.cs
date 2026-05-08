using System.Collections.Generic;
using PlayerScripts;
using UnityEngine;

public enum InteriorControlType
{
    Horn,
    LightSwitch1,
    LightSwitch2,
    WindshieldWiper,
    AirConditionSwitch,
    FlashorSwitch,
    RadioSwitch
}

public class InteriorControl : MonoBehaviour, IInteractable
{
    [SerializeField] private InteriorControlType controlType;

    private static readonly Dictionary<InteriorControlType, string[]> IssueMap =
        new Dictionary<InteriorControlType, string[]>
        {
            { InteriorControlType.Horn,               new[] { "Horn_Failure" } },
            {InteriorControlType.LightSwitch1,        new [] {"Light_Aged", "Light_Fail",}},
            { InteriorControlType.LightSwitch2,       new[] {  "Turn_Signal_Relay_Failure" } },
            { InteriorControlType.FlashorSwitch,      new[] { "Turn_Signal_Relay_Failure" } },
            { InteriorControlType.WindshieldWiper,    new[] { "Windshield_Wiper_Motor_Failure" } },
            { InteriorControlType.AirConditionSwitch, new[] { "Air_Condition_Failure" } },
            { InteriorControlType.RadioSwitch,        new[] { "Radio_Failure" } },
        };

    public void Interact()
    {
        if (!IsValidInteraction()) return;

        GameLogger.Log($"[InteriorControl] {controlType} interacted.");

        if (!IssueMap.TryGetValue(controlType, out string[] issueNames)) return;

        Vehicle vehicle = FindObjectOfType<Vehicle>();
        VehicleManager vehicleManager = FindObjectOfType<VehicleManager>();
        if (vehicle == null || vehicleManager?.IssueDatabase == null) return;

        List<VehiclePart> allParts = GetAllVehicleParts(vehicle);

        foreach (string issueName in issueNames)
        {
            Issue issue = vehicleManager.IssueDatabase.GetByName(issueName);
            if (issue == null) continue;

            foreach (VehiclePart part in allParts)
            {
                if (part == null || !part.assignedIssues.Contains(issue)) continue;

                if (!part.predictedIssues.Contains(issue))
                {
                    part.predictedIssues.Add(issue);
                    GameLogger.Log($"[InteriorControl] '{issueName}' detected via {controlType} — added to predictedIssues on '{part.name}'");
                }
            }
        }
    }

    private bool IsValidInteraction()
    {
        PlayerCharacter character = FindObjectOfType<PlayerCharacter>();
        if (character == null || !character.IsSeated)
        {
            GameLogger.Log($"[InteriorControl] {controlType} blocked — player not seated.");
            return false;
        }

        MarketItem selected = PlayerDataManager.Instance?.playerData?.selectedItem;
        if (selected == null || selected.toolObject != Tool.Handle)
        {
            GameLogger.Log($"[InteriorControl] {controlType} blocked — tool is not Hand.");
            return false;
        }

        return true;
    }

    private List<VehiclePart> GetAllVehicleParts(Vehicle vehicle)
    {
        List<VehiclePart> parts = new List<VehiclePart>();
        if (vehicle.exteriorParts != null) parts.AddRange(vehicle.exteriorParts);
        if (vehicle.wheels != null) parts.AddRange(vehicle.wheels);
        if (vehicle.glasses != null) parts.AddRange(vehicle.glasses);
        if (vehicle.lights != null) parts.AddRange(vehicle.lights);
        if (vehicle.battery != null) parts.Add(vehicle.battery);
        if (vehicle.engine != null) parts.Add(vehicle.engine);
        if (vehicle.radiator != null) parts.Add(vehicle.radiator);
        if (vehicle.exhaust != null) parts.Add(vehicle.exhaust);
        if (vehicle.coolantReservoir != null) parts.Add(vehicle.coolantReservoir);
        return parts;
    }
}
