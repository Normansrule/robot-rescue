using RobotRescue.Bots;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>Trigger volume (pits, water) that respawns bots at their checkpoint and objects at their start.</summary>
    [RequireComponent(typeof(Collider))]
    public class KillZone : MonoBehaviour
    {
        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (!rb) return;
            if (rb.TryGetComponent(out BotController bot)) bot.Respawn();
            else if (rb.TryGetComponent(out Respawnable respawnable)) respawnable.Respawn();
        }
    }
}
