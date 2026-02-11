using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaintingGaugeHandler : MonoBehaviour,IUsableTool
{
    [Header("References")]
    [SerializeField] private Transform bigCylinder;
    [SerializeField] private Transform smallCylinder;

    [Header("Settings")]
    [SerializeField] private float maxLiftHeight = 8f;
    [SerializeField] private float liftSpeedFactor = 0.05f;
    [SerializeField] private float smallCylinderSpeed = 5f;
    
    
    private Vector3 bigStartPos;
    private Vector3 smallStartPos;

    private Vector3 previousMousePosition;

    private bool isDragging = false;
    private bool isSmallCylinderRising = false;
    private bool hasResetTriggered = false;

    public void Start()
    {
        CacheStartPositions();
    }

    public void StartJob(InputAction.CallbackContext context)
    {
      //  if(PlayerDataManager.Instance.)
        BeginDrag();
    }

    public void ResumeJob(InputAction.CallbackContext context)
    {
        if (isDragging && !isSmallCylinderRising && !hasResetTriggered)
            HandleBigCylinderDrag();

    }

    public void Update()
    {
        if (isSmallCylinderRising && !hasResetTriggered)
            MoveSmallCylinderUp();
    }

    public void FinishJob(InputAction.CallbackContext context)
    {
        ResetPositions();
    }
    private void BeginDrag()
    {
        isDragging = true;
        hasResetTriggered = false;
    }

    private void HandleBigCylinderDrag()
    {
        float verticalSpeed = GetMouseDragSpeed();
        MoveBigCylinder(verticalSpeed);

        if (ReachedMaxHeight(bigCylinder, bigStartPos))
            isSmallCylinderRising = true;
    }

    private void MoveSmallCylinderUp()
    {
        Vector3 targetPos = smallStartPos + Vector3.up * maxLiftHeight;
        smallCylinder.localPosition = Vector3.MoveTowards(
            smallCylinder.localPosition,
            targetPos,
            smallCylinderSpeed * Time.deltaTime
        );

        if (HasReachedPosition(smallCylinder, targetPos))
            TriggerReset();
    }

    private void TriggerReset()
    {
        isSmallCylinderRising = false;
        hasResetTriggered = true;
        ResetPositions();
    }

    private void ResetPositions()
    {
        isDragging = false;
        bigCylinder.localPosition = bigStartPos;
        smallCylinder.localPosition = smallStartPos;
    }


    private void CacheStartPositions()
    {
        bigStartPos = bigCylinder.localPosition;
        smallStartPos = smallCylinder.localPosition;
    }

    private void MoveBigCylinder(float speed)
    {
        float targetY = Mathf.Min(
            bigCylinder.localPosition.y + speed,
            bigStartPos.y + maxLiftHeight
        );

        bigCylinder.localPosition = new Vector3(
            bigCylinder.localPosition.x,
            targetY,
            bigCylinder.localPosition.z
        );
    }
//Utilities
    private bool ReachedMaxHeight(Transform obj, Vector3 startPos)
    {
        return obj.localPosition.y >= startPos.y + maxLiftHeight - 0.01f;
    }

    private bool HasReachedPosition(Transform obj, Vector3 target)
    {
        return Vector3.Distance(obj.localPosition, target) < 0.01f;
    }
    
    private float GetMouseDragSpeed()
    {
        Vector3 mouseDelta = Input.mousePosition - previousMousePosition;
        return mouseDelta.magnitude * liftSpeedFactor;
    }
}