using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    public enum CookMethod
    {
        /// <summary>In a pot of water on a powered stove.</summary>
        Boil,
        /// <summary>In a mixing bowl, worked with the Whisk tool.</summary>
        Whisk,
    }

    /// <summary>What goes in and how it's cooked. Order tickets in Pasta Panic ask for these.</summary>
    [CreateAssetMenu(menuName = "Robot Rescue/Cooking/Recipe", fileName = "Recipe_")]
    public class RecipeDefinition : ScriptableObject
    {
        public string dishName = "Dish";
        public CookMethod method = CookMethod.Boil;
        [Tooltip("Boil only: needs a pot of water.")]
        public bool needsWater = true;
        public IngredientDefinition[] ingredients;
        [Tooltip("Seconds of boiling (or whisking) once everything is in.")]
        public float cookTime = 8f;
        public Color dishColor = new Color(1f, 0.92f, 0.65f);
    }
}
