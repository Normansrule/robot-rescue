using RobotRescue.Bots;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// Stand on it to recharge (and, for a service bay, to have broken parts fixed). Also tops up Helper Drones parked
    /// on it, and becomes PIP's tow-truck drop-off point.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ChargePad : MonoBehaviour, IChargeSource
    {
        public float chargePerSecond = 0.25f;
        [Tooltip("Service bay: also reattaches every broken part.")]
        public bool repairsParts = true;
        [Tooltip("Ring that pulses while charging.")]
        public Renderer glow;
        public Color idleColor = new Color(0.3f, 0.7f, 1f);
        public Color chargingColor = new Color(0.4f, 1f, 0.55f);

        public string SourceName => repairsParts ? "Service bay" : "Charge pad";

        BotBattery occupant;
        MaterialPropertyBlock mpb;
        bool cueSent;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        public float ChargeRate(BotBattery battery) => battery == occupant && battery.Charge01 < 1f ? chargePerSecond : 0f;

        void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (!rb || !rb.TryGetComponent(out BotBattery battery)) return;
            occupant = battery;
            battery.AddSource(this);
            battery.LastChargerPosition = transform.position + Vector3.up * 0.1f;
            if (rb.TryGetComponent(out BotController bot)) bot.RespawnPoint = battery.LastChargerPosition.Value;
            if (repairsParts && rb.TryGetComponent(out BotParts parts) && parts.MissingCount > 0)
            {
                parts.RepairAll();
                EventBus<NarrationCue>.Raise(new NarrationCue("parts_repaired"));
            }
            if (!cueSent && battery.Charge01 < 0.6f)
            {
                cueSent = true;
                EventBus<NarrationCue>.Raise(new NarrationCue("charging"));
            }
        }

        void OnTriggerExit(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (!rb || !rb.TryGetComponent(out BotBattery battery)) return;
            battery.RemoveSource(this);
            if (occupant == battery) occupant = null;
        }

        void OnTriggerStay(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb && rb.TryGetComponent(out HelperDrone drone)) drone.Recharge(chargePerSecond * Time.fixedDeltaTime);
        }

        void Update()
        {
            if (!glow) return;
            mpb ??= new MaterialPropertyBlock();
            bool charging = occupant && occupant.IsCharging;
            float pulse = charging ? 0.6f + 0.4f * Mathf.Sin(Time.time * 8f) : 0.35f + 0.15f * Mathf.Sin(Time.time * 2f);
            Color c = (charging ? chargingColor : idleColor) * pulse;
            mpb.SetColor("_EmissionColor", c * 2f);
            mpb.SetColor("_BaseColor", charging ? chargingColor : idleColor);
            glow.SetPropertyBlock(mpb);
        }
    }
}
