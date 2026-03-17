using UnityEngine;
using System;

public class VehicleExhaust : VehiclePart, IInteractable, IReadable
{
    [Header("Exhaust Properties")]
    public bool HasSmoke { get; set; }
    public bool IsBroken { get; set; }
    public float CO_Emission { get; set; } // Carbon Monoxide percentage
    public float HC_Emission { get; set; } // Hydrocarbons ppm
    public float NOx_Emission { get; set; } // Nitrogen Oxides ppm

    [Header("Emission Limits")]
    [SerializeField] private float maxCO = 0.5f; // Percentage
    [SerializeField] private float maxHC = 100f; // ppm
    [SerializeField] private float maxNOx = 1000f; // ppm

    private void Awake()
    {
        // Initialize on awake for proper setup
    }

    /// <summary>
    /// Initializes the exhaust system with random realistic values
    /// </summary>
    public void InitializeExhaust()
    {
        // Generate random emission values
        // Normal: CO < 0.5%, HC < 100 ppm, NOx < 1000 ppm
        CO_Emission = UnityEngine.Random.Range(0.1f, 0.8f);
        HC_Emission = UnityEngine.Random.Range(20f, 150f);
        NOx_Emission = UnityEngine.Random.Range(100f, 1200f);

        // 5% chance of being broken
        IsBroken = UnityEngine.Random.value < 0.05f;

        // Smoke occurs when emissions are high or system is broken
        HasSmoke = CO_Emission > maxCO || HC_Emission > maxHC || NOx_Emission > maxNOx || IsBroken;

        // If broken, increase emissions significantly
        if (IsBroken)
        {
            CO_Emission *= 2f;
            HC_Emission *= 2f;
            NOx_Emission *= 1.5f;
        }

        GameLogger.Log($"[VehicleExhaust] Initialized: CO={CO_Emission:F2}%, HC={HC_Emission:F0}ppm, NOx={NOx_Emission:F0}ppm, " +
                       $"{(HasSmoke ? "Smoke" : "No Smoke")}, {(IsBroken ? "Broken" : "Good")}");
    }

    /// <summary>
    /// Checks if emissions exceed limits
    /// </summary>
    public bool HasEmissionFault()
    {
        return CO_Emission > maxCO || HC_Emission > maxHC || NOx_Emission > maxNOx;
    }

    /// <summary>
    /// Gets the emission status message
    /// </summary>
    public string GetEmissionStatus()
    {
        if (IsBroken)
            return "BROKEN - System damaged";

        int faultCount = 0;
        if (CO_Emission > maxCO) faultCount++;
        if (HC_Emission > maxHC) faultCount++;
        if (NOx_Emission > maxNOx) faultCount++;

        if (faultCount == 0)
            return "Good - All emissions within limits";
        else if (faultCount == 1)
            return "Warning - 1 emission exceeds limit";
        else
            return $"FAIL - {faultCount} emissions exceed limits";
    }

    public void Interact()
    {
        // Interact functionality - could be used to inspect exhaust closely
        GameLogger.Log("Interacting with exhaust system");
    }

    public void Read()
    {
        string exhaustInfo = GetExhaustInfoString();
        GameLogger.Log($"[VehicleExhaust] Reading: {exhaustInfo}");
        DebugToScreen.ShowMessage(exhaustInfo, 5f);
    }

    private string GetExhaustInfoString()
    {
        return $"EXHAUST SYSTEM\n" +
               $"CO: {CO_Emission:F2}% (Limit: {maxCO:F2}%)\n" +
               $"HC: {HC_Emission:F0} ppm (Limit: {maxHC:F0} ppm)\n" +
               $"NOx: {NOx_Emission:F0} ppm (Limit: {maxNOx:F0} ppm)\n" +
               $"Status: {GetEmissionStatus()}\n" +
               $"{(HasSmoke ? "[VISIBLE SMOKE]" : "")}\n" +
               $"{(IsBroken ? "[BROKEN EXHAUST]" : "")}";
    }

    /// <summary>
    /// Gets a detailed breakdown of emission readings
    /// </summary>
    public EmissionReadings GetReadings()
    {
        return new EmissionReadings
        {
            CO = CO_Emission,
            HC = HC_Emission,
            NOx = NOx_Emission,
            COPass = CO_Emission <= maxCO,
            HCPass = HC_Emission <= maxHC,
            NOxPass = NOx_Emission <= maxNOx,
            OverallPass = !HasEmissionFault()
        };
    }
}

/// <summary>
    /// Struct containing emission test results
/// </summary>
[Serializable]
public struct EmissionReadings
{
    public float CO;
    public float HC;
    public float NOx;
    public bool COPass;
    public bool HCPass;
    public bool NOxPass;
    public bool OverallPass;

    public override string ToString()
    {
        return $"CO: {CO:F2}% {(COPass ? "PASS" : "FAIL")}, " +
               $"HC: {HC:F0}ppm {(HCPass ? "PASS" : "FAIL")}, " +
               $"NOx: {NOx:F0}ppm {(NOxPass ? "PASS" : "FAIL")} - " +
               $"Overall: {(OverallPass ? "PASS" : "FAIL")}";
    }
}
