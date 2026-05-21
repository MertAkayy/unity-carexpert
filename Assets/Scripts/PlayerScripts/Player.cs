using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerScripts
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter playerCharacter;
        [SerializeField]  private PlayerDataManager playerDataManager;
        [SerializeField] private PlayerCamera playerCamera;

        [Header("Tool Targeting")]
        [SerializeField] private LayerMask vehiclePartLayerMask = 1 << 6; // Default to layer 6
        [SerializeField] private float maxToolDistance = 3f;
        [SerializeField] private Material highlightMaterial;

        private IUsableTool _tool;
        private PlayerInputActions _playerInputActions;
        private VehiclePart _currentTargetedPart;
        private Material _originalMaterial;
        private Renderer _targetRenderer;
        void Start()
        {
             Cursor.lockState = CursorLockMode.Locked;
            _playerInputActions = new PlayerInputActions();
            playerCharacter.Initialize();
            playerCamera.Initialize(playerCharacter.GetCameraTransform());
            _playerInputActions.Enable();
            _playerInputActions.Gameplay.Grab.performed  += PerformGrab;
            _playerInputActions.Gameplay.Grab.performed  += PerformInteract;
            _playerInputActions.Gameplay.Job.performed += DoJob;
            _playerInputActions.Gameplay.PointerPosition.performed += ResumeJob;
            _playerInputActions.Gameplay.Job.canceled += CancelJob;
            _playerInputActions.Gameplay.Read.performed += PerformRead;

            if (playerDataManager != null)
                playerDataManager.OnToolChanged += OnToolChanged;

            // Initial tool will be set when PlayerDataManager.Start() calls SelectTool
            _tool = GetComponentInChildren<IUsableTool>();
        }

        private void OnToolChanged(IUsableTool newTool)
        {
            _tool = newTool;
        }

        private void PerformRead(InputAction.CallbackContext obj)
        {
            playerDataManager.CanReadDocument();
        }

        private void DoJob(InputAction.CallbackContext context)
        {
            if (_tool == null) return;
            _tool.StartJob(context);
        }
        private void ResumeJob(InputAction.CallbackContext obj)
        {
            if (_tool == null) return;
            _tool.ResumeJob(obj);
        }

        private void CancelJob(InputAction.CallbackContext obj)
        {
            if (_tool == null) return;
            _tool.FinishJob(obj);
        }

        private void PerformInteract(InputAction.CallbackContext obj)
        {
            if (playerCharacter.IsSeated)
            {
                if (playerDataManager.HasActiveInteractable())
                    playerDataManager.CanInteract();
                else
                    playerCharacter.ExitSeat();
                return;
            }
            playerDataManager.CanInteract();
        }

        private void PerformGrab(InputAction.CallbackContext obj)
        {
            if(playerDataManager!=null)
                playerDataManager.CanGrabObejct();
        }
        void OnDestroy()
        {
            if (playerDataManager != null)
                playerDataManager.OnToolChanged -= OnToolChanged;
            _playerInputActions.Dispose();
        }

        void Update()
        {
            var input = _playerInputActions.Gameplay;
            var deltaTime = Time.deltaTime;
            var cameraInput = new CameraInput { Look = input.Look.ReadValue<Vector2>() };
            playerCamera.UpdateRotation(cameraInput);
            var characterInput = new CharacterInput
            {
                Rotation = playerCamera.transform.rotation,
                Move = input.Move.ReadValue<Vector2>(),
                Jump = input.Jump.WasPressedThisFrame(),
                Crouch = input.Crouch.WasPressedThisFrame()
                    ? CrouchInput.Toggle
                    : CrouchInput.None,
                Grab = input.Grab.WasPressedThisFrame()
            };
            playerCharacter.UpdateInput(characterInput);
            playerCharacter.UpdateBody(deltaTime);

            // Fallback: if tool is null (e.g. Start order issue), try finding it
            if (_tool == null)
            {
                _tool = GetComponentInChildren<IUsableTool>();
            }

            UpdateTargetHighlight();
        }

        private void LateUpdate()
        {
            playerCamera.UpdatePosition(playerCharacter.GetCameraTransform());
        }

        #region Tool Targeting System
        /// <summary>
        /// Gets the vehicle part currently targeted by the player's camera
        /// </summary>
        /// <returns>The targeted VehiclePart or null if none found</returns>
        public VehiclePart GetTargetPart()
        {
            if (playerCamera == null) return null;

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxToolDistance, vehiclePartLayerMask))
            {
                return hit.collider.GetComponentInParent<VehiclePart>();
            }

            return null;
        }

        /// <summary>
        /// Updates the visual highlight on the targeted part
        /// </summary>
        private void UpdateTargetHighlight()
        {
            VehiclePart newTarget = GetTargetPart();

            // If target changed, clear old highlight
            if (newTarget != _currentTargetedPart)
            {
                ClearHighlight();
                _currentTargetedPart = newTarget;

                // Apply new highlight if target exists and tool is equipped
                if (_currentTargetedPart != null && _tool != null)
                {
                    ApplyHighlight();
                }
            }
        }

        /// <summary>
        /// Applies highlight material to the current target
        /// </summary>
        private void ApplyHighlight()
        {
            if (_currentTargetedPart == null) return;

            _targetRenderer = _currentTargetedPart.GetComponent<Renderer>();
            if (_targetRenderer != null && highlightMaterial != null)
            {
                _originalMaterial = _targetRenderer.material;
                _targetRenderer.material = highlightMaterial;
            }
        }

        /// <summary>
        /// Removes highlight from the current target
        /// </summary>
        private void ClearHighlight()
        {
            if (_targetRenderer != null && _originalMaterial != null)
            {
                _targetRenderer.material = _originalMaterial;
            }
            _targetRenderer = null;
            _originalMaterial = null;
        }

        /// <summary>
        /// Checks if a target is valid for the current tool
        /// </summary>
        /// <param name="target">The part to check</param>
        /// <returns>True if the target is valid and within range</returns>
        public bool IsTargetValid(VehiclePart target)
        {
            if (target == null) return false;
            if (playerCamera == null) return false;

            float distance = Vector3.Distance(playerCamera.transform.position, target.transform.position);
            return distance <= maxToolDistance;
        }
        #endregion
    }
}
