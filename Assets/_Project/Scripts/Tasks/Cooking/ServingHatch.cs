using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>
    /// Bring (or throw) a finished dish here. With an <see cref="OrderBoard"/>, it must match an open order and the
    /// empty pot/bowl goes back to its station. Without one, any cooked pot completes the "serve" step.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ServingHatch : MonoBehaviour
    {
        public OrderBoard orderBoard;
        [Tooltip("Shown once the dish is served (no order board only).")]
        public GameObject servedVisual;
        float lastRejectTime = -10f;

        void Start() { if (servedVisual) servedVisual.SetActive(false); }

        void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (!rb || !rb.TryGetComponent(out IDish dish) || dish.IsEmpty) return;

            var recipe = dish.FinishedRecipe;
            if (recipe == null)
            {
                Reject(dish.IsRuined ? "served_burnt" : "served_raw");
                return;
            }

            if (orderBoard)
            {
                if (!orderBoard.TryFulfill(recipe)) { Reject("order_wrong"); return; }
                dish.Empty();
                if (rb.TryGetComponent(out Respawnable home)) home.Respawn();
                return;
            }

            dish.gameObject.SetActive(false);
            if (servedVisual) servedVisual.SetActive(true);
            EventBus<NarrationCue>.Raise(new NarrationCue("served"));
            EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted("serve"));
        }

        void Reject(string cue)
        {
            if (Time.time - lastRejectTime > 4f) EventBus<NarrationCue>.Raise(new NarrationCue(cue));
            lastRejectTime = Time.time;
        }
    }
}
