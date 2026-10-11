using System.Collections.Generic;
using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>
    /// Carryable pot. Fill it under a running tap, drop/throw ingredients in its mouth,
    /// and keep it on a powered stove until a recipe is cooked. Leave it boiling too long and it burns.
    /// </summary>
    [RequireComponent(typeof(Carryable))]
    public class CookingPot : MonoBehaviour, IIngredientReceiver, IDish, IProgressIndicator
    {
        [Tooltip("Single-recipe pot (used when 'recipes' is empty).")]
        public RecipeDefinition recipe;
        [Tooltip("Any of these can be cooked in this pot; the contents decide which.")]
        public RecipeDefinition[] recipes;
        [Tooltip("Seconds a cooked dish can keep boiling before it burns. 0 = never burns.")]
        public float burnAfter;

        [Tooltip("Water surface, hidden until filled.")]
        public Renderer waterVisual;
        [Tooltip("Food inside, hidden until an ingredient goes in.")]
        public Renderer contentsVisual;
        public Color waterColor = new Color(0.45f, 0.75f, 1f);
        public Color boilingColor = new Color(0.8f, 0.92f, 1f);
        public Color burntColor = new Color(0.15f, 0.12f, 0.1f);

        public bool HasWater { get; private set; }
        public bool IsCooked => cooked != null && !IsRuined;
        public bool IsRuined { get; private set; }
        public bool IsEmpty => !HasWater && contents.Count == 0;
        public RecipeDefinition FinishedRecipe => IsRuined ? null : cooked;
        public float CookProgress01 => target && target.cookTime > 0f ? Mathf.Clamp01(cookTime / target.cookTime) : 0f;
        public bool IsBoiling => HasWater && stove && stove.IsOn;
        public bool HasAllIngredients => target != null;

        // HUD progress bar: cooking (yellow), then burning (red).
        public bool ShowProgress => isActiveAndEnabled && IsBoiling && (target != null && !IsRuined) && (!IsCooked || burnAfter > 0f);
        public Vector3 IndicatorPosition => transform.position + Vector3.up * 0.9f;
        public float Progress01 => IsCooked ? Mathf.Clamp01(burnTime / burnAfter) : CookProgress01;
        public Color ProgressColor => IsCooked ? new Color(1f, 0.3f, 0.2f) : new Color(1f, 0.75f, 0.2f);

        RecipeDefinition[] Candidates => recipes != null && recipes.Length > 0 ? recipes : new[] { recipe };

        readonly List<IngredientDefinition> contents = new List<IngredientDefinition>();
        RecipeDefinition target, cooked;
        Stove stove;
        float cookTime, burnTime;
        bool boilCueSent;
        Vector3 waterRest;

        void Awake()
        {
            if (waterVisual) waterRest = waterVisual.transform.localPosition;
            RefreshVisuals();
        }

        public void SetStove(Stove s) => stove = s;
        public void ClearStove(Stove s) { if (stove == s) stove = null; }

        public void AddWater()
        {
            if (HasWater || cooked != null) return;
            HasWater = true;
            RefreshVisuals();
            EventBus<NarrationCue>.Raise(new NarrationCue("water_filled"));
            EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted("fill_water"));
        }

        public bool TryAddIngredient(Ingredient ingredient)
        {
            var def = ingredient.definition;
            if (cooked != null || !def || contents.Contains(def) || !RecipeMatch.StillPossible(Candidates, contents, def))
            {
                EventBus<NarrationCue>.Raise(new NarrationCue("wrong_ingredient"));
                return false;
            }

            contents.Add(def);
            target = RecipeMatch.Exact(Candidates, contents);
            cookTime = 0f;
            ingredient.Consume();
            RefreshVisuals();
            EventBus<NarrationCue>.Raise(new NarrationCue("ingredient_added"));
            EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted("add_" + def.id));
            return true;
        }

        public void Empty()
        {
            contents.Clear();
            HasWater = false;
            IsRuined = false;
            target = cooked = null;
            cookTime = burnTime = 0f;
            boilCueSent = false;
            RefreshVisuals();
        }

        void Update()
        {
            if (!IsBoiling) return;

            if (!boilCueSent) { boilCueSent = true; EventBus<NarrationCue>.Raise(new NarrationCue("boiling")); }
            if (waterVisual)
                waterVisual.transform.localPosition = waterRest + Vector3.up * (Mathf.Sin(Time.time * 25f) * 0.01f);

            if (target == null || IsRuined) return;

            if (cooked == null)
            {
                cookTime += Time.deltaTime;
                if (cookTime < target.cookTime) return;
                cooked = target;
                RefreshVisuals();
                EventBus<NarrationCue>.Raise(new NarrationCue("cooked"));
                EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted("cook"));
            }
            else if (burnAfter > 0f)
            {
                burnTime += Time.deltaTime;
                if (burnTime < burnAfter) return;
                IsRuined = true;
                RefreshVisuals();
                EventBus<NarrationCue>.Raise(new NarrationCue("burnt"));
            }
        }

        void RefreshVisuals()
        {
            if (waterVisual)
            {
                waterVisual.enabled = HasWater;
                waterVisual.material.color = IsBoiling ? boilingColor : waterColor;
            }
            if (contentsVisual)
            {
                contentsVisual.enabled = contents.Count > 0;
                if (contents.Count > 0)
                    contentsVisual.material.color = IsRuined ? burntColor : cooked ? cooked.dishColor : contents[contents.Count - 1].rawColor;
            }
        }
    }
}
