using System;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;


public class ExteriorPart : VehiclePart ,IInteractable,IExteriorPart,IReadable
{ 
    public int paintThickness;
    [SerializeField] private string productionName;
    private DateTime _productionDateTime;
    private VehicleRegistration _vehicleRegistration;
    public bool IsPartReplaced { get; set; } = false;

    private static readonly string[] AftermarketBrands =
        { "Valeo", "TRW", "Bosch", "Denso", "Monroe", "Brembo", "Hella", "SKF", "Febi", "Lemforder" };
    public bool IsPartPainted { get; set; } = false;
    public bool IsPartDentRepaired { get; set; } = false;
    public bool IsPartRepaired { get; set; } = false;
    public bool hingedPart = false;
    private PaintZone _paintZone;
    private PaintZoneThresholds _paintZoneThresholds;
    public ExteriorPartPosition partPosition;
    [SerializeField] private OpeningDirection openingDirection = OpeningDirection.Upward;
    private bool _partOpeningState = false;
    private enum OpeningDirection
    {
        None,
        Upward,
        Downward,
        Right,
        Left
        
    }
    public void InitializePart(VehicleRegistration registration)
    {
        _vehicleRegistration = registration;
        productionName = registration.Brand;
        _productionDateTime = registration.ModelDateTime;
    }

    public override void AssignIssue(Issue issue)
    {
        if (issue.IsValidFor(this) && !assignedIssues.Contains(issue))
        {
            if (String.Compare(issue.FailureName, "Replaced_Part", StringComparison.Ordinal) == 0)
            {
                assignedIssues.Clear();
                IsPartReplaced = true;
                IsPartPainted = false;
                IsPartDentRepaired = false;
                IsPartRepaired = true;
                // Reset paint thickness to normal range since Replaced_Part
                // clears previous issues (including any Painted_Part that may
                // have set a high thickness value)
                paintThickness = UnityEngine.Random.Range(60, 100);
                if (_vehicleRegistration != null)
                {
                    productionName = GetAftermarketBrand(_vehicleRegistration.Brand);
                    _productionDateTime = GetReplacedPartDate(_vehicleRegistration.ModelDateTime);
                }
            }
            else if (String.Compare(issue.FailureName, "Painted_Part", StringComparison.Ordinal) == 0)
            {
                IsPartPainted = true;
                IsPartRepaired = true;
                paintThickness=UnityEngine.Random.Range(110, 450);
                Debug.Log(this.GetInstanceID()+"   ++++   "+ partPosition+" : Painted_Part : "+paintThickness);
            }
            else if (String.Compare(issue.FailureName, "Dent_Repaired", StringComparison.Ordinal) == 0)
            {
                IsPartDentRepaired = true;
                IsPartRepaired = true; 
            }
            
            assignedIssues.Add(issue);
        }
    }
    public void Interact()
    {
        if (!_partOpeningState)
            switch (openingDirection)
            {
                case  OpeningDirection.None:
                    break;
                case OpeningDirection.Upward: OpenUpward();
                    break;
                case OpeningDirection.Downward: OpenDawnward();
                    break;
                case OpeningDirection.Right: OpenRight();
                    break;
                case OpeningDirection.Left: OpenLeft();
                    break;
            }
        else
        ClosePart();
    }
    
    private void OpenUpward()
    {
    transform.DOLocalRotate(new Vector3(60,0,0), 1.5f);
    _partOpeningState=true;
    }
    private void OpenDawnward()
    {
        transform.DOLocalRotate(new Vector3(-60,0,0), 1.5f);
        _partOpeningState=true;
    }
    private void OpenRight()
    {
        transform.DOLocalRotate(new Vector3(0,-60,0), 1.5f);
        _partOpeningState=true;
    }
    private void OpenLeft()
    {
        transform.DOLocalRotate(new Vector3(0,60,0), 1.5f);
        _partOpeningState=true;
    }
    private void ClosePart()
    {
        transform.DOLocalRotate(new Vector3(0,0,0), 1.5f);
        _partOpeningState=false;
    }

    public void Read()
    {
        GameLogger.Log("[ExteriorPart] Reading part");
        GameLogger.Log(productionName + "\n" + _productionDateTime);

        string message = "PART LABEL\n\n";
        message += $"Part: {name}\n";
        message += $"Brand: {productionName}\n";
        message += $"Production Date: {_productionDateTime:yyyy-MM}\n";

        bool replaced = DetectReplacedPartFromLabel();

        if (replaced)
        {
            message += "\n⚠ REPLACED PART DETECTED!";
        }

        ShowReadResult(message);
    }

    private bool DetectReplacedPartFromLabel()
    {
        if (_vehicleRegistration == null) return false;

        bool brandMismatch = !string.Equals(productionName, _vehicleRegistration.Brand, StringComparison.OrdinalIgnoreCase);
        bool dateMismatch = _productionDateTime.Year != _vehicleRegistration.ModelDateTime.Year;

        if (!brandMismatch && !dateMismatch) return false;

        if (brandMismatch)
            GameLogger.Log($"[ExteriorPart] Brand mismatch on {name}: label='{productionName}' registration='{_vehicleRegistration.Brand}'");
        if (dateMismatch)
            GameLogger.Log($"[ExteriorPart] Date mismatch on {name}: label='{_productionDateTime:yyyy}' registration='{_vehicleRegistration.ModelDateTime:yyyy}'");

        VehicleManager vehicleManager = FindObjectOfType<VehicleManager>();
        if (vehicleManager?.IssueDatabase == null) return false;

        Issue issue = vehicleManager.IssueDatabase.GetByName("Replaced_Part");
        if (issue == null) return false;

        if (!predictedIssues.Contains(issue))
        {
            predictedIssues.Add(issue);
            GameLogger.Log($"[ExteriorPart] 'Replaced_Part' added to predicted issues on {name}");
        }

        return true;
    }

    private string GetAftermarketBrand(string originalBrand)
    {
        var options = AftermarketBrands
            .Where(b => !b.Equals(originalBrand, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return options[UnityEngine.Random.Range(0, options.Count)];
    }

    private DateTime GetReplacedPartDate(DateTime originalDate)
    {
        DateTime minDate = originalDate.AddYears(1);
        DateTime maxDate = DateTime.Now.AddMonths(-1);
        if (minDate >= maxDate)
            minDate = originalDate.AddMonths(6);
        int daysRange = (int)(maxDate - minDate).TotalDays;
        if (daysRange <= 0)
            return originalDate.AddYears(2);
        return minDate.AddDays(UnityEngine.Random.Range(0, daysRange));
    }
}

public enum ExteriorPartPosition
{
    FrontBumper,
    RearBumper,
    FrontLeftDoor,
    FrontRightDoor,
    RearLeftDoor,
    RearRightDoor,
    Hood,  
    Trunk,
    FrontLeftFender,
    FrontRightFender,
    RearLeftFender,
    RearRightFender,
    Roof
}