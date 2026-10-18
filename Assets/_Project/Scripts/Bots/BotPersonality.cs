using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// PIP's personality: reacts to everything with its face, voice and body. Cheers when a step is done, swoons with
    /// heart eyes when a rescue's finished, gets dizzy after a fall, says "wheee" on swings, grunts lifting heavy things,
    /// looks at whatever it could grab, gets bored and nods off if you leave it alone, and waves (G / D-pad up).
    /// </summary>
    public class BotPersonality : MonoBehaviour
    {
        public BotFace face;
        public Babble voice;
        public BotAnimator animator;
        [Tooltip("Seconds without input before PIP starts looking around, then falls asleep.")]
        public float boredAfter = 9f, sleepAfter = 26f;

        BotController bot;
        Grabber grabber;
        TetherCable tether;
        BotBattery battery;
        BotParts parts;
        BotInput input;
        float idle, nextIdleLook, nextSnore;
        bool wasHolding, wasSwinging, wasCharging;

        void Awake()
        {
            bot = GetComponent<BotController>();
            grabber = GetComponent<Grabber>();
            tether = GetComponent<TetherCable>();
            battery = GetComponent<BotBattery>();
            parts = GetComponent<BotParts>();
            input = GetComponent<BotInput>();
        }

        void OnEnable()
        {
            EventBus<TaskStepCompleted>.Subscribe(OnStep);
            EventBus<TaskCompleted>.Subscribe(OnTask);
            EventBus<NarrationCue>.Subscribe(OnCue);
            EventBus<BotEmoted>.Subscribe(OnEmote);
            EventBus<BoltCollected>.Subscribe(OnBolt);
            if (bot) { bot.Landed += OnLanded; bot.Jumped += OnJumped; }
            if (battery) { battery.PoweredDown += OnPowerDown; battery.PoweredUp += OnPowerUp; battery.WentLow += OnLow; }
            if (parts) parts.Changed += OnPart;
        }

        void OnDisable()
        {
            EventBus<TaskStepCompleted>.Unsubscribe(OnStep);
            EventBus<TaskCompleted>.Unsubscribe(OnTask);
            EventBus<NarrationCue>.Unsubscribe(OnCue);
            EventBus<BotEmoted>.Unsubscribe(OnEmote);
            EventBus<BoltCollected>.Unsubscribe(OnBolt);
            if (bot) { bot.Landed -= OnLanded; bot.Jumped -= OnJumped; }
            if (battery) { battery.PoweredDown -= OnPowerDown; battery.PoweredUp -= OnPowerUp; battery.WentLow -= OnLow; }
            if (parts) parts.Changed -= OnPart;
        }

        void React(Mood mood, float seconds, Babble.Tone tone, int syllables)
        {
            if (battery && battery.IsDead) return;
            if (face) { face.SetMood(mood, seconds); if (syllables > 0) face.Talk(syllables * 0.1f); }
            if (voice && syllables > 0) voice.Say(syllables, tone);
            idle = 0f;
        }

        void OnStep(TaskStepCompleted _) { React(Mood.Happy, 1.6f, Babble.Tone.Happy, 3); if (animator) animator.Hop(); }

        void OnTask(TaskCompleted _)
        {
            React(Mood.Love, 3f, Babble.Tone.Happy, 6);
            if (animator) animator.Spin(1.1f, 1f);
        }

        void OnCue(NarrationCue cue)
        {
            if (cue.Id == "fell") React(Mood.Dizzy, 2.2f, Babble.Tone.Sad, 2);
        }

        void OnEmote(BotEmoted _)
        {
            React(Mood.Happy, 1.6f, Babble.Tone.Happy, 4);
            if (animator) animator.Wave();
        }

        void OnBolt(BoltCollected _) { React(Mood.Love, 1.2f, Babble.Tone.Happy, 2); if (animator) animator.Hop(); }

        void OnLanded(float speed)
        {
            if (speed > 11f) React(Mood.Surprised, 0.7f, Babble.Tone.Surprised, 1);
        }

        void OnJumped()
        {
            if (voice && Random.value < 0.35f) voice.Say(1, Babble.Tone.Effort);
        }

        void OnPowerDown()
        {
            if (voice) voice.Say(4, Babble.Tone.Sleepy);
            if (face) face.Powered = false;
        }

        void OnPowerUp()
        {
            if (face) face.Powered = true;
            React(Mood.Surprised, 0.8f, Babble.Tone.Question, 2);
        }

        void OnLow() => React(Mood.Sad, 1.5f, Babble.Tone.Sad, 2);

        void OnPart(BotPart part, bool attached)
        {
            if (attached) React(Mood.Happy, 1.2f, Babble.Tone.Happy, 2);
            else { React(Mood.Surprised, 0.5f, Babble.Tone.Surprised, 1); Invoke(nameof(SadAfterLoss), 0.5f); }
        }

        void SadAfterLoss() => React(Mood.Sad, 1.6f, Babble.Tone.Sad, 2);

        void Update()
        {
            float dt = Time.deltaTime;
            if (input && input.AnyActivity)
            {
                if (face && face.CurrentMood == Mood.Sleepy) React(Mood.Surprised, 0.6f, Babble.Tone.Surprised, 1);
                idle = 0f;
            }
            else idle += dt;

            bool holding = grabber && grabber.Held;
            if (holding && !wasHolding)
                React(grabber.Held.Body.mass > 1.2f ? Mood.Determined : Mood.Happy, 1.2f, Babble.Tone.Effort, 1);
            wasHolding = holding;

            bool swinging = tether && tether.IsSwinging;
            if (swinging && !wasSwinging) React(Mood.Happy, 1.5f, Babble.Tone.Happy, 4);
            wasSwinging = swinging;

            bool charging = battery && battery.IsCharging;
            if (charging && !wasCharging) React(Mood.Happy, 1.2f, Babble.Tone.Happy, 2);
            wasCharging = charging;

            if (!face) return;
            face.Drowsy = battery ? Mathf.Clamp01((battery.lowThreshold - battery.Charge01) / battery.lowThreshold) * 0.6f : 0f;
            face.Glitch = parts && !parts.Has(BotPart.Antenna) ? 0.25f : 0f;

            // Where to look: the thing PIP could grab or tether, else the camera now and then when idle.
            if (grabber && grabber.Focus) face.LookAt(grabber.Focus.transform.position);
            else if (tether && tether.AimTarget) face.LookAt(tether.AimTarget.AttachPosition);
            else if (idle > boredAfter && Time.time > nextIdleLook)
            {
                nextIdleLook = Time.time + Random.Range(1.2f, 2.5f);
                var cam = Camera.main;
                Vector3 target = cam && Random.value < 0.5f
                    ? cam.transform.position
                    : transform.position + Vector3.up + Quaternion.Euler(0f, Random.Range(-80f, 80f), 0f) * transform.forward * 3f;
                face.LookAt(target, 1.2f);
            }

            if (idle > sleepAfter && !(battery && battery.IsDead))
            {
                face.SetMood(Mood.Sleepy, 1f);
                if (voice && Time.time > nextSnore)
                {
                    nextSnore = Time.time + 3.5f;
                    voice.Say(2, Babble.Tone.Sleepy);
                }
            }
        }
    }
}
