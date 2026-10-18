using System;
using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// Rigidbody bot movement: camera-relative run, buffered/coyote jump, swing pumping while tethered,
    /// Sokoban-style pushing, PEAK-style climbing on <see cref="ClimbSurface"/>s with stamina, and squash-and-stretch
    /// on the visual. The root sits at the bot's feet. Battery and broken parts scale speed and jump.
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

        [Header("Climbing")]
        public float climbSpeed = 2.2f;
        [Tooltip("Stamina per second while hanging still / while moving on the wall.")]
        public float staminaHang = 0.05f, staminaMove = 0.14f;
        public float staminaLeapCost = 0.2f;
        public float staminaRegen = 0.5f;

        public Rigidbody Body { get; private set; }
        public bool IsGrounded { get; private set; }
        /// <summary>What PIP is standing on (null in the air). Lets the Grabber ignore the drone underfoot.</summary>
        public Collider GroundCollider { get; private set; }
        public Vector3 MoveDirection { get; private set; }
        public Vector3 RespawnPoint { get; set; }
        public bool IsClimbing { get; private set; }
        public float Stamina01 { get; private set; } = 1f;
        /// <summary>0..1 of top speed, for animation.</summary>
        public float Speed01 => Mathf.Clamp01(new Vector3(Body.linearVelocity.x, 0f, Body.linearVelocity.z).magnitude / moveSpeed);
        /// <summary>Flat battery: PIP can't move until towed.</summary>
        public bool PoweredDown { get; set; }

        /// <summary>Landing speed (m/s) whenever PIP touches down after a fall.</summary>
        public event Action<float> Landed;
        public event Action Jumped;
        /// <summary>Relative speed of a heavy object that hit PIP.</summary>
        public event Action<float> HitByHeavy;

        BotInput input;
        CapsuleCollider capsule;
        TetherCable tether;
        Grabber grabber;
        BotBattery battery;
        BotParts parts;
        float lastGroundedTime = -10f;
        float lastJumpPressedTime = -10f;
        float squash, squashVelocity;
        Vector3 climbNormal;
        float climbBlockedUntil;
        float climbStaminaScale = 1f;
        static bool announcedGrip;

        const int IgnoreRaycastLayer = 2;
        static int GroundMask => ~(1 << IgnoreRaycastLayer);
        float SpeedScale => (battery ? battery.SpeedScale : 1f) * (parts ? parts.SpeedScale : 1f);
        float JumpScale => parts ? parts.JumpScale : 1f;
        float JumpVelocity => Mathf.Sqrt(2f * -Physics.gravity.y * jumpHeight) * JumpScale;

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
            battery = GetComponent<BotBattery>();
            parts = GetComponent<BotParts>();
            RespawnPoint = transform.position;
        }

        void Update()
        {
            Vector2 mv = PoweredDown ? Vector2.zero : input.Move;
            Vector3 fwd = cameraRig ? cameraRig.PlanarForward : Vector3.forward;
            Vector3 right = cameraRig ? cameraRig.PlanarRight : Vector3.right;
            MoveDirection = Vector3.ClampMagnitude(fwd * mv.y + right * mv.x, 1f);

            if (!PoweredDown && input.JumpPressed) lastJumpPressedTime = Time.time;
            if (!PoweredDown && input.EmotePressed && !IsClimbing) EventBus<BotEmoted>.Raise(new BotEmoted(transform.position));

            UpdateSquash();
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            bool wasGrounded = IsGrounded;
            float landingSpeed = -Body.linearVelocity.y;
            GroundCheck();
            if (IsGrounded && !wasGrounded)
            {
                if (landingSpeed > 3f) Squash(-Mathf.Clamp(landingSpeed * 0.04f, 0.1f, 0.35f));
                if (landingSpeed > 1f) Landed?.Invoke(landingSpeed);
            }

            if (IsClimbing) { ClimbStep(dt); return; }
            if (IsGrounded) Stamina01 = Mathf.MoveTowards(Stamina01, 1f, staminaRegen * dt);
            if (TryStartClimb()) return;

            bool swinging = tether && tether.IsSwinging && !IsGrounded;
            Vector3 v = Body.linearVelocity;
            Vector3 horiz = new Vector3(v.x, 0f, v.z);
            float speed = moveSpeed * SpeedScale;

            if (IsGrounded)
            {
                Vector3 surface = GroundCollider && GroundCollider.TryGetComponent(out ISurfaceVelocity s) ? s.SurfaceVelocity : Vector3.zero;
                horiz = Vector3.MoveTowards(horiz, MoveDirection * speed + surface, groundAcceleration * dt);
            }
            else if (swinging)
            {
                horiz += MoveDirection * (swingPumpAcceleration * dt);
            }
            else
            {
                // Air control can steer and speed up to moveSpeed, but never bleeds off momentum
                // gained from a swing or a throw.
                float cap = Mathf.Max(speed, horiz.magnitude);
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
                    Jumped?.Invoke();
                }
                else if (Time.time - lastGroundedTime <= coyoteTime)
                {
                    v.y = JumpVelocity;
                    lastGroundedTime = -10f;
                    lastJumpPressedTime = -10f;
                    IsGrounded = false;
                    Squash(0.25f);
                    Jumped?.Invoke();
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

        // ------------------------------------------------------------------ climbing

        bool ProbeWall(Vector3 dir, float height, out RaycastHit hit)
        {
            Vector3 origin = Body.position + Vector3.up * height;
            if (Physics.SphereCast(origin, 0.22f, dir, out hit, 0.65f, GroundMask, QueryTriggerInteraction.Ignore)
                && Mathf.Abs(hit.normal.y) < 0.6f)
            {
                var surface = hit.collider.GetComponentInParent<ClimbSurface>();
                if (surface)
                {
                    climbStaminaScale = surface.staminaMultiplier;
                    return true;
                }
            }
            return false;
        }

        bool TryStartClimb()
        {
            if (PoweredDown || Time.time < climbBlockedUntil || Stamina01 < 0.1f) return false;
            if (battery && battery.OnReserve) return false;   // reserve power won't lift PIP up a wall
            if (MoveDirection.sqrMagnitude < 0.2f || (grabber && grabber.Held) || (tether && tether.IsAttached)) return false;
            bool wantsGrip = !IsGrounded || input.JumpHeld;
            if (!wantsGrip) return false;
            Vector3 dir = MoveDirection.normalized;
            if (!ProbeWall(dir, 0.6f, out RaycastHit hit) || Vector3.Dot(dir, -hit.normal) < 0.5f) return false;

            IsClimbing = true;
            if (!announcedGrip) { announcedGrip = true; EventBus<NarrationCue>.Raise(new NarrationCue("grip")); }
            climbNormal = new Vector3(hit.normal.x, 0f, hit.normal.z).normalized;
            Body.useGravity = false;
            Body.linearVelocity = Vector3.zero;
            lastJumpPressedTime = -10f;
            Squash(-0.1f);
            return true;
        }

        void ClimbStep(float dt)
        {
            Vector2 mv = PoweredDown ? Vector2.zero : input.Move;
            Vector3 into = -climbNormal;
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(Vector3.up, into).normalized;

            bool mid = ProbeWall(into, 0.6f, out RaycastHit hit);
            bool high = ProbeWall(into, 1.15f, out _);
            if (mid) climbNormal = Vector3.Slerp(climbNormal, new Vector3(hit.normal.x, 0f, hit.normal.z).normalized, 0.3f);

            // Ran out of wall above: mantle over the top.
            if (!high && mv.y > 0.1f)
            {
                EndClimb(0.4f);
                Body.linearVelocity = Vector3.up * 5.2f + into * 2.4f;
                Squash(0.2f);
                Jumped?.Invoke();
                return;
            }
            if (!mid && !high) { EndClimb(0.3f); return; }

            float drain = (mv.sqrMagnitude > 0.01f ? staminaMove : staminaHang) * climbStaminaScale;
            Stamina01 = Mathf.Max(0f, Stamina01 - drain * dt);

            if (Time.time - lastJumpPressedTime <= jumpBufferTime && Stamina01 >= staminaLeapCost)
            {
                Stamina01 -= staminaLeapCost;
                lastJumpPressedTime = -10f;
                EndClimb(0.25f);
                Body.linearVelocity = Vector3.up * JumpVelocity * 1.15f + climbNormal * 1.2f;
                Squash(0.25f);
                Jumped?.Invoke();
                return;
            }

            // Let go: out of stamina, pulling away from the wall, or stepping down onto the ground.
            if (Stamina01 <= 0f || (IsGrounded && mv.y < -0.5f) || PoweredDown)
            {
                EndClimb(Stamina01 <= 0f ? 1f : 0.4f);
                Body.linearVelocity = climbNormal * 1.5f;
                return;
            }

            Vector3 vel = (up * mv.y + right * mv.x) * (climbSpeed * SpeedScale) + into * 1.5f;
            Body.linearVelocity = vel;
            Body.MoveRotation(Quaternion.RotateTowards(Body.rotation, Quaternion.LookRotation(into), turnSpeed * dt));
        }

        void EndClimb(float blockSeconds)
        {
            IsClimbing = false;
            Body.useGravity = true;
            climbBlockedUntil = Time.time + blockSeconds;
        }

        // ------------------------------------------------------------------ contacts

        void OnCollisionEnter(Collision collision)
        {
            var rb = collision.rigidbody;
            if (rb && !rb.isKinematic && rb.mass >= 1.4f && !(grabber && grabber.Held && grabber.Held.Body == rb))
            {
                float speed = collision.relativeVelocity.magnitude;
                if (speed > 6f) HitByHeavy?.Invoke(speed);
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

        /// <summary>
        /// A jolt (the Hum shaking the ground): costs grip while climbing, and shakes PIP loose if it runs out.
        /// On the ground it's just a little hop.
        /// </summary>
        public void Jolt(float staminaLoss)
        {
            if (IsClimbing)
            {
                Stamina01 = Mathf.Max(0f, Stamina01 - staminaLoss);
                if (Stamina01 <= 0f)
                {
                    EndClimb(1f);
                    Body.linearVelocity = climbNormal * 2f;
                }
            }
            else if (IsGrounded) Body.linearVelocity += Vector3.up * 2f;
            Squash(0.2f);
        }

        public void Respawn() => Respawn(true);

        public void Respawn(bool announce)
        {
            if (IsClimbing) EndClimb(0.2f);
            if (tether) tether.Detach();
            if (grabber) grabber.Drop();
            Body.position = RespawnPoint;
            transform.position = RespawnPoint;
            Body.linearVelocity = Vector3.zero;
            if (announce) EventBus<NarrationCue>.Raise(new NarrationCue("fell"));
        }

        /// <summary>Puts PIP somewhere new (fast travel, area start) facing <paramref name="yaw"/>.</summary>
        public void Teleport(Vector3 position, float yaw)
        {
            if (IsClimbing) EndClimb(0.2f);
            if (tether) tether.Detach();
            if (grabber) grabber.Drop();
            Body.position = position;
            Body.rotation = Quaternion.Euler(0f, yaw, 0f);
            transform.SetPositionAndRotation(position, Body.rotation);
            Body.linearVelocity = Vector3.zero;
            RespawnPoint = position;
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
