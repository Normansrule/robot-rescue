using RobotRescue.Core;
using RobotRescue.Puzzles;
using RobotRescue.World;
using UnityEngine;

namespace RobotRescue.Tasks.Rescue
{
    /// <summary>
    /// Chapter 7 Big Rescue: Unit Zero's battery is nearly flat. It drains on its own, and every hum costs him a chunk
    /// (he's spending his last power calling for help). While powered, his charging line fills it back up. Charge him
    /// all the way and he wakes up enough to be carried out. If it runs out, the rescue fails.
    /// </summary>
    public class ZeroRescue : MonoBehaviour, IPowerable, IBossMeter, IAreaGated
    {
        public ZeroBot zero;
        [Tooltip("Zero's face/eyes; dim when flat, bright when charged.")]
        public Renderer[] eyes;
        public float startCharge = 0.35f;
        public float drainSeconds = 200f;
        public float chargeSeconds = 25f;
        public float humCost = 0.07f;
        public string chargedStepId = "charged";

        public bool Powered { get; private set; }
        public float Charge { get; private set; }
        public bool Charged { get; private set; }
        public bool Flat { get; private set; }

        public string MeterTitle => Charged ? "Unit Zero: AWAKE" : "Unit Zero's battery";
        public float Meter01 => Charge;
        public Color MeterColor => Charge < 0.2f ? new Color(0.95f, 0.4f, 0.35f) : Powered ? new Color(0.45f, 0.85f, 0.55f) : new Color(1f, 0.8f, 0.35f);
        public string MeterStatus => Charged ? "He's awake! Carry him to the rescue lift." : Powered ? "Charging..." : "Draining! Get his charging line working.";
        public string FailureText => Flat ? "Unit Zero's battery ran out." : null;

        MaterialPropertyBlock mpb;
        bool warned;

        void Awake() => Charge = startCharge;
        void OnEnable() => HumTremor.Pulsed += OnHum;
        void OnDisable() => HumTremor.Pulsed -= OnHum;

        public void SetPowered(bool on)
        {
            if (on == Powered) return;
            Powered = on;
            if (!Charged && !Flat) EventBus<NarrationCue>.Raise(new NarrationCue(on ? "zero_charging" : "zero_unplugged"));
        }

        void OnHum(HumTremor hum)
        {
            if (Charged || Flat) return;
            Charge = Mathf.Max(0f, Charge - humCost);
            EventBus<NarrationCue>.Raise(new NarrationCue("zero_sos"));
        }

        void Update()
        {
            if (!Charged && !Flat)
            {
                Charge = Mathf.Clamp01(Charge + (Powered ? 1f / chargeSeconds : -1f / drainSeconds) * Time.deltaTime);
                if (Charge < 0.15f && !warned)
                {
                    warned = true;
                    EventBus<NarrationCue>.Raise(new NarrationCue("zero_low"));
                }
                if (Charge >= 1f)
                {
                    Charged = true;
                    if (zero) zero.awake = true;
                    if (!string.IsNullOrEmpty(chargedStepId)) EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted(chargedStepId));
                    EventBus<NarrationCue>.Raise(new NarrationCue("zero_awake"));
                }
                else if (Charge <= 0f)
                {
                    Flat = true;
                    EventBus<NarrationCue>.Raise(new NarrationCue("zero_flat"));
                }
            }

            // Eyes: a slow, dim flicker when low; steady and bright as he charges.
            if (eyes == null) return;
            mpb ??= new MaterialPropertyBlock();
            float level = Charged ? 1f : Charge * (0.75f + 0.25f * Mathf.Sin(Time.time * (Powered ? 6f : 2f)));
            var c = Color.Lerp(new Color(0.15f, 0.2f, 0.25f), new Color(0.45f, 0.9f, 1f), level);
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_EmissionColor", c * (0.5f + 2.5f * level));
            foreach (var r in eyes) if (r) r.SetPropertyBlock(mpb);
        }
    }
}
