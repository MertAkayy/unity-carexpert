using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;


public class ExteriorPart : VehiclePart ,IInteractable,IExteriorPart,IReadable
{ 
    public int paintThickness;
    [SerializeField] private string productionName;
    private DateTime _productionDateTime;
    public bool IsPartReplaced { get; set; } = false;
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

            if (String.Compare(issue.FailureName, "Lock_Actuator_Failure", StringComparison.Ordinal) == 0 &&
                this.hingedPart == false) 
                return;
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
        DebugToScreen.ShowMessage("Label: \n"+productionName+"\n"+_productionDateTime,5F);
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