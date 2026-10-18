using System.Collections.Generic;
using RobotRescue.Bots;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Narrative
{
    /// <summary>Listens for NarrationCues and queues DISPATCH's matching lines as subtitles.</summary>
    public class DispatchNarrator : MonoBehaviour, IAreaGated
    {
        public DispatchScript script;
        [Tooltip("Shared lines (battery, broken parts, bolts) used when the level script has none for a cue.")]
        public DispatchScript common;
        [Tooltip("Lines waiting beyond this are dropped so DISPATCH never lags behind the action.")]
        public int maxQueue = 2;

        public string CurrentText { get; private set; }

        readonly Queue<NarrationLine> queue = new Queue<NarrationLine>();
        readonly HashSet<NarrationLine> played = new HashSet<NarrationLine>();
        readonly Dictionary<string, int> nextIndex = new Dictionary<string, int>();
        float timeLeft;

        void OnEnable() => EventBus<NarrationCue>.Subscribe(OnCue);
        void OnDisable() => EventBus<NarrationCue>.Unsubscribe(OnCue);

        void OnCue(NarrationCue cue)
        {
            var matches = new List<NarrationLine>();
            Collect(script, cue.Id, matches);
            if (matches.Count == 0) Collect(common, cue.Id, matches);
            if (matches.Count == 0 || queue.Count >= maxQueue) return;

            nextIndex.TryGetValue(cue.Id, out int i);
            var line = matches[i % matches.Count];
            nextIndex[cue.Id] = i + 1;
            played.Add(line);
            queue.Enqueue(line);
        }

        void Collect(DispatchScript from, string id, List<NarrationLine> into)
        {
            if (!from) return;
            foreach (var l in from.lines)
                if (l.cue == id && (l.repeatable || !played.Contains(l))) into.Add(l);
        }

        /// <summary>True for a frame when a new line starts (the HUD uses it to start DISPATCH's voice).</summary>
        public int LineCounter { get; private set; }

        void Update()
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft > 0f) return;
            if (queue.Count > 0)
            {
                var line = queue.Dequeue();
                CurrentText = BotLookStore.Personalize(line.text);
                timeLeft = line.duration;
                LineCounter++;
            }
            else CurrentText = null;
        }
    }
}
