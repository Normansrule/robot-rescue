using System;
using System.Collections.Generic;
using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>Anything that can top up PIP's battery: charge pads, charger sockets, a friendly drone.</summary>
    public interface IChargeSource
    {
        /// <summary>Charge per second given to <paramref name="battery"/> this frame (0 = nothing to give).</summary>
        float ChargeRate(BotBattery battery);
        string SourceName { get; }
    }

    /// <summary>
    /// PIP's battery. It drains while PIP moves, faster while running, climbing, using a tool or powering something through
    /// the Tether. Standing still, reading and thinking are free: the battery is for planning, not a tax. Recharge on a charge pad, by plugging into a charger socket, or by carrying a Helper Drone (it shares
    /// its own charge). At zero PIP goes onto its reserve: it can still walk slowly, carry things, reattach parts and plug
    /// into a charger, but it can't climb, use tools or power anything. If it doesn't reach a charger in time, DISPATCH
    /// tows it back to the last one. You never need power to reach the only power.
    /// </summary>
    [RequireComponent(typeof(BotController))]
    public class BotBattery : MonoBehaviour
    {
        [Tooltip("Seconds a full battery lasts walking about.")]
        public float capacitySeconds = 210f;
        [Tooltip("Extra drain (as multiples of the walking rate) for each activity.")]
        public float runDrain = 0.4f, climbDrain = 1.2f, toolDrain = 0.8f, powerDrain = 1.5f;
        public float lowThreshold = 0.25f, criticalThreshold = 0.1f;
        public float towDelay = 2.5f;
        [Tooltip("Charge PIP has after being towed.")]
        public float towCharge = 0.5f;
        [Tooltip("Walking speed on reserve, as a fraction of normal.")]
        public float reserveSpeed = 0.45f;
        [Tooltip("Seconds on reserve, away from any charger, before the tow truck comes.")]
        public float reserveSeconds = 75f;
        [Tooltip("Charge needed to leave the reserve.")]
        public float reserveExitCharge = 0.08f;

        public float Charge01 { get; private set; } = 1f;
        public bool IsCharging { get; private set; }
        public bool IsDead { get; private set; }
        /// <summary>Flat but still walking: slow, no climbing, no tools, no powering things.</summary>
        public bool OnReserve { get; private set; }
        /// <summary>Seconds until the tow truck comes while on reserve.</summary>
        public float ReserveLeft { get; private set; }
        /// <summary>False on reserve or flat: tools, climbing and powering sockets wait for a charger.</summary>
        public bool CanDoPoweredWork => !IsDead && !OnReserve;
        public bool IsLow => Charge01 < lowThreshold;
        public bool IsCritical => Charge01 < criticalThreshold;
        public string ChargingFrom { get; private set; }
        /// <summary>Movement multiplier: sluggish when nearly flat.</summary>
        public float SpeedScale => IsDead ? 0f : OnReserve ? reserveSpeed : IsCritical ? 0.65f : 1f;
        /// <summary>Where the tow truck drops PIP (the last charge pad touched; falls back to the checkpoint).</summary>
        public Vector3? LastChargerPosition { get; set; }

        public event Action PoweredDown, PoweredUp, WentLow, ReserveStarted, ReserveEnded;

        readonly List<IChargeSource> sources = new List<IChargeSource>();
        BotController bot;
        TetherCable tether;
        Grabber grabber;
        ToolArm toolArm;
        float towTimer;
        bool lowWarned, criticalWarned;

        void Awake()
        {
            bot = GetComponent<BotController>();
            tether = GetComponent<TetherCable>();
            grabber = GetComponent<Grabber>();
            toolArm = GetComponent<ToolArm>();
        }

        public void AddSource(IChargeSource s) { if (s != null && !sources.Contains(s)) sources.Add(s); }
        public void RemoveSource(IChargeSource s) => sources.Remove(s);

        public void Fill()
        {
            Charge01 = 1f;
            lowWarned = criticalWarned = false;
            if (OnReserve) LeaveReserve();
        }

        /// <summary>Takes charge away directly (a zap, a scripted test). Reaching zero starts the reserve.</summary>
        public void Drain(float amount01)
        {
            if (IsDead) return;
            Charge01 = Mathf.Clamp01(Charge01 - amount01);
            if (Charge01 <= 0f && !OnReserve) EnterReserve();
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (IsDead)
            {
                towTimer -= dt;
                if (towTimer <= 0f) Tow();
                return;
            }

            float charge = 0f;
            ChargingFrom = null;
            sources.RemoveAll(s => s == null || (s is UnityEngine.Object o && !o));
            foreach (var s in sources)
            {
                float r = s.ChargeRate(this);
                if (r > 0f) { charge += r; ChargingFrom = s.SourceName; }
            }
            if (grabber && grabber.Held && grabber.Held.TryGetComponent(out IChargeSource held))
            {
                float r = held.ChargeRate(this);
                if (r > 0f) { charge += r; ChargingFrom = held.SourceName; }
            }

            IsCharging = charge > 0f && Charge01 < 1f;
            if (IsCharging)
            {
                Charge01 = Mathf.Min(1f, Charge01 + charge * dt);
                if (Charge01 > lowThreshold + 0.05f) lowWarned = false;
                if (Charge01 > criticalThreshold + 0.05f) criticalWarned = false;
                if (OnReserve && Charge01 >= reserveExitCharge) LeaveReserve();
                return;
            }

            if (OnReserve)
            {
                // Reserve power is only for getting somewhere safe: let go of anything PIP is powering.
                if (tether && tether.IsAttached && tether.Mode == TetherMode.Power && !(tether.Target is ChargerSocket)) tether.Detach();
                ReserveLeft -= dt;
                if (ReserveLeft <= 0f) Die();
                return;
            }

            float drain = 0f;
            if (bot.Speed01 > 0.05f || !bot.IsGrounded) drain += 1f;
            if (bot.Speed01 > 0.2f && bot.IsGrounded) drain += runDrain;
            if (bot.IsClimbing) drain += climbDrain;
            if (toolArm && toolArm.isActiveAndEnabled && toolArm.IsUsing) drain += toolDrain;
            if (tether && tether.IsAttached && tether.Mode == TetherMode.Power && !(tether.Target is ChargerSocket)) drain += powerDrain;
            Charge01 = Mathf.Max(0f, Charge01 - drain / capacitySeconds * dt);

            if (!lowWarned && Charge01 < lowThreshold)
            {
                lowWarned = true;
                WentLow?.Invoke();
                EventBus<NarrationCue>.Raise(new NarrationCue("battery_low"));
            }
            if (!criticalWarned && Charge01 < criticalThreshold)
            {
                criticalWarned = true;
                EventBus<NarrationCue>.Raise(new NarrationCue("battery_critical"));
            }
            if (Charge01 <= 0f) EnterReserve();
        }

        void EnterReserve()
        {
            OnReserve = true;
            ReserveLeft = reserveSeconds;
            if (tether && tether.IsAttached && tether.Mode == TetherMode.Power && !(tether.Target is ChargerSocket)) tether.Detach();
            ReserveStarted?.Invoke();
            EventBus<NarrationCue>.Raise(new NarrationCue("battery_reserve"));
        }

        void LeaveReserve()
        {
            OnReserve = false;
            ReserveEnded?.Invoke();
            EventBus<NarrationCue>.Raise(new NarrationCue("battery_reserve_ok"));
        }

        void Die()
        {
            OnReserve = false;
            IsDead = true;
            towTimer = towDelay;
            bot.PoweredDown = true;
            if (tether) tether.Detach();
            if (grabber) grabber.Drop();
            PoweredDown?.Invoke();
            EventBus<NarrationCue>.Raise(new NarrationCue("battery_dead"));
        }

        void Tow()
        {
            IsDead = false;
            bot.PoweredDown = false;
            Charge01 = towCharge;
            lowWarned = criticalWarned = false;
            if (LastChargerPosition.HasValue) bot.RespawnPoint = LastChargerPosition.Value;
            bot.Respawn(false);
            PoweredUp?.Invoke();
            EventBus<NarrationCue>.Raise(new NarrationCue("battery_towed"));
        }
    }
}
