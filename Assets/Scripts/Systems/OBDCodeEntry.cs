using UnityEngine;

namespace Systems
{
    /// <summary>
    /// OBD code entry for the reference book database.
    /// Contains information about a single OBD trouble code.
    /// </summary>
    [CreateAssetMenu(fileName = "New OBD Code", menuName = "Vehicle/OBD Code")]
    public class OBDCodeEntry : ScriptableObject
    {
        [Header("Code Information")]
        [SerializeField] public string code; // e.g., "P0300", "P0420"
        [SerializeField] public string description; // e.g., "Random/Multiple Cylinder Misfire Detected"

        [Header("Category")]
        [SerializeField] public OBDCodeCategory category;

        [Header("Common Causes")]
        [SerializeField] public string[] commonCauses;

        [Header("Additional Info")]
        [SerializeField] public string severity; // Low, Medium, High, Critical
        [SerializeField] public string symptoms;
    }

    public enum OBDCodeCategory
    {
        Powertrain,      // P0xxx - P3xxx: Engine, Transmission
        Chassis,         // C0xxx - C3xxx: Brakes, Suspension
        Body,            // B0xxx - B3xxx: HVAC, Seats, Airbags
        Network,         // U0xxx - U3xxx: Communication, Network
        Manufacturer     // P1xxx, P3xxx, etc.: Manufacturer specific
    }
}
