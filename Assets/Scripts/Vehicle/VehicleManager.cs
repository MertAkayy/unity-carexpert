using System.Collections.Generic;
using Core;
using Inspection;
using Report;
using UnityEngine;

/// <summary>
/// MonoBehaviour wrapper that creates and manages the VehicleFactory.
/// Place this GameObject in your scene with the required references.
/// </summary>
public class VehicleManager : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private IssueDataBase _issueDatabase;
    [SerializeField] private List<VehicleData> _vehicleTypes = new List<VehicleData>();

    [Header("Settings")]
    [SerializeField] private bool _debugMode = false;

    private VehicleFactory _vehicleFactory;
    private FaultGenerator _faultGenerator;
    private InspectionService _inspectionService;
    private ReportService _reportService;

    public VehicleFactory Factory => _vehicleFactory;
    public IssueDataBase IssueDatabase => _issueDatabase;

    private void Awake()
    {
        // Validate references
        if (_issueDatabase == null)
        {
            Debug.LogError("[VehicleManager] IssueDatabase is not assigned!");
            return;
        }

        if (_vehicleTypes == null || _vehicleTypes.Count == 0)
        {
            Debug.LogWarning("[VehicleManager] No vehicle types assigned!");
        }

        // Create and register FaultGenerator
        _faultGenerator = new FaultGenerator(_issueDatabase);
        ServiceLocator.Register<IFaultGenerator>(_faultGenerator);

        // Create and register VehicleFactory
        _vehicleFactory = new VehicleFactory(_issueDatabase, _vehicleTypes);
        ServiceLocator.Register<IVehicleFactory>(_vehicleFactory);

        // Create and register InspectionService
        _inspectionService = new InspectionService();
        ServiceLocator.Register<IInspectionService>(_inspectionService);

        // Create and register ReportService
        _reportService = new ReportService();
        ServiceLocator.Register<IReportService>(_reportService);

        if (_debugMode)
        {
            Debug.Log($"[VehicleManager] Created and registered VehicleFactory with {_vehicleTypes.Count} vehicle types");
        }
    }

    private void OnDestroy()
    {
        if (ServiceLocator.IsRegistered<IReportService>())
            ServiceLocator.Unregister<IReportService>();

        if (ServiceLocator.IsRegistered<IInspectionService>())
            ServiceLocator.Unregister<IInspectionService>();

        if (ServiceLocator.IsRegistered<IFaultGenerator>())
            ServiceLocator.Unregister<IFaultGenerator>();

        if (ServiceLocator.IsRegistered<IVehicleFactory>())
            ServiceLocator.Unregister<IVehicleFactory>();
    }

    #region Debug

    [ContextMenu("Debug: List Vehicle Types")]
    private void DebugListVehicleTypes()
    {
        Debug.Log($"=== Vehicle Types ({_vehicleTypes.Count}) ===");
        foreach (var v in _vehicleTypes)
        {
            if (v != null)
            {
                Debug.Log($"  - {v.VehicleName} | Level: {v.UnlockLevel} | Weight: {v.SpawnWeight} | Prefab: {(v.Prefab != null ? v.Prefab.name : "NULL")}");
            }
        }
    }

    [ContextMenu("Debug: Spawn Random Vehicle")]
    private void DebugSpawnRandomVehicle()
    {
        if (_vehicleFactory == null)
        {
            Debug.LogError("[VehicleManager] Factory not initialized!");
            return;
        }

        var vehicle = _vehicleFactory.SpawnRandomVehicle(transform.position, Quaternion.identity);
        if (vehicle != null)
        {
            Debug.Log($"[VehicleManager] Spawned vehicle: {vehicle.VehicleId}");
        }
        else
        {
            Debug.LogWarning("[VehicleManager] Failed to spawn vehicle");
        }
    }

    [ContextMenu("Debug: Show Active Vehicles")]
    private void DebugShowActiveVehicles()
    {
        if (_vehicleFactory == null)
        {
            Debug.LogError("[VehicleManager] Factory not initialized!");
            return;
        }

        Debug.Log($"[VehicleManager] Active vehicles: {_vehicleFactory.ActiveVehicleCount}");
    }

    #endregion
}
