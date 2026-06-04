using System.Text;
using Core;
using Inspection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the accident history of the current vehicle on a panel
/// inside the computer screen (PCFrame).
///
/// Setup in Unity:
/// 1. Inside PCFrame, create a Panel named "AccidentReportPanel"
/// 2. Stretch anchors to fill the full PCFrame area
/// 3. Add a ScrollRect with Content → TextMeshProUGUI (assign to reportText)
/// 4. Add a close Button (assigned via LaptopHandler)
/// 5. Start the panel DISABLED in the Inspector
/// </summary>
public class AccidentReportScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI reportText;

    public void Show()
    {
        // Only show vehicle data if an inspection is actively running
        IInspectionService inspectionService = null;
        ServiceLocator.TryGet(out inspectionService);

        if (inspectionService == null || !inspectionService.IsInspectionActive)
        {
            reportText.text = "No active inspection.\nStart serving a customer first.";
            gameObject.SetActive(true);
            return;
        }

        Vehicle vehicle = FindAnyObjectByType<Vehicle>();
        if (vehicle == null)
        {
            GameLogger.LogWarning("[AccidentReportScreen] No vehicle in scene.");
            reportText.text = "No vehicle data available.";
        }
        else
        {
            reportText.text = BuildReport(vehicle);
        }

        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private string BuildReport(Vehicle vehicle)
    {
        StringBuilder sb = new StringBuilder();
        var reg = vehicle.Registration;

        sb.AppendLine("<b>VEHICLE INFORMATION</b>");
        sb.AppendLine($"Plate Number    : {reg.PlateNumber}");
        sb.AppendLine($"Brand           : {reg.Brand}");
        sb.AppendLine($"Model           : {reg.Model}");
        sb.AppendLine($"Model Date      : {reg.ModelDateTime:dd.MM.yyyy}");
        sb.AppendLine($"First Reg. Date : {reg.FirstRegistrationDate:dd.MM.yyyy}");
        sb.AppendLine($"Color           : {reg.Color}");
        sb.AppendLine($"Fuel            : {reg.FuelType}");
        sb.AppendLine($"Transmission    : {reg.Transmission}");
        sb.AppendLine($"Engine          : {reg.EngineCapacity}L  /  {reg.MaxHorsePower} HP");
        sb.AppendLine($"Mileage         : {vehicle.milage} km");
        sb.AppendLine();

        sb.AppendLine("<b>ACCIDENT HISTORY</b>");
        sb.AppendLine();

        if (vehicle.AccidentReports == null || vehicle.AccidentReports.Count == 0)
        {
            sb.AppendLine("No accident records found.");
        }
        else
        {
            for (int i = 0; i < vehicle.AccidentReports.Count; i++)
            {
                var report = vehicle.AccidentReports[i];

                sb.AppendLine($"--- Accident #{i + 1} ---");
                sb.AppendLine($"Date         : {report.AccidentDate:dd.MM.yyyy}");
                sb.AppendLine($"Repair Cost  : {report.RepairCost:F0} $");
                sb.AppendLine("Damaged Parts:");

                foreach (var part in report.DamagedParts)
                {
                    sb.AppendLine($"  - {part}");
                }

                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
