using System;
using UnityEngine;

namespace RobotRescue.Tasks
{
    /// <summary>
    /// A rescue job: who needs help and the steps to do it. Used by campaign chambers now,
    /// and by Rescue Rush waves later (timeLimit, bolts).
    /// </summary>
    [CreateAssetMenu(menuName = "Robot Rescue/Task Definition", fileName = "Task_")]
    public class TaskDefinition : ScriptableObject
    {
        public string taskId = "task";
        public string title = "Help!";
        [Tooltip("Who (or what) is being saved.")]
        public string rescuee = "Someone";
        [TextArea] public string description;
        [Tooltip("Seconds. 0 = untimed (campaign).")]
        public float timeLimit;
        public int boltsReward = 10;
        public TaskStep[] steps = Array.Empty<TaskStep>();
    }

    [Serializable]
    public class TaskStep
    {
        public string id;
        public string label;
    }
}
