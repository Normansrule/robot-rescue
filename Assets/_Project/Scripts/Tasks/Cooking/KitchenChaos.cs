using System.Collections.Generic;
using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>
    /// "The kitchen falls apart": while the order board is running, random mishaps hit every so often.
    /// Fuse blows (every plugged-in cable pops out), a pot rattles off its stove, the conveyor runs backwards.
    /// </summary>
    public class KitchenChaos : MonoBehaviour, Core.IAreaGated
    {
        public enum Mishap { FuseBlows, PotRattle, BeltReverses }

        public OrderBoard board;
        public float firstMishapAfter = 25f;
        public Vector2 interval = new Vector2(18f, 28f);
        public Mishap[] mishaps = { Mishap.FuseBlows, Mishap.PotRattle, Mishap.BeltReverses };
        public float rattleImpulse = 3.5f;

        /// <summary>Last mishap, for the HUD flash.</summary>
        public string LastMishap { get; private set; }
        public float LastMishapTime { get; private set; } = -100f;

        float timer;
        int nextMishap;

        void Start() => timer = firstMishapAfter;

        void Update()
        {
            if (board && !board.Running) return;
            timer -= Time.deltaTime;
            if (timer > 0f || mishaps.Length == 0) return;
            timer = Random.Range(interval.x, interval.y);

            // Cycle through the list so every mishap shows up, starting at a random one.
            if (nextMishap == 0) nextMishap = Random.Range(0, mishaps.Length);
            var m = mishaps[nextMishap % mishaps.Length];
            nextMishap++;
            Trigger(m);
        }

        public void Trigger(Mishap m)
        {
            switch (m)
            {
                case Mishap.FuseBlows:
                    foreach (var socket in InScope<PowerSocket>())
                        if (socket.AttachedCable) socket.AttachedCable.Detach();
                    Announce("FUSE BLOWN!", "chaos_fuse");
                    break;

                case Mishap.PotRattle:
                    var pots = new List<CookingPot>();
                    foreach (var p in InScope<CookingPot>())
                        if (p.TryGetComponent(out Carryable c) && !c.IsHeld) pots.Add(p);
                    if (pots.Count == 0) return;
                    var rb = pots[Random.Range(0, pots.Count)].GetComponent<Rigidbody>();
                    Vector2 dir = Random.insideUnitCircle.normalized;
                    rb.AddForce(new Vector3(dir.x, 1.6f, dir.y) * rattleImpulse, ForceMode.VelocityChange);
                    Announce("POT RATTLE!", "chaos_rattle");
                    break;

                case Mishap.BeltReverses:
                    var belts = InScope<Conveyor>();
                    if (belts.Length == 0) return;
                    foreach (var belt in belts) belt.reversed = !belt.reversed;
                    Announce("CONVEYOR REVERSED!", "chaos_belt");
                    break;
            }
        }

        /// <summary>Only this kitchen's things: in the open world, the area it sits in; in a standalone level, the scene.</summary>
        T[] InScope<T>() where T : Component
        {
            var area = GetComponentInParent<World.Area>();
            return area ? area.GetComponentsInChildren<T>() : FindObjectsByType<T>();
        }

        void Announce(string text, string cue)
        {
            LastMishap = text;
            LastMishapTime = Time.time;
            EventBus<NarrationCue>.Raise(new NarrationCue(cue));
        }
    }
}
