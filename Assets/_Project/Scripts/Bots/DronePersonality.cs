using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Bots
{
    public enum Temperament { Cheerful, Nervous, Grumpy, Sleepy, ShowOff }

    /// <summary>
    /// Every Helper Drone has a name and a temperament. They watch PIP, react to being picked up and thrown, wave
    /// back when PIP waves, and have idle quirks: Cheerful ones bounce, Nervous ones jitter, Grumpy ones huff,
    /// Sleepy ones snore, Show-offs spin. Quirks only move the visual, never the physics body, so puzzles stay fair.
    /// </summary>
    [RequireComponent(typeof(HelperDrone))]
    public class DronePersonality : MonoBehaviour
    {
        public string droneName = "Drone";
        public Temperament temperament;
        [Tooltip("Child holding the meshes (moved for hops and spins).")]
        public Transform visual;
        public BotFace face;
        public Babble voice;

        public string Description => temperament switch
        {
            Temperament.Cheerful => "cheerful",
            Temperament.Nervous => "nervous",
            Temperament.Grumpy => "grumpy",
            Temperament.Sleepy => "sleepy",
            _ => "show-off",
        };

        HelperDrone drone;
        Carryable carry;
        Transform pip;
        Vector3 visualBase;
        float nextQuirk, hopT = 1f, spinT = 1f, jitterUntil, reactAt = -1f;
        bool wasHeld;

        void Awake()
        {
            drone = GetComponent<HelperDrone>();
            carry = GetComponent<Carryable>();
            if (visual) visualBase = visual.localPosition;
            nextQuirk = Time.time + Random.Range(2f, 7f);
            ApplyTemperament();
        }

        void FindPip()
        {
            // PIP can be switched off (open-world title screen), so keep looking until it shows up.
            var bot = FindAnyObjectByType<BotController>();
            if (bot) pip = bot.transform;
        }

        void OnEnable() => EventBus<BotEmoted>.Subscribe(OnEmote);
        void OnDisable() => EventBus<BotEmoted>.Unsubscribe(OnEmote);

        void ApplyTemperament()
        {
            if (!face) return;
            switch (temperament)
            {
                case Temperament.Cheerful: face.restingSmile = 0.7f; break;
                case Temperament.Nervous: face.restingSmile = -0.15f; face.restingSad = 0.35f; break;
                case Temperament.Grumpy: face.restingSmile = -0.45f; face.restingAngry = 0.55f; break;
                case Temperament.Sleepy: face.restingSmile = 0.15f; face.restingSleepy = 0.55f; break;
                case Temperament.ShowOff: face.restingSmile = 0.9f; break;
            }
        }

        void Say(Mood mood, float seconds, Babble.Tone tone, int syllables)
        {
            if (face) { face.SetMood(mood, seconds); face.Talk(syllables * 0.1f); }
            if (voice) voice.Say(syllables, tone);
        }

        void OnEmote(BotEmoted e)
        {
            if ((e.Position - transform.position).sqrMagnitude < 64f) reactAt = Time.time + Random.Range(0.2f, 0.7f);
        }

        void WaveBack()
        {
            switch (temperament)
            {
                case Temperament.Grumpy: Say(Mood.Angry, 1.2f, Babble.Tone.Sad, 1); Hop(0.5f); break;
                case Temperament.Nervous: Say(Mood.Surprised, 0.5f, Babble.Tone.Surprised, 1); Invoke(nameof(ShyHappy), 0.5f); break;
                case Temperament.Sleepy: Say(Mood.Happy, 1f, Babble.Tone.Sleepy, 2); break;
                case Temperament.ShowOff: Say(Mood.Love, 1.4f, Babble.Tone.Happy, 3); Spin(); break;
                default: Say(Mood.Happy, 1.4f, Babble.Tone.Happy, 3); Hop(1f); break;
            }
        }

        void ShyHappy() { Say(Mood.Happy, 1f, Babble.Tone.Happy, 2); Hop(0.6f); }

        void Hop(float strength) { hopT = 0f; hopStrength = strength; }
        float hopStrength = 1f;
        void Spin() => spinT = 0f;

        void Update()
        {
            float t = Time.time, dt = Time.deltaTime;
            if (!pip && Time.frameCount % 30 == 0) FindPip();
            if (reactAt > 0f && t >= reactAt) { reactAt = -1f; WaveBack(); }

            // Picked up / let go.
            bool held = carry.IsHeld;
            if (held && !wasHeld)
            {
                switch (temperament)
                {
                    case Temperament.Grumpy: Say(Mood.Angry, 1.5f, Babble.Tone.Sad, 2); break;
                    case Temperament.Nervous: Say(Mood.Surprised, 1.2f, Babble.Tone.Surprised, 3); jitterUntil = t + 1f; break;
                    case Temperament.Sleepy: Say(Mood.Surprised, 0.6f, Babble.Tone.Question, 1); break;
                    case Temperament.ShowOff: Say(Mood.Love, 1.2f, Babble.Tone.Happy, 3); break;
                    default: Say(Mood.Happy, 1.5f, Babble.Tone.Happy, 3); break;
                }
            }
            wasHeld = held;

            if (face)
            {
                face.Drowsy = Mathf.Clamp01((0.3f - drone.Charge01) / 0.3f);
                face.Powered = drone.Charge01 > 0.01f;
                if (pip && (pip.position - transform.position).sqrMagnitude < 49f) face.LookAt(pip.position + Vector3.up * 0.9f);
            }

            // Idle quirks while PIP is around to see them.
            if (!held && pip && t > nextQuirk && (pip.position - transform.position).sqrMagnitude < 144f)
            {
                nextQuirk = t + Random.Range(4f, 9f);
                switch (temperament)
                {
                    case Temperament.Cheerful: Hop(1f); Say(Mood.Happy, 1f, Babble.Tone.Happy, 2); break;
                    case Temperament.Nervous: jitterUntil = t + 0.8f; Say(Mood.Surprised, 0.6f, Babble.Tone.Question, 1); break;
                    case Temperament.Grumpy: Say(Mood.Angry, 1.2f, Babble.Tone.Sad, 1); break;
                    case Temperament.Sleepy: Say(Mood.Sleepy, 3f, Babble.Tone.Sleepy, 2); break;
                    case Temperament.ShowOff: Spin(); Say(Mood.Love, 1.2f, Babble.Tone.Happy, 2); break;
                }
            }

            if (!visual) return;
            hopT = Mathf.Min(1f, hopT + dt / 0.45f);
            spinT = Mathf.Min(1f, spinT + dt / 0.9f);
            float hop = Mathf.Sin(hopT * Mathf.PI) * 0.35f * hopStrength;
            Vector3 jitter = t < jitterUntil ? Random.insideUnitSphere * 0.03f : Vector3.zero;
            visual.localPosition = visualBase + Vector3.up * hop + jitter;
            float squash = hopT < 1f ? 1f + Mathf.Sin(hopT * Mathf.PI * 2f) * 0.08f : 1f;
            visual.localScale = new Vector3(1f / Mathf.Sqrt(squash), squash, 1f / Mathf.Sqrt(squash));
            visual.localRotation = Quaternion.Euler(0f, Mathf.SmoothStep(0f, 360f, spinT), 0f);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (collision.relativeVelocity.magnitude > 5f && !carry.IsHeld)
                Say(Mood.Dizzy, 1.6f, Babble.Tone.Surprised, 1);
        }
    }
}
