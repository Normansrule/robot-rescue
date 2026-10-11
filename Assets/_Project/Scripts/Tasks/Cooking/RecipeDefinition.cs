using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>What goes in the pot and how long it cooks. Pasta Panic will chain several of these.</summary>
    [CreateAssetMenu(menuName = "Robot Rescue/Cooking/Recipe", fileName = "Recipe_")]
    public class RecipeDefinition : ScriptableObject
    {
        public string dishName = "Dish";
        public bool needsWater = true;
        public IngredientDefinition[] ingredients;
        [Tooltip("Seconds of boiling once everything is in.")]
        public float cookTime = 8f;
    }
}
