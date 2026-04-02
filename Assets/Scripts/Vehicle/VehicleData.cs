using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject definition for vehicle types.
/// Contains vehicle configuration, visual customization, and compatible issue types.
/// </summary>
[CreateAssetMenu(fileName = "VehicleData", menuName = "Vehicle/VehicleData")]
public class VehicleData : ScriptableObject
{
    [Header("Basic Information")]
    [SerializeField] private string vehicleName;
    [SerializeField] [TextArea(2, 4)] private string description;
    [SerializeField] private GameObject prefab;
    [SerializeField] private Sprite thumbnail;

    [Header("Vehicle Value")]
    [SerializeField] private float baseValue = 10000f;
    [SerializeField] private int minimumLevel = 1;

    [Header("Difficulty Settings")]
    [SerializeField] private VehicleDifficulty difficulty = VehicleDifficulty.Medium;
    [SerializeField] private int baseIssueCount = 5;
    [SerializeField] private float issueCountVariation = 2f;

    [Header("Compatible Issue Types")]
    [SerializeField] private List<AffectedPartType> compatibleIssueTypes = new List<AffectedPartType>
    {
        AffectedPartType.Exterior,
        AffectedPartType.Glass,
        AffectedPartType.Light,
        AffectedPartType.Wheel,
        AffectedPartType.Engine,
        AffectedPartType.Battery,
        AffectedPartType.Radiator
    };

    [Header("Visual Customization")]
    [SerializeField] private List<Color> availableColors = new List<Color>
    {
        Color.white,
        Color.black,
        Color.red,
        Color.blue,
        Color.gray,
        Color.green
    };
    [SerializeField] private bool supportsMetallicPaint = true;
    [SerializeField] private bool supportsPearlPaint = false;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnWeight = 1f;
    [SerializeField] private bool isPremium = false;
    [SerializeField] private int unlockLevel = 1;

    #region Properties

    /// <summary>
    /// Display name of the vehicle.
    /// </summary>
    public string VehicleName => vehicleName;

    /// <summary>
    /// Description text for the vehicle.
    /// </summary>
    public string Description => description;

    /// <summary>
    /// The prefab to instantiate when spawning this vehicle.
    /// </summary>
    public GameObject Prefab => prefab;

    /// <summary>
    /// Thumbnail sprite for UI display.
    /// </summary>
    public Sprite Thumbnail => thumbnail;

    /// <summary>
    /// Base monetary value of the vehicle.
    /// </summary>
    public float BaseValue => baseValue;

    /// <summary>
    /// Minimum player level required to inspect this vehicle.
    /// </summary>
    public int MinimumLevel => minimumLevel;

    /// <summary>
    /// Difficulty rating affecting inspection complexity.
    /// </summary>
    public VehicleDifficulty Difficulty => difficulty;

    /// <summary>
    /// Base number of issues this vehicle type typically has.
    /// </summary>
    public int BaseIssueCount => baseIssueCount;

    /// <summary>
    /// Random variation applied to issue count (+/-).
    /// </summary>
    public float IssueCountVariation => issueCountVariation;

    /// <summary>
    /// List of issue types that can affect this vehicle.
    /// </summary>
    public List<AffectedPartType> CompatibleIssueTypes => compatibleIssueTypes;

    /// <summary>
    /// Available paint colors for this vehicle.
    /// </summary>
    public List<Color> AvailableColors => availableColors;

    /// <summary>
    /// Whether this vehicle supports metallic paint finish.
    /// </summary>
    public bool SupportsMetallicPaint => supportsMetallicPaint;

    /// <summary>
    /// Whether this vehicle supports pearl paint finish.
    /// </summary>
    public bool SupportsPearlPaint => supportsPearlPaint;

    /// <summary>
    /// Relative weight for random vehicle selection.
    /// </summary>
    public float SpawnWeight => spawnWeight;

    /// <summary>
    /// Whether this is a premium vehicle requiring special access.
    /// </summary>
    public bool IsPremium => isPremium;

    /// <summary>
    /// Player level at which this vehicle becomes available.
    /// </summary>
    public int UnlockLevel => unlockLevel;

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets a random color from available colors.
    /// </summary>
    public Color GetRandomColor()
    {
        if (availableColors == null || availableColors.Count == 0)
            return Color.white;

        return availableColors[UnityEngine.Random.Range(0, availableColors.Count)];
    }

    /// <summary>
    /// Checks if a specific issue type is compatible with this vehicle.
    /// </summary>
    public bool IsIssueTypeCompatible(AffectedPartType issueType)
    {
        return compatibleIssueTypes.Contains(issueType);
    }

    /// <summary>
    /// Gets the calculated issue count with variation.
    /// </summary>
    public int GetRandomizedIssueCount()
    {
        int variation = Mathf.RoundToInt(UnityEngine.Random.Range(-issueCountVariation, issueCountVariation));
        return Mathf.Max(1, baseIssueCount + variation);
    }

    /// <summary>
    /// Calculates the value multiplier based on difficulty.
    /// </summary>
    public float GetValueMultiplier()
    {
        return difficulty switch
        {
            VehicleDifficulty.Easy => 0.8f,
            VehicleDifficulty.Medium => 1.0f,
            VehicleDifficulty.Hard => 1.3f,
            VehicleDifficulty.Expert => 1.6f,
            _ => 1.0f
        };
    }

    /// <summary>
    /// Gets the final calculated value of the vehicle.
    /// </summary>
    public float GetCalculatedValue()
    {
        return baseValue * GetValueMultiplier();
    }

    /// <summary>
    /// Checks if the vehicle is available for the given player level.
    /// </summary>
    public bool IsAvailableForLevel(int playerLevel)
    {
        return playerLevel >= unlockLevel && playerLevel >= minimumLevel;
    }

    #endregion
}

/// <summary>
/// Difficulty levels for vehicle inspection.
/// </summary>
[Serializable]
public enum VehicleDifficulty
{
    Easy,
    Medium,
    Hard,
    Expert
}

/// <summary>
/// Interface for vehicle data access.
/// </summary>
public interface IVehicleData
{
    string VehicleName { get; }
    GameObject Prefab { get; }
    float BaseValue { get; }
    VehicleDifficulty Difficulty { get; }
    int BaseIssueCount { get; }
    bool IsAvailableForLevel(int playerLevel);
}
