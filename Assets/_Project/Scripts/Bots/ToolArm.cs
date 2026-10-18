using System.Collections.Generic;
using RobotRescue.Tools;
using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// PIP's swappable Tool Arm. Hold Use (F / B) to work on the <see cref="IToolUsable"/> in front of you with the
    /// equipped tool; swap tools with Tab / Y. Picking things up stays on the Grabber controls so carrying always works.
    /// </summary>
    [RequireComponent(typeof(BotController))]
    public class ToolArm : MonoBehaviour
    {
        public ToolDefinition[] startingTools;
        [Tooltip("Every tool head in the game, so tools unlocked in earlier sessions come back (saved by id).")]
        public ToolDefinition[] allTools;
        [Tooltip("Tool head on the arm; tinted and spun to show the current tool.")]
        public Renderer toolHead;
        public float reach = 0.9f;
        public float radius = 1.0f;

        public ToolDefinition Current => unlocked.Count > 0 ? unlocked[index] : null;
        public IReadOnlyList<ToolDefinition> Unlocked => unlocked;
        /// <summary>What Use would work on right now (null if nothing). Used by the HUD.</summary>
        public IToolUsable Focus { get; private set; }
        public bool IsUsing { get; private set; }

        readonly List<ToolDefinition> unlocked = new List<ToolDefinition>();
        readonly Collider[] overlap = new Collider[16];
        BotInput input;
        BotBattery battery;
        int index;

        const string SavedKey = "rr.tools";

        /// <summary>Forget saved tool unlocks (new game).</summary>
        public static void ResetSaved() => PlayerPrefs.DeleteKey(SavedKey);

        void Awake()
        {
            input = GetComponent<BotInput>();
            battery = GetComponent<BotBattery>();
            if (startingTools != null)
                foreach (var t in startingTools)
                    if (t && !unlocked.Contains(t)) unlocked.Add(t);
            string saved = "," + PlayerPrefs.GetString(SavedKey, "") + ",";
            if (allTools != null)
                foreach (var t in allTools)
                    if (t && !unlocked.Contains(t) && saved.Contains("," + t.id + ",")) unlocked.Add(t);
            ApplyVisual();
        }

        public void Unlock(ToolDefinition tool)
        {
            if (!tool) return;
            if (!unlocked.Contains(tool)) unlocked.Add(tool);
            index = unlocked.IndexOf(tool);
            string saved = PlayerPrefs.GetString(SavedKey, "");
            if (!("," + saved + ",").Contains("," + tool.id + ","))
            {
                PlayerPrefs.SetString(SavedKey, string.IsNullOrEmpty(saved) ? tool.id : saved + "," + tool.id);
                PlayerPrefs.Save();
            }
            ApplyVisual();
        }

        void Update()
        {
            if (input.CycleToolPressed && unlocked.Count > 1)
            {
                index = (index + 1) % unlocked.Count;
                ApplyVisual();
            }

            Focus = FindUsable();
            IsUsing = Focus != null && input.UseToolHeld && (!battery || battery.CanDoPoweredWork);
            if (IsUsing) Focus.UseTool(this, Time.deltaTime);

            if (toolHead && IsUsing) toolHead.transform.Rotate(0f, 0f, 900f * Time.deltaTime, Space.Self);
        }

        IToolUsable FindUsable()
        {
            var tool = Current;
            if (!tool) return null;
            Vector3 center = transform.position + Vector3.up * 0.8f + transform.forward * reach;
            int n = Physics.OverlapSphereNonAlloc(center, radius, overlap, ~0, QueryTriggerInteraction.Collide);
            IToolUsable best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < n; i++)
            {
                var usable = overlap[i].GetComponentInParent<IToolUsable>();
                if (usable == null || usable.RequiredTool != tool.kind || !usable.CanUse) continue;
                float d = (overlap[i].transform.position - center).sqrMagnitude;
                if (d < bestDist) { best = usable; bestDist = d; }
            }
            return best;
        }

        void ApplyVisual()
        {
            if (toolHead && Current) toolHead.material.color = Current.color;
        }
    }
}
