using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// Swings something into place when powered: a drawbridge drops across a pond, a fallen ladder stands back up.
    /// Latches by default, so a bridge stays down once it's fixed. Put it on a kinematic body (or a parent pivot).
    /// </summary>
    public class Hinge : MonoBehaviour, IPowerable
    {
        [Tooltip("Local rotation while powered (the 'fixed' pose).")]
        public Vector3 poweredEuler;
        public float degreesPerSecond = 120f;
        [Tooltip("Stay in the powered pose once reached, even if power drops.")]
        public bool latch = true;
        [Tooltip("Little overshoot wobble when it lands, in degrees.")]
        public float bounce = 6f;

        Quaternion closed, open;
        bool powered, latched;
        float landedAt = -1f;
        Rigidbody body;

        public bool IsOpen => powered || latched;

        void Awake()
        {
            closed = transform.localRotation;
            open = Quaternion.Euler(poweredEuler);
            body = GetComponent<Rigidbody>();
        }

        public void SetPowered(bool on)
        {
            powered = on;
            if (on && latch) latched = true;
        }

        void FixedUpdate()
        {
            var target = IsOpen ? open : closed;
            var next = Quaternion.RotateTowards(transform.localRotation, target, degreesPerSecond * Time.fixedDeltaTime);
            if (Quaternion.Angle(next, target) < 0.01f && Quaternion.Angle(transform.localRotation, target) > 0.01f) landedAt = Time.time;
            if (landedAt >= 0f && IsOpen)
            {
                float t = Time.time - landedAt;
                if (t < 0.6f) next = target * Quaternion.Euler(Mathf.Sin(t * 25f) * bounce * (1f - t / 0.6f), 0f, 0f);
            }
            var world = transform.parent ? transform.parent.rotation * next : next;
            if (body && body.isKinematic) body.MoveRotation(world);
            else transform.localRotation = next;
        }
    }
}
