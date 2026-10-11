using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>Receives ingredients dropped or thrown into it (through a PotMouth trigger).</summary>
    public interface IIngredientReceiver
    {
        /// <summary>Returns false if it refuses the ingredient (it bounces back out).</summary>
        bool TryAddIngredient(Ingredient ingredient);
    }

    /// <summary>A container that ends up holding a finished dish: pots and mixing bowls.</summary>
    public interface IDish
    {
        /// <summary>The finished recipe, or null if it isn't ready (or is ruined).</summary>
        RecipeDefinition FinishedRecipe { get; }
        bool IsRuined { get; }
        bool IsEmpty { get; }
        /// <summary>Tip everything out and start again.</summary>
        void Empty();
        GameObject gameObject { get; }
    }

    /// <summary>Shared helpers for matching contents against recipes.</summary>
    public static class RecipeMatch
    {
        public static bool UsedByAny(RecipeDefinition[] recipes, IngredientDefinition ingredient)
        {
            foreach (var r in recipes)
                if (r && r.ingredients != null && System.Array.IndexOf(r.ingredients, ingredient) >= 0) return true;
            return false;
        }

        /// <summary>The recipe whose ingredient set exactly matches the contents, or null.</summary>
        public static RecipeDefinition Exact(RecipeDefinition[] recipes, System.Collections.Generic.List<IngredientDefinition> contents)
        {
            foreach (var r in recipes)
            {
                if (!r || r.ingredients == null || r.ingredients.Length != contents.Count) continue;
                bool all = true;
                foreach (var i in r.ingredients) if (!contents.Contains(i)) { all = false; break; }
                if (all) return r;
            }
            return null;
        }

        /// <summary>True if the contents could still become one of the recipes.</summary>
        public static bool StillPossible(RecipeDefinition[] recipes, System.Collections.Generic.List<IngredientDefinition> contents, IngredientDefinition adding)
        {
            foreach (var r in recipes)
            {
                if (!r || r.ingredients == null || System.Array.IndexOf(r.ingredients, adding) < 0) continue;
                bool fits = true;
                foreach (var c in contents) if (System.Array.IndexOf(r.ingredients, c) < 0) { fits = false; break; }
                if (fits) return true;
            }
            return false;
        }
    }
}
