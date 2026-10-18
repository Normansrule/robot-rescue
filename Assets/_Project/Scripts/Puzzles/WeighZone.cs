using System.Collections.Generic;
using RobotRescue.Bots;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// Trigger that weighs what's in it: a counterweight pan, or the "is anything under this gate?" safety check.
    /// Held objects weigh nothing (same rule as pressure plates); kinematic bodies are ignored.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeighZone : MonoBehaviour
    {
        public float Mass { get; private set; }
        public bool BotInside { get; private set; }
        public bool AnythingInside { get; private set; }

        readonly HashSet<Collider> touching = new HashSet<Collider>();
        readonly HashSet<Rigidbody> counted = new HashSet<Rigidbody>();
        Collider zone;

        void Reset() => GetComponent<Collider>().isTrigger = true;
        void Awake() => zone = GetComponent<Collider>();

        void OnTriggerEnter(Collider other) { if (other.attachedRigidbody) touching.Add(other); }
        void OnTriggerExit(Collider other) => touching.Remove(other);

        void FixedUpdate()
        {
            // Teleported objects (respawns, resets) leave without an exit callback.
            var b = zone.bounds;
            touching.RemoveWhere(c => !c || !c.enabled || !c.gameObject.activeInHierarchy || !c.attachedRigidbody || !b.Intersects(c.bounds));

            counted.Clear();
            float mass = 0f;
            bool bot = false;
            foreach (var c in touching)
            {
                var rb = c.attachedRigidbody;
                if (rb.isKinematic || !counted.Add(rb)) continue;
                if (rb.TryGetComponent(out Carryable carry) && carry.IsHeld) continue;
                mass += rb.mass;
                bot |= rb.TryGetComponent<BotController>(out _);
            }
            Mass = mass;
            BotInside = bot;
            AnythingInside = counted.Count > 0;
        }
    }
}
