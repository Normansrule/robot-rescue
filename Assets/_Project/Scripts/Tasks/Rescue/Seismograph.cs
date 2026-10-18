using RobotRescue.Core;
using RobotRescue.Puzzles;
using RobotRescue.World;
using UnityEngine;

namespace RobotRescue.Tasks.Rescue
{
    /// <summary>
    /// Priya's science-fair seismograph. Once it's powered (and its stand is fixed) it's armed; the next time the Hum
    /// comes through it scribbles the pattern onto its paper drum and the step completes.
    /// </summary>
    public class Seismograph : MonoBehaviour, IPowerable
    {
        public Transform drum;
        public Transform pen;
        public Renderer readyLight;
        public string armedCue = "seismo_armed";
        public string recordedStepId = "record_hum";
        public string recordedCue = "hum_recorded";

        public bool Armed { get; private set; }
        public bool Recorded { get; private set; }

        float scribbleUntil;
        MaterialPropertyBlock mpb;

        void OnEnable() => HumTremor.Pulsed += OnHum;
        void OnDisable() => HumTremor.Pulsed -= OnHum;

        void Start() => Light(new Color(1f, 0.3f, 0.3f));

        public void SetPowered(bool on)
        {
            if (on == Armed) return;
            Armed = on;
            Light(on ? new Color(0.3f, 1f, 0.45f) : new Color(1f, 0.3f, 0.3f));
            if (on && !Recorded && !string.IsNullOrEmpty(armedCue)) EventBus<NarrationCue>.Raise(new NarrationCue(armedCue));
        }

        void OnHum(HumTremor hum)
        {
            if (!Armed) return;
            scribbleUntil = Time.time + 2.5f;
            if (Recorded) return;
            Recorded = true;
            if (!string.IsNullOrEmpty(recordedStepId)) EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted(recordedStepId));
            if (!string.IsNullOrEmpty(recordedCue)) EventBus<NarrationCue>.Raise(new NarrationCue(recordedCue));
        }

        void Update()
        {
            if (drum && Armed) drum.Rotate(0f, 40f * Time.deltaTime, 0f, Space.Self);
            if (!pen) return;
            // Short short short, long long long: the pen twitches in the S.O.S. rhythm while it records.
            float wiggle = 0f;
            if (Time.time < scribbleUntil)
            {
                float t = (scribbleUntil - Time.time) * 4f;
                wiggle = Mathf.Sin(t * (Mathf.Repeat(t, 6f) < 3f ? 18f : 6f)) * 25f;
            }
            pen.localRotation = Quaternion.Euler(0f, 0f, wiggle);
        }

        void Light(Color c)
        {
            if (!readyLight) return;
            mpb ??= new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_EmissionColor", c * 2f);
            readyLight.SetPropertyBlock(mpb);
        }
    }
}
