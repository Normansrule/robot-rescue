using RobotRescue.Bots;
using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Tasks
{
    /// <summary>Completes a task step (and optionally says something, or powers things) the first time a bot walks in.</summary>
    [RequireComponent(typeof(Collider))]
    public class TaskStepTrigger : MonoBehaviour
    {
        public string stepId;
        public string cue;
        [Tooltip("Powered on (and left on) when the bot first walks in.")]
        public GameObject[] poweredObjects;
        bool fired;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (fired || !other.attachedRigidbody || !other.attachedRigidbody.TryGetComponent<BotController>(out _)) return;
            fired = true;
            if (!string.IsNullOrEmpty(stepId)) EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted(stepId));
            if (!string.IsNullOrEmpty(cue)) EventBus<NarrationCue>.Raise(new NarrationCue(cue));
            if (poweredObjects != null && poweredObjects.Length > 0) PowerUtil.Send(poweredObjects, true);
        }
    }
}
