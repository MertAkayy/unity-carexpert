using System;
using System.Collections.Generic;
using Core;
using PlayerScripts;
using UnityEngine;

/// <summary>
/// Interface for the VehicleFactory system.
/// </summary>
public interface IVehicleFactory : ISystem
{
    Vehicle SpawnVehicle(VehicleData vehicleData, Vector3 position, Quaternion rotation);
    Vehicle SpawnRandomVehicle(Vector3 position, Quaternion rotation);
    Vehicle SpawnVehicleForLevel(int playerLevel, Vector3 position, Quaternion rotation);
    Vehicle SpawnVehicleForCustomer(VehicleData vehicleData, int playerLevel, Vector3 position, Quaternion rotation);
    void ReturnVehicle(Vehicle vehicle);
    List<VehicleData> GetAvailableVehiclesForLevel(int playerLevel);
    VehicleData GetRandomVehicleDataForLevel(int playerLevel);
    int ActiveVehicleCount { get; }
}

/// <summary>
/// Vehicle spawning service with object pooling support.
/// Handles vehicle creation, initialization, and lifecycle management.
/// </summary>
public class VehicleFactory : IVehicleFactory
{
    private readonly IssueDataBase _issueDatabase;
    private readonly List<VehicleData> _availableVehicleTypes;
    private readonly Dictionary<string, Queue<Vehicle>> _vehiclePool;
    private readonly List<Vehicle> _activeVehicles;
    private readonly Transform _poolContainer;

    private const int DefaultPoolSize = 5;
    private const int MaxPoolSize = 20;

    public int Priority => 30;
    public int ActiveVehicleCount => _activeVehicles.Count;

    /// <summary>
    /// Creates a new VehicleFactory instance.
    /// </summary>
    /// <param name="issueDatabase">The issue database for fault generation</param>
    /// <param name="vehicleTypes">List of available vehicle types</param>
    public VehicleFactory(IssueDataBase issueDatabase, List<VehicleData> vehicleTypes)
    {
        _issueDatabase = issueDatabase;
        _availableVehicleTypes = vehicleTypes ?? new List<VehicleData>();
        _vehiclePool = new Dictionary<string, Queue<Vehicle>>();
        _activeVehicles = new List<Vehicle>();

        // Create pool container
        GameObject poolObject = new GameObject("[VehiclePool]");
        _poolContainer = poolObject.transform;
        UnityEngine.Object.DontDestroyOnLoad(poolObject);
    }

    #region ISystem Implementation

    public void OnRegistered()
    {
        GameLogger.Log("[VehicleFactory] Registered with ServiceLocator");
        PreWarmPools();
    }

    public void Initialize()
    {
        GameLogger.Log("[VehicleFactory] Initialized");
    }

    public void Shutdown()
    {
        GameLogger.Log("[VehicleFactory] Shutting down...");

        // Return all active vehicles to pool
        for (int i = _activeVehicles.Count - 1; i >= 0; i--)
        {
            if (_activeVehicles[i] != null)
            {
                DestroyVehicle(_activeVehicles[i]);
            }
        }
        _activeVehicles.Clear();

        // Clear pools
        foreach (var pool in _vehiclePool.Values)
        {
            while (pool.Count > 0)
            {
                var vehicle = pool.Dequeue();
                if (vehicle != null)
                {
                    UnityEngine.Object.Destroy(vehicle.gameObject);
                }
            }
        }
        _vehiclePool.Clear();

        if (_poolContainer != null)
        {
            UnityEngine.Object.Destroy(_poolContainer.gameObject);
        }

        GameLogger.Log("[VehicleFactory] Shutdown complete");
    }

    #endregion

    #region Pool Management

    /// <summary>
    /// Pre-warms pools for frequently used vehicle types.
    /// </summary>
    private void PreWarmPools()
    {
        if (_availableVehicleTypes == null) return;

        foreach (var vehicleData in _availableVehicleTypes)
        {
            if (vehicleData?.Prefab == null) continue;

            string poolKey = GetPoolKey(vehicleData);
            if (!_vehiclePool.ContainsKey(poolKey))
            {
                _vehiclePool[poolKey] = new Queue<Vehicle>();
            }

            // Pre-instantiate a few vehicles
            int prewarmCount = Mathf.Min(DefaultPoolSize, MaxPoolSize);
            for (int i = 0; i < prewarmCount; i++)
            {
                Vehicle vehicle = CreateNewVehicle(vehicleData);
                if (vehicle != null)
                {
                    vehicle.gameObject.SetActive(false);
                    vehicle.transform.SetParent(_poolContainer);
                    _vehiclePool[poolKey].Enqueue(vehicle);
                }
            }

            GameLogger.Log($"[VehicleFactory] Pre-warmed pool for {vehicleData.VehicleName} with {prewarmCount} instances");
        }
    }

