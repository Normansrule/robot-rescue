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

            Vector3 shakeOffset = Vector3.zero;
            if (shake > 0f)
            {
                shake = Mathf.Max(0f, shake - Time.deltaTime * 1.5f);
                float t = Time.time * 28f;
                shakeOffset = new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f, 0f) * (shake * 0.5f);
            }
            transform.SetPositionAndRotation(smoothedPivot + back * dist + rot * shakeOffset, rot);
        }

        float shake;

        /// <summary>Rattle the camera (a rumble, the Hum). Amount is roughly metres of jitter.</summary>
        public void Shake(float amount) => shake = Mathf.Max(shake, amount);

        /// <summary>Jump straight behind the target (after a teleport) instead of gliding there.</summary>
        public void Snap()
        {
            if (!target) return;
            yaw = target.eulerAngles.y;
            pitch = 18f;
            smoothedPivot = target.position + targetOffset;
            transform.SetPositionAndRotation(smoothedPivot + Quaternion.Euler(pitch, yaw, 0f) * Vector3.back * distance, Quaternion.Euler(pitch, yaw, 0f));
        }

        void OnEnable()
        {
            if (!target) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// <summary>Flat forward/right for camera-relative movement.</summary>
        public Vector3 PlanarForward => Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        public Vector3 PlanarRight => Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
    }
}
