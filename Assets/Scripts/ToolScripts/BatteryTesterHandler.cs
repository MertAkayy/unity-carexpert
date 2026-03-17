using UnityEngine;
using UnityEngine.InputSystem;
using ToolScripts.Base;
using System;
using PlayerScripts;
using ToolScripts.UI;

namespace ToolScripts
{
    /// <summary>
    /// Battery tester tool handler.
    /// Tests battery charge level, voltage, and condition.
    ///
    /// Detection logic:
    /// - Low charge (< 20%): Low_Battery_Charge issue
    /// - Corrosion on terminals: Battery_Corrosion issue
    /// - Old battery (> 3 years): Warning message
    /// - Low voltage (< 12V): Warning message
    /// </summary>
    public class BatteryTesterHandler : ToolHandlerBase
    {
        [Header("Battery Test Settings")]
        [SerializeField] private float minChargeThreshold = 20f; // Percentage
        [SerializeField] private float goodChargeThreshold = 80f; // Percentage
        [SerializeField] private float minGoodVoltage = 12.4f; // Volts
        [SerializeField] private float oldBatteryAge = 3f; // Years

        private VehicleBattery _targetBattery;

        protected override void Awake()
        {
            base.Awake();
            toolType = Tool.BatteryTester;
            toolName = "Battery Tester";
            inspectionDuration = 3f; // Battery test takes time
            compatiblePartInterfaces = new string[] { "IVehicleBattery" };
        }

        protected override VehiclePart GetTargetPart()
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                return player.GetTargetPart();
            }
            return base.GetTargetPart();
        }

        protected override bool ValidateTarget()
        {
            if (currentTargetPart == null)
            {
                return false;
            }

            _targetBattery = currentTargetPart as VehicleBattery;

            if (_targetBattery == null)
            {
                _targetBattery = currentTargetPart.GetComponent<VehicleBattery>();
            }

            return _targetBattery != null;
        }

        protected override ToolInspectionResult PerformInspection()
        {
            if (_targetBattery == null)
            {
                return ToolInspectionResult.CreateFailure("Invalid target for battery testing.");
            }

            var result = ToolInspectionResult.CreateSuccess(currentTargetPart, "Battery test complete.");

            // Get battery data
            double chargeLevel = _targetBattery.chargeLevel;
            int voltage = _targetBattery.voltage;
            float batteryAge = _targetBattery.GetBatteryAge();
            bool hasCorrosion = _targetBattery.HasCorrosion;
            bool isWorking = _targetBattery.isWorking;

            // Add measurements
            result.AddMeasurement("Charge Level", $"{chargeLevel:F1}%");
            result.AddMeasurement("Voltage", $"{voltage}V");
            result.AddMeasurement("Battery Age", $"{batteryAge:F1} years");
            result.AddMeasurement("Status", isWorking ? "Working" : "Not Working");

            // Check charge level
            if (chargeLevel < minChargeThreshold)
            {
                result.AddMeasurement("Charge Status", "CRITICAL - Low charge");
                result.AddDetectedIssue("Low_Battery_Charge");
            }
            else if (chargeLevel < goodChargeThreshold)
            {
                result.AddMeasurement("Charge Status", "Fair - Consider charging");
            }
            else
            {
                result.AddMeasurement("Charge Status", "Good");
            }

            // Check voltage
            if (voltage < minGoodVoltage)
            {
                result.AddMeasurement("Voltage Status", "Low - May need charging or replacement");
            }
            else
            {
                result.AddMeasurement("Voltage Status", "Good");
            }

            // Check corrosion
            if (hasCorrosion)
            {
                result.AddMeasurement("Terminals", "CORRODED");
                result.AddDetectedIssue("Battery_Corrosion");
            }
            else
            {
                result.AddMeasurement("Terminals", "Clean");
            }

            // Check age
            if (batteryAge > oldBatteryAge)
            {
                result.AddMeasurement("Age Status", "Old - Consider replacement");
            }
            else
            {
                result.AddMeasurement("Age Status", "Good");
            }

            // Build display message
            string message = BuildBatteryMessage(chargeLevel, voltage, batteryAge, hasCorrosion, isWorking);
            result.DisplayMessage = message;

            GameLogger.Log($"[BatteryTester] Charge: {chargeLevel}%, Voltage: {voltage}V, Age: {batteryAge:F1} years, Corrosion: {hasCorrosion}");

            return result;
        }

        private string BuildBatteryMessage(double charge, int voltage, float age, bool hasCorrosion, bool isWorking)
        {
            string message = $"Battery Test Results:\n";
            message += $"Charge: {charge:F1}% | Voltage: {voltage}V\n";
            message += $"Age: {age:F1} years | Terminals: {(hasCorrosion ? "CORRODED" : "Clean")}\n";

            if (!isWorking)
            {
                message += "\nBATTERY NOT WORKING - Needs charging or replacement";
            }
            else if (hasCorrosion)
            {
                message += "\nWARNING: Terminal corrosion detected. Clean terminals.";
            }
            else if (charge < minChargeThreshold)
            {
                message += "\nWARNING: Low charge detected. Charge or replace battery.";
            }
            else if (age > oldBatteryAge)
            {
                message += $"\nNOTICE: Battery is {age:F1} years old. Consider replacement.";
            }
            else
            {
                message += "\nBattery is in good condition.";
            }

            return message;
        }

        protected override void OnInspectionStarted()
        {
            ToolUIManager.Instance?.ShowInstruction("Testing battery... Please wait...");
        }

        protected override void OnInspectionComplete(ToolInspectionResult result)
        {
            ToolUIManager.Instance?.ClearInstruction();

            // Additional warnings for critical issues
            if (_targetBattery != null && !_targetBattery.isWorking)
            {
                GameLogger.Log("[BatteryTester] CRITICAL: Battery not working!");
            }
        }

        protected override void OnInspectionCancelled()
        {
            ToolUIManager.Instance?.ClearInstruction();
            base.OnInspectionCancelled();
        }

        protected override void OnTargetInvalid()
        {
            ToolUIManager.Instance?.ShowMessage("Aim at battery to test", 2f);
        }
    }
}
