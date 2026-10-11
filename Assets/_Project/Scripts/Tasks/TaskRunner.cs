using System.Collections.Generic;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Tasks
{
    /// <summary>
    /// Tracks one TaskDefinition in the scene. Puzzle pieces raise <see cref="TaskStepCompleted"/>
    /// and this marks steps done, then raises <see cref="TaskCompleted"/> when all are done.
    /// </summary>
    public class TaskRunner : MonoBehaviour
    {
        public TaskDefinition task;

        public bool IsComplete { get; private set; }
        public float Elapsed { get; private set; }

        readonly HashSet<string> done = new HashSet<string>();

        public bool IsStepDone(string stepId) => done.Contains(stepId);

        void OnEnable() => EventBus<TaskStepCompleted>.Subscribe(OnStep);
        void OnDisable() => EventBus<TaskStepCompleted>.Unsubscribe(OnStep);

        void Update()
        {
            if (!IsComplete) Elapsed += Time.deltaTime;
        }

        void OnStep(TaskStepCompleted evt)
        {
            if (IsComplete || !task || !done.Add(evt.StepId)) return;
            foreach (var step in task.steps)
                if (!done.Contains(step.id)) return;

            IsComplete = true;
            EventBus<TaskCompleted>.Raise(new TaskCompleted(task.taskId));
        }
    }
}
