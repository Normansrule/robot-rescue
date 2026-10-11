using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>Trigger on top of a Stove: tells pots when they're on the heat.</summary>
    [RequireComponent(typeof(Collider))]
    public class BurnerZone : MonoBehaviour
    {
        Stove stove;

        void Awake() => stove = GetComponentInParent<Stove>();

        void OnTriggerStay(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (!rb || !rb.TryGetComponent(out CookingPot pot)) return;
            // A pot held overhead isn't on the heat.
            var carryable = rb.GetComponent<RobotRescue.Puzzles.Carryable>();
            if (carryable && carryable.IsHeld) pot.ClearStove(stove);
            else pot.SetStove(stove);
        }

        void OnTriggerExit(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb && rb.TryGetComponent(out CookingPot pot)) pot.ClearStove(stove);
        }
    }
}
