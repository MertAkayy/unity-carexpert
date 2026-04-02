using UnityEngine;
using Core;
using Progression;
using Economy;
using Report;
using Inspection;
using Customer;
using Task;

public class TestSystems : MonoBehaviour
{
    void Start()
    {
        // Test ServiceLocator
        Debug.Log("=== Testing ServiceLocator ===");

        if (ServiceLocator.TryGet(out IPlayerDataSystem playerData))
            Debug.Log($"PlayerData System: OK - Balance: ${playerData.PlayerData.money}");

        if (ServiceLocator.TryGet(out ITimeSystem time))
            Debug.Log($"Time System: OK - Day {time.CurrentDay}, {time.CurrentHour}:{time.CurrentMinute}");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestEconomy();
            TestProgression();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            TestCustomerVehicleFlow();
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            TestVehicleFactoryOnly();
        }
    }

    void TestCustomerVehicleFlow()
    {
        Debug.Log("=== Testing Customer-Vehicle Flow ===");

        if (!ServiceLocator.TryGet(out ICustomerManager customerManager))
        {
            Debug.LogError("CustomerManager not registered!");
            return;
        }

        Debug.Log($"Queue Count: {customerManager.QueueCount}/{customerManager.MaxQueueSize}");
        Debug.Log($"Current Customer: {(customerManager.CurrentCustomer?.Data?.CustomerName ?? "None")}");
        Debug.Log($"Is Serving: {customerManager.IsServingCustomer}");

        // Test spawning a customer
        Debug.Log("--- Spawning Customer ---");
        var customer = customerManager.SpawnCustomer();

        if (customer != null)
        {
            Debug.Log($"Spawned: {customer.Data.CustomerName}");
            Debug.Log($"Requested Vehicle: {customer.Request?.RequestedVehicleType?.VehicleName ?? "None"}");
            Debug.Log($"Assigned Vehicle: {(customer.Request?.AssignedVehicle != null ? "Yes" : "No (waits until service starts)")}");
            Debug.Log($"New Queue Count: {customerManager.QueueCount}");
        }
        else
        {
            Debug.LogWarning("Failed to spawn customer (queue full?)");
        }
    }

    void TestVehicleFactoryOnly()
    {
        Debug.Log("=== Testing VehicleFactory ===");

        if (!ServiceLocator.TryGet(out IVehicleFactory vehicleFactory))
        {
            Debug.LogError("VehicleFactory not registered!");
            return;
        }

        Debug.Log($"Active Vehicles: {vehicleFactory.ActiveVehicleCount}");

        // Get available vehicles for level 1
        var availableVehicles = vehicleFactory.GetAvailableVehiclesForLevel(1);
        Debug.Log($"Available Vehicles for Level 1: {availableVehicles.Count}");

        foreach (var v in availableVehicles)
        {
            Debug.Log($"  - {v.VehicleName} (Spawn Weight: {v.SpawnWeight})");
        }

        // Spawn a random vehicle
        Debug.Log("--- Spawning Random Vehicle ---");
        var vehicle = vehicleFactory.SpawnVehicleForLevel(1, Vector3.zero, Quaternion.identity);

        if (vehicle != null)
        {
            Debug.Log($"Spawned Vehicle ID: {vehicle.VehicleId}");
            Debug.Log($"Active Vehicles Now: {vehicleFactory.ActiveVehicleCount}");

            // Return it to pool
            Debug.Log("--- Returning Vehicle to Pool ---");
            vehicleFactory.ReturnVehicle(vehicle);
            Debug.Log($"Active Vehicles After Return: {vehicleFactory.ActiveVehicleCount}");
        }
        else
        {
            Debug.LogWarning("Failed to spawn vehicle");
        }
    }

    void TestEconomy()
    {
        Debug.Log("=== Testing Economy ===");
        if (ServiceLocator.TryGet(out IEconomySystem economy))
        {
            Debug.Log($"Current Balance: ${economy.Balance}");
            economy.AddIncome(100f, TransactionCategory.Inspection, "Test inspection");
            Debug.Log($"After +$100: ${economy.Balance}");
        }
        else
        {
            Debug.LogWarning("EconomySystem not registered!");
        }
    }

    void TestProgression()
    {
        Debug.Log("=== Testing Progression ===");
        if (ServiceLocator.TryGet(out IProgressionManager progression))
        {
            Debug.Log($"Current Level: {progression.CurrentLevel}, XP: {progression.CurrentXP}");
            progression.AddXP(100, "Test reward");
            Debug.Log($"After +100 XP: Level {progression.CurrentLevel}, XP: {progression.CurrentXP}");
        }
        else
        {
            Debug.LogWarning("ProgressionManager not registered!");
        }
    }
}