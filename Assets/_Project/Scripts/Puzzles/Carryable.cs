using System.Collections;
using System.Collections.Generic;
using RobotRescue.Bots;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// A physics object PIP can pick up, carry overhead, drop or throw.
    /// Also a Tether target: fire the cable at it to reel it in.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Carryable : TetherTarget
    {
        [Tooltip("Shown in the HUD prompt, e.g. 'Pot'.")]
        public string displayName = "Thing";
        [Tooltip("Optional DISPATCH cue raised the first time this is picked up.")]
        public string firstPickupCue;
        [Tooltip("Off for things the Tether shouldn't yank across a room (a frightened rescuee).")]
        public bool tetherPull = true;

        public Rigidbody Body { get; private set; }
        public bool IsHeld => Holder != null;
        public Grabber Holder { get; private set; }

        /// <summary>False while something (a fleeing cat, a parked prop) shouldn't be grabbed yet.</summary>
        public virtual bool CanPickUp => isActiveAndEnabled;

        public override TetherMode Mode => TetherMode.Pull;
        public override bool CanAttach => base.CanAttach && tetherPull && !IsHeld;
        public override string PromptVerb => "Pull " + displayName;

        const int IgnoreRaycastLayer = 2;
        readonly Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
        Collider[] myColliders;
        bool pickedUpBefore;

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            myColliders = GetComponentsInChildren<Collider>();
        }

        public void OnPickedUp(Grabber grabber, Collider[] holderColliders)
        {
            Holder = grabber;
            if (AttachedCable) AttachedCable.Detach();
            Body.useGravity = false;
            SetIgnoreCollisions(holderColliders, true);

            // Held objects shouldn't block the camera, the ground check or the tether aim.
            originalLayers.Clear();
            foreach (Transform t in GetComponentsInChildren<Transform>())
            {
                originalLayers[t.gameObject] = t.gameObject.layer;
                t.gameObject.layer = IgnoreRaycastLayer;
            }

            if (!pickedUpBefore && !string.IsNullOrEmpty(firstPickupCue))
                EventBus<NarrationCue>.Raise(new NarrationCue(firstPickupCue));
            pickedUpBefore = true;
        }

        public void OnReleased(Collider[] holderColliders)
        {
            Holder = null;
            Body.useGravity = true;
            foreach (var kv in originalLayers)
                if (kv.Key) kv.Key.layer = kv.Value;
            originalLayers.Clear();
            if (isActiveAndEnabled) StartCoroutine(RestoreCollisionsSoon(holderColliders));
            else SetIgnoreCollisions(holderColliders, false);
        }

        IEnumerator RestoreCollisionsSoon(Collider[] holderColliders)
        {
            // Let a dropped/thrown object clear the bot before they can collide again.
            yield return new WaitForSeconds(0.3f);
            SetIgnoreCollisions(holderColliders, false);
        }

        void SetIgnoreCollisions(Collider[] others, bool ignore)
        {
            foreach (var a in myColliders)
                foreach (var b in others)
                    if (a && b) Physics.IgnoreCollision(a, b, ignore);
        }

        protected override void OnDisable()
        {
            if (Holder) Holder.Drop();
            base.OnDisable();
        }
    }
}
