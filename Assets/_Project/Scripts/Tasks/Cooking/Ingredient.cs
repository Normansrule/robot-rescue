using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>A carryable food item. Drop or throw it into a pot or bowl.</summary>
    [RequireComponent(typeof(Carryable))]
    public class Ingredient : MonoBehaviour
    {
        public IngredientDefinition definition;
        public Carryable Carryable { get; private set; }

        void Awake() => Carryable = GetComponent<Carryable>();

        /// <summary>Used up by a pot or bowl. Dispenser-made copies are destroyed; hand-placed ones just hide.</summary>
        public void Consume()
        {
            if (TryGetComponent(out DispensedMarker _)) Destroy(gameObject);
            else gameObject.SetActive(false);
        }
    }

    /// <summary>Tags ingredients spawned by an <see cref="IngredientDispenser"/>.</summary>
    public class DispensedMarker : MonoBehaviour
    {
        public IngredientDispenser source;
    }
}
