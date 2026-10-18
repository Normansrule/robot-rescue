using RobotRescue.Bots;
using RobotRescue.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RobotRescue.Narrative
{
    /// <summary>
    /// Hints in layers for one room, asked for with H (D-pad down): first the goal, then the system that matters, then
    /// how the pieces relate, and only last the exact action. Put it on a trigger covering the room; the room the bot
    /// last walked into answers. After a while without progress DISPATCH mentions, once, that a hint is available.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class LayeredHints : MonoBehaviour
    {
        [Tooltip("Cue ids, from the goal down to the exact action. The last one repeats.")]
        public string[] layers;
        [Tooltip("Task step that means this room is solved (hints stop).")]
        public string solvedStep;
        public float nudgeAfter = 60f;
        public string nudgeCue = "hint_available";
        public string solvedCue = "hint_solved";

        public static LayeredHints Current { get; private set; }
        public int LayersGiven => next;

        InputAction hintAction;
        int next;
        bool solved, nudged;
        float timeHere;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake()
        {
            hintAction = new InputAction("Hint", InputActionType.Button);
            hintAction.AddBinding("<Keyboard>/h");
            hintAction.AddBinding("<Gamepad>/dpad/down");
        }

        void OnEnable()
        {
            hintAction.Enable();
            EventBus<TaskStepCompleted>.Subscribe(OnStep);
        }

        void OnDisable()
        {
            hintAction.Disable();
            EventBus<TaskStepCompleted>.Unsubscribe(OnStep);
            if (Current == this) Current = null;
        }

        void OnDestroy() => hintAction.Dispose();

        void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody && other.attachedRigidbody.TryGetComponent<BotController>(out _)) Current = this;
        }

        void OnStep(TaskStepCompleted evt)
        {
            if (evt.StepId == solvedStep) solved = true;
        }

        void Update()
        {
            if (Current != this) return;
            if (hintAction.WasPressedThisFrame()) RequestHint();
            if (solved || nudged) return;
            timeHere += Time.deltaTime;
            if (timeHere < nudgeAfter) return;
            nudged = true;
            Say(nudgeCue);
        }

        /// <summary>The next layer for this room (what H does). Returns the cue said.</summary>
        public string RequestHint()
        {
            string cue = solved || layers == null || layers.Length == 0 ? solvedCue : layers[Mathf.Min(next++, layers.Length - 1)];
            Say(cue);
            timeHere = 0f;
            return cue;
        }

        static void Say(string cue)
        {
            if (!string.IsNullOrEmpty(cue)) EventBus<NarrationCue>.Raise(new NarrationCue(cue));
        }
    }
}
