using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>Bring (or throw) a cooked pot here to serve it.</summary>
    [RequireComponent(typeof(Collider))]
    public class ServingHatch : MonoBehaviour
    {
        [Tooltip("Shown once the dish is served (a plate of pasta).")]
        public GameObject servedVisual;
        float lastRejectTime = -10f;

        void Start() { if (servedVisual) servedVisual.SetActive(false); }

        void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (!rb || !rb.TryGetComponent(out CookingPot pot)) return;

            if (!pot.IsCooked)
            {
                if (Time.time - lastRejectTime > 4f)
                    EventBus<NarrationCue>.Raise(new NarrationCue("served_raw"));
                lastRejectTime = Time.time;
                return;
            }

            pot.gameObject.SetActive(false);
            if (servedVisual) servedVisual.SetActive(true);
            EventBus<NarrationCue>.Raise(new NarrationCue("served"));
            EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted("serve"));
        }
    }
}
