using RobotRescue.Core;
using RobotRescue.Puzzles;
using RobotRescue.World;
using UnityEngine;

namespace RobotRescue.Tasks.Rescue
{
    /// <summary>
    /// Chapter 8 Big Rescue: link the Founders' relay to the anchor engine so it runs without a keeper. While all the
    /// pylons power the relay the link builds; when power drops it slowly fades. Now Zero is safe, the hum is just the
    /// engine's beat, and every beat gives the link a push. Reach a full link and the Reach is anchored for good.
    /// </summary>
    public class AnchorLink : MonoBehaviour, IPowerable, IBossMeter, IAreaGated
    {
        public float secondsToLink = 40f;
        public float fadeSeconds = 120f;
        public float beatBoost = 0.06f;
        public string linkedStepId = "anchor_linked";
        [Tooltip("Rings that spin faster as the link builds.")]
        public Transform[] rings;
        [Tooltip("Shown once the link is complete (fireworks, lights).")]
        public GameObject[] showWhenLinked;

        public bool Powered { get; private set; }
        public float Link { get; private set; }
        public bool Linked { get; private set; }

        public string MeterTitle => Linked ? "Anchor link: CONNECTED" : "Anchor link";
        public float Meter01 => Link;
        public Color MeterColor => Linked ? new Color(1f, 0.8f, 0.3f) : Powered ? new Color(0.45f, 0.85f, 0.95f) : new Color(0.6f, 0.65f, 0.78f);
        public string MeterStatus => Linked ? "The Reach is anchored. Let the festival begin!" : Powered ? "Linking... every beat helps" : "Power all three pylons!";
        public string FailureText => null;

        void Awake()
        {
            if (showWhenLinked != null) foreach (var go in showWhenLinked) if (go) go.SetActive(false);
        }

        void OnEnable() => HumTremor.Pulsed += OnBeat;
        void OnDisable() => HumTremor.Pulsed -= OnBeat;

        public void SetPowered(bool on)
        {
            if (on == Powered) return;
            Powered = on;
            if (!Linked) EventBus<NarrationCue>.Raise(new NarrationCue(on ? "link_on" : "link_off"));
        }

        void OnBeat(HumTremor hum)
        {
            if (Linked || !Powered) return;
            Link = Mathf.Min(1f, Link + beatBoost);
            EventBus<NarrationCue>.Raise(new NarrationCue("link_beat"));
        }

        void Update()
        {
            if (!Linked)
            {
                Link = Mathf.Clamp01(Link + (Powered ? 1f / secondsToLink : -1f / fadeSeconds) * Time.deltaTime);
                if (Link >= 1f)
                {
                    Linked = true;
                    if (showWhenLinked != null) foreach (var go in showWhenLinked) if (go) go.SetActive(true);
                    if (!string.IsNullOrEmpty(linkedStepId)) EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted(linkedStepId));
                    EventBus<NarrationCue>.Raise(new NarrationCue("anchor_linked"));
                }
            }
            float speed = Linked ? 90f : 10f + 70f * Link;
            if (rings == null) return;
            for (int i = 0; i < rings.Length; i++)
                if (rings[i]) rings[i].Rotate(Vector3.up, (i % 2 == 0 ? speed : -speed) * Time.deltaTime, Space.World);
        }
    }
}
