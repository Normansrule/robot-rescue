using System.Collections.Generic;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// A cup that takes a <see cref="PowerCell"/>. Two jobs, and a cradle can do either or both:
    ///  - Output: while a FULL cell sits in it, it powers <see cref="poweredObjects"/>. Take the cell out and they stop.
    ///  - Charging: wire a <see cref="PowerSocket"/> to it. While the bot is plugged into that socket, the cell in the
    ///    cradle fills up.
    /// The lamp on the cradle says which state it's in: dark (empty), amber (cell not full), blue pulse (charging),
    /// green (powering).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CellCradle : MonoBehaviour, IPowerable
    {
        public GameObject[] poweredObjects;
        [Tooltip("Charge added per second while the charging socket is live (1 = full in a second).")]
        public float chargePerSecond = 0.28f;
        [Tooltip("Where a resting cell is nudged to, so it sits neatly in the cup.")]
        public Transform seat;
        public Renderer lamp;
        public string poweredCue;
        [Tooltip("Said once when a cell that isn't full is set in an output cradle.")]
        public string notFullCue = "cell_not_full";

        public PowerCell Cell { get; private set; }
        public bool IsOutputOn { get; private set; }
        public bool IsCharging => charging && Cell && !Cell.IsFull;

        static readonly Color Dark = new Color(0.2f, 0.2f, 0.25f), Amber = new Color(1f, 0.7f, 0.2f),
            Blue = new Color(0.35f, 0.7f, 1f), Green = new Color(0.35f, 1f, 0.5f);

        readonly HashSet<PowerCell> inside = new HashSet<PowerCell>();
        Collider zone;
        MaterialPropertyBlock mpb;
        bool charging, notFullSaid;

        bool HasOutputs => poweredObjects != null && poweredObjects.Length > 0;

        void Reset() => GetComponent<Collider>().isTrigger = true;
        void Awake() => zone = GetComponent<Collider>();

        public void SetPowered(bool on) => charging = on;

        void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody && other.attachedRigidbody.TryGetComponent(out PowerCell c)) inside.Add(c);
        }

        void OnTriggerExit(Collider other)
        {
            if (other.attachedRigidbody && other.attachedRigidbody.TryGetComponent(out PowerCell c)) inside.Remove(c);
        }

        void FixedUpdate()
        {
            // A reset teleports cells away without an exit callback, so check what's really still here.
            var b = zone.bounds;
            b.Expand(0.3f);
            inside.RemoveWhere(c => !c || !c.isActiveAndEnabled || !b.Contains(c.Body.worldCenterOfMass));

            PowerCell resting = null;
            foreach (var c in inside)
                if (!c.IsHeld) { resting = c; break; }
            if (resting != Cell) notFullSaid = false;
            Cell = resting;

            if (Cell && seat && Cell.Body.linearVelocity.sqrMagnitude < 1f)
            {
                Vector3 to = seat.position - Cell.Body.position;
                to.y = 0f;
                Cell.Body.AddForce(to * 20f, ForceMode.Acceleration);
            }

            if (charging && Cell) Cell.AddCharge(chargePerSecond * Time.fixedDeltaTime);

            if (HasOutputs && Cell && !Cell.IsFull && !charging && !notFullSaid && !string.IsNullOrEmpty(notFullCue))
            {
                notFullSaid = true;
                EventBus<NarrationCue>.Raise(new NarrationCue(notFullCue));
            }

            bool output = HasOutputs && Cell && Cell.IsFull;
            if (output == IsOutputOn) return;
            IsOutputOn = output;
            PowerUtil.Send(poweredObjects, output);
            if (output && !string.IsNullOrEmpty(poweredCue)) EventBus<NarrationCue>.Raise(new NarrationCue(poweredCue));
        }

        void Update()
        {
            if (!lamp) return;
            mpb ??= new MaterialPropertyBlock();
            Color c = IsOutputOn ? Green
                : IsCharging ? Blue * (0.6f + 0.4f * Mathf.Sin(Time.time * 8f))
                : Cell ? (Cell.IsFull ? Green : Amber)
                : Dark;
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_Color", c);
            mpb.SetColor("_EmissionColor", c == Dark ? Color.black : c * 1.8f);
            lamp.SetPropertyBlock(mpb);
        }
    }
}
