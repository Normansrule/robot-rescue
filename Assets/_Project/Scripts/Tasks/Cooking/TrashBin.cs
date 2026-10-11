using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>Throw a burnt pot or a wrong mix in here to empty it. Stray ingredients are binned too.</summary>
    [RequireComponent(typeof(Collider))]
    public class TrashBin : MonoBehaviour
    {
        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (!rb) return;
            if (rb.TryGetComponent(out IDish dish))
            {
                if (dish.IsEmpty) return;
                dish.Empty();
                if (rb.TryGetComponent(out Respawnable home)) home.Respawn();
                EventBus<NarrationCue>.Raise(new NarrationCue("trash"));
            }
            else if (rb.TryGetComponent(out Ingredient ingredient) && !ingredient.Carryable.IsHeld)
            {
                ingredient.Consume();
            }
        }
    }
}
