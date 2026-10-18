using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Tasks.Rescue
{
    /// <summary>
    /// Where a rescued thing goes home: a cat basket, a kid's arms, a lost-property box. Set the carryable down in
    /// the zone (not held) and it settles onto <see cref="seat"/>, completes the step and says something.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class RescueDropZone : MonoBehaviour
    {
        [Tooltip("Carryable display name this zone accepts (e.g. 'Mittens'). Empty: anything.")]
        public string accepts;
        public Transform seat;
        public string stepId;
        public string cue;
        public GameObject[] poweredObjects;

        public bool Done { get; private set; }

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerStay(Collider other)
        {
            if (Done) return;
            var rb = other.attachedRigidbody;
            if (!rb || !rb.TryGetComponent(out Carryable c) || c.IsHeld) return;
            if (!string.IsNullOrEmpty(accepts) && c.displayName != accepts) return;

            Done = true;
            if (seat)
            {
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rb.isKinematic = true;
                rb.position = seat.position;
                rb.rotation = seat.rotation;
                c.transform.SetPositionAndRotation(seat.position, seat.rotation);
                c.transform.SetParent(seat, true);
                c.enabled = false;
            }
            if (c is CatNPC cat) cat.GoHome();
            PowerUtil.Send(poweredObjects, true);
            if (!string.IsNullOrEmpty(stepId)) EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted(stepId));
            if (!string.IsNullOrEmpty(cue)) EventBus<NarrationCue>.Raise(new NarrationCue(cue));
        }
    }
}
