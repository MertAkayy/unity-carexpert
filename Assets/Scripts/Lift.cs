using System;
using DG.Tweening;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.Serialization;

public class Lift : MonoBehaviour,IInteractable
{
   [SerializeField] BoxCollider liftCollider;
   [SerializeField] private Transform liftChild;
   private bool _playerOnButton=false;
   private enum LiftAnimationState
   {
     Up,
     Down
   }
   private LiftAnimationState _liftAnimationState = LiftAnimationState.Down;

   public void Interact()
   {
       if (!_playerOnButton)
           return;
       switch (_liftAnimationState)
       {
           case LiftAnimationState.Up:
               LiftDownward();
               _liftAnimationState = LiftAnimationState.Down;
               break;
           case LiftAnimationState.Down:
               LiftUpward();
               _liftAnimationState = LiftAnimationState.Up;
               break;
       }
   }
    private void LiftUpward()
    {
        liftChild.DOLocalMove( new Vector3(0, 2.75f, 0), 2.5f).SetEase(Ease.Linear);
    }
    private void LiftDownward()
    {
        liftChild.DOLocalMove( new Vector3(0, 0.15f, 0), 2.5f).SetEase(Ease.Linear);
    }
    private void OnTriggerEnter(Collider other)
    {
        _playerOnButton=true;
    }

    private void OnTriggerExit(Collider other)
    {
        _playerOnButton=false;
    }
}
