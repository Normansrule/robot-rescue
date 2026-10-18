using System;
using System.Collections.Generic;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    [Serializable]
    public class OrderSpec
    {
        public RecipeDefinition recipe;
        [Tooltip("Seconds before the customer gives up.")]
        public float patience = 60f;
        [Tooltip("Seconds after the previous ticket before this one arrives.")]
        public float delayBefore = 5f;
    }

    /// <summary>
    /// Ticket rail for timed cooking (Pasta Panic, and Rescue Rush waves later). Tickets arrive one after another;
    /// each has a patience timer. Serve every ticket you can; too many expired tickets and the critic storms out.
    /// </summary>
    public class OrderBoard : MonoBehaviour, IBossMeter, IAreaGated
    {
        public string criticName = "Mr. Grumbleton";
        public OrderSpec[] orders = Array.Empty<OrderSpec>();
        public int maxActive = 3;
        public int maxStrikes = 3;
        [Tooltip("Task step raised when the last ticket is dealt with and the critic is still here.")]
        public string completeStepId = "orders_done";
        [Tooltip("Seconds left on a ticket that counts as a Hero moment.")]
        public float heroWindow = 3f;

        public class ActiveOrder
        {
            public OrderSpec spec;
            public float timeLeft;
            public float Patience01 => spec.patience > 0f ? Mathf.Clamp01(timeLeft / spec.patience) : 1f;
        }

        public List<ActiveOrder> Active { get; } = new List<ActiveOrder>();
        public int Served { get; private set; }
        public int Strikes { get; private set; }
        public int HeroMoments { get; private set; }
        public bool Won { get; private set; }
        public bool Lost { get; private set; }
        public bool Running => !Won && !Lost;

        int nextIndex;
        float spawnTimer;

        // Boss meter: the critic's mood.
        public string MeterTitle => criticName + "'s mood";
        public float Meter01 => maxStrikes > 0 ? 1f - Strikes / (float)maxStrikes : 1f;
        public Color MeterColor => Color.Lerp(new Color(1f, 0.35f, 0.3f), new Color(0.4f, 0.9f, 0.5f), Meter01);
        public string MeterStatus => $"Served {Served}/{orders.Length}   ·   Strikes {Strikes}/{maxStrikes}";
        public string FailureText => Lost ? criticName + " stormed out!" : null;

        void Start() => spawnTimer = orders.Length > 0 ? orders[0].delayBefore : 0f;

        void Update()
        {
            if (!Running) return;
            float dt = Time.deltaTime;

            spawnTimer -= dt;
            if (nextIndex < orders.Length && Active.Count < maxActive && spawnTimer <= 0f)
            {
                var spec = orders[nextIndex++];
                Active.Add(new ActiveOrder { spec = spec, timeLeft = spec.patience });
                EventBus<NarrationCue>.Raise(new NarrationCue("order_new"));
                spawnTimer = nextIndex < orders.Length ? orders[nextIndex].delayBefore : 0f;
            }

            for (int i = Active.Count - 1; i >= 0; i--)
            {
                Active[i].timeLeft -= dt;
                if (Active[i].timeLeft > 0f) continue;
                Active.RemoveAt(i);
                Strikes++;
                EventBus<NarrationCue>.Raise(new NarrationCue("order_expired"));
                if (Strikes >= maxStrikes)
                {
                    Lost = true;
                    EventBus<NarrationCue>.Raise(new NarrationCue("panic_failed"));
                    return;
                }
            }

            if (nextIndex >= orders.Length && Active.Count == 0)
            {
                Won = true;
                EventBus<NarrationCue>.Raise(new NarrationCue("panic_won"));
                EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted(completeStepId));
            }
        }

        /// <summary>Serves the oldest open ticket for this recipe. False if nobody ordered it.</summary>
        public bool TryFulfill(RecipeDefinition recipe)
        {
            if (!Running) return false;
            var order = Active.Find(o => o.spec.recipe == recipe);
            if (order == null) return false;
            Active.Remove(order);
            Served++;
            bool hero = order.timeLeft <= heroWindow;
            if (hero) HeroMoments++;
            EventBus<NarrationCue>.Raise(new NarrationCue(hero ? "order_hero" : "order_served"));
            return true;
        }
    }
}
