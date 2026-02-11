using UnityEngine;
using UnityEngine.InputSystem;

public interface IUsableTool
{
    void StartJob(InputAction.CallbackContext context);
    void ResumeJob(InputAction.CallbackContext context);
    void FinishJob(InputAction.CallbackContext context);
}
