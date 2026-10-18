using System.Collections.Generic;
using RobotRescue.Bots;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Campaign
{
    /// <summary>
    /// A golden bolt hidden around the world (on rock spires, behind buildings, at the end of a climb).
    /// Collected ids are saved, so each bolt can only be found once.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Collectible : MonoBehaviour
    {
        const string SaveKey = "rr.bolts";

        public string id;
        public Transform visual;

        static HashSet<string> collected;
        static readonly HashSet<string> known = new HashSet<string>();

        public static int Count { get { Load(); return collected.Count; } }
        public static int Total => known.Count;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { collected = null; known.Clear(); }

        static void Load()
        {
            if (collected != null) return;
            collected = new HashSet<string>();
            foreach (var s in PlayerPrefs.GetString(SaveKey, "").Split(','))
                if (!string.IsNullOrEmpty(s)) collected.Add(s);
        }

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            collected = new HashSet<string>();
        }

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Awake()
        {
            Load();
            if (string.IsNullOrEmpty(id)) id = name;
            known.Add(id);
            if (collected.Contains(id)) gameObject.SetActive(false);
        }

        void Update()
        {
            if (!visual) return;
            visual.Rotate(0f, 120f * Time.deltaTime, 0f, Space.World);
            visual.localPosition = Vector3.up * (Mathf.Sin(Time.time * 2.4f + transform.position.x) * 0.12f);
        }

        void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (!rb || !rb.TryGetComponent<BotController>(out _)) return;
            bool first = collected.Count == 0;
            collected.Add(id);
            PlayerPrefs.SetString(SaveKey, string.Join(",", collected));
            PlayerPrefs.Save();
            EventBus<BoltCollected>.Raise(new BoltCollected(id));
            EventBus<NarrationCue>.Raise(new NarrationCue(first ? "bolt_first" : "bolt"));
            // Every bolt is a memory bolt: the Nth one found plays the Nth piece of Unit Zero's story.
            EventBus<NarrationCue>.Raise(new NarrationCue("lore_" + collected.Count));
            gameObject.SetActive(false);
        }
    }
}
