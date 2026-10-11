using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>Trigger on top of a pot or bowl that catches dropped or thrown ingredients.</summary>
    [RequireComponent(typeof(Collider))]
    public class PotMouth : MonoBehaviour
    {
        public float rejectBounce = 4f;
        IIngredientReceiver receiver;

        void Awake() => receiver = GetComponentInParent<IIngredientReceiver>();

        void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (receiver == null || !rb || !rb.TryGetComponent(out Ingredient ingredient) || ingredient.Carryable.IsHeld) return;
            if (!receiver.TryAddIngredient(ingredient))
                rb.linearVelocity = Vector3.up * rejectBounce + Random.insideUnitSphere;
        }
    }
}
