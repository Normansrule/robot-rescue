using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// PIP's signature Tether Cable. Fire at a <see cref="TetherTarget"/> to:
    ///  - Power: plug into a socket. The bot is the battery, so it stays leashed by the cable length.
    ///  - Pull: hook a loose object and reel it in; the bot catches it when it arrives.
    ///  - Swing: hook an anchor and swing as a pendulum. Reel in/out changes rope length; jump releases.
    /// Rope is a distance constraint applied to the bot's Rigidbody (no joints), which keeps it stable.
    /// </summary>
    [RequireComponent(typeof(BotController))]
    public class TetherCable : MonoBehaviour
    {
        enum State { Idle, Firing, Attached }

        [Header("References")]
        public BotCamera cameraRig;
        [Tooltip("Cable port on the bot's back.")]
        public Transform port;

        [Header("Aim")]
        public float maxRange = 14f;
        public float aimRadius = 0.25f;
        [Tooltip("Degrees off the crosshair a target can be and still get picked (gamepad aim assist).")]
        public float aimAssistAngle = 10f;
        public float fireSpeed = 45f;

        [Header("Modes")]
        public float powerCableLength = 7f;
        public float minSwingLength = 1.5f;
        public float maxSwingLength = 12f;
        public float reelSpeed = 4f;
        public float pullAcceleration = 30f;
        public float maxPullSpeed = 10f;
        public float catchDistance = 1.7f;

        [Header("Visuals")]
        public Material cableMaterial;
        public Color cableColor = new Color(1f, 0.55f, 0.15f);
        public float cableWidth = 0.07f;
        const int Segments = 20;

        public bool IsAttached => state == State.Attached;
        public bool IsSwinging => state == State.Attached && target && target.Mode == TetherMode.Swing;
        public TetherMode? Mode => target ? target.Mode : (TetherMode?)null;
        /// <summary>What the crosshair is on right now (null if nothing valid). Used by the HUD.</summary>
        public TetherTarget AimTarget { get; private set; }
        public TetherTarget Target => target;

        BotController bot;
        BotInput input;
        Grabber grabber;
        Camera cam;
        LineRenderer line;
        State state;
        TetherTarget target;
        float fireTravel;
        BotBattery battery;
        float ropeLength;
        bool tautCueSent;

        const int IgnoreRaycastLayer = 2;
        static int Mask => ~(1 << IgnoreRaycastLayer);
        Vector3 PortPosition => port ? port.position : transform.position + Vector3.up * 0.9f;
        float CurrentLength => target && target.Mode == TetherMode.Power ? powerCableLength : ropeLength;

        void Awake()
        {
            bot = GetComponent<BotController>();
            input = GetComponent<BotInput>();
            grabber = GetComponent<Grabber>();
            battery = GetComponent<BotBattery>();
            if (cameraRig) cam = cameraRig.GetComponent<Camera>();

            line = gameObject.AddComponent<LineRenderer>();
            line.positionCount = Segments;
            line.widthMultiplier = cableWidth;
            line.numCapVertices = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.material = cableMaterial ? cableMaterial : new Material(Shader.Find("Sprites/Default"));
            line.startColor = line.endColor = cableColor;
            line.enabled = false;
        }

        void Update()
        {
            AimTarget = state == State.Idle ? FindAimTarget() : null;

            if (input.TetherPressed)
            {
                if (state != State.Idle) Detach();
                else if (AimTarget) Fire(AimTarget);
            }

            if (state == State.Firing)
            {
                if (!target || !target.CanAttach) { Detach(); return; }
                fireTravel += fireSpeed * Time.deltaTime;
                if (fireTravel >= Vector3.Distance(PortPosition, target.AttachPosition)) Attach();
            }
            else if (IsSwinging)
            {
                ropeLength = Mathf.Clamp(ropeLength - input.Reel * reelSpeed * Time.deltaTime, minSwingLength, maxSwingLength);
            }
        }

        void FixedUpdate()
        {
            if (state != State.Attached) return;
            if (!target || !target.CanAttach) { Detach(); return; }

            if (target.Mode == TetherMode.Pull) PullTarget();
            else ConstrainBot();
        }

        void LateUpdate() => DrawCable();

        // ---------------------------------------------------------------- aiming

        TetherTarget FindAimTarget()
        {
            if (!cam) return null;
            Vector3 origin = cam.transform.position;
            Vector3 fwd = cam.transform.forward;
            float castDist = maxRange + Vector3.Distance(origin, PortPosition);

            if (Physics.SphereCast(origin, aimRadius, fwd, out RaycastHit hit, castDist, Mask, QueryTriggerInteraction.Ignore))
            {
                var t = hit.collider.GetComponentInParent<TetherTarget>();
                if (t && IsValid(t)) return t;
            }

            // Aim assist: nearest-to-crosshair target inside a small cone.
            TetherTarget best = null;
            float bestAngle = aimAssistAngle;
            foreach (var t in TetherTarget.All)
            {
                float angle = Vector3.Angle(fwd, t.AttachPosition - origin);
                if (angle < bestAngle && IsValid(t)) { best = t; bestAngle = angle; }
            }
            return best;
        }

        bool IsValid(TetherTarget t)
        {
            if (!t.CanAttach) return false;
            if (t is Carryable c && c.IsHeld) return false;
            // On reserve PIP can only plug into chargers: it has no power to give.
            if (battery && battery.OnReserve && t.Mode == TetherMode.Power && !(t is ChargerSocket)) return false;
            Vector3 from = PortPosition, to = t.AttachPosition;
            if (Vector3.Distance(from, to) > maxRange) return false;
            // Line of sight from the bot (not the camera) to the target. Grazing whatever the target
            // rests on (a shelf lip) right next to it doesn't count as blocked.
            if (Physics.Linecast(from, to, out RaycastHit hit, Mask, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<TetherTarget>() != t
                && Vector3.Distance(hit.point, to) > 0.6f)
                return false;
            return true;
        }

        // ---------------------------------------------------------------- state changes

        void Fire(TetherTarget t)
        {
            target = t;
            state = State.Firing;
            fireTravel = 0f;
            line.enabled = true;
        }

        void Attach()
        {
            state = State.Attached;
            tautCueSent = false;
            ropeLength = Mathf.Clamp(Vector3.Distance(PortPosition, target.AttachPosition), minSwingLength, maxSwingLength);
            target.OnTetherAttached(this);
            EventBus<NarrationCue>.Raise(new NarrationCue("tether_" + target.Mode.ToString().ToLowerInvariant()));
        }

        public void Detach()
        {
            if (target && state == State.Attached) target.OnTetherDetached(this);
            target = null;
            state = State.Idle;
            if (line) line.enabled = false;
        }

        // ---------------------------------------------------------------- physics

        void ConstrainBot()
        {
            Rigidbody body = bot.Body;
            Vector3 anchor = target.AttachPosition;
            Vector3 portOffset = PortPosition - body.position;
            Vector3 delta = body.position + portOffset - anchor;
            float dist = delta.magnitude;
            float length = CurrentLength;

            // A big jump (respawn, teleport) yanks the plug out instead of dragging the bot.
            if (dist > length + 3f) { Detach(); return; }
            if (dist <= length) return;

            Vector3 dir = delta / dist;
            body.position -= dir * (dist - length);
            Vector3 v = body.linearVelocity;
            float outward = Vector3.Dot(v, dir);
            if (outward > 0f) body.linearVelocity = v - dir * outward;

            if (target.Mode == TetherMode.Power && !tautCueSent)
            {
                tautCueSent = true;
                EventBus<NarrationCue>.Raise(new NarrationCue("cable_taut"));
            }
        }

        void PullTarget()
        {
            Rigidbody rb = (target as Carryable)?.Body;
            if (!rb) { Detach(); return; }

            Vector3 catchPoint = bot.Body.position + Vector3.up * 1.2f;
            Vector3 toBot = catchPoint - rb.worldCenterOfMass;
            float dist = toBot.magnitude;

            if (dist < catchDistance)
            {
                var carryable = (Carryable)target;
                Detach();
                if (grabber) grabber.TryGrab(carryable);
                return;
            }
            if (dist > maxRange + 4f) { Detach(); return; }

            Vector3 dir = toBot / dist;
            rb.AddForce((dir + Vector3.up * 0.25f) * pullAcceleration, ForceMode.Acceleration);
            float along = Vector3.Dot(rb.linearVelocity, dir);
            if (along > maxPullSpeed) rb.linearVelocity -= dir * (along - maxPullSpeed);
        }

        // ---------------------------------------------------------------- visuals

        void DrawCable()
        {
            if (state == State.Idle || !target) { line.enabled = false; return; }
            line.enabled = true;

            Vector3 start = PortPosition;
            Vector3 targetPos = target.AttachPosition;
            float dist = Vector3.Distance(start, targetPos);
            Vector3 end;
            float slack;
            if (state == State.Firing)
            {
                end = Vector3.Lerp(start, targetPos, Mathf.Clamp01(fireTravel / Mathf.Max(dist, 0.01f)));
                slack = 0f;
            }
            else
            {
                end = targetPos;
                slack = target.Mode == TetherMode.Pull ? 0f : Mathf.Max(0f, CurrentLength - dist);
            }

            Vector3 mid = (start + end) * 0.5f + Vector3.down * (slack * 0.6f);
            for (int i = 0; i < Segments; i++)
            {
                float t = i / (float)(Segments - 1);
                Vector3 a = Vector3.Lerp(start, mid, t);
                Vector3 b = Vector3.Lerp(mid, end, t);
                line.SetPosition(i, Vector3.Lerp(a, b, t));
            }
        }
    }
}
