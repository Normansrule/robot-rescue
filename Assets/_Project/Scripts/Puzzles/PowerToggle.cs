using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// Shows things while powered (a fountain's jets, a lamp's glow, a sign lighting up), with an optional gentle
    /// bob so water and lights feel alive.
    /// </summary>
    public class PowerToggle : MonoBehaviour, IPowerable
    {
        public GameObject[] showWhenPowered;
        public GameObject[] hideWhenPowered;
        [Tooltip("Bob the shown objects' height scale by this much (0 = still).")]
        public float pulse = 0.08f;
        public float pulseSpeed = 5f;

        bool powered;
        Vector3[] scales;

        void Awake()
        {
            scales = new Vector3[showWhenPowered != null ? showWhenPowered.Length : 0];
            for (int i = 0; i < scales.Length; i++) if (showWhenPowered[i]) scales[i] = showWhenPowered[i].transform.localScale;
            Apply();
        }

        public void SetPowered(bool on)
        {
            powered = on;
            Apply();
        }

        void Apply()
        {
            if (showWhenPowered != null) foreach (var go in showWhenPowered) if (go) go.SetActive(powered);
            if (hideWhenPowered != null) foreach (var go in hideWhenPowered) if (go) go.SetActive(!powered);
        }

        void Update()
        {
            if (!powered || pulse <= 0f || scales == null) return;
            for (int i = 0; i < scales.Length; i++)
            {
                var go = showWhenPowered[i];
                if (!go) continue;
                float s = 1f + Mathf.Sin(Time.time * pulseSpeed + i * 1.7f) * pulse;
                go.transform.localScale = new Vector3(scales[i].x, scales[i].y * s, scales[i].z);
            }
        }
    }
}
