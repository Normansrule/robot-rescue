using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// A part that fell off PIP. Bump into it to snap it back on. It's also a Carryable, so the Tether can reel it
    /// in from a ledge. A bobbing marker and light beam show where it is. Lost parts are comedy, never a chore: a part
    /// that lands in water floats for a moment, and one that falls out of the world or sits uncollected too long is
    /// "returned by the service tray": it hops home and clicks back on by itself.
    /// </summary>
    public class BotPartPickup : Carryable
    {
        public BotParts owner;
        public BotPart part;
        [Tooltip("Seconds before an uncollected part comes home on its own.")]
        public float autoReturnSeconds = 45f;
        [Tooltip("Seconds a part bobs in water before the service tray fishes it out.")]
        public float floatSeconds = 2.5f;
        public float returnFlightSeconds = 0.8f;

        float armedAt;
        float spawnY;
        float returnAt;
        float floatUntil = -1f;
        float flightStart = -1f;
        Vector3 flightFrom, floatPoint;
        Transform marker;

        /// <summary>True while the part is flying itself home.</summary>
        public bool IsReturning => flightStart >= 0f;
        public Transform Marker => marker;

        public static BotPartPickup Create(GameObject copy, BotParts owner, BotPart part, Vector3 impulse)
        {
            foreach (Transform t in copy.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 0;
            foreach (var c in copy.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var mb in copy.GetComponentsInChildren<MonoBehaviour>()) Destroy(mb);

            var bounds = new Bounds(copy.transform.position, Vector3.zero);
            foreach (var r in copy.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
            var box = copy.AddComponent<BoxCollider>();
            box.center = copy.transform.InverseTransformPoint(bounds.center);
            Vector3 s = copy.transform.lossyScale;
            box.size = new Vector3(
                Mathf.Max(0.12f, bounds.size.x / Mathf.Max(0.01f, Mathf.Abs(s.x))),
                Mathf.Max(0.12f, bounds.size.y / Mathf.Max(0.01f, Mathf.Abs(s.y))),
                Mathf.Max(0.12f, bounds.size.z / Mathf.Max(0.01f, Mathf.Abs(s.z))));

            var rb = copy.AddComponent<Rigidbody>();
            rb.mass = 0.3f;
            var pickup = copy.AddComponent<BotPartPickup>();
            pickup.owner = owner;
            pickup.part = part;
            pickup.displayName = "PIP's " + BotParts.PartName(part);
            rb.linearVelocity = impulse;
            rb.angularVelocity = Random.insideUnitSphere * 8f;
            pickup.marker = BuildMarker(copy);
            return pickup;
        }

        /// <summary>
        /// A small spinning diamond and a thin light beam above the part, painted with the part's own material so it
        /// reads as "that's mine". Not parented, so it stays upright while the part tumbles.
        /// </summary>
        static Transform BuildMarker(GameObject part)
        {
            var src = part.GetComponentInChildren<Renderer>();
            var mat = src ? src.sharedMaterial : null;
            var root = new GameObject("LostPartMarker").transform;
            root.position = part.transform.position;

            var gem = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(gem.GetComponent<Collider>());
            gem.name = "Gem";
            gem.transform.SetParent(root, false);
            gem.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            gem.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
            gem.transform.localScale = Vector3.one * 0.16f;

            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(beam.GetComponent<Collider>());
            beam.name = "Beam";
            beam.transform.SetParent(root, false);
            beam.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            beam.transform.localScale = new Vector3(0.035f, 1f, 0.035f);

            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (mat) r.sharedMaterial = mat;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2;   // Ignore Raycast
            return root;
        }

        protected override void Awake()
        {
            base.Awake();
            armedAt = Time.time + 0.9f;
            spawnY = transform.position.y;
            returnAt = Time.time + autoReturnSeconds;
        }

        void OnDestroy()
        {
            if (marker) Destroy(marker.gameObject);
        }

        void Update()
        {
            if (!owner) { Destroy(gameObject); return; }
            if (IsReturning) { Fly(); return; }
            if (IsHeld && Holder && Holder.gameObject == owner.gameObject) { owner.Reattach(part); return; }

            if (floatUntil >= 0f)
            {
                // Bobbing on the surface where it splashed down.
                transform.position = floatPoint + Vector3.up * (Mathf.Sin(Time.time * 3f) * 0.08f);
                transform.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
                if (Time.time >= floatUntil) ReturnHome();
            }
            else if (transform.position.y < spawnY - 25f || Time.time >= returnAt) ReturnHome();

            UpdateMarker();
        }

        void UpdateMarker()
        {
            if (!marker) return;
            marker.position = transform.position + Vector3.up * (0.15f + Mathf.Sin(Time.time * 2.5f) * 0.08f);
            marker.rotation = Quaternion.Euler(0f, Time.time * 120f, 0f);
        }

        /// <summary>The service tray sends the part home: it hops back to its owner and clicks on.</summary>
        public void ReturnHome()
        {
            if (!owner || IsReturning) return;
            if (Holder) Holder.Drop();
            if (TryGetComponent(out Rigidbody rb)) { rb.isKinematic = true; rb.detectCollisions = false; }
            if (marker) marker.gameObject.SetActive(false);
            flightFrom = transform.position;
            flightStart = Time.time;
            EventBus<NarrationCue>.Raise(new NarrationCue("part_returned"));
        }

        void Fly()
        {
            float t = Mathf.Clamp01((Time.time - flightStart) / Mathf.Max(0.05f, returnFlightSeconds));
            Vector3 to = owner.transform.position + Vector3.up * 0.9f;
            Vector3 p = Vector3.Lerp(flightFrom, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 2.5f);
            transform.position = p;
            transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World);
            if (t >= 1f) owner.Reattach(part);
        }

        void OnTriggerEnter(Collider other)
        {
            // Water and pits are KillZones: float instead of vanishing.
            if (IsReturning || floatUntil >= 0f || !other.GetComponent<KillZone>()) return;
            if (Holder) Holder.Drop();
            if (TryGetComponent(out Rigidbody rb)) { rb.isKinematic = true; }
            floatPoint = new Vector3(transform.position.x, other.bounds.max.y + 0.1f, transform.position.z);
            floatUntil = Time.time + floatSeconds;
            EventBus<NarrationCue>.Raise(new NarrationCue("part_floating"));
        }

        void OnCollisionEnter(Collision collision) => TryReturn(collision.rigidbody);
        void OnCollisionStay(Collision collision) => TryReturn(collision.rigidbody);

        void TryReturn(Rigidbody rb)
        {
            if (Time.time < armedAt || !owner || !rb || rb.gameObject != owner.gameObject) return;
            owner.Reattach(part);
        }
    }
}
