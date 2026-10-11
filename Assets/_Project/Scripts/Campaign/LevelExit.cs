using RobotRescue.Bots;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Campaign
{
    /// <summary>End-of-chamber trigger.</summary>
    [RequireComponent(typeof(Collider))]
    public class LevelExit : MonoBehaviour
    {
        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (other.attachedRigidbody && other.attachedRigidbody.TryGetComponent<BotController>(out _))
                EventBus<LevelCompleted>.Raise(new LevelCompleted());
        }
    }
}
