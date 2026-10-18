using RobotRescue.Core;
using RobotRescue.Puzzles;
using RobotRescue.World;
using UnityEngine;

namespace RobotRescue.Tasks.Rescue
{
    /// <summary>
    /// Chapter 4 Big Rescue: Theo's volcano is slightly too big. Foam pours out and the gym slowly fills; every hum
    /// makes it surge. Each closed valve and each open drain (wired in as power inputs) slows it, and with all of
    /// them working the foam drains away. If it reaches the top: bubble bath (restart).
    /// </summary>
    public class FoamFlood : MonoBehaviour, IPowerable, IBossMeter, IAreaGated
    {
        public int inputCount = 5;
        [Range(0f, 1f)] public float startLevel = 0.12f;
        [Tooltip("Rise per second (0..1 of the max height) with nothing fixed.")]
        public float risePerSecond = 0.012f;
        public float drainPerSecond = 0.06f;
        public float humSurge = 0.07f;
        public string clearedStepId = "foam_cleared";

        [Header("Visuals")]
        public Transform foam;
        public float maxHeight = 1.4f;
        public Transform crater;

        public float Level { get; private set; }
        public int ActiveInputs { get; private set; }
        public bool Cleared { get; private set; }
        public bool Overflowed { get; private set; }

        public string MeterTitle => Cleared ? "Foam: CLEARED" : "Foam level";
        public float Meter01 => Level;
        public Color MeterColor => Color.Lerp(new Color(0.55f, 0.85f, 1f), new Color(1f, 0.45f, 0.75f), Level);
        public string MeterStatus => Cleared ? "Squeaky clean." : $"Valves and drains working {ActiveInputs}/{inputCount}";
        public string FailureText => Overflowed ? "FOAMPOCALYPSE! The gym is now a bubble bath." : null;

        Vector3 foamScale, craterScale;
        bool warned;

        void Awake()
        {
            Level = startLevel;
            if (foam) foamScale = foam.localScale;
            if (crater) craterScale = crater.localScale;
            Apply();
        }

        void OnEnable() => HumTremor.Pulsed += OnHum;
        void OnDisable() => HumTremor.Pulsed -= OnHum;

        public void SetPowered(bool on)
        {
            ActiveInputs = Mathf.Clamp(ActiveInputs + (on ? 1 : -1), 0, inputCount);
        }

        void OnHum(HumTremor hum)
        {
            if (Cleared || Overflowed) return;
            Level = Mathf.Min(1f, Level + humSurge);
            EventBus<NarrationCue>.Raise(new NarrationCue("foam_surge"));
        }

        void Update()
        {
            if (Cleared || Overflowed) { Apply(); return; }
            float rate = ActiveInputs >= inputCount ? -drainPerSecond : risePerSecond * (1f - ActiveInputs / (float)inputCount);
            Level = Mathf.Clamp01(Level + rate * Time.deltaTime);
            if (!warned && Level > 0.75f)
            {
                warned = true;
                EventBus<NarrationCue>.Raise(new NarrationCue("foam_high"));
            }
            if (Level >= 1f)
            {
                Overflowed = true;
                EventBus<NarrationCue>.Raise(new NarrationCue("foam_overflow"));
            }
            else if (Level <= 0f && ActiveInputs >= inputCount)
            {
                Cleared = true;
                if (!string.IsNullOrEmpty(clearedStepId)) EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted(clearedStepId));
                EventBus<NarrationCue>.Raise(new NarrationCue("foam_cleared"));
            }
            Apply();
        }

        void Apply()
        {
            if (foam)
            {
                float h = Mathf.Max(0.001f, Level * maxHeight);
                foam.gameObject.SetActive(Level > 0.005f);
                foam.localScale = new Vector3(foamScale.x, h, foamScale.z);
                foam.localPosition = new Vector3(foam.localPosition.x, h * 0.5f, foam.localPosition.z);
            }
            if (crater)
            {
                float bubble = Cleared ? 0.2f : 1f + Mathf.Sin(Time.time * 6f) * 0.15f;
                crater.localScale = craterScale * bubble;
            }
        }
    }
}
