using RobotRescue.Bots;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Tools
{
    /// <summary>A spinning tool on a pedestal. Walk into it to unlock that tool on PIP's Tool Arm.</summary>
    [RequireComponent(typeof(Collider))]
    public class ToolPickup : MonoBehaviour
    {
        public ToolDefinition tool;
        public Transform visual;
        public string stepId;
        public string cue;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Update()
        {
            if (visual) visual.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
        }

        void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (!rb || !rb.TryGetComponent(out ToolArm arm)) return;
            arm.Unlock(tool);
            if (!string.IsNullOrEmpty(stepId)) EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted(stepId));
            if (!string.IsNullOrEmpty(cue)) EventBus<NarrationCue>.Raise(new NarrationCue(cue));
            gameObject.SetActive(false);
        }
    }
}
