using RobotRescue.Bots;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// A puzzle-local reset: stand on the pad for a moment and this room's objects go back to where they started
    /// (cells lose the charge you gave them). Nothing else in the level changes, finished steps stay finished, and
    /// rescued things stay rescued. Step off and back on to reset again.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PuzzleReset : MonoBehaviour, IProgressIndicator
    {
        public Respawnable[] objects;
        public float holdSeconds = 1.2f;
        public string cue = "room_reset";

        public int ResetCount { get; private set; }

        BotController standing;
        float held;
        bool fired;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (!other.attachedRigidbody || !other.attachedRigidbody.TryGetComponent(out BotController bot)) return;
            standing = bot;
            held = 0f;
            fired = false;
        }

        void OnTriggerExit(Collider other)
        {
            if (other.attachedRigidbody && other.attachedRigidbody.TryGetComponent(out BotController bot) && bot == standing) standing = null;
        }

        void Update()
        {
            if (!standing || fired) return;
            held += Time.deltaTime;
            if (held < holdSeconds) return;
            fired = true;
            ResetRoom();
        }

        public void ResetRoom()
        {
            foreach (var o in objects)
            {
                if (!o) continue;
                // A rescue drop zone switches the Carryable off once it's home: leave it there.
                if (o.TryGetComponent(out Carryable c) && !c.enabled) continue;
                if (c && c.AttachedCable) c.AttachedCable.Detach();
                o.Respawn();
                if (o.TryGetComponent(out PowerCell cell)) cell.ResetCharge();
            }
            ResetCount++;
            if (!string.IsNullOrEmpty(cue)) EventBus<NarrationCue>.Raise(new NarrationCue(cue));
        }

        public bool ShowProgress => standing && !fired && held > 0.05f;
        public Vector3 IndicatorPosition => transform.position + Vector3.up * 1.6f;
        public float Progress01 => Mathf.Clamp01(held / holdSeconds);
        public Color ProgressColor => new Color(1f, 0.5f, 0.45f);
    }
}
