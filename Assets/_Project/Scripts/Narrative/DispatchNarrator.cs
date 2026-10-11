using System.Collections.Generic;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Narrative
{
    /// <summary>Listens for NarrationCues and queues DISPATCH's matching lines as subtitles.</summary>
    public class DispatchNarrator : MonoBehaviour
    {
        public DispatchScript script;
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
            if (!script) return;
            var matches = new List<NarrationLine>();
            foreach (var l in script.lines)
                if (l.cue == cue.Id && (l.repeatable || !played.Contains(l))) matches.Add(l);
            if (matches.Count == 0 || queue.Count >= maxQueue) return;

            nextIndex.TryGetValue(cue.Id, out int i);
            var line = matches[i % matches.Count];
            nextIndex[cue.Id] = i + 1;
            played.Add(line);
            queue.Enqueue(line);
        }

        void Update()
        {
            timeLeft -= Time.deltaTime;
            if (timeLeft > 0f) return;
            if (queue.Count > 0)
            {
                var line = queue.Dequeue();
                CurrentText = line.text;
                timeLeft = line.duration;
            }
            else CurrentText = null;
        }
    }
}