    /// <summary>
    /// Gets a vehicle from the pool or creates a new one.
    /// </summary>
    private Vehicle GetFromPool(VehicleData vehicleData)
    {
        if (vehicleData?.Prefab == null)
        {
            GameLogger.LogError("[VehicleFactory] Invalid vehicle data or prefab");
            return null;
        }

        string poolKey = GetPoolKey(vehicleData);

        // Check if pool exists and has available vehicles
        if (_vehiclePool.TryGetValue(poolKey, out Queue<Vehicle> pool) && pool.Count > 0)
        {
            Vehicle vehicle = pool.Dequeue();
            if (vehicle != null)
            {
                vehicle.gameObject.SetActive(true);
                return vehicle;
            }
        }

        // Create new vehicle if pool is empty
        return CreateNewVehicle(vehicleData);
    }

    /// <summary>
    /// Returns a vehicle to the pool for reuse.
    /// </summary>
    private void ReturnToPool(Vehicle vehicle, VehicleData vehicleData)
    {
        if (vehicle == null || vehicleData?.Prefab == null) return;

        string poolKey = GetPoolKey(vehicleData);

        // Create pool if it doesn't exist
        if (!_vehiclePool.ContainsKey(poolKey))
        {
            _vehiclePool[poolKey] = new Queue<Vehicle>();
        }

        var pool = _vehiclePool[poolKey];

        // Only add to pool if under max size
        if (pool.Count < MaxPoolSize)
        {
            ResetVehicle(vehicle);
            vehicle.gameObject.SetActive(false);
            vehicle.transform.SetParent(_poolContainer);
            pool.Enqueue(vehicle);
            GameLogger.Log($"[VehicleFactory] Returned {vehicleData.VehicleName} to pool");
        }
        else
        {
            // Destroy if pool is full
            DestroyVehicle(vehicle);
            GameLogger.Log($"[VehicleFactory] Pool full, destroyed {vehicleData.VehicleName}");
        }
    }

    /// <summary>
    /// Generates a pool key for the vehicle data.
    /// </summary>
    private string GetPoolKey(VehicleData vehicleData)
    {
        return vehicleData?.Prefab?.name ?? vehicleData?.VehicleName ?? "Unknown";
    }

    #endregion

    #region Vehicle Creation

    /// <summary>
    /// Creates a new vehicle instance from the prefab.
    /// </summary>
    private Vehicle CreateNewVehicle(VehicleData vehicleData)
    {
        if (vehicleData?.Prefab == null)
        {
            GameLogger.LogError("[VehicleFactory] Cannot create vehicle: invalid prefab");
            return null;
        }

        GameObject vehicleObject = UnityEngine.Object.Instantiate(vehicleData.Prefab);
        Vehicle vehicle = vehicleObject.GetComponent<Vehicle>();

        if (vehicle == null)
        {
            GameLogger.LogError($"[VehicleFactory] Prefab {vehicleData.Prefab.name} does not have a Vehicle component");
            UnityEngine.Object.Destroy(vehicleObject);
            return null;
        }

        vehicleObject.name = $"{vehicleData.VehicleName}_{vehicle.VehicleId.ToString().Substring(0, 8)}";
        return vehicle;
    }

    /// <summary>
    /// Resets a vehicle to its default state.
    /// </summary>
    private void ResetVehicle(Vehicle vehicle)
    {
        if (vehicle == null) return;

        // Clear assigned issues from all parts
        foreach (var part in vehicle.exteriorParts)
        {
            part?.assignedIssues?.Clear();
            part?.predictedIssues?.Clear();
        }
        foreach (var part in vehicle.wheels)
        {
            part?.assignedIssues?.Clear();
            part?.predictedIssues?.Clear();
        }
        foreach (var part in vehicle.glasses)
        {
            part?.assignedIssues?.Clear();
            part?.predictedIssues?.Clear();
        }
        foreach (var part in vehicle.lights)
        {
            part?.assignedIssues?.Clear();
            part?.predictedIssues?.Clear();
        }

        vehicle.battery?.assignedIssues?.Clear();
        vehicle.battery?.predictedIssues?.Clear();
        vehicle.engine?.assignedIssues?.Clear();
        vehicle.engine?.predictedIssues?.Clear();
        vehicle.radiator?.assignedIssues?.Clear();
        vehicle.radiator?.predictedIssues?.Clear();
        vehicle.exhaust?.assignedIssues?.Clear();
        vehicle.exhaust?.predictedIssues?.Clear();

        vehicle.AccidentReports?.Clear();
    }

    /// <summary>
    /// Destroys a vehicle instance.
    /// </summary>
    private void DestroyVehicle(Vehicle vehicle)
    {
        if (vehicle != null && vehicle.gameObject != null)
        {
            UnityEngine.Object.Destroy(vehicle.gameObject);
        }
    }

    #endregion

    #region IVehicleFactory Implementation

