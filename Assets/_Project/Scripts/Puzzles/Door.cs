using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>Sliding door. Opens while powered, or for good once the scene's task is complete.</summary>
    public class Door : MonoBehaviour, IPowerable
    {
        public Vector3 openOffset = new Vector3(0f, 3.6f, 0f);
        public float speed = 3f;
        public bool openOnTaskComplete;

        Vector3 closedPosition;
        bool powered, taskDone;

        public bool IsOpen => powered || taskDone;

        void Awake() => closedPosition = transform.localPosition;
        void OnEnable() => EventBus<TaskCompleted>.Subscribe(OnTaskCompleted);
        void OnDisable() => EventBus<TaskCompleted>.Unsubscribe(OnTaskCompleted);

        public void SetPowered(bool on) => powered = on;

        void OnTaskCompleted(TaskCompleted _) { if (openOnTaskComplete) taskDone = true; }

        void Update()
        {
            Vector3 target = closedPosition + (IsOpen ? openOffset : Vector3.zero);
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, target, speed * Time.deltaTime);
        }
    }
}
