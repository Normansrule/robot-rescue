using System.Collections.Generic;
using RobotRescue.Bots;
using RobotRescue.Campaign;
using RobotRescue.Core;
using RobotRescue.Narrative;
using RobotRescue.Puzzles;
using RobotRescue.Tasks;
using UnityEngine;

namespace RobotRescue.World
{
    /// <summary>
    /// One level ("call") placed in the open world. A trigger volume around it tells the <see cref="WorldDirector"/>
    /// when PIP walks in; then its timers, task runner and DISPATCH lines switch on (<see cref="IAreaGated"/>).
    /// The entrance gate opens once the level is unlocked; finished levels keep their exit open.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class Area : MonoBehaviour
    {
        public string sceneId;
        public string title;
        public string chapterTitle;
        public int levelIndex;
        public bool bigRescue;
        public Transform spawnPoint;
        public Door entranceDoor;
        public Door[] exitDoors;
        public TaskRunner runner;
        public DispatchNarrator narrator;
        [Tooltip("Behaviours that only run while PIP is in this area (filled in by the world builder).")]
        public Behaviour[] gated;

        public bool IsActive { get; private set; }
        public bool IsUnlocked => CampaignProgress.IsUnlocked(levelIndex);
        public bool IsComplete => CampaignProgress.IsComplete(levelIndex) || completedThisSession;
        public string DisplayTitle => (bigRescue ? "BIG RESCUE: " : "") + title;

        bool visited, completedThisSession;

        void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        void Awake()
        {
            if (gated == null || gated.Length == 0)
            {
                var list = new List<Behaviour>();
                foreach (var b in GetComponentsInChildren<Behaviour>(true))
                    if (b is IAreaGated) list.Add(b);
                gated = list.ToArray();
            }
            SetGated(false);
        }

        void Start()
        {
            if (IsComplete && exitDoors != null)
                foreach (var d in exitDoors) if (d) d.ForceOpen();
            if (entranceDoor) entranceDoor.SetPowered(IsUnlocked);
        }

        void SetGated(bool on)
        {
            foreach (var b in gated) if (b) b.enabled = on;
        }

        public void Activate()
        {
            IsActive = true;
            if (IsComplete) return;
            SetGated(true);
            if (!visited)
            {
                visited = true;
                EventBus<NarrationCue>.Raise(new NarrationCue("intro"));
            }
        }

        public void Deactivate()
        {
            IsActive = false;
            SetGated(false);
        }

        public void Unlock()
        {
            if (entranceDoor) entranceDoor.SetPowered(true);
        }

        /// <summary>Exit reached: remember it, keep the doors open and let DISPATCH say goodbye.</summary>
        public void MarkComplete()
        {
            completedThisSession = true;
            CampaignProgress.MarkComplete(sceneId);
            if (exitDoors != null) foreach (var d in exitDoors) if (d) d.ForceOpen();
        }

        void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (rb && rb.TryGetComponent<BotController>(out _) && WorldDirector.Instance) WorldDirector.Instance.Enter(this);
        }
    }
}
