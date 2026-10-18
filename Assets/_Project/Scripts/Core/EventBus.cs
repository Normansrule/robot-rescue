using System;

namespace RobotRescue.Core
{
    /// <summary>
    /// Tiny typed event bus. Raise a struct, anyone subscribed to that struct type hears it.
    /// Subscribers must unsubscribe in OnDisable.
    /// </summary>
    public static class EventBus<T> where T : struct
    {
        static event Action<T> Handlers;

        static EventBus() => EventBusReset.Register(() => Handlers = null);

        public static void Subscribe(Action<T> handler) => Handlers += handler;
        public static void Unsubscribe(Action<T> handler) => Handlers -= handler;
        public static void Raise(T evt) => Handlers?.Invoke(evt);
    }

    /// <summary>
    /// Clears every EventBus on entering play mode. Static events survive play mode when domain reload
    /// is disabled, and Unity can't run [RuntimeInitializeOnLoadMethod] inside a generic type, so each
    /// EventBus&lt;T&gt; registers its reset here.
    /// </summary>
    static class EventBusReset
    {
        static readonly System.Collections.Generic.List<Action> resets = new System.Collections.Generic.List<Action>();

        public static void Register(Action reset) => resets.Add(reset);

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetAll()
        {
            foreach (var reset in resets) reset();
        }
    }

    // ---- Shared game events ----

    /// <summary>A line for DISPATCH to (maybe) say. Cue ids are matched against the narrator's script.</summary>
    public struct NarrationCue
    {
        public string Id;
        public NarrationCue(string id) { Id = id; }
    }

    public struct TaskStepCompleted
    {
        public string StepId;
        public TaskStepCompleted(string stepId) { StepId = stepId; }
    }

    public struct TaskCompleted
    {
        public string TaskId;
        public TaskCompleted(string taskId) { TaskId = taskId; }
    }

    public struct LevelCompleted { }

    /// <summary>PIP waved (G / D-pad up). Nearby drones wave back.</summary>
    public struct BotEmoted
    {
        public UnityEngine.Vector3 Position;
        public BotEmoted(UnityEngine.Vector3 position) { Position = position; }
    }

    /// <summary>A golden bolt was picked up.</summary>
    public struct BoltCollected
    {
        public string Id;
        public BoltCollected(string id) { Id = id; }
    }
}
