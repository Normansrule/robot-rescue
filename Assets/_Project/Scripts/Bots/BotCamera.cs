using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// Third-person orbit camera with wall collision. Deliberately simple so the prototype
    /// runs without Cinemachine; replace with a CinemachineCamera + OrbitalFollow later.
    /// </summary>
    public class BotCamera : MonoBehaviour
    {
        public Transform target;
        public BotInput input;
        public Vector3 targetOffset = new Vector3(0f, 1.3f, 0f);
        public float distance = 6f;
        public float mouseSensitivity = 0.12f;
        public float stickSensitivity = 160f;
        public float minPitch = -35f, maxPitch = 70f;
        public float collisionRadius = 0.25f;
        public float followSharpness = 15f;

        float yaw, pitch = 18f;
        Vector3 smoothedPivot;
        const int IgnoreRaycastLayer = 2;

        void Start()
        {
            if (target) { yaw = target.eulerAngles.y; smoothedPivot = target.position + targetOffset; }
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void LateUpdate()
        {
            if (!target) return;
            bool paused = Core.GameManager.Instance && Core.GameManager.Instance.Paused;
            if (input && !paused)
            {
                Vector2 m = input.LookMouse * mouseSensitivity;
                Vector2 s = input.LookStick * (stickSensitivity * Time.deltaTime);
                yaw += m.x + s.x;
                pitch = Mathf.Clamp(pitch - m.y - s.y, minPitch, maxPitch);
            }

            Vector3 pivot = target.position + targetOffset;
            smoothedPivot = Vector3.Lerp(smoothedPivot, pivot, 1f - Mathf.Exp(-followSharpness * Time.deltaTime));

            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 back = rot * Vector3.back;
            float dist = distance;
            int mask = ~(1 << IgnoreRaycastLayer);
            if (Physics.SphereCast(smoothedPivot, collisionRadius, back, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
                dist = Mathf.Max(0.5f, hit.distance);

            transform.SetPositionAndRotation(smoothedPivot + back * dist, rot);
        }

        /// <summary>Flat forward/right for camera-relative movement.</summary>
        public Vector3 PlanarForward => Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        public Vector3 PlanarRight => Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
    }
}
