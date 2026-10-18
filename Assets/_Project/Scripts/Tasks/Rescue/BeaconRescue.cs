using RobotRescue.Core;
using RobotRescue.Puzzles;
using RobotRescue.World;
using UnityEngine;

namespace RobotRescue.Tasks.Rescue
{
    /// <summary>
    /// Chapter 5 Big Rescue: the lighthouse beacon guides Captain Mo's boat home through the fog. While the beacon is
    /// powered the boat sails in; while it's dark she drifts back. Every hum makes the beacon flicker on its own (in
    /// morse: Z, E, R, O). Bring the boat all the way to the dock.
    /// </summary>
    public class BeaconRescue : MonoBehaviour, IPowerable, IBossMeter, IAreaGated
    {
        public float secondsToHome = 40f;
        public float driftSeconds = 90f;
        public string homeStepId = "boat_home";

        [Header("Visuals")]
        public Transform beam;
        public Renderer lamp;
        public Transform boat;
        public Transform[] boatPath;

        public bool Powered { get; private set; }
        public float Progress { get; private set; }
        public bool Home { get; private set; }

        public string MeterTitle => Home ? "Captain Mo: SAFE IN HARBOUR" : "Captain Mo's boat";
        public float Meter01 => Progress;
        public Color MeterColor => Lit ? new Color(1f, 0.85f, 0.35f) : new Color(0.55f, 0.6f, 0.75f);
        public string MeterStatus => Home ? "Welcome home!" : Lit ? "Beacon on: she's following the light" : "Beacon dark: she's drifting in the fog!";
        public string FailureText => null;

        bool Lit => Powered && Time.time > flickerUntil;
        float flickerStart = -10f, flickerUntil = -10f;
        bool lastLit;
        MaterialPropertyBlock mpb;

        void OnEnable() => HumTremor.Pulsed += OnHum;
        void OnDisable() => HumTremor.Pulsed -= OnHum;

        public void SetPowered(bool on)
        {
            if (on == Powered) return;
            Powered = on;
            if (!Home) EventBus<NarrationCue>.Raise(new NarrationCue(on ? "beacon_on" : "beacon_off"));
        }

        void OnHum(HumTremor hum)
        {
            flickerStart = Time.time;
            flickerUntil = Time.time + 4f;
            EventBus<NarrationCue>.Raise(new NarrationCue("beacon_morse"));
        }

        void Update()
        {
            if (!Home)
            {
                Progress = Mathf.Clamp01(Progress + (Lit ? 1f / secondsToHome : -1f / driftSeconds) * Time.deltaTime);
                if (Progress >= 1f)
                {
                    Home = true;
                    if (!string.IsNullOrEmpty(homeStepId)) EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted(homeStepId));
                    EventBus<NarrationCue>.Raise(new NarrationCue("boat_home"));
                }
            }
            MoveBoat();

            // The lamp: steady when lit, a morse stutter while the hum is coming through (Z E R O: --.. . .-. ---).
            bool on = Lit || Home;
            if (Time.time < flickerUntil)
            {
                const string morse = "111010110001000101101000111011101110000";
                int i = Mathf.FloorToInt((Time.time - flickerStart) * 10f) % morse.Length;
                on = morse[i] == '1';
            }
            if (beam)
            {
                beam.gameObject.SetActive(on);
                if (on) beam.Rotate(0f, 60f * Time.deltaTime, 0f, Space.World);
            }
            if (lamp && on != lastLit)
            {
                lastLit = on;
                mpb ??= new MaterialPropertyBlock();
                var c = on ? new Color(1f, 0.9f, 0.55f) : new Color(0.35f, 0.35f, 0.4f);
                mpb.SetColor("_BaseColor", c);
                mpb.SetColor("_EmissionColor", on ? c * 3f : Color.black);
                lamp.SetPropertyBlock(mpb);
            }
        }

        void MoveBoat()
        {
            if (!boat || boatPath == null || boatPath.Length < 2) return;
            float f = Progress * (boatPath.Length - 1);
            int i = Mathf.Min(Mathf.FloorToInt(f), boatPath.Length - 2);
            float t = f - i;
            Vector3 p = Vector3.Lerp(boatPath[i].position, boatPath[i + 1].position, t);
            p.y += Mathf.Sin(Time.time * 1.6f) * 0.08f;
            Vector3 dir = boatPath[i + 1].position - boatPath[i].position;
            dir.y = 0f;
            boat.position = p;
            if (dir.sqrMagnitude > 0.01f)
                boat.rotation = Quaternion.Slerp(boat.rotation, Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 1.1f) * 4f), 3f * Time.deltaTime);
        }
    }
}
