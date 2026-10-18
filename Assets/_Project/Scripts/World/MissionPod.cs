using RobotRescue.Bots;
using RobotRescue.Campaign;
using UnityEngine;

namespace RobotRescue.World
{
    /// <summary>
    /// A drop pod between destinations (the Bureau Station and the places it sends PIP). Walk in and it launches,
    /// once the mission is unlocked. The capsule lifts off while the next destination loads.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MissionPod : MonoBehaviour
    {
        [Tooltip("Scene to fly to.")]
        public string targetScene;
        [Tooltip("Campaign index of the call to start at there, or -1 to step out at that destination's pod bay.")]
        public int targetLevel = -1;
        public string destinationName = "Brightwater Reach";
        [Tooltip("Campaign index that must be unlocked before this pod flies (-1: always open).")]
        public int requiresLevel = -1;
        [Tooltip("The part that lifts off.")]
        public Transform capsule;
        [Tooltip("Light that shows green when the pod is ready, red when locked.")]
        public Renderer readyLight;

        bool launched;
        float launchTime;
        MaterialPropertyBlock mpb;

        public bool Ready => requiresLevel < 0 || CampaignProgress.IsUnlocked(requiresLevel);

        void Reset() => GetComponent<Collider>().isTrigger = true;

        void Start()
        {
            if (!readyLight) return;
            mpb = new MaterialPropertyBlock();
            var c = Ready ? new Color(0.3f, 1f, 0.45f) : new Color(1f, 0.3f, 0.3f);
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_EmissionColor", c * 2f);
            readyLight.SetPropertyBlock(mpb);
        }

        void OnTriggerEnter(Collider other)
        {
            var rb = other.attachedRigidbody;
            if (launched || !rb || !rb.TryGetComponent<BotController>(out _)) return;
            var world = WorldDirector.Instance;
            if (!world) return;
            if (!Ready)
            {
                world.ShowToast("Mission locked: finish the calls here first.");
                return;
            }
            launched = true;
            launchTime = Time.time;
            world.Launch(targetScene, targetLevel >= 0 ? targetLevel : WorldDirector.PodKey, destinationName);
        }

        void Update()
        {
            if (!launched || !capsule) return;
            float t = Time.time - launchTime;
            if (t < 0.5f) capsule.localPosition += Random.insideUnitSphere * 0.02f;   // rumble
            else capsule.position += Vector3.up * (t - 0.5f) * 30f * Time.deltaTime;  // lift off
        }
    }
}
