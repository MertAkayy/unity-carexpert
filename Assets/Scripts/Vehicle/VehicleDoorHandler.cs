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

    private void OpenWindow()
    {
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
