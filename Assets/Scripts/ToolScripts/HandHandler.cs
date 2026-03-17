using System;
using PlayerScripts;
using UnityEngine;
using UnityEngine.InputSystem;
using ToolScripts.Base;
using ToolScripts.UI;

namespace ToolScripts
{
    /// <summary>
    /// Hand handler for close-up visual inspection.
    /// Allows player to inspect any vehicle part closely and add manual notes.
    ///
    /// Features:
    /// - Close-up visual inspection mode
    /// - Camera zoom to target when inspecting
    /// - Manual issue notes functionality
    /// - Works with any VehiclePart
    /// - Shows part details when inspecting
    /// - "Add Note" button to add custom observation
    /// - Press and hold to zoom and inspect
    /// </summary>
    public class HandHandler : ToolHandlerBase
    {
        [Header("Inspection Settings")]
        [SerializeField] private float zoomDistance = 1.5f; // Distance to zoom to
        [SerializeField] private float zoomSpeed = 3f; // Speed of zoom transition
        [SerializeField] private float fieldOfView = 30f; // Zoomed FOV

        [Header("Camera")]
        [SerializeField] private float normalFieldOfView = 60f;

        private PlayerCamera _playerCamera;
        private Vector3 _originalCameraPosition;
        private float _originalFieldOfView;
        private bool _isZoomed = false;

        protected override void Awake()
        {
            base.Awake();
            toolType = Tool.Handle;
            toolName = "Close-Up Inspection";
            inspectionDuration = 0f; // Manual inspection - no timer
            compatiblePartInterfaces = new string[0]; // Works with any part
        }

        protected override void Start()
        {
            base.Start();
            _playerCamera = FindObjectOfType<PlayerCamera>();
            if (_playerCamera != null)
            {
                // Get the camera component
                Camera cam = _playerCamera.GetComponent<Camera>();
                if (cam != null)
                {
                    _originalFieldOfView = cam.fieldOfView;
                }
            }
        }

        protected override VehiclePart GetTargetPart()
        {
            Player player = FindObjectOfType<Player>();
            if (player != null)
            {
                return player.GetTargetPart();
            }
            return base.GetTargetPart();
        }

        protected override bool ValidateTarget()
        {
            // Hand works with any VehiclePart
            return currentTargetPart != null;
        }

        protected override void BeginInspection()
        {
            base.BeginInspection();

            // Enter inspection mode (zoom in)
            StartZoomIn();

            // Show part information
            ShowPartInfo();

            // Keep inspection running until released
            isInspecting = true;
        }

        protected override void UpdateInspection()
        {
            // No timer for hand inspection - continues until released
            // Update zoom transition
            UpdateZoom();
        }

        protected override ToolInspectionResult PerformInspection()
        {
            if (currentTargetPart == null)
            {
                return ToolInspectionResult.CreateFailure("No target selected for inspection.");
            }

            var result = ToolInspectionResult.CreateSuccess(currentTargetPart, "Part inspected.");
            string partType = currentTargetPart.GetType().Name;
            result.AddMeasurement("Part Type", partType);
            result.AddMeasurement("Part Name", currentTargetPart.name);

            // Add unique type if available
            if (currentTargetPart.partUniqueType != null)
            {
                result.AddMeasurement("Part Location", currentTargetPart.partUniqueType.ToString());
            }

            // Check for existing issues
            int assignedCount = currentTargetPart.assignedIssues.Count;
            int predictedCount = currentTargetPart.predictedIssues.Count;

            result.AddMeasurement("Assigned Issues", assignedCount.ToString());
            result.AddMeasurement("Predicted Issues", predictedCount.ToString());

            // Build display message
            string message = $"CLOSE-UP INSPECTION\n\n";
            message += $"Part: {currentTargetPart.name}\n";
            message += $"Type: {partType}\n";
            message += $"Assigned Issues: {assignedCount}\n";
            message += $"Predicted Issues: {predictedCount}\n\n";
            message += "Manual observation mode.\nUse Add Note to record findings.";

            result.DisplayMessage = message;

            GameLogger.Log($"[HandHandler] Inspected {currentTargetPart.name}");

            return result;
        }

        private void ShowPartInfo()
        {
            if (currentTargetPart == null) return;

            string info = $"Inspecting: {currentTargetPart.name}\n";
            info += $"Type: {currentTargetPart.GetType().Name}";

            ToolUIManager.Instance?.ShowInstruction(info);
        }

        private void StartZoomIn()
        {
            if (_playerCamera == null || currentTargetPart == null) return;

            _originalCameraPosition = _playerCamera.transform.position;
            _isZoomed = true;

            GameLogger.Log("[HandHandler] Starting close-up inspection");
        }

        private void UpdateZoom()
        {
            if (!_isZoomed || _playerCamera == null || currentTargetPart == null) return;

            Camera cam = _playerCamera.GetComponent<Camera>();
            if (cam == null) return;

            // Smoothly change FOV for zoom effect
            float targetFOV = _isZoomed ? fieldOfView : normalFieldOfView;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
        }

        private void ResetZoom()
        {
            if (_playerCamera == null) return;

            Camera cam = _playerCamera.GetComponent<Camera>();
            if (cam != null)
            {
                cam.fieldOfView = normalFieldOfView;
            }

            _isZoomed = false;
        }

        protected override void CompleteInspection()
        {
            base.CompleteInspection();

            // Exit inspection mode (zoom out)
            ResetZoom();
        }

        protected override void CancelInspection()
        {
            base.CancelInspection();

            // Exit inspection mode
            ResetZoom();
        }

        /// <summary>
        /// Adds a manual note to the current part
        /// </summary>
        public void AddManualNote(string note)
        {
            if (currentTargetPart == null)
            {
                ToolUIManager.Instance?.ShowMessage("No part selected to add note", 2f);
                return;
            }

            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.AddNoteToVehicle(
                    $"[{currentTargetPart.name}] {note}"
                );
                ToolUIManager.Instance?.ShowMessage("Note added to vehicle", 2f);
                GameLogger.Log($"[HandHandler] Added manual note to {currentTargetPart.name}: {note}");
            }
        }

        protected override void OnInspectionCancelled()
        {
            ToolUIManager.Instance?.ClearInstruction();
            base.OnInspectionCancelled();
        }

        protected override void OnTargetInvalid()
        {
            ToolUIManager.Instance?.ShowMessage("Aim at a part to inspect", 2f);
        }
    }
}
