using System;
using System.Collections.Generic;
using PlayerScripts;
using UnityEngine;
using UnityEngine.InputSystem;
using ToolScripts.Base;
using ToolScripts.UI;

namespace ToolScripts.Base
{
    /// <summary>
    /// Abstract base class for all tool handlers.
    /// Implements IUsableTool and provides common inspection functionality.
    /// </summary>
    public abstract class ToolHandlerBase : MonoBehaviour, IUsableTool
    {
        [Header("Tool Configuration")]
        [SerializeField] protected Tool toolType = Tool.Null;
        [SerializeField] protected float inspectionDuration = 2f;
        [SerializeField] protected float maxInspectionDistance = 3f;
        [SerializeField] protected string toolName = "Tool";

        [Header("Target Settings")]
        [SerializeField] protected LayerMask targetLayerMask = -1;
        [SerializeField] protected string[] compatiblePartInterfaces;

        [Header("Audio")]
        [SerializeField] protected AudioClip inspectionStartSound;
        [SerializeField] protected AudioClip inspectionCompleteSound;
        [SerializeField] protected AudioClip inspectionFailSound;

        protected VehiclePart currentTargetPart;
        protected bool isInspecting = false;
        protected float inspectionProgress = 0f;
        protected ToolInspectionResult lastResult;
        protected Player player;

        protected AudioSource _audioSource;

        #region Properties
        public Tool ToolType => toolType;
        public bool IsInspecting => isInspecting;
        public float InspectionProgress => inspectionProgress;
        public VehiclePart CurrentTarget => currentTargetPart;
        #endregion

        #region Unity Lifecycle
        protected virtual void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        protected virtual void Start()
        {
            player = FindObjectOfType<Player>();
        }

        protected virtual void Update()
        {
            if (isInspecting)
            {
                UpdateInspection();
            }
        }
        #endregion

        #region IUsableTool Implementation
        public virtual void StartJob(InputAction.CallbackContext context)
        {
          
            if (context.performed)
            {
               
                TryStartInspection();
            }
        }

        public virtual void ResumeJob(InputAction.CallbackContext context)
        {
            // Default behavior - override if needed for continuous interaction
        }

        public virtual void FinishJob(InputAction.CallbackContext context)
        {
            if (context.canceled)
            {
                CancelInspection();
            }
        }
        #endregion

        #region Inspection Flow
        protected virtual void TryStartInspection()
        {
            Debug.Log("TryStartInspection");
            if (isInspecting)
            {
                CancelInspection();
                return;
            }
            Debug.Log("TryStartInspection is inspectinbg true");
            currentTargetPart = GetTargetPart();
            if (!ValidateTarget())
            {
                OnTargetInvalid();
                return;
            }

            BeginInspection();
        }

        protected virtual VehiclePart GetTargetPart()
        {
            Debug.Log("GET TARGET PART");
            if (player == null)
            {
                player = FindObjectOfType<Player>();
                if (player == null) return null;
            }
            // Use PlayerCamera for raycasting
            PlayerCamera playerCamera = FindObjectOfType<PlayerCamera>();
            if (playerCamera == null)
            {
                return null;
            }
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxInspectionDistance, targetLayerMask))
            {
                return hit.collider.GetComponentInParent<VehiclePart>();
            }

            return null;
        }

        protected virtual void BeginInspection()
        {
            isInspecting = true;
            inspectionProgress = 0f;

            OnInspectionStarted();

            if (inspectionStartSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(inspectionStartSound);
            }

            ToolUIManager.Instance?.ShowProgress(toolName, inspectionDuration);
        }

        protected virtual void UpdateInspection()
        {
            inspectionProgress += Time.deltaTime;

            ToolUIManager.Instance?.UpdateProgress(inspectionProgress / inspectionDuration);

            if (inspectionProgress >= inspectionDuration)
            {
                CompleteInspection();
            }
        }

        protected virtual void CompleteInspection()
        {
            lastResult = PerformInspection();

            isInspecting = false;
            inspectionProgress = 0f;

            OnInspectionComplete(lastResult);

            ToolUIManager.Instance?.HideProgress();

            if (lastResult.Success)
            {
                if (inspectionCompleteSound != null && _audioSource != null)
                {
                    _audioSource.PlayOneShot(inspectionCompleteSound);
                }

                AddDetectedIssuesToPart(lastResult);
                ToolUIManager.Instance?.ShowResult(lastResult);
            }
            else
            {
                if (inspectionFailSound != null && _audioSource != null)
                {
                    _audioSource.PlayOneShot(inspectionFailSound);
                }

                ToolUIManager.Instance?.ShowMessage(lastResult.DisplayMessage);
            }

            currentTargetPart = null;
        }

        protected virtual void CancelInspection()
        {
            if (!isInspecting) return;

            isInspecting = false;
            inspectionProgress = 0f;

            OnInspectionCancelled();

            ToolUIManager.Instance?.HideProgress();
            currentTargetPart = null;
        }
        #endregion

        #region Abstract Methods
        /// <summary>
        /// Validates if the current target is compatible with this tool.
        /// </summary>
        /// <returns>True if target is valid for inspection</returns>
        protected abstract bool ValidateTarget();

        /// <summary>
        /// Performs the actual inspection and returns the result.
        /// Called after inspection duration completes.
        /// </summary>
        /// <returns>Inspection result with measurements and detected issues</returns>
        protected abstract ToolInspectionResult PerformInspection();
        #endregion

        #region Virtual Methods for Override
        protected virtual void OnInspectionStarted() { }
        protected virtual void OnInspectionComplete(ToolInspectionResult result) { }
        protected virtual void OnInspectionCancelled() { }
        protected virtual void OnTargetInvalid()
        {
            ToolUIManager.Instance?.ShowMessage("No valid target or too far away", 2f);
        }
        #endregion

        #region Helper Methods
        protected virtual void AddDetectedIssuesToPart(ToolInspectionResult result)
        {
            if (result.TargetPart == null || result.DetectedIssues == null) return;

            // Get IssueDataBase via VehicleManager
            VehicleManager vehicleManager = FindObjectOfType<VehicleManager>();
            IssueDataBase issueDatabase = vehicleManager != null ? vehicleManager.IssueDatabase : null;
            if (issueDatabase == null)
            {
                GameLogger.LogWarning("IssueDataBase not found in scene.");
                return;
            }

            foreach (string issueName in result.DetectedIssues)
            {
                Issue issue = issueDatabase.GetByName(issueName);
                if (issue != null)
                {
                    // Only add to predictedIssues, not assignedIssues
                    if (!result.TargetPart.predictedIssues.Contains(issue))
                    {
                        result.TargetPart.predictedIssues.Add(issue);
                        GameLogger.Log($"Added predicted issue '{issueName}' to {result.TargetPart.name}");
                    }
                }
            }
        }

        protected virtual bool IsInterfaceCompatible(VehiclePart part)
        {
            if (part == null || compatiblePartInterfaces == null || compatiblePartInterfaces.Length == 0)
                return false;

            foreach (string interfaceName in compatiblePartInterfaces)
            {
                Type interfaceType = Type.GetType($"Vehicle.{interfaceName}");
                if (interfaceType != null && interfaceType.IsInstanceOfType(part))
                    return true;
            }

            return false;
        }

        protected T GetComponentFromPart<T>(VehiclePart part) where T : class
        {
            if (part == null) return null;
            return part.GetComponent<T>();
        }
        #endregion
    }
}
