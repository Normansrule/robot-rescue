using RobotRescue.Bots;
using RobotRescue.Core;
using RobotRescue.Tools;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// A heavy metal thing (cargo crate, fuel tank) that's too heavy to pick up. With the Magnet on the Tool Arm, hold
    /// Use near it and it floats along just in front of PIP: walk it wherever it needs to go, let go to drop it.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class MagnetBlock : MonoBehaviour, IToolUsable
    {
        public float holdDistance = 1.5f;
        public float holdHeight = 0.55f;
        public float followSpeed = 8f;
        public float maxSpeed = 5f;
        public string firstCue = "magnet_first";

        public ToolKind RequiredTool => ToolKind.Magnet;
        public bool CanUse => true;
        public bool IsHeld => Time.time - lastUsed < 0.1f;

        Rigidbody body;
        ToolArm holder;
        float lastUsed = -10f;
        static bool firstSaid;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => firstSaid = false;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
        }

        public void UseTool(ToolArm arm, float deltaTime)
        {
            holder = arm;
            lastUsed = Time.time;
            if (!firstSaid && !string.IsNullOrEmpty(firstCue))
            {
                firstSaid = true;
                EventBus<NarrationCue>.Raise(new NarrationCue(firstCue));
            }
        }

        void FixedUpdate()
        {
            if (!IsHeld || !holder) return;
            Vector3 target = holder.transform.position + holder.transform.forward * holdDistance + Vector3.up * holdHeight;
            Vector3 delta = target - body.position;
            if (delta.magnitude > holdDistance * 2.5f) { lastUsed = -10f; return; }   // pulled too far: it drops
            body.linearVelocity = Vector3.ClampMagnitude(delta * followSpeed, maxSpeed);
        }
    }
}
