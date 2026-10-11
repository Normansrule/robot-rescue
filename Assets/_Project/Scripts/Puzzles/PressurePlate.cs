using System.Collections.Generic;
using RobotRescue.Bots;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// Trigger volume that powers its targets while enough mass sits on it.
    /// A lone PIP is too light for heavy plates: that's what crates (and later Helper Drones) are for.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PressurePlate : MonoBehaviour
    {
        public float requiredMass = 2f;
        public GameObject[] poweredObjects;
        [Tooltip("Visual that sinks when pressed.")]
        public Transform plateVisual;
        public float pressDepth = 0.06f;
        public string pressedCue;
        [Tooltip("Cue when a bot stands on it but is too light.")]
        public string tooLightCue;

        public bool IsPressed { get; private set; }

        readonly HashSet<Rigidbody> bodies = new HashSet<Rigidbody>();
        Vector3 visualRest;
        bool tooLightCueSent;

        void Reset() => GetComponent<Collider>().isTrigger = true;
        void Awake() { if (plateVisual) visualRest = plateVisual.localPosition; }

        void OnTriggerEnter(Collider other) { if (other.attachedRigidbody) bodies.Add(other.attachedRigidbody); }
        void OnTriggerExit(Collider other) { if (other.attachedRigidbody) bodies.Remove(other.attachedRigidbody); }

        void FixedUpdate()
        {
            bodies.RemoveWhere(b => b == null || !b.gameObject.activeInHierarchy);
            float mass = 0f;
            bool botOnPlate = false;
            foreach (var b in bodies)
            {
                // Held objects don't weigh on anything.
                if (b.TryGetComponent(out Carryable c) && c.IsHeld) continue;
                mass += b.mass;
                botOnPlate |= b.TryGetComponent<BotController>(out _);
            }

            bool pressed = mass >= requiredMass;
            if (botOnPlate && !pressed && !tooLightCueSent && !string.IsNullOrEmpty(tooLightCue))
            {
                tooLightCueSent = true;
                EventBus<NarrationCue>.Raise(new NarrationCue(tooLightCue));
            }
            if (pressed == IsPressed) return;

            IsPressed = pressed;
            PowerUtil.Send(poweredObjects, pressed);
            if (plateVisual) plateVisual.localPosition = visualRest + (pressed ? Vector3.down * pressDepth : Vector3.zero);
            if (pressed && !string.IsNullOrEmpty(pressedCue)) EventBus<NarrationCue>.Raise(new NarrationCue(pressedCue));
        }
    }
}
