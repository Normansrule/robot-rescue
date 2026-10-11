using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Tasks.Rescue
{
    /// <summary>
    /// Chapter 1 Big Rescue: DISPATCH's coffee machine is about to blow. Pressure climbs on its own; every open
    /// <see cref="SteamVent"/> bleeds some off. Hold all the vents open at once long enough to stabilise it.
    /// Let the pressure hit 100% and... KABOOM (restart).
    /// </summary>
    public class CoffeeMachine : MonoBehaviour, IBossMeter
    {
        public int ventCount = 3;
        [Range(0f, 100f)] public float startPressure = 25f;
        public float risePerSecond = 1.6f;
        public float ventPerSecond = 1.1f;
        [Tooltip("Seconds with every vent open to save the machine.")]
        public float stabiliseSeconds = 8f;
        public string savedStepId = "coffee_saved";

        [Header("Visuals")]
        public Transform body;
        public Transform gaugeNeedle;
        public Renderer warningLight;

        public float Pressure { get; private set; }
        public float Stabilised01 => Mathf.Clamp01(stableTime / stabiliseSeconds);
        public bool Saved { get; private set; }
        public bool Exploded { get; private set; }
        public int OpenVents { get; private set; }

        public string MeterTitle => Saved ? "Coffee machine: SAVED" : "Coffee machine pressure";
        public float Meter01 => Pressure / 100f;
        public Color MeterColor => Color.Lerp(new Color(0.4f, 0.85f, 0.5f), new Color(1f, 0.3f, 0.25f), Pressure / 100f);
        public string MeterStatus => Saved ? "Phew." : $"Vents open {OpenVents}/{ventCount}   ·   Stabilising {Mathf.RoundToInt(Stabilised01 * 100f)}%";
        public string FailureText => Exploded ? "KABOOM! The coffee machine exploded." : null;

        bool[] vents;
        float stableTime;
        Vector3 bodyScale;
        Quaternion needleBase = Quaternion.identity;
        bool halfwayCue, dangerCue;

        void Awake()
        {
            vents = new bool[ventCount];
            Pressure = startPressure;
            if (body) bodyScale = body.localScale;
            if (gaugeNeedle) needleBase = gaugeNeedle.localRotation;
        }

        public void SetVent(int index, bool open)
        {
            if (index < 0 || index >= vents.Length || vents[index] == open) return;
            vents[index] = open;
            OpenVents = 0;
            foreach (var v in vents) if (v) OpenVents++;
            if (open) EventBus<NarrationCue>.Raise(new NarrationCue(OpenVents == ventCount ? "vents_all_open" : "vent_open"));
        }

        void Update()
        {
            if (Saved || Exploded) { AnimateBody(); return; }
            float dt = Time.deltaTime;

            Pressure = Mathf.Clamp(Pressure + (risePerSecond - OpenVents * ventPerSecond) * dt, 0f, 100f);
            if (OpenVents == ventCount) stableTime += dt;
            else stableTime = Mathf.Max(0f, stableTime - dt * 0.5f);

            if (!halfwayCue && Pressure > 60f) { halfwayCue = true; EventBus<NarrationCue>.Raise(new NarrationCue("pressure_high")); }
            if (!dangerCue && Pressure > 85f) { dangerCue = true; EventBus<NarrationCue>.Raise(new NarrationCue("pressure_danger")); }

            if (Pressure >= 100f)
            {
                Exploded = true;
                EventBus<NarrationCue>.Raise(new NarrationCue("coffee_boom"));
            }
            else if (stableTime >= stabiliseSeconds)
            {
                Saved = true;
                Pressure = 0f;
                EventBus<NarrationCue>.Raise(new NarrationCue("coffee_saved"));
                EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted(savedStepId));
            }
            AnimateBody();
        }

        void AnimateBody()
        {
            float p = Pressure / 100f;
            if (body)
            {
                float shake = Exploded ? 0f : p * p * 0.06f;
                float swell = Exploded ? 1.25f : 1f + p * 0.08f;
                body.localScale = bodyScale * swell;
                body.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 40f) * shake * 60f, 0f, Mathf.Cos(Time.time * 37f) * shake * 60f);
            }
            if (gaugeNeedle) gaugeNeedle.localRotation = needleBase * Quaternion.Euler(0f, 0f, Mathf.Lerp(80f, -80f, p));
            if (warningLight)
            {
                bool blink = p > 0.6f && Mathf.Repeat(Time.time * (2f + p * 6f), 1f) > 0.5f;
                warningLight.material.color = Saved ? new Color(0.3f, 1f, 0.5f) : blink ? new Color(1f, 0.2f, 0.15f) : new Color(0.35f, 0.3f, 0.3f);
            }
        }
    }
}
