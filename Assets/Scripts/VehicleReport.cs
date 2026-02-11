using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class VehicleReport : MonoBehaviour
{
    public static VehicleReport Instance;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private Button confirmButton;
    
    private Vehicle _vehicle;
    private List<VehiclePart> _allParts=new List<VehiclePart>();
    public readonly Dictionary<string, List<Issue>> AssignedIssuesToCarPart = new Dictionary<string, List<Issue>>();
    
    [SerializeField] private TextMeshProUGUI registrationText;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        confirmButton.onClick.AddListener(OnConfirmation);
    }


    public void RefreshInfo()
    {
        _vehicle = FindAnyObjectByType<Vehicle>();
        if (_vehicle == null) 
            GameLogger.LogError("No vehicle found");
        RefreshRegistrationInformation();
        RefreshIssues();
    }

    public void OnConfirmation()
    {
        
    }
    private void RefreshRegistrationInformation()
    {
        if (_vehicle != null)
        {
            registrationText.text =
                "Brand : " + _vehicle.Registration.Brand + "\n" +
                "Model : " + _vehicle.Registration.Model + "\n" +
                "Engine Number : " + _vehicle.Registration.EngineNumber + "\n" +
                "Chassis Number : " + _vehicle.Registration.ChassisNumber + "\n" +
                "First Registration Date : " + _vehicle.Registration.FirstRegistrationDate.ToString("dd.MM.yyyy") +
                "\n" +
                "Model Date : " + _vehicle.Registration.ModelDateTime.ToString("dd.MM.yyyy") + "\n" +
                "Milage : " + _vehicle.milage.ToString() + "\n" +
                "---Accident Reports--- \n";
            foreach (var report in _vehicle.AccidentReports)
            {
                registrationText.text += "Accident Date : " + report.AccidentDate.ToString("dd.MM.yyyy") + "\n" +
                                         "Damaged Parts : \n";
                foreach (var part in report.DamagedParts)
                {
                    registrationText.text += part.ToString() + "\n";
                }
                registrationText.text += "\n" + "Accident Cost : " + report.RepairCost + " $ ";
            }
        }
        else
        {
            GameLogger.LogWarning("VehicleUIManager: No Vehicle Found");
            registrationText.text="No Vehicle Found";
        }
    }
    private void RefreshIssues()
    {
        _allParts.AddRange(_vehicle.exteriorParts);
        _allParts.AddRange(_vehicle.wheels);
        _allParts.AddRange(_vehicle.glasses);
        _allParts.AddRange(_vehicle.lights);
        _allParts.Add(_vehicle.battery);
        _allParts.Add(_vehicle.engine);
        _allParts.Add(_vehicle.radiator);
        StringBuilder sb = new StringBuilder();
        foreach (var var in _allParts)
        {
            if (var.assignedIssues.Count > 0 || var.predictedIssues.Count > 0)
            {
                sb.AppendLine(Utilities.EnumToString(var.partUniqueType));
                foreach (var issue in var.assignedIssues)
                {
                    sb.AppendLine("Assigned issue: " + issue);
                }
                foreach (var issue in var.predictedIssues)
                {
                    sb.AppendLine("Predicted issue: " + issue);
                }   
            }

        }
        infoText.text = sb.ToString();
    }
}
