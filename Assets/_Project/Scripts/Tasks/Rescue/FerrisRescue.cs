using RobotRescue.Core;
using RobotRescue.Puzzles;
using RobotRescue.World;
using UnityEngine;

namespace RobotRescue.Tasks.Rescue
{
    /// <summary>
    /// Chapter 6 Big Rescue: the Ferris wheel is stuck with a gondola at the very top. While powered the wheel turns
    /// slowly and brings that gondola down; every hum jams it for a moment and rolls it back a little. Get the stuck
    /// gondola all the way to the bottom. Gondolas stay hanging upright as the wheel turns.
    /// </summary>
    public class FerrisRescue : MonoBehaviour, IPowerable, IBossMeter, IAreaGated
    {
        [Tooltip("The part of the wheel that turns (spokes, rim, gondola hangers).")]
        public Transform wheel;
        [Tooltip("Gondolas hanging from the wheel; they're kept level.")]
        public Transform[] gondolas;
        public Vector3 axis = Vector3.forward;
        [Tooltip("How far the stuck gondola has to travel (degrees) to reach the bottom.")]
        public float degreesToBottom = 180f;
        public float secondsToBottom = 45f;
        public float jamSeconds = 4f;
        [Tooltip("Fraction of the trip lost each time the hum jams the wheel.")]
        public float jamLoss = 0.04f;
        public string downStepId = "wheel_down";

        public bool Powered { get; private set; }
        public float Progress { get; private set; }
        public bool Down { get; private set; }

        public string MeterTitle => Down ? "Grandpa Sal and the twins: SAFE" : "Stuck gondola";
        public float Meter01 => Progress;
        public Color MeterColor => Jammed ? new Color(0.95f, 0.45f, 0.4f) : Powered ? new Color(0.45f, 0.85f, 0.55f) : new Color(0.6f, 0.65f, 0.78f);
        public string MeterStatus => Down ? "Everybody off! Mind the step." : Jammed ? "The hum jammed the wheel!" : Powered ? "Turning... bringing them down" : "The wheel's stopped";
        public string FailureText => null;

        bool Jammed => Time.time < jamUntil;
        float jamUntil = -10f;
        Quaternion startRotation;
        Quaternion[] gondolaRotations;

        void Awake()
        {
            if (!wheel) wheel = transform;
            startRotation = wheel.localRotation;
            gondolaRotations = new Quaternion[gondolas?.Length ?? 0];
            for (int i = 0; i < gondolaRotations.Length; i++) gondolaRotations[i] = gondolas[i] ? gondolas[i].rotation : Quaternion.identity;
        }

        void OnEnable() => HumTremor.Pulsed += OnHum;
        void OnDisable() => HumTremor.Pulsed -= OnHum;

        public void SetPowered(bool on)
        {
            if (on == Powered) return;
            Powered = on;
            if (!Down) EventBus<NarrationCue>.Raise(new NarrationCue(on ? "wheel_on" : "wheel_off"));
        }

        void OnHum(HumTremor hum)
        {
            if (Down) return;
            jamUntil = Time.time + jamSeconds;
            Progress = Mathf.Max(0f, Progress - jamLoss);
            EventBus<NarrationCue>.Raise(new NarrationCue("wheel_jammed"));
        }

        void Update()
        {
            if (!Down && Powered && !Jammed)
            {
                Progress = Mathf.Clamp01(Progress + Time.deltaTime / secondsToBottom);
                if (Progress >= 1f)
                {
                    Down = true;
                    if (!string.IsNullOrEmpty(downStepId)) EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted(downStepId));
                    EventBus<NarrationCue>.Raise(new NarrationCue("wheel_down"));
                }
            }

            // Ease toward the target angle so a jam reads as a shudder and a lurch back, not a snap.
            float shudder = Jammed ? Mathf.Sin(Time.time * 40f) * 0.6f : 0f;
            var target = startRotation * Quaternion.AngleAxis(Progress * degreesToBottom + shudder, axis);
            wheel.localRotation = Quaternion.Slerp(wheel.localRotation, target, 4f * Time.deltaTime);

            // Gondolas hang level, with a little swing.
            for (int i = 0; i < gondolaRotations.Length; i++)
                if (gondolas[i]) gondolas[i].rotation = gondolaRotations[i] * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.3f + i) * (Jammed ? 6f : 2f));
        }
    }
}
