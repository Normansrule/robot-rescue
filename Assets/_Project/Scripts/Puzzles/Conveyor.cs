using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// Belt that moves loose objects (and PIP, via <see cref="ISurfaceVelocity"/>) along its forward axis.
    /// Runs while powered, or always if <see cref="startsOn"/> and nothing switches it off.
    /// Put this on the belt's collider object; stripes are separate visuals that scroll so you can see it move.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Conveyor : MonoBehaviour, IPowerable, ISurfaceVelocity
    {
        public float speed = 2.5f;
        public bool startsOn;
        [Tooltip("Flip the belt direction.")]
        public bool reversed;
        [Tooltip("How quickly objects match the belt speed.")]
        public float grip = 20f;

        [Header("Visuals")]
        public Transform[] stripes;
        [Tooltip("Belt length along its forward axis, for wrapping stripes.")]
        public float length = 4f;

        public bool IsRunning { get; private set; }
        public Vector3 SurfaceVelocity => IsRunning ? transform.forward * (reversed ? -speed : speed) : Vector3.zero;

        float[] stripeOffsets;

        void Start()
        {
            IsRunning = startsOn;
            if (stripes == null) return;
            stripeOffsets = new float[stripes.Length];
            for (int i = 0; i < stripes.Length; i++)
                stripeOffsets[i] = Vector3.Dot(stripes[i].position - transform.position, transform.forward);
        }

        public void SetPowered(bool on) => IsRunning = on;

        void OnCollisionStay(Collision collision)
        {
            var rb = collision.rigidbody;
            // Bots read SurfaceVelocity themselves so their own movement stays in charge.
            if (!rb || rb.isKinematic || rb.TryGetComponent<Bots.BotController>(out _)) return;
            Vector3 v = rb.linearVelocity;
            Vector3 target = SurfaceVelocity;
            Vector3 horiz = Vector3.MoveTowards(new Vector3(v.x, 0f, v.z), target, grip * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(horiz.x, v.y, horiz.z);
        }

        void Update()
        {
            if (!IsRunning || stripeOffsets == null) return;
            float step = (reversed ? -speed : speed) * Time.deltaTime;
            float half = length * 0.5f;
            for (int i = 0; i < stripes.Length; i++)
            {
                float o = stripeOffsets[i] + step;
                if (o > half) o -= length;
                else if (o < -half) o += length;
                stripeOffsets[i] = o;
                Vector3 p = stripes[i].position;
                Vector3 along = transform.forward * Vector3.Dot(p - transform.position, transform.forward);
                stripes[i].position = p - along + transform.forward * o;
            }
        }
    }
}
