using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Tasks
{
    /// <summary>Completes a task step (and optionally says something) the first time it's powered.</summary>
    public class TaskStepSignal : MonoBehaviour, IPowerable
    {
        public string stepId;
        public string cue;
        bool fired;

        public void SetPowered(bool on)
        {
            if (!on || fired) return;
            fired = true;
            if (!string.IsNullOrEmpty(stepId)) EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted(stepId));
            if (!string.IsNullOrEmpty(cue)) EventBus<NarrationCue>.Raise(new NarrationCue(cue));
        }
    }
}
