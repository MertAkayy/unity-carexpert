using UnityEngine;
using System;
using System.Collections.Generic;

public class VehicleExhaust : VehiclePart, IInteractable, IReadable, IVehicleExhaust
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
    /// Initializes the exhaust with normal baseline emission values.
    /// Issues assigned later will push values into abnormal ranges.
    /// </summary>
    public void InitializeExhaust()
    {
        CO_Emission  = UnityEngine.Random.Range(0.05f, 0.35f);
        HC_Emission  = UnityEngine.Random.Range(15f,   75f);
        NOx_Emission = UnityEngine.Random.Range(80f,   700f);
        IsBroken = false;
        HasSmoke = false;

        GameLogger.Log($"[VehicleExhaust] Initialized: CO={CO_Emission:F2}%, HC={HC_Emission:F0}ppm, NOx={NOx_Emission:F0}ppm");
    }

    public override void AssignIssue(Issue issue)
    {
        base.AssignIssue(issue);

        switch (issue.FailureName)
        {
            case "Exhaust_Gasket_Leak":
                CO_Emission  += UnityEngine.Random.Range(0.4f,  0.8f);
                HC_Emission  += UnityEngine.Random.Range(50f,   100f);
                break;

            case "Exhaust_Leak":
                CO_Emission  += UnityEngine.Random.Range(0.5f,  1.0f);
                HC_Emission  += UnityEngine.Random.Range(60f,   120f);
                NOx_Emission += UnityEngine.Random.Range(0f,    150f);
                break;

            case "Exhaust_Manifold_Crack":
                CO_Emission  += UnityEngine.Random.Range(0.6f,  1.2f);
                HC_Emission  += UnityEngine.Random.Range(80f,   150f);
                NOx_Emission += UnityEngine.Random.Range(100f,  300f);
                break;

            case "EGR_System_Failure":
                // EGR recirculates exhaust to reduce NOx — failure causes NOx spike
                NOx_Emission += UnityEngine.Random.Range(500f,  1200f);
                CO_Emission  += UnityEngine.Random.Range(0f,    0.1f);
                break;

            case "Emission_Failure":
                CO_Emission  += UnityEngine.Random.Range(0.8f,  1.5f);
                HC_Emission  += UnityEngine.Random.Range(100f,  200f);
                NOx_Emission += UnityEngine.Random.Range(300f,  600f);
                break;

            case "Catalytic_Converter_Clog":
                CO_Emission  += UnityEngine.Random.Range(1.0f,  2.0f);
                HC_Emission  += UnityEngine.Random.Range(150f,  300f);
                NOx_Emission += UnityEngine.Random.Range(400f,  800f);
                break;

            case "Catalyst_Efficiency_Below_Threshold":
                CO_Emission  += UnityEngine.Random.Range(0.3f,  0.8f);
                HC_Emission  += UnityEngine.Random.Range(60f,   150f);
                NOx_Emission += UnityEngine.Random.Range(200f,  500f);
                break;

            case "Blue_Smoke":
                // Burning oil produces high HC, visible blue smoke
                HC_Emission  += UnityEngine.Random.Range(150f,  400f);
                CO_Emission  += UnityEngine.Random.Range(0.1f,  0.3f);
                HasSmoke = true;
                break;
        }

        HasSmoke = HasSmoke || CO_Emission > maxCO || HC_Emission > maxHC || NOx_Emission > maxNOx;

        GameLogger.Log($"[VehicleExhaust] Issue '{issue.FailureName}' assigned — CO={CO_Emission:F2}%, HC={HC_Emission:F0}ppm, NOx={NOx_Emission:F0}ppm");
    }

    public bool HasEmissionFault()
    {
        return IsBroken || CO_Emission > maxCO || HC_Emission > maxHC || NOx_Emission > maxNOx;
    }

    /// <summary>
    /// Infers which issues are likely present based on the current emission signature.
    /// Checks are ordered from most specific pattern to most general.
    /// </summary>
    public List<string> GetDetectedIssueNames()
    {
        var detected = new List<string>();

        if (!HasEmissionFault()) return detected;

        // Blue Smoke: very high HC is the unique fingerprint of burning oil
        if (HC_Emission > 200f)
            detected.Add("Blue_Smoke");

        // EGR System Failure: extreme NOx spike with CO and HC near normal
        if (NOx_Emission > 1500f && CO_Emission < maxCO + 0.15f)
            detected.Add("EGR_System_Failure");

        // Catalytic Converter Clog: all three severely elevated
        if (CO_Emission > 1.0f && HC_Emission > 150f && NOx_Emission > 500f)
            detected.Add("Catalytic_Converter_Clog");

        // Exhaust Manifold Crack: CO + HC + moderate NOx all elevated
        if (CO_Emission > 0.6f && HC_Emission > 80f && NOx_Emission > 100f
            && !detected.Contains("Catalytic_Converter_Clog"))
            detected.Add("Exhaust_Manifold_Crack");

        // Catalyst Efficiency Below Threshold: all three above limits, moderate
        if (CO_Emission > maxCO && HC_Emission > maxHC && NOx_Emission > maxNOx
            && !detected.Contains("Catalytic_Converter_Clog")
            && !detected.Contains("Exhaust_Manifold_Crack"))
            detected.Add("Catalyst_Efficiency_Below_Threshold");

        // Exhaust Leak: CO + HC above limits, NOx still within range
        if (CO_Emission > maxCO && HC_Emission > maxHC && NOx_Emission <= maxNOx
            && !detected.Contains("Exhaust_Manifold_Crack"))
            detected.Add("Exhaust_Leak");

        // Exhaust Gasket Leak: CO elevated and some HC, no NOx
        if (CO_Emission > maxCO && HC_Emission > 50f && NOx_Emission <= maxNOx
            && !detected.Contains("Exhaust_Leak")
            && !detected.Contains("Exhaust_Manifold_Crack"))
            detected.Add("Exhaust_Gasket_Leak");

        // General fallback: emission fault with no specific pattern matched
        if (detected.Count == 0)
            detected.Add("Emission_Failure");

        return detected;
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
