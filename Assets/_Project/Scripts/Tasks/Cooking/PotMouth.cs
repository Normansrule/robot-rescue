using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>Trigger on top of a pot that catches dropped or thrown ingredients.</summary>
    [RequireComponent(typeof(Collider))]
    public class PotMouth : MonoBehaviour
    {
        public float rejectBounce = 4f;
        CookingPot pot;

        void Awake() => pot = GetComponentInParent<CookingPot>();

        void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (!pot || !rb || !rb.TryGetComponent(out Ingredient ingredient) || ingredient.Carryable.IsHeld) return;
            if (!pot.TryAddIngredient(ingredient))
                rb.linearVelocity = Vector3.up * rejectBounce + Random.insideUnitSphere;
        }
    }
}
