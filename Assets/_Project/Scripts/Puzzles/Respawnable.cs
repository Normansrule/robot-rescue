using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>Remembers where an object started so a KillZone can put it back.</summary>
    public class Respawnable : MonoBehaviour
    {
        Vector3 startPosition;
        Quaternion startRotation;

        void Awake()
        {
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        public void Respawn()
        {
            var carryable = GetComponent<Carryable>();
            if (carryable && carryable.Holder) carryable.Holder.Drop();
            var rb = GetComponent<Rigidbody>();
            if (rb)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.position = startPosition;
                rb.rotation = startRotation;
            }
            transform.SetPositionAndRotation(startPosition, startRotation);
        }
    }
}
