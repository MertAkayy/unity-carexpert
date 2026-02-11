using DG.Tweening;
using NUnit.Framework.Constraints;
using UnityEngine;

public class DoorController : MonoBehaviour,IInteractable
{
    [SerializeField] private bool _doorDirection=true;
    private bool _doorState = false;
    private void OpenDoor()
    {
        if(_doorDirection)
        transform.DOLocalRotate( new Vector3(0, 90, 0), 1.5f);
        else
            transform.DOLocalRotate( new Vector3(0, -90, 0), 1.5f);
        _doorState = true;
    }
    private void CloseDoor()
    {
        transform.DOLocalRotate( new Vector3(0, 0, 0), 1.5f);
        _doorState = false;
    }

    public void Interact()
    {
        if(!_doorState)
            OpenDoor();
        else
            CloseDoor();
    }
}
