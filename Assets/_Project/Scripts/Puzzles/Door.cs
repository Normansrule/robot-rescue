using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>Sliding door. Opens while powered, or for good once its task is complete.</summary>
    public class Door : MonoBehaviour, IPowerable
    {
        public Vector3 openOffset = new Vector3(0f, 3.6f, 0f);
        public float speed = 3f;
        public bool openOnTaskComplete;
        [Tooltip("Stay open once powered, even if the power goes away (so a room you've solved can't trap you).")]
        public bool latch;
        [Tooltip("Only this task opens the door (empty = any task). Needed once several levels share one world.")]
        public string taskId;

        Vector3 closedPosition;
        bool powered, taskDone;

        public bool IsOpen => powered || taskDone;

        void Awake() => closedPosition = transform.localPosition;
        void OnEnable() => EventBus<TaskCompleted>.Subscribe(OnTaskCompleted);
        void OnDisable() => EventBus<TaskCompleted>.Unsubscribe(OnTaskCompleted);

        public void SetPowered(bool on)
        {
            powered = on;
            if (on && latch) taskDone = true;
        }

        /// <summary>Opens for good, e.g. for a level that was finished in an earlier session.</summary>
        public void ForceOpen() => taskDone = true;

        void OnTaskCompleted(TaskCompleted evt)
        {
            if (!openOnTaskComplete) return;
            if (string.IsNullOrEmpty(taskId) || taskId == evt.TaskId) taskDone = true;
        }

        void Update()
        {
            Vector3 target = closedPosition + (IsOpen ? openOffset : Vector3.zero);
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, speed * Time.deltaTime);
        }
    }
}
