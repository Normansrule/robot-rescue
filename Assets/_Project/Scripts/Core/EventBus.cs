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

        public static void Subscribe(Action<T> handler) => Handlers += handler;
        public static void Unsubscribe(Action<T> handler) => Handlers -= handler;
        public static void Raise(T evt) => Handlers?.Invoke(evt);

        // Static events survive play mode when domain reload is disabled.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => Handlers = null;
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
}
