using RobotRescue.Bots;
using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Tasks.Rescue
{
    /// <summary>
    /// A very real, very stubborn cat up a tree. Get close and she hops to the next branch up, until she reaches the
    /// top perch and realises she can't get down. Then she can be picked up (or, frowned upon, Tether-yanked).
    /// The Hum spooks her: she yowls and fluffs up.
    /// </summary>
    public class CatNPC : Carryable
    {
        [Tooltip("Branch spots she flees along, lowest first. She's catchable on the last one.")]
        public Transform[] perches;
        public float fleeRadius = 3f;
        public float hopSeconds = 0.75f;
        public float hopHeight = 1.4f;
        public string gotStepId = "got_cat";

        [Header("Look")]
        public Transform body;
        public Transform tail;
        public Babble voice;

        public bool Settled { get; private set; }
        public bool Home { get; private set; }

        public override bool CanPickUp => base.CanPickUp && Settled && !hopping;
        public override bool CanAttach => base.CanAttach && Settled && !hopping;

        int perch;
        bool hopping, reportedGrab;
        float hopStart, nextMeow, fluffUntil, nextLook;
        Vector3 hopFrom;
        Transform pip;

        protected override void Awake()
        {
            base.Awake();
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;   // kinematic-safe
            Body.isKinematic = true;
            if (perches != null && perches.Length > 0 && perches[0]) transform.SetPositionAndRotation(perches[0].position, perches[0].rotation);
            Settled = perches == null || perches.Length <= 1;
            if (Settled) Body.isKinematic = false;
            nextMeow = Time.time + Random.Range(3f, 6f);
        }

        void Update()
        {
            if (Home) { Idle(); return; }
            if (IsHeld && !reportedGrab)
            {
                reportedGrab = true;
                if (!string.IsNullOrEmpty(gotStepId)) EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted(gotStepId));
                Meow(Babble.Tone.Question);
            }

            if (hopping) { Hop(); return; }
            if (!Settled && Time.time > nextLook)
            {
                nextLook = Time.time + 0.2f;
                if (!pip) { var bot = FindAnyObjectByType<BotController>(); if (bot) pip = bot.transform; }
                if (pip && (pip.position - transform.position).sqrMagnitude < fleeRadius * fleeRadius) StartHop();
            }
            if (Time.time > nextMeow)
            {
                nextMeow = Time.time + Random.Range(4f, 9f);
                Meow(Settled ? Babble.Tone.Sad : Babble.Tone.Chatter);
            }
            Idle();
        }

        void StartHop()
        {
            if (perches == null || perch + 1 >= perches.Length) return;
            perch++;
            hopping = true;
            hopStart = Time.time;
            hopFrom = transform.position;
            Meow(Babble.Tone.Surprised);
            EventBus<NarrationCue>.Raise(new NarrationCue(perch + 1 >= perches.Length ? "cat_top" : "cat_flee_" + perch));
        }

        void Hop()
        {
            var to = perches[perch];
            float t = Mathf.Clamp01((Time.time - hopStart) / hopSeconds);
            Vector3 p = Vector3.Lerp(hopFrom, to.position, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * hopHeight);
            Quaternion r = Quaternion.Slerp(transform.rotation, to.rotation, t);
            Body.MovePosition(p);
            Body.MoveRotation(r);
            transform.SetPositionAndRotation(p, r);
            if (t < 1f) return;
            hopping = false;
            if (perch + 1 >= perches.Length)
            {
                Settled = true;
                Body.isKinematic = false;
            }
        }

        /// <summary>The Hum (or anything loud) startles her.</summary>
        public void Spook()
        {
            fluffUntil = Time.time + 1.2f;
            Meow(Babble.Tone.Surprised);
            if (Settled && !IsHeld && !Body.isKinematic) Body.linearVelocity += Vector3.up * 2.5f;
        }

        /// <summary>Safe in the basket: purr and stay put.</summary>
        public void GoHome()
        {
            Home = true;
            Settled = true;
            Meow(Babble.Tone.Happy);
        }

        void Meow(Babble.Tone tone)
        {
            if (voice) voice.Say(tone == Babble.Tone.Sad ? 3 : 2, tone);
        }

        void Idle()
        {
            if (tail)
            {
                float speed = Home ? 2f : Time.time < fluffUntil ? 14f : 4f;
                tail.localRotation = Quaternion.Euler(-35f + Mathf.Sin(Time.time * speed * 0.5f) * 8f, Mathf.Sin(Time.time * speed) * 25f, 0f);
            }
            if (body)
            {
                float fluff = Time.time < fluffUntil ? 1.18f : 1f;
                float breathe = 1f + Mathf.Sin(Time.time * (Home ? 2f : 3.5f)) * 0.03f;
                body.localScale = Vector3.Lerp(body.localScale, Vector3.one * fluff * breathe, 12f * Time.deltaTime);
            }
        }
    }
}
