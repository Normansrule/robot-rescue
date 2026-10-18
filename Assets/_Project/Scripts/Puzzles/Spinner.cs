using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// Turns round and round while powered (a carousel, a fan, a windmill), easing up to speed and back down.
    /// With a kinematic Rigidbody it turns through physics, so things standing on it ride along.
    /// </summary>
    public class Spinner : MonoBehaviour, IPowerable
    {
        public Vector3 axis = Vector3.up;
        public float degreesPerSecond = 30f;
        public float spinUpSeconds = 2f;
        public bool startsOn;

        public bool Powered { get; private set; }
        float speed01;
        Rigidbody body;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            Powered = startsOn;
            speed01 = startsOn ? 1f : 0f;
        }

        public void SetPowered(bool on) => Powered = on;

        void FixedUpdate()
        {
            speed01 = Mathf.MoveTowards(speed01, Powered ? 1f : 0f, Time.fixedDeltaTime / Mathf.Max(0.01f, spinUpSeconds));
            if (speed01 <= 0f) return;
            var step = Quaternion.AngleAxis(degreesPerSecond * speed01 * Time.fixedDeltaTime, axis);
            if (body && body.isKinematic) body.MoveRotation(body.rotation * step);
            else transform.localRotation *= step;
        }
    }
}
