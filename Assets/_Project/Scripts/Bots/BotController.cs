using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// Rigidbody bot movement: camera-relative run, buffered/coyote jump, swing pumping while
    /// tethered, Sokoban-style pushing, and squash-and-stretch on the visual.
    /// The root sits at the bot's feet.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(BotInput))]
    public class BotController : MonoBehaviour
    {
        [Header("References")]
        public BotCamera cameraRig;
        [Tooltip("Child holding the meshes; scaled for squash and stretch.")]
        public Transform visual;

        [Header("Movement")]
        public float moveSpeed = 5.5f;
        public float groundAcceleration = 60f;
        public float airAcceleration = 14f;
        public float swingPumpAcceleration = 10f;
        public float turnSpeed = 720f;

        [Header("Jumping")]
        public float jumpHeight = 1.3f;
        public float coyoteTime = 0.12f;
        public float jumpBufferTime = 0.12f;
        public float fallGravityMultiplier = 1.8f;

        [Header("Ground")]
        public float groundCheckDistance = 0.15f;
        public float maxSlopeAngle = 50f;

        [Header("Pushing")]
        public float pushForce = 30f;

        public Rigidbody Body { get; private set; }
        public bool IsGrounded { get; private set; }
        /// <summary>What PIP is standing on (null in the air). Lets the Grabber ignore the drone underfoot.</summary>
        public Collider GroundCollider { get; private set; }
        public Vector3 MoveDirection { get; private set; }
        public Vector3 RespawnPoint { get; set; }

        BotInput input;
        CapsuleCollider capsule;
        TetherCable tether;
        Grabber grabber;
        float lastGroundedTime = -10f;
        float lastJumpPressedTime = -10f;
        float squash, squashVelocity;

        const int IgnoreRaycastLayer = 2;
        static int GroundMask => ~(1 << IgnoreRaycastLayer);
        float JumpVelocity => Mathf.Sqrt(2f * -Physics.gravity.y * jumpHeight);

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.constraints = RigidbodyConstraints.FreezeRotation;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            capsule = GetComponent<CapsuleCollider>();
            input = GetComponent<BotInput>();
            tether = GetComponent<TetherCable>();
            grabber = GetComponent<Grabber>();
            RespawnPoint = transform.position;
        }

        void Update()
        {
            Vector2 mv = input.Move;
            Vector3 fwd = cameraRig ? cameraRig.PlanarForward : Vector3.forward;
            Vector3 right = cameraRig ? cameraRig.PlanarRight : Vector3.right;
            MoveDirection = Vector3.ClampMagnitude(fwd * mv.y + right * mv.x, 1f);

            if (input.JumpPressed) lastJumpPressedTime = Time.time;

            UpdateSquash();
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            bool wasGrounded = IsGrounded;
            float landingSpeed = -Body.linearVelocity.y;
            GroundCheck();
            if (IsGrounded && !wasGrounded && landingSpeed > 3f)
                Squash(-Mathf.Clamp(landingSpeed * 0.04f, 0.1f, 0.35f));

            bool swinging = tether && tether.IsSwinging && !IsGrounded;
            Vector3 v = Body.linearVelocity;
            Vector3 horiz = new Vector3(v.x, 0f, v.z);

            if (IsGrounded)
            {
                Vector3 surface = GroundCollider && GroundCollider.TryGetComponent(out ISurfaceVelocity s) ? s.SurfaceVelocity : Vector3.zero;
                horiz = Vector3.MoveTowards(horiz, MoveDirection * moveSpeed + surface, groundAcceleration * dt);
            }
            else if (swinging)
            {
                horiz += MoveDirection * (swingPumpAcceleration * dt);
            }
            else
            {
                // Air control can steer and speed up to moveSpeed, but never bleeds off momentum
                // gained from a swing or a throw.
                float cap = Mathf.Max(moveSpeed, horiz.magnitude);
                horiz = Vector3.ClampMagnitude(horiz + MoveDirection * (airAcceleration * dt), cap);
            }
            v.x = horiz.x;
            v.z = horiz.z;

            if (Time.time - lastJumpPressedTime <= jumpBufferTime)
            {
                if (swinging)
                {
                    tether.Detach();
                    v.y = Mathf.Max(v.y, 0f) + JumpVelocity * 0.75f;
                    lastJumpPressedTime = -10f;
                    Squash(0.25f);
                }
                else if (Time.time - lastGroundedTime <= coyoteTime)
                {
                    v.y = JumpVelocity;
                    lastGroundedTime = -10f;
                    lastJumpPressedTime = -10f;
                    IsGrounded = false;
                    Squash(0.25f);
                }
            }

            if (!IsGrounded && !swinging && v.y < 0f)
                v += Physics.gravity * ((fallGravityMultiplier - 1f) * dt);

            Body.linearVelocity = v;

            if (MoveDirection.sqrMagnitude > 0.01f)
            {
                Quaternion look = Quaternion.LookRotation(new Vector3(MoveDirection.x, 0f, MoveDirection.z));
                Body.MoveRotation(Quaternion.RotateTowards(Body.rotation, look, turnSpeed * dt));
            }
        }

        void GroundCheck()
        {
            float r = capsule.radius * 0.9f;
            Vector3 origin = Body.position + Vector3.up * (r + 0.05f);
            IsGrounded = false;
            GroundCollider = null;
            if (Physics.SphereCast(origin, r, Vector3.down, out RaycastHit hit, groundCheckDistance + 0.05f, GroundMask, QueryTriggerInteraction.Ignore)
                && Vector3.Angle(hit.normal, Vector3.up) <= maxSlopeAngle
                && Body.linearVelocity.y <= 0.5f)
            {
                IsGrounded = true;
                GroundCollider = hit.collider;
                lastGroundedTime = Time.time;
            }
        }

        void OnCollisionStay(Collision collision)
        {
            if (!IsGrounded || MoveDirection.sqrMagnitude < 0.1f || collision.rigidbody == null) return;
            var pushable = collision.rigidbody.GetComponent<Pushable>();
            if (pushable == null) return;
            Vector3 normal = collision.GetContact(0).normal;
            // Contact normal points out of the crate toward us; push only when walking into it.
            if (Vector3.Dot(MoveDirection.normalized, -normal) < 0.5f) return;
            pushable.Push(MoveDirection * pushForce);
        }

        public void Respawn()
        {
            if (tether) tether.Detach();
            if (grabber) grabber.Drop();
            Body.position = RespawnPoint;
            transform.position = RespawnPoint;
            Body.linearVelocity = Vector3.zero;
            EventBus<NarrationCue>.Raise(new NarrationCue("fell"));
        }

        // ---- Squash & stretch: a critically-ish damped spring on one scalar ----

        public void Squash(float amount) => squashVelocity += amount * 25f;

        void UpdateSquash()
        {
            if (!visual) return;
            float dt = Time.deltaTime;
            squashVelocity += (-squash * 300f - squashVelocity * 18f) * dt;
            squash += squashVelocity * dt;
            float s = Mathf.Clamp(squash, -0.4f, 0.4f);
            visual.localScale = new Vector3(1f - s * 0.5f, 1f + s, 1f - s * 0.5f);
        }
    }
}
