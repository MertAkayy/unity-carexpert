using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

public class VehicleDoorHandler : MonoBehaviour,IInteractable
{
    
    [SerializeField] private Transform windowTransform;
    private bool _isWindowOpen=false;
    private Vector3 _windowPosition;
    private Vector3 _windowRotation;
    [SerializeField] private WindowLocation windowLocation;
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
    private void OpenFrontLeftWindow()
    {
        windowTransform.DOLocalMove(new Vector3(_windowPosition.x-0.095f,_windowPosition.y-0.2668764f,_windowPosition.z+0.1270038f), 1.5f);
        windowTransform.DOLocalRotate(new Vector3(_windowRotation.x,_windowRotation.y,_windowRotation.z+8.9f), 1.5f);
        _isWindowOpen=true;
    }
    private void OpenFrontRightWindow()
    {
        windowTransform.DOLocalMove(new Vector3(_windowPosition.x+0.095f,_windowPosition.y-0.2668764f,_windowPosition.z+0.1270038f), 1.5f);
        windowTransform.DOLocalRotate(new Vector3(_windowRotation.x,_windowRotation.y,_windowRotation.z-8.9f), 1.5f);
        _isWindowOpen=true;
    }
    private void OpenRearLeftWindow()
    {
        windowTransform.DOLocalMove(new Vector3(_windowPosition.x-0.1592976f,_windowPosition.y-0.3662445f,_windowPosition.z+0.0329833f), 1.5f);
        _isWindowOpen=true;
    }
    private void OpenRearRightWindow()
    {
        windowTransform.DOLocalMove(new Vector3(_windowPosition.x+0.1592976f,_windowPosition.y-0.3662445f,_windowPosition.z+0.0329833f), 1.5f);
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
            switch (windowLocation)
            {
                case WindowLocation.FrontLeft:
                    OpenFrontLeftWindow();
                    break;
                case WindowLocation.FrontRight:
                    OpenFrontRightWindow();
                    break;
                case WindowLocation.RearLeft:
                    OpenRearLeftWindow();
                    break;
                case WindowLocation.RearRight:
                    OpenRearRightWindow();
                    break;
            }
        }
        else
            CloseWindow();
    }
}
