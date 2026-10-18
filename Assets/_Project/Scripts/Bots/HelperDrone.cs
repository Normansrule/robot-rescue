using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// Single-player Bot Stack: a wind-up Helper Drone. Carry it, throw it, Tether-pull it, stand on it,
    /// stack drones into stairs, or park one on a pressure plate as a counterweight.
    /// Drones stay upright and snap onto a drone below them so stacks are easy to build.
    /// They also carry a little battery: while PIP carries one, it shares its charge (a friend's jump-start).
    /// </summary>
    [RequireComponent(typeof(Carryable))]
    public class HelperDrone : MonoBehaviour, IChargeSource
    {
        public float halfHeight = 0.5f;
        [Tooltip("How hard a drone slides to centre itself on the drone below.")]
        public float stackSnap = 10f;

        [Header("Charge sharing")]
        public float shareRate = 0.08f;
        [Tooltip("Drone charge lost per unit given (drones are a little inefficient).")]
        public float shareCost = 1.4f;

        [Header("Visuals")]
        public Transform windUpKey;
        [Tooltip("Legacy box eyes (drones built before the LED face). Blink if set.")]
        public Transform[] eyes;

        public float Charge01 { get; private set; } = 1f;
        public string SourceName => GetComponent<Carryable>().displayName;
        public bool IsSharing { get; private set; }

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
            if (eyes != null && eyes.Length > 0 && eyes[0]) eyeScale = eyes[0].localScale;
            nextBlink = Time.time + Random.Range(1f, 4f);
        }

        public float ChargeRate(BotBattery battery)
        {
            IsSharing = Charge01 > 0.02f && battery.Charge01 < 0.995f && carryable.IsHeld;
            if (!IsSharing) return 0f;
            Charge01 = Mathf.Max(0f, Charge01 - shareRate * shareCost * Time.deltaTime);
            return shareRate;
        }

        public void Recharge(float amount) => Charge01 = Mathf.Min(1f, Charge01 + amount);

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
            if (!carryable.IsHeld) IsSharing = false;
            // The key spins faster while PIP is carrying it: it's excited to help. A flat drone's key barely turns.
            float keySpeed = (carryable.IsHeld ? 360f : 60f) * Mathf.Lerp(0.15f, 1f, Charge01);
            if (windUpKey) windUpKey.Rotate(0f, 0f, keySpeed * Time.deltaTime, Space.Self);

            if (eyes == null || eyes.Length == 0) return;
            bool blinking = Time.time > nextBlink;
            if (Time.time > nextBlink + 0.12f) nextBlink = Time.time + Random.Range(2f, 5f);
            foreach (var e in eyes)
                if (e) e.localScale = blinking ? new Vector3(eyeScale.x, eyeScale.y * 0.1f, eyeScale.z) : eyeScale;
        }
    }
}
