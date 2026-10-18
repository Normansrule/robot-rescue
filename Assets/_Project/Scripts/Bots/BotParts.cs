using System;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Bots
{
    public enum BotPart { ArmL, ArmR, Antenna, FootL, FootR }

    /// <summary>
    /// PIP can lose bits. A hard landing knocks a foot off, a heavy object can take an arm or the antenna.
    /// Lost parts bounce off as physics pickups: walk into one (or Tether-pull it) to snap it back on,
    /// or stand in a service bay to fix everything.
    ///  - No left arm: weaker lifting and throwing.   - No right (tool) arm: no tool.   - No arms: can't carry.
    ///  - No antenna: half Tether range, fuzzy radio. - Missing feet: slower, lower jumps.
    /// </summary>
    [RequireComponent(typeof(BotController))]
    public class BotParts : MonoBehaviour
    {
        [Tooltip("Visual pivots, in BotPart order: ArmL, ArmR, Antenna, FootL, FootR.")]
        public Transform[] partVisuals = new Transform[5];
        [Tooltip("Landing faster than this (m/s) breaks something. A 1.3 m jump lands at about 6.")]
        public float breakLandingSpeed = 13f;
        public float heavyHitSpeed = 7.5f;
        [Tooltip("Seconds of grace after losing a part, so one bad fall doesn't strip PIP bare.")]
        public float graceSeconds = 1.5f;
        public bool breakable = true;

        public event Action<BotPart, bool> Changed;

        readonly bool[] missing = new bool[5];
        readonly BotPartPickup[] pickups = new BotPartPickup[5];
        BotController bot;
        Grabber grabber;
        ToolArm toolArm;
        TetherCable tether;
        float baseCarryMass, baseThrowSpeed, baseTetherRange;
        float safeUntil;

        public bool Has(BotPart p) => !missing[(int)p];
        public int MissingCount { get { int n = 0; foreach (bool m in missing) if (m) n++; return n; } }
        public bool HasArm => Has(BotPart.ArmL) || Has(BotPart.ArmR);

        public float SpeedScale => FeetMissing switch { 0 => 1f, 1 => 0.72f, _ => 0.5f };
        public float JumpScale => FeetMissing switch { 0 => 1f, 1 => 0.82f, _ => 0.62f };
        int FeetMissing => (missing[(int)BotPart.FootL] ? 1 : 0) + (missing[(int)BotPart.FootR] ? 1 : 0);

        public static string PartName(BotPart p) => p switch
        {
            BotPart.ArmL => "left arm",
            BotPart.ArmR => "tool arm",
            BotPart.Antenna => "antenna",
            BotPart.FootL => "left foot",
            _ => "right foot",
        };

        void Awake()
        {
            bot = GetComponent<BotController>();
            grabber = GetComponent<Grabber>();
            toolArm = GetComponent<ToolArm>();
            tether = GetComponent<TetherCable>();
            if (grabber) { baseCarryMass = grabber.maxCarryMass; baseThrowSpeed = grabber.throwSpeed; }
            if (tether) baseTetherRange = tether.maxRange;
        }

        void OnEnable()
        {
            bot.Landed += OnLanded;
            bot.HitByHeavy += OnHeavyHit;
        }

        void OnDisable()
        {
            bot.Landed -= OnLanded;
            bot.HitByHeavy -= OnHeavyHit;
        }

        void OnLanded(float speed)
        {
            if (!breakable || speed < breakLandingSpeed || Time.time < safeUntil) return;
            BotPart? p = Pick(UnityEngine.Random.value < 0.5f
                ? new[] { BotPart.FootL, BotPart.FootR, BotPart.ArmL, BotPart.Antenna }
                : new[] { BotPart.FootR, BotPart.FootL, BotPart.ArmR, BotPart.Antenna });
            if (p.HasValue) Break(p.Value, Vector3.up * 3f + UnityEngine.Random.insideUnitSphere * 2f);
        }

        void OnHeavyHit(float speed)
        {
            if (!breakable || speed < heavyHitSpeed || Time.time < safeUntil) return;
            BotPart? p = Pick(UnityEngine.Random.value < 0.5f
                ? new[] { BotPart.Antenna, BotPart.ArmL, BotPart.ArmR }
                : new[] { BotPart.ArmR, BotPart.ArmL, BotPart.Antenna });
            if (p.HasValue) Break(p.Value, Vector3.up * 2.5f - transform.forward * 2f);
        }

        BotPart? Pick(BotPart[] preference)
        {
            foreach (var p in preference)
                if (Has(p)) return p;
            return null;
        }

        /// <summary>Knocks a part off and flings it away as a pickup.</summary>
        public void Break(BotPart part, Vector3 impulse)
        {
            int i = (int)part;
            if (missing[i]) return;
            var visual = partVisuals.Length > i ? partVisuals[i] : null;
            missing[i] = true;
            safeUntil = Time.time + graceSeconds;

            if (visual)
            {
                var copy = Instantiate(visual.gameObject, visual.position, visual.rotation);
                copy.name = "Lost_" + part;
                copy.transform.localScale = visual.lossyScale;
                visual.gameObject.SetActive(false);
                pickups[i] = BotPartPickup.Create(copy, this, part, impulse);
            }
            ApplyEffects();
            Changed?.Invoke(part, false);
            EventBus<NarrationCue>.Raise(new NarrationCue("part_lost_" + part.ToString().ToLowerInvariant()));
            EventBus<NarrationCue>.Raise(new NarrationCue("part_lost"));
        }

        public void Reattach(BotPart part)
        {
            int i = (int)part;
            if (!missing[i]) return;
            missing[i] = false;
            if (pickups[i]) Destroy(pickups[i].gameObject);
            pickups[i] = null;
            if (partVisuals.Length > i && partVisuals[i]) partVisuals[i].gameObject.SetActive(true);
            ApplyEffects();
            Changed?.Invoke(part, true);
            EventBus<NarrationCue>.Raise(new NarrationCue("part_fixed"));
        }

        public void RepairAll()
        {
            for (int i = 0; i < missing.Length; i++)
                if (missing[i]) Reattach((BotPart)i);
        }

        void ApplyEffects()
        {
            if (grabber)
            {
                bool both = Has(BotPart.ArmL) && Has(BotPart.ArmR);
                if (!HasArm && grabber.Held) grabber.Drop();
                grabber.enabled = HasArm;
                grabber.maxCarryMass = both ? baseCarryMass : baseCarryMass * 0.7f;
                grabber.throwSpeed = both ? baseThrowSpeed : baseThrowSpeed * 0.6f;
            }
            if (toolArm) toolArm.enabled = Has(BotPart.ArmR);
            if (tether) tether.maxRange = Has(BotPart.Antenna) ? baseTetherRange : baseTetherRange * 0.55f;
        }
    }
}
