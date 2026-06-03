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
            inspectionDuration = 0.4f;
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
            // Update zoom transition
            UpdateZoom();

            // Run the timer + progress bar (base handles CompleteInspection when done)
            base.UpdateInspection();
        }

        /// <summary>
        /// Override FinishJob so releasing the button early cancels instead of
        /// completing — the player must hold for the full duration.
        /// </summary>
        public override void FinishJob(InputAction.CallbackContext context)
        {
            if (context.canceled && isInspecting)
            {
                CancelInspection();
            }
        }

        protected override ToolInspectionResult PerformInspection()
        {
            if (currentTargetPart == null)
            {
                return ToolInspectionResult.CreateFailure("No target selected for inspection.");
            }

            var result = ToolInspectionResult.CreateSuccess(currentTargetPart, "Part inspected.");
            string partType = currentTargetPart.GetType().Name;

            // Detect issues that require Hand tool (skip interaction-only issues like Window_Regulator)
            int detectedCount = 0;
            if (currentTargetPart.assignedIssues != null)
            {
                foreach (var issue in currentTargetPart.assignedIssues)
                {
                    if (issue.RequiredTool == Tool.Handle && !issue.RequiresInteraction)
                    {
                        result.AddDetectedIssue(issue.FailureName);
                        detectedCount++;
                        GameLogger.Log($"[HandHandler] Detected issue: {issue.FailureName} on {currentTargetPart.name}");
                    }
                }
            }

            // Build display message
            string message = $"CLOSE-UP INSPECTION\n\n";
            message += $"Part: {currentTargetPart.name}\n";
            message += $"Type: {partType}\n\n";

            if (detectedCount > 0)
            {
                message += $"Issues Found: {detectedCount}\n";
                foreach (string issueName in result.DetectedIssues)
                {
                    message += $"  - {issueName}\n";
                }
            }
            else
            {
                message += "No visible issues detected.";
            }

            result.DisplayMessage = message;

            GameLogger.Log($"[HandHandler] Inspected {currentTargetPart.name} - Found {detectedCount} issue(s)");

            return result;
        }

        private void ShowPartInfo()
        {
            ToolUIManager.Instance?.ShowInstruction("Close-Up Inspection");
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
