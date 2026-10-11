using RobotRescue.Bots;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>Trigger that moves the bot's respawn point here.</summary>
    [RequireComponent(typeof(Collider))]
    public class Checkpoint : MonoBehaviour
    {
        [Tooltip("Optional DISPATCH cue when first reached.")]
        public string cue;
        bool reached;

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (!other.attachedRigidbody || !other.attachedRigidbody.TryGetComponent(out BotController bot)) return;
            bot.RespawnPoint = transform.position;
            if (!reached && !string.IsNullOrEmpty(cue)) EventBus<NarrationCue>.Raise(new NarrationCue(cue));
            reached = true;
        }
    }
}
