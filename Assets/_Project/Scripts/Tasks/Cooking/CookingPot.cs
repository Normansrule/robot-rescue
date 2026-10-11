using System.Collections.Generic;
using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>
    /// Carryable pot. Fill it under a running tap, drop/throw ingredients in its mouth,
    /// and keep it on a powered stove until the recipe is cooked.
    /// </summary>
    [RequireComponent(typeof(Carryable))]
    public class CookingPot : MonoBehaviour
    {
        public RecipeDefinition recipe;
        [Tooltip("Water surface, hidden until filled.")]
        public Renderer waterVisual;
        [Tooltip("Food inside, hidden until an ingredient goes in.")]
        public Renderer contentsVisual;
        public Color waterColor = new Color(0.45f, 0.75f, 1f);
        public Color boilingColor = new Color(0.8f, 0.92f, 1f);

        public bool HasWater { get; private set; }
        public bool IsCooked { get; private set; }
        public float CookProgress01 => recipe && recipe.cookTime > 0f ? Mathf.Clamp01(cookTime / recipe.cookTime) : 0f;
        public bool IsBoiling => HasWater && stove && stove.IsOn;
        public bool HasAllIngredients
        {
            get
            {
                if (!recipe) return false;
                foreach (var i in recipe.ingredients) if (!contents.Contains(i)) return false;
                return true;
            }
        }

        readonly List<IngredientDefinition> contents = new List<IngredientDefinition>();
        Stove stove;
        float cookTime;
        bool boilCueSent;
        Vector3 waterRest;

        void Awake()
        {
            if (waterVisual) { waterVisual.enabled = false; waterRest = waterVisual.transform.localPosition; }
            if (contentsVisual) contentsVisual.enabled = false;
        }

        public void SetStove(Stove s) => stove = s;
        public void ClearStove(Stove s) { if (stove == s) stove = null; }

        public void AddWater()
        {
            if (HasWater || IsCooked || (recipe && !recipe.needsWater)) return;
            HasWater = true;
            if (waterVisual) { waterVisual.enabled = true; waterVisual.material.color = waterColor; }
            EventBus<NarrationCue>.Raise(new NarrationCue("water_filled"));
            EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted("fill_water"));
        }

        /// <summary>Called by the PotMouth trigger. Returns false if the pot refuses it.</summary>
        public bool TryAddIngredient(Ingredient ingredient)
        {
            if (IsCooked || !ingredient.definition) return false;
            if (!recipe || System.Array.IndexOf(recipe.ingredients, ingredient.definition) < 0 || contents.Contains(ingredient.definition))
            {
                EventBus<NarrationCue>.Raise(new NarrationCue("wrong_ingredient"));
                return false;
            }

            contents.Add(ingredient.definition);
            if (contentsVisual)
            {
                contentsVisual.enabled = true;
                contentsVisual.material.color = ingredient.definition.rawColor;
            }
            ingredient.gameObject.SetActive(false);
            EventBus<NarrationCue>.Raise(new NarrationCue("ingredient_added"));
            EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted("add_" + ingredient.definition.id));
            return true;
        }

        void Update()
        {
            if (!IsBoiling) return;

            if (!boilCueSent) { boilCueSent = true; EventBus<NarrationCue>.Raise(new NarrationCue("boiling")); }

            if (waterVisual)
            {
                waterVisual.material.color = boilingColor;
                waterVisual.transform.localPosition = waterRest + Vector3.up * (Mathf.Sin(Time.time * 25f) * 0.01f);
            }

            if (IsCooked || !HasAllIngredients) return;
            cookTime += Time.deltaTime;
            if (cookTime < recipe.cookTime) return;

            IsCooked = true;
            if (contentsVisual && recipe.ingredients.Length > 0) contentsVisual.material.color = recipe.ingredients[0].cookedColor;
            EventBus<NarrationCue>.Raise(new NarrationCue("cooked"));
            EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted("cook"));
        }
    }
}
