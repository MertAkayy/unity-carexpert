using KinematicCharacterController;
using UnityEngine;

namespace PlayerScripts
{
    public enum CrouchInput
    {
        None,Toggle
    }

    public enum Stance
    {
        Stand,Crouch
    }
    public struct CharacterInput
    {
        public Quaternion Rotation;
        public Vector2 Move;
        public bool Jump;
        public CrouchInput Crouch;
        public bool Grab;
    }
    public class PlayerCharacter : MonoBehaviour , ICharacterController
    {
        [SerializeField] private KinematicCharacterMotor motor;

        [SerializeField] private Transform cameraTarget;
        [Space]
        [SerializeField] private float walkSpeed=20f;

        [SerializeField] private float gravitiy = -90 ;
        [SerializeField] private float jumpSpeed = 20f;
        [SerializeField] private float crouchSpeed = 7f;
        [Space] 
        [SerializeField] private float standHeight = 2f;
        [SerializeField] private float crouchHeight = 1f;
        [Range(0f,1f)]
        [SerializeField] private float standCameraTargetHeight = 0.9f;
        [Range(0f,1f)]
        [SerializeField] private float crouchCameraTargetHeight = 0.7f;

        [SerializeField] private float crouchHeightResponse = 15f;
        private Stance _stance;
        private Quaternion _requestedRotation;
        private Vector3 _requestedMovement;
        private bool _requestedJump;
        private bool _requestedCrouch;
        public void Initialize()
        {
            _stance=Stance.Stand;
            motor.CharacterController = this;
        }

        public void UpdateInput(CharacterInput input)
        {
            _requestedRotation = input.Rotation;
            _requestedMovement=new Vector3(input.Move.x, 0, input.Move.y);
            _requestedMovement=Vector3.ClampMagnitude(_requestedMovement, 1f);
            _requestedMovement=input.Rotation*_requestedMovement;
            _requestedJump=_requestedJump || input.Jump;
            _requestedCrouch =input.Crouch switch
            {
                CrouchInput.Toggle => !_requestedCrouch,
                CrouchInput.None => _requestedCrouch,
                _=>_requestedCrouch 
            };
        }

        public void UpdateBody(float deltaTime)
        {
            var currentHeight = motor.Capsule.height;
            var cameraTargetHeight = currentHeight * (
                _stance is Stance.Stand
                    ?standCameraTargetHeight
                    :crouchCameraTargetHeight
            );
            cameraTarget.localPosition = Vector3.Lerp(
                a: cameraTarget.localPosition,
                b:new Vector3(0f,cameraTargetHeight,-0.45f),
                t:crouchHeightResponse*deltaTime
            );
        }

        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            var forward = Vector3.ProjectOnPlane
            (
                _requestedRotation*Vector3.forward,
                motor.CharacterUp
            );
            if(forward!=Vector3.zero)
                currentRotation = Quaternion.LookRotation(forward,motor.CharacterUp);
        }

        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            if (motor.GroundingStatus.IsStableOnGround)
            {
                var groundedMovement = motor.GetDirectionTangentToSurface
                (
                    direction: _requestedMovement,
                    surfaceNormal: motor.GroundingStatus.GroundNormal
                ) * _requestedMovement.magnitude;
                var speed = _stance is Stance.Stand
                    ? walkSpeed
                    : crouchSpeed;
                currentVelocity = _requestedMovement * speed;
            }
            else
            {
                currentVelocity +=motor.CharacterUp * (gravitiy * deltaTime);
            }

            if (_requestedJump && motor.GroundingStatus.IsStableOnGround)
            {
                _requestedJump = false;
                motor.ForceUnground(time:0);
                var currentVerticalSpeed=Vector3.Dot(currentVelocity, motor.CharacterUp);
                var targetVerticalSpeed = Mathf.Max(currentVerticalSpeed,jumpSpeed);
            
                currentVelocity+=motor.CharacterUp*(targetVerticalSpeed-currentVerticalSpeed);
            }
        }

        public void BeforeCharacterUpdate(float deltaTime)
        {
            if (_requestedCrouch && _stance == Stance.Stand)
            {
                _stance = Stance.Crouch;
                motor.SetCapsuleDimensions
                (
                    radius:motor.Capsule.radius,
                    height:crouchHeight,
                    yOffset:crouchHeight*0.5f
                );
            }
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            // throw new System.NotImplementedException();
        }

        public void AfterCharacterUpdate(float deltaTime)
        {
            if (!_requestedCrouch && _stance is not Stance.Stand)
            {
                _stance=Stance.Stand;
                motor.SetCapsuleDimensions
                (
                    radius:motor.Capsule.radius,
                    height:standHeight,
                    yOffset:standHeight*0.5f
                );
            }
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            // throw new System.NotImplementedException();
            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
            // throw new System.NotImplementedException();
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint,
            ref HitStabilityReport hitStabilityReport)
        {
            // throw new System.NotImplementedException();
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition,
            Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
            // throw new System.NotImplementedException();
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
            // throw new System.NotImplementedException();
        }

        public Transform GetCameraTransform() => cameraTarget;
    }
}