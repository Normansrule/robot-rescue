using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// Single-player Bot Stack: a wind-up Helper Drone. Carry it, throw it, Tether-pull it, stand on it,
    /// stack drones into stairs, or park one on a pressure plate as a counterweight.
    /// Drones stay upright and snap onto a drone below them so stacks are easy to build.
    /// </summary>
    [RequireComponent(typeof(Carryable))]
    public class HelperDrone : MonoBehaviour
    {
        public float halfHeight = 0.5f;
        [Tooltip("How hard a drone slides to centre itself on the drone below.")]
        public float stackSnap = 10f;

        [Header("Visuals")]
        public Transform windUpKey;
        public Transform[] eyes;

        Carryable carryable;
        Rigidbody body;
        float nextBlink;
        Vector3 eyeScale;

        const int IgnoreRaycastLayer = 2;

        void Awake()
        {
            carryable = GetComponent<Carryable>();
            body = GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            if (eyes != null && eyes.Length > 0) eyeScale = eyes[0].localScale;
            nextBlink = Time.time + Random.Range(1f, 4f);
        }

        void FixedUpdate()
        {
            if (carryable.IsHeld) return;
            if (!Physics.Raycast(body.position, Vector3.down, out RaycastHit hit, halfHeight + 0.15f, ~(1 << IgnoreRaycastLayer), QueryTriggerInteraction.Ignore))
                return;
            if (!hit.rigidbody || hit.rigidbody == body || !hit.rigidbody.TryGetComponent(out HelperDrone below)) return;

            Vector3 offset = below.body.position - body.position;
            offset.y = 0f;
            if (offset.magnitude > 0.7f) return;
            body.linearVelocity += offset * (stackSnap * Time.fixedDeltaTime);
            body.MoveRotation(Quaternion.Slerp(body.rotation, below.body.rotation, 0.2f));
        }

        void Update()
        {
            // The key spins faster while PIP is carrying it: it's excited to help.
            if (windUpKey) windUpKey.Rotate(0f, 0f, (carryable.IsHeld ? 360f : 60f) * Time.deltaTime, Space.Self);

            if (eyes == null || eyes.Length == 0) return;
            bool blinking = Time.time > nextBlink;
            if (Time.time > nextBlink + 0.12f) nextBlink = Time.time + Random.Range(2f, 5f);
            foreach (var e in eyes)
                if (e) e.localScale = blinking ? new Vector3(eyeScale.x, eyeScale.y * 0.1f, eyeScale.z) : eyeScale;
        }
    }
}
