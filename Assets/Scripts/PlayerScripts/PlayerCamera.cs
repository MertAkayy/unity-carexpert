using UnityEngine;

namespace PlayerScripts
{
    public struct CameraInput
    {
        public Vector2 Look;
    }

    public class PlayerCamera : MonoBehaviour
    {
        private Vector3 _eulerAngles;
        [SerializeField] private float sensitivity=0.1f;
        internal void Initialize(Transform cameraTarget)
        {
            transform.position = cameraTarget.position;
            transform.rotation = cameraTarget.rotation;
            transform.eulerAngles = _eulerAngles = cameraTarget.eulerAngles;
        }

        public void UpdateRotation(CameraInput input)
        {
            _eulerAngles +=new Vector3(-input.Look.y, input.Look.x)*sensitivity;
            transform.eulerAngles = _eulerAngles;
        }

        public void UpdatePosition(Transform target)
        {
            transform.position = target.position;
        }
    }
}