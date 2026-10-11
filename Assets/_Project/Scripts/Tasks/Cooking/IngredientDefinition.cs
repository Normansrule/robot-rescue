using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    [CreateAssetMenu(menuName = "Robot Rescue/Cooking/Ingredient", fileName = "Ingredient_")]
    public class IngredientDefinition : ScriptableObject
    {
        [Tooltip("Used in task step ids: adding it completes step \"add_<id>\".")]
        public string id = "ingredient";
        public string displayName = "Ingredient";
        public Color rawColor = Color.yellow;
        public Color cookedColor = new Color(1f, 0.9f, 0.6f);
    }
}