    /// <summary>
    /// Spawns a specific vehicle at the given position.
    /// </summary>
    public Vehicle SpawnVehicle(VehicleData vehicleData, Vector3 position, Quaternion rotation)
    {
        if (vehicleData == null)
        {
            GameLogger.LogError("[VehicleFactory] Cannot spawn vehicle: vehicleData is null");
            return null;
        }

        Vehicle vehicle = GetFromPool(vehicleData);
        if (vehicle == null)
        {
            GameLogger.LogError($"[VehicleFactory] Failed to create vehicle from {vehicleData.VehicleName}");
            return null;
        }

        vehicle.transform.position = position;
        vehicle.transform.rotation = rotation;

        _activeVehicles.Add(vehicle);

        GameLogger.Log($"[VehicleFactory] Spawned {vehicleData.VehicleName} at {position}");
        return vehicle;
    }

    /// <summary>
    /// Spawns a random vehicle at the given position.
    /// </summary>
    public Vehicle SpawnRandomVehicle(Vector3 position, Quaternion rotation)
    {
        VehicleData randomData = GetRandomVehicleData();
        if (randomData == null)
        {
            GameLogger.LogError("[VehicleFactory] No vehicle types available for spawning");
            return null;
        }

        return SpawnVehicle(randomData, position, rotation);
    }

    /// <summary>
    /// Spawns a vehicle appropriate for the player's level.
    /// </summary>
    public Vehicle SpawnVehicleForLevel(int playerLevel, Vector3 position, Quaternion rotation)
    {
        VehicleData vehicleData = GetRandomVehicleDataForLevel(playerLevel);
        if (vehicleData == null)
        {
            GameLogger.LogWarning($"[VehicleFactory] No vehicles available for level {playerLevel}, using random");
            return SpawnRandomVehicle(position, rotation);
        }

        return SpawnVehicle(vehicleData, position, rotation);
    }

    /// <summary>
    /// Spawns a vehicle for a customer request with level-appropriate settings.
    /// </summary>
    public Vehicle SpawnVehicleForCustomer(VehicleData vehicleData, int playerLevel, Vector3 position, Quaternion rotation)
    {
        if (vehicleData == null || !vehicleData.IsAvailableForLevel(playerLevel))
        {
            GameLogger.LogWarning($"[VehicleFactory] Requested vehicle not available for level {playerLevel}");
            return SpawnVehicleForLevel(playerLevel, position, rotation);
        }

        return SpawnVehicle(vehicleData, position, rotation);
    }

    /// <summary>
    /// Returns a vehicle to the factory for recycling.
    /// </summary>
    public void ReturnVehicle(Vehicle vehicle)
    {
        if (vehicle == null) return;

        _activeVehicles.Remove(vehicle);

        // Try to find matching vehicle data
        VehicleData matchingData = FindVehicleDataForVehicle(vehicle);
        if (matchingData != null)
        {
            ReturnToPool(vehicle, matchingData);
        }
        else
        {
            DestroyVehicle(vehicle);
            GameLogger.Log("[VehicleFactory] Destroyed unidentifiable vehicle");
        }
    }

    /// <summary>
    /// Gets all vehicles available for a specific player level.
    /// </summary>
    public List<VehicleData> GetAvailableVehiclesForLevel(int playerLevel)
    {
        List<VehicleData> available = new List<VehicleData>();

        foreach (var vehicleData in _availableVehicleTypes)
        {
            if (vehicleData != null && vehicleData.IsAvailableForLevel(playerLevel))
            {
                available.Add(vehicleData);
            }
        }

        return available;
    }

    /// <summary>
    /// Gets a random vehicle data appropriate for the player level.
    /// </summary>
    public VehicleData GetRandomVehicleDataForLevel(int playerLevel)
    {
        List<VehicleData> available = GetAvailableVehiclesForLevel(playerLevel);

        if (available.Count == 0)
        {
            // Fallback to any available vehicle
            return GetRandomVehicleData();
        }

        // Use weighted random selection based on spawn weight
        return Utilities.WeightedRandom(available, v => Mathf.RoundToInt(v.SpawnWeight * 100));
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Gets a random vehicle data from all available types.
    /// </summary>
    private VehicleData GetRandomVehicleData()
    {
        if (_availableVehicleTypes == null || _availableVehicleTypes.Count == 0)
        {
            return null;
        }

        // Use weighted random selection
        return Utilities.WeightedRandom(_availableVehicleTypes, v => Mathf.RoundToInt(v?.SpawnWeight * 100 ?? 0));
    }

    /// <summary>
    /// Finds the VehicleData that matches a spawned vehicle.
    /// </summary>
    private VehicleData FindVehicleDataForVehicle(Vehicle vehicle)
    {
        if (vehicle == null || _availableVehicleTypes == null) return null;

        foreach (var vehicleData in _availableVehicleTypes)
        {
            if (vehicleData?.Prefab == null) continue;

            // Check if the vehicle was created from this prefab
            if (vehicle.name.StartsWith(vehicleData.VehicleName) ||
                vehicleData.Prefab.name == vehicle.gameObject.name.Replace($"_{vehicle.VehicleId.ToString().Substring(0, 8)}", ""))
            {
                return vehicleData;
            }
        }

        return null;
    }

    #endregion
}
