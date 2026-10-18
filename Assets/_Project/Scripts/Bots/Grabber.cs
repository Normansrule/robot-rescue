using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// Push/Carry/Throw. Picks up the nearest <see cref="Carryable"/> in front of the bot and holds it
    /// overhead (cute, and it keeps the view clear). Held objects follow by velocity, so they still
    /// bump into the world instead of clipping through it.
    /// The Tool Arm will grow out of this (Grabber is its first tool).
    /// </summary>
    [RequireComponent(typeof(BotController))]
    public class Grabber : MonoBehaviour
    {
        public BotCamera cameraRig;
        public Vector3 holdOffset = new Vector3(0f, 1.6f, 0.15f);
        public float pickupReach = 0.9f;
        public float pickupRadius = 0.9f;
        public float maxCarryMass = 2.5f;
        public float throwSpeed = 9f;
        public float throwLift = 0.3f;
        [Tooltip("Put-down is a gentle forward toss so things land in front of PIP (on a stove, in a pot), not on its head.")]
        public float putDownSpeed = 2.5f;
        public float followSharpness = 0.6f;
        [Tooltip("If the held object gets snagged this far from the hold point, it's dropped.")]
        public float breakDistance = 2.2f;

        public Carryable Held { get; private set; }
        /// <summary>The carryable that Interact would pick up right now. Used by the HUD.</summary>
        public Carryable Focus { get; private set; }

        BotController bot;
        BotInput input;
        Collider[] botColliders;
        readonly Collider[] overlap = new Collider[16];

        void Awake()
        {
            bot = GetComponent<BotController>();
            input = GetComponent<BotInput>();
            botColliders = GetComponentsInChildren<Collider>();
        }

        void Update()
        {
            Focus = Held ? null : FindNearest();

            if (input.InteractPressed)
            {
                if (Held) Drop();
                else TryGrab(Focus);
            }
            if (input.ThrowPressed && Held) Throw();
        }

        void FixedUpdate()
        {
            if (!Held) return;
            Rigidbody rb = Held.Body;
            Vector3 target = transform.TransformPoint(holdOffset);
            Vector3 delta = target - rb.position;
            if (delta.magnitude > breakDistance) { Drop(); return; }

            rb.linearVelocity = Vector3.ClampMagnitude(delta / Time.fixedDeltaTime * followSharpness, 25f);
            rb.angularVelocity = Vector3.zero;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, transform.rotation, 0.25f));
        }

        Carryable FindNearest()
        {
            Vector3 center = transform.position + Vector3.up * 0.6f + transform.forward * pickupReach;
            int n = Physics.OverlapSphereNonAlloc(center, pickupRadius, overlap, ~0, QueryTriggerInteraction.Ignore);
            Carryable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var rb = overlap[i].attachedRigidbody;
                if (!rb || !rb.TryGetComponent(out Carryable c) || c.IsHeld || !c.CanPickUp || c.Body.mass > maxCarryMass) continue;
                // Don't pick up the thing PIP is standing on.
                if (bot.GroundCollider && bot.GroundCollider.attachedRigidbody == rb) continue;
                float d = (rb.position - center).sqrMagnitude;
                if (d < bestDist) { best = c; bestDist = d; }
            }
            return best;
        }

        public bool TryGrab(Carryable c)
        {
            if (!isActiveAndEnabled || Held || !c || c.IsHeld || !c.CanPickUp || c.Body.mass > maxCarryMass) return false;
            Held = c;
            c.OnPickedUp(this, botColliders);
            bot.Squash(-0.12f);
            return true;
        }

        public void Drop()
        {
            if (!Held) return;
            var c = Held;
            Held = null;
            c.OnReleased(botColliders);
            c.Body.linearVelocity = bot.Body.linearVelocity + transform.forward * putDownSpeed + Vector3.up;
        }

        public void Throw()
        {
            if (!Held) return;
            var c = Held;
            Vector3 dir = cameraRig ? cameraRig.transform.forward : transform.forward;
            dir = (dir + Vector3.up * throwLift).normalized;
            Held = null;
            c.OnReleased(botColliders);
            // Heavier things fly slower.
            float speed = throwSpeed / Mathf.Sqrt(Mathf.Max(1f, c.Body.mass));
            c.Body.linearVelocity = bot.Body.linearVelocity * 0.5f + dir * speed;
            bot.Squash(0.15f);
            EventBus<NarrationCue>.Raise(new NarrationCue("throw"));
        }
    }
}
