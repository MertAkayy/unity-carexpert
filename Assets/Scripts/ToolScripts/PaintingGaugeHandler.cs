using System;
using PlayerScripts;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mechanical paint thickness gauge handler.
/// Uses physical cylinder animation to simulate the mechanical measurement process.
/// Includes random variance for mechanical inaccuracy.
///
/// Detection logic:
/// - 60-100 μm: Normal (factory paint thickness)
/// - 110-450 μm: Painted_Part (repainted area)
/// - < 60 μm: Possible scratch or wear
/// - > 450 μm: Aftermarket part or body filler
/// </summary>
public class PaintingGaugeHandler : MonoBehaviour, IUsableTool
{
    [Header("References")]
    [SerializeField] private Transform bigCylinder;
    [SerializeField] private Transform smallCylinder;

    [Header("Settings")]
    [SerializeField] private float maxLiftHeight = 8f;
    [SerializeField] private float liftSpeedFactor = 0.05f;
    [SerializeField] private float smallCylinderSpeed = 5f;

    [Header("Measurement Settings")]
    [SerializeField] private float variancePercentage = 0.08f; // ±8% mechanical inaccuracy
    [SerializeField] private float maxInspectionDistance = 3f;
    [SerializeField] private LayerMask targetLayerMask = -1;

    [Header("Paint Thickness Thresholds")]
    [SerializeField] private int minNormalThickness = 60;
    [SerializeField] private int maxNormalThickness = 100;
    [SerializeField] private int paintedThreshold = 110;
    [SerializeField] private int aftermarketThreshold = 450;

    private Vector3 bigStartPos;
    private Vector3 smallStartPos;
    private Vector3 previousMousePosition;

    private bool isDragging = false;
    private bool isSmallCylinderRising = false;
    private bool hasResetTriggered = false;

    private ExteriorPart _targetPart;
    private int _measuredThickness;
    private Player _player;

    private void Awake()
    {
        _player = FindObjectOfType<Player>();
    }

    public void Start()
    {
        CacheStartPositions();
    }

    public void StartJob(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // Find target before starting measurement
            _targetPart = GetTargetPart();

            if (_targetPart == null)
            {
                ToolScripts.UI.ToolUIManager.Instance?.ShowMessage("No exterior part targeted", 2f);
                return;
            }

            BeginDrag();
        }
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
        if (context.canceled && !hasResetTriggered)
        {
            // Cancelled before completion
            ResetPositions();
        }
    }

    private ExteriorPart GetTargetPart()
    {
        if (_player == null) return null;

        PlayerCamera playerCamera = FindObjectOfType<PlayerCamera>();
        if (playerCamera == null) return null;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxInspectionDistance, targetLayerMask))
        {
            return hit.collider.GetComponentInParent<ExteriorPart>();
        }

        return null;
    }

    private void BeginDrag()
    {
        isDragging = true;
        hasResetTriggered = false;

        ToolScripts.UI.ToolUIManager.Instance?.ShowInstruction("Pull the gauge to measure...");
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
            TriggerResetWithResult();
    }

    private void TriggerResetWithResult()
    {
        isSmallCylinderRising = false;
        hasResetTriggered = true;

        // Perform measurement
        PerformMeasurement();

        // Show result BEFORE reset (reset nulls _targetPart)
        DisplayResult();

        // Reset positions
        ResetPositions();
    }

    private void PerformMeasurement()
    {
        if (_targetPart == null) return;

        // Get actual paint thickness
        int actualThickness = _targetPart.paintThickness;

        // Add random variance for mechanical inaccuracy
        float variance = actualThickness * variancePercentage;
        int varianceAmount = Mathf.RoundToInt(UnityEngine.Random.Range(-variance, variance));
        _measuredThickness = Mathf.Max(0, actualThickness + varianceAmount);

        GameLogger.Log($"[MechanicalPaintGauge] Actual: {actualThickness} μm, Measured: {_measuredThickness} μm");
    }

    private void DisplayResult()
    {
        if (_targetPart == null) return;

        string statusMessage = "";
        string detectedIssue = null;

        if (_measuredThickness < minNormalThickness)
        {
            statusMessage = $"Thickness: {_measuredThickness} μm (Below Normal - Possible scratch)";
        }
        else if (_measuredThickness <= maxNormalThickness)
        {
            statusMessage = $"Thickness: {_measuredThickness} μm (Normal - Factory paint)";
        }
        else if (_measuredThickness <= paintedThreshold)
        {
            statusMessage = $"Thickness: {_measuredThickness} μm (Slightly Elevated)";
        }
        else if (_measuredThickness <= aftermarketThreshold)
        {
            detectedIssue = "Painted_Part";
            statusMessage = $"Thickness: {_measuredThickness} μm (REPAINTED PART DETECTED!)";
        }
        else
        {
            detectedIssue = "Painted_Part";
            statusMessage = $"Thickness: {_measuredThickness} μm (AFTERMARKET PART - Very high thickness!)";
        }

        ToolScripts.UI.ToolUIManager.Instance?.ShowMessage(statusMessage, 5f);

        // Add detected issue to predicted issues
        if (!string.IsNullOrEmpty(detectedIssue))
        {
            AddIssueToPredicted(detectedIssue);
        }
    }

    private void AddIssueToPredicted(string issueName)
    {
        if (_targetPart == null) return;

        VehicleManager vehicleManager = FindObjectOfType<VehicleManager>();
        IssueDataBase issueDatabase = vehicleManager != null ? vehicleManager.IssueDatabase : null;
        if (issueDatabase == null)
        {
            GameLogger.LogWarning("IssueDataBase not found in scene.");
            return;
        }

        Issue issue = issueDatabase.GetByName(issueName);
        if (issue != null)
        {
            if (!_targetPart.predictedIssues.Contains(issue))
            {
                _targetPart.predictedIssues.Add(issue);
                GameLogger.Log($"Added predicted issue '{issueName}' to {_targetPart.name}");
            }
        }
    }

    private void ResetPositions()
    {
        isDragging = false;
        bigCylinder.localPosition = bigStartPos;
        smallCylinder.localPosition = smallStartPos;
        _targetPart = null;

        ToolScripts.UI.ToolUIManager.Instance?.ClearInstruction();
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

    // Utilities
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
        previousMousePosition = Input.mousePosition;
        return mouseDelta.magnitude * liftSpeedFactor;
    }
}
