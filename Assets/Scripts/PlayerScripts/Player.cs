using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerScripts
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private PlayerCharacter playerCharacter;
        [SerializeField]  private PlayerDataManager playerDataManager;
        [SerializeField] private PlayerCamera playerCamera;
        private IUsableTool _tool;
        private PlayerInputActions _playerInputActions;
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
             _tool = this.GetComponentInChildren<IUsableTool>();
        }

        private void PerformRead(InputAction.CallbackContext obj)
        {
            playerDataManager.CanReadDocument();
        }

        private void DoJob(InputAction.CallbackContext context)
        {
            _tool.StartJob(context);
        }
        private void ResumeJob(InputAction.CallbackContext obj)
        {
            _tool.ResumeJob(obj);
        }

        private void CancelJob(InputAction.CallbackContext obj)
        {
            _tool.FinishJob(obj);
        }

        private void PerformInteract(InputAction.CallbackContext obj)
        {
            playerDataManager.CanInteract();
        }

        private void PerformGrab(InputAction.CallbackContext obj)
        {
            if(playerDataManager!=null)
                playerDataManager.CanGrabObejct();
        }
        void OnDestroy()
        {
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
        }

        private void LateUpdate()
        {
            playerCamera.UpdatePosition(playerCharacter.GetCameraTransform());
        }
    }
}
