using System.Collections.Generic;
using RobotRescue.Bots;
using RobotRescue.Core;
using RobotRescue.Puzzles;
using RobotRescue.Tools;
using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>
    /// Carryable mixing bowl. Drop ingredients in, set it down, then hold Use with the Whisk equipped until the
    /// sauce comes together. Then carry it to the hatch.
    /// </summary>
    [RequireComponent(typeof(Carryable))]
    public class MixingBowl : MonoBehaviour, IIngredientReceiver, IDish, IToolUsable, IProgressIndicator
    {
        public RecipeDefinition[] recipes;
        public Renderer contentsVisual;
        [Tooltip("Spins while being whisked.")]
        public Transform swirl;

        public bool IsDone => done != null;
        public bool IsRuined => false;
        public bool IsEmpty => contents.Count == 0;
        public RecipeDefinition FinishedRecipe => done;

        public ToolKind RequiredTool => ToolKind.Whisk;
        public bool CanUse => target != null && done == null && !carryable.IsHeld;

        public bool ShowProgress => isActiveAndEnabled && target != null && done == null && whiskTime > 0f;
        public Vector3 IndicatorPosition => transform.position + Vector3.up * 0.7f;
        public float Progress01 => target ? Mathf.Clamp01(whiskTime / target.cookTime) : 0f;
        public Color ProgressColor => new Color(0.55f, 0.85f, 1f);

        readonly List<IngredientDefinition> contents = new List<IngredientDefinition>();
        RecipeDefinition target, done;
        Carryable carryable;
        float whiskTime;
        bool whiskCueSent;

        void Awake()
        {
            carryable = GetComponent<Carryable>();
            RefreshVisuals();
        }

        public bool TryAddIngredient(Ingredient ingredient)
        {
            var def = ingredient.definition;
            if (done != null || !def || contents.Contains(def) || !RecipeMatch.StillPossible(recipes, contents, def))
            {
                EventBus<NarrationCue>.Raise(new NarrationCue("wrong_ingredient"));
                return false;
            }
            contents.Add(def);
            target = RecipeMatch.Exact(recipes, contents);
            whiskTime = 0f;
            ingredient.Consume();
            RefreshVisuals();
            EventBus<NarrationCue>.Raise(new NarrationCue(target ? "bowl_ready_to_whisk" : "ingredient_added"));
            return true;
        }

        public void UseTool(ToolArm arm, float deltaTime)
        {
            if (!CanUse) return;
            if (!whiskCueSent) { whiskCueSent = true; EventBus<NarrationCue>.Raise(new NarrationCue("whisking")); }
            whiskTime += deltaTime;
            if (swirl) swirl.Rotate(0f, 720f * deltaTime, 0f, Space.Self);
            if (whiskTime < target.cookTime) return;

            done = target;
            RefreshVisuals();
            EventBus<NarrationCue>.Raise(new NarrationCue("whisked"));
            EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted("whisk"));
        }

        public void Empty()
        {
            contents.Clear();
            target = done = null;
            whiskTime = 0f;
            RefreshVisuals();
        }

        void RefreshVisuals()
        {
            if (!contentsVisual) return;
            contentsVisual.enabled = contents.Count > 0;
            if (contents.Count > 0)
                contentsVisual.material.color = done ? done.dishColor : contents[contents.Count - 1].rawColor;
        }
    }
}
