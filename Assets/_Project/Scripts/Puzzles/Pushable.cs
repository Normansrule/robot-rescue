using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// Heavy Sokoban-style object: too heavy to carry, slides when PIP walks into it.
    /// Rotation is frozen so crates stay square to the grid.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Pushable : MonoBehaviour
    {
        public float maxPushSpeed = 2.5f;
        [Tooltip("Snap pushes to the nearest world X/Z axis, like grid puzzles.")]
        public bool axisAligned = true;

        Rigidbody body;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.constraints = RigidbodyConstraints.FreezeRotation;
        }

        public void Push(Vector3 force)
        {
            force.y = 0f;
            if (axisAligned)
                force = Mathf.Abs(force.x) > Mathf.Abs(force.z)
                    ? new Vector3(force.magnitude * Mathf.Sign(force.x), 0f, 0f)
                    : new Vector3(0f, 0f, force.magnitude * Mathf.Sign(force.z));

            Vector3 dir = force.normalized;
            if (Vector3.Dot(body.linearVelocity, dir) < maxPushSpeed)
                body.AddForce(force, ForceMode.Force);
        }
    }
}
