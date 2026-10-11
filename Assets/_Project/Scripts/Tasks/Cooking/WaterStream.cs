using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>Tap stream: a trigger that fills pots passing through it while the tap runs.</summary>
    [RequireComponent(typeof(Collider))]
    public class WaterStream : MonoBehaviour, IPowerable
    {
        public GameObject streamVisual;
        public bool startsOn;

        public bool IsRunning { get; private set; }

        void Start() => SetPowered(startsOn);

        public void SetPowered(bool on)
        {
            if (on && !IsRunning) EventBus<NarrationCue>.Raise(new NarrationCue("tap_on"));
            IsRunning = on;
            if (streamVisual) streamVisual.SetActive(on);
        }

        void OnTriggerStay(Collider other)
        {
            if (!IsRunning) return;
            var rb = other.attachedRigidbody;
            if (rb && rb.TryGetComponent(out CookingPot pot)) pot.AddWater();
        }
    }
}
