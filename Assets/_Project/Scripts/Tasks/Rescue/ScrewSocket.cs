using RobotRescue.Bots;
using RobotRescue.Core;
using RobotRescue.Puzzles;
using RobotRescue.Tools;
using UnityEngine;

namespace RobotRescue.Tasks.Rescue
{
    /// <summary>
    /// A loose screw the Hum rattled out. Hold Use with the Screwdriver in front of it: the screw spins and sinks in,
    /// and once it's tight it powers its targets (a bridge swings into place, a pump starts, a ladder stands up).
    /// Wire several into a <see cref="PowerCombiner"/> when a thing needs more than one screw.
    /// </summary>
    public class ScrewSocket : MonoBehaviour, IToolUsable, IProgressIndicator
    {
        public float seconds = 1.6f;
        [Tooltip("The screw head that spins and sinks in.")]
        public Transform screw;
        [Tooltip("How far the screw sinks along its own up axis.")]
        public float sinkDepth = 0.14f;
        [Tooltip("Glowing ring that marks the screw as loose; hidden once it's tight.")]
        public GameObject looseMarker;
        public GameObject[] poweredObjects;
        public string stepId;
        public string cue = "screw_done";

        public bool IsTight { get; private set; }

        float progress;
        Vector3 screwStart;
        static bool firstScrewSaid;

        public ToolKind RequiredTool => ToolKind.Screwdriver;
        public bool CanUse => !IsTight;

        public bool ShowProgress => progress > 0f && !IsTight;
        public Vector3 IndicatorPosition => transform.position + Vector3.up * 0.7f;
        public float Progress01 => progress;
        public Color ProgressColor => new Color(1f, 0.85f, 0.25f);

        void Awake()
        {
            if (screw) screwStart = screw.localPosition;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => firstScrewSaid = false;

        public void UseTool(ToolArm arm, float deltaTime)
        {
            if (IsTight) return;
            if (!firstScrewSaid)
            {
                firstScrewSaid = true;
                EventBus<NarrationCue>.Raise(new NarrationCue("screw_first"));
            }
            progress = Mathf.Min(1f, progress + deltaTime / Mathf.Max(0.1f, seconds));
            if (screw)
            {
                screw.Rotate(0f, 720f * deltaTime, 0f, Space.Self);
                screw.localPosition = screwStart - (screw.parent ? screw.parent.InverseTransformDirection(screw.up) : screw.up) * (sinkDepth * progress);
            }
            if (progress < 1f) return;

            IsTight = true;
            if (looseMarker) looseMarker.SetActive(false);
            PowerUtil.Send(poweredObjects, true);
            if (!string.IsNullOrEmpty(stepId)) EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted(stepId));
            if (!string.IsNullOrEmpty(cue)) EventBus<NarrationCue>.Raise(new NarrationCue(cue));
        }
    }
}
