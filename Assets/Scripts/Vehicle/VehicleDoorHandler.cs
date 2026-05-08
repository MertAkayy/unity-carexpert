using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class VehicleDoorHandler : MonoBehaviour,IInteractable
{
    
    [SerializeField] private Transform windowTransform;
    [SerializeField] private VehicleGlass linkedGlass;
    private bool _isWindowOpen=false;
    private Vector3 _windowPosition;
    private Vector3 _windowRotation;
    [SerializeField] private WindowLocation windowLocation;
    [SerializeField] private Transform windowEndPosition;
    public enum WindowLocation
    {
        FrontLeft,
        FrontRight,
        RearLeft,
        RearRight
    }
    private void Start()
    {
        _windowPosition = windowTransform.localPosition;
        _windowRotation = windowTransform.localRotation.eulerAngles;
    }

    private bool HasRegulatorFailure()
    {
        if (linkedGlass == null) return false;

        VehicleManager vehicleManager = FindObjectOfType<VehicleManager>();
        if (vehicleManager?.IssueDatabase == null) return false;

        Issue issue = vehicleManager.IssueDatabase.GetByName("Window_Regulator_Failure");
        if (issue == null || !linkedGlass.assignedIssues.Contains(issue)) return false;

        if (!linkedGlass.predictedIssues.Contains(issue))
        {
            linkedGlass.predictedIssues.Add(issue);
            GameLogger.Log($"[VehicleDoorHandler] 'Window_Regulator_Failure' detected on '{linkedGlass.name}' — added to predictedIssues");
        }

        return true;
    }

    private void OpenWindow()
    {
        if (HasRegulatorFailure())
        {
            GameLogger.Log($"[VehicleDoorHandler] Window cannot open — regulator failure.");
            return;
        }
        else
        {
            GameLogger.Log($"[VehicleDoorHandler] Window opened");
        }

        windowTransform.DOLocalMove(windowEndPosition.localPosition, 1.5f);
        windowTransform.DOLocalRotate(windowEndPosition.localEulerAngles, 1.5f);

        _isWindowOpen=true;
    }
    private void CloseWindow()
    {
        windowTransform.DOLocalMove(_windowPosition, 1.5f);
        windowTransform.DOLocalRotate(_windowRotation, 1.5f);
        _isWindowOpen=false;
    }
    public void Interact()
    {
        if (!_isWindowOpen)
        {
            OpenWindow();
        }
        else
        {
            CloseWindow();
        }
    }
}
