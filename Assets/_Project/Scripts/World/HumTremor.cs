using RobotRescue.Bots;
using RobotRescue.Core;
using RobotRescue.Puzzles;
using RobotRescue.Tasks.Rescue;
using UnityEngine;

namespace RobotRescue.World
{
    /// <summary>
    /// The Hum, felt: every so often a low S.O.S. rumbles up from under the sea. The camera rattles, loose things hop,
    /// leaves shake, cats yowl, and a climbing bot loses grip (hold on with stamina to spare). The meter shows it
    /// building, so players can find a branch to sit on before it hits.
    /// </summary>
    public class HumTremor : MonoBehaviour, IAreaGated, IBossMeter
    {
        public float interval = 26f;
        public float firstDelay = 16f;
        public float duration = 2.2f;
        [Tooltip("Stamina a climbing bot loses per hum.")]
        public float gripLoss = 0.45f;
        public float radius = 30f;
        public Wobble[] shakeWobbles;
        public CatNPC[] cats;
        [Tooltip("Low radio drone for the hum itself.")]
        public Babble voice;

        public bool Humming => Time.time < humUntil;

        /// <summary>Raised on every hum (seismographs record it, foam surges...).</summary>
        public static event System.Action<HumTremor> Pulsed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Pulsed = null;

        float timer, humUntil;
        int count;
        BotController bot;
        BotCamera cam;
        readonly Collider[] hits = new Collider[64];

        public string MeterTitle => "THE HUM";
        public float Meter01 => Humming ? 1f : Mathf.Clamp01(timer / interval);
        public Color MeterColor => Humming ? new Color(1f, 0.45f, 0.9f) : new Color(0.62f, 0.5f, 1f);
        public string MeterStatus => Humming ? "HUMMING! Hang on tight!"
            : timer > interval - 4f ? "It's building... find a branch to sit on" : "A low rumble from under the sea";
        public string FailureText => null;

        void OnEnable() => timer = interval - firstDelay;

        void Update()
        {
            timer += Time.deltaTime;
            if (timer < interval) return;
            timer = 0f;
            Pulse();
        }

        void Pulse()
        {
            humUntil = Time.time + duration;
            count++;
            EventBus<NarrationCue>.Raise(new NarrationCue(count == 1 ? "hum_tremor_first" : "hum_tremor"));
            if (voice) voice.Say(6, Babble.Tone.Sleepy);
            Pulsed?.Invoke(this);

            if (!bot) bot = FindAnyObjectByType<BotController>();
            if (!cam && Camera.main) cam = Camera.main.GetComponent<BotCamera>();
            if (cam) cam.Shake(0.6f);
            if (bot && (bot.transform.position - transform.position).sqrMagnitude < radius * radius) bot.Jolt(gripLoss);

            if (shakeWobbles != null)
                foreach (var w in shakeWobbles) if (w) w.Kick(Random.insideUnitCircle.normalized * 120f);
            if (cats != null)
                foreach (var c in cats) if (c) c.Spook();

            int n = Physics.OverlapSphereNonAlloc(transform.position, radius, hits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var rb = hits[i].attachedRigidbody;
                if (!rb || rb.isKinematic || rb.GetComponent<BotController>() || rb.GetComponent<CatNPC>()) continue;
                if (rb.TryGetComponent(out Carryable c) && c.IsHeld) continue;
                rb.AddForce(Vector3.up * Random.Range(1.5f, 2.5f) + Random.insideUnitSphere * 0.6f, ForceMode.VelocityChange);
            }
        }
    }
}
