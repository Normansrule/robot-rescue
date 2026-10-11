using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>A carryable food item. Drop or throw it into a pot.</summary>
    [RequireComponent(typeof(Carryable))]
    public class Ingredient : MonoBehaviour
    {
        public IngredientDefinition definition;
        public Carryable Carryable { get; private set; }

        void Awake() => Carryable = GetComponent<Carryable>();
    }
}
