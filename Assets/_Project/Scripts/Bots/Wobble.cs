using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// Springy secondary motion: tilts this transform against its parent's acceleration, then wobbles back.
    /// On PIP's head and antenna, a drone's body, a lamp post... anything that should feel a bit jelly.
    /// </summary>
    public class Wobble : MonoBehaviour
    {
        [Tooltip("Degrees of tilt per m/s² of acceleration.")]
        public float response = 0.6f;
        public float stiffness = 140f;
        public float damping = 9f;
        public float maxAngle = 22f;
        [Tooltip("Extra idle sway (degrees), for antennas.")]
        public float idleSway = 0f;

        Quaternion baseRotation;
        float swayPhase;
        Vector3 lastPosition, lastVelocity, smoothedAcc;
        Vector2 angle, angularVelocity;
        bool primed;

        void Awake()
        {
            baseRotation = transform.localRotation;
            swayPhase = Random.value * 100f;
        }

        void OnEnable() => primed = false;

        /// <summary>Kick the spring, e.g. on landing or a bonk.</summary>
        public void Kick(Vector2 degreesPerSecond) => angularVelocity += degreesPerSecond;

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Vector3 pos = transform.parent ? transform.parent.position : transform.position;
            if (!primed)
            {
                lastPosition = pos;
                lastVelocity = Vector3.zero;
                primed = true;
                return;
            }
            Vector3 vel = (pos - lastPosition) / dt;
            Vector3 rawAcc = Vector3.ClampMagnitude((vel - lastVelocity) / dt, 80f);
            smoothedAcc = Vector3.Lerp(smoothedAcc, rawAcc, 1f - Mathf.Exp(-20f * dt));
            Vector3 acc = smoothedAcc;
            lastPosition = pos;
            lastVelocity = vel;

            Transform frame = transform.parent ? transform.parent : transform;
            Vector3 local = frame.InverseTransformDirection(acc);
            // Accelerating forward tips the top backward (x), sideways tips it the other way (z).
            Vector2 drive = new Vector2(-local.z, local.x) * response;
            angularVelocity += ((drive - angle) * stiffness - angularVelocity * damping) * dt;
            angle += angularVelocity * dt;
            angle = Vector2.ClampMagnitude(angle, maxAngle);

            float sway = idleSway > 0f ? Mathf.Sin(Time.time * 1.7f + swayPhase) * idleSway : 0f;
            transform.localRotation = baseRotation * Quaternion.Euler(angle.x + sway * 0.5f, 0f, angle.y + sway);
        }
    }
}
