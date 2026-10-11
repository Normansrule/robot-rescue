using RobotRescue.Bots;
using RobotRescue.Core;
using RobotRescue.Narrative;
using RobotRescue.Puzzles;
using RobotRescue.Tasks;
using RobotRescue.Tasks.Cooking;
using UnityEngine;

namespace RobotRescue.UI
{
    /// <summary>
    /// Throwaway IMGUI HUD for greyboxing: crosshair, context prompts, task checklist, cook timer,
    /// DISPATCH subtitles. Replace with a UI Toolkit HUD in the polish milestone.
    /// </summary>
    public class PrototypeHUD : MonoBehaviour
    {
        public TetherCable tether;
        public Grabber grabber;
        public TaskRunner taskRunner;
        public DispatchNarrator narrator;
        public CookingPot pot;

        GUIStyle box, label, big, subtitle;

        void BuildStyles()
        {
            if (box != null) return;
            box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 16, padding = new RectOffset(12, 12, 10, 10), wordWrap = true, richText = true };
            label = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            big = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            subtitle = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = true, padding = new RectOffset(16, 16, 10, 10) };
        }

        void OnGUI()
        {
            BuildStyles();
            float w = Screen.width, h = Screen.height;

            DrawCrosshair(w, h);
            DrawPrompts(w, h);
            DrawTask();
            DrawCooking(w);

            if (narrator && !string.IsNullOrEmpty(narrator.CurrentText))
                GUI.Box(new Rect(w * 0.2f, h - 140f, w * 0.6f, 70f), "<b>DISPATCH:</b> " + narrator.CurrentText, subtitle);

            if (GameManager.Instance && GameManager.Instance.LevelComplete)
            {
                float t = taskRunner ? taskRunner.Elapsed : 0f;
                GUI.Label(new Rect(0, h * 0.3f, w, 60f), "CALL COMPLETE!", big);
                GUI.Label(new Rect(0, h * 0.3f + 60f, w, 30f), $"Rescue time {(int)t / 60}:{(int)t % 60:00}   ·   Backspace / Select to replay", label);
            }
        }

        void DrawCrosshair(float w, float h)
        {
            bool onTarget = tether && tether.AimTarget;
            Color old = GUI.color;
            GUI.color = onTarget ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 1f, 1f, 0.7f);
            float s = onTarget ? 12f : 6f;
            GUI.DrawTexture(new Rect(w / 2 - s / 2, h / 2 - s / 2, s, s), Texture2D.whiteTexture);
            GUI.color = old;
        }

        void DrawPrompts(float w, float h)
        {
            var lines = new System.Collections.Generic.List<string>();
            if (tether)
            {
                if (tether.IsSwinging) lines.Add("[Q / RB] reel in   [Z / LB] reel out   [Space / A] let go   [RMB / LT] release");
                else if (tether.IsAttached) lines.Add("[RMB / LT] unplug cable");
                else if (tether.AimTarget) lines.Add($"[RMB / LT] {tether.AimTarget.PromptVerb}");
            }
            if (grabber)
            {
                if (grabber.Held) lines.Add($"[LMB / RT] throw {grabber.Held.displayName}   [E / X] put down");
                else if (grabber.Focus) lines.Add($"[E / X] pick up {grabber.Focus.displayName}");
            }
            for (int i = 0; i < lines.Count; i++)
                GUI.Label(new Rect(0, h / 2 + 30f + i * 24f, w, 24f), lines[i], label);
        }

        void DrawTask()
        {
            if (!taskRunner || !taskRunner.task) return;
            var task = taskRunner.task;
            var sb = new System.Text.StringBuilder();
            sb.Append("<b>").Append(task.title).Append("</b>\n<i>for ").Append(task.rescuee).Append("</i>\n\n");
            foreach (var step in task.steps)
                sb.Append(taskRunner.IsStepDone(step.id) ? "[x] " : "[  ] ").Append(step.label).Append('\n');
            GUI.Box(new Rect(16f, 16f, 320f, 60f + task.steps.Length * 22f), sb.ToString(), box);
        }

        void DrawCooking(float w)
        {
            if (!pot || !pot.isActiveAndEnabled || pot.IsCooked || !pot.IsBoiling || !pot.HasAllIngredients) return;
            Rect bg = new Rect(w / 2 - 150f, 20f, 300f, 22f);
            GUI.Box(bg, GUIContent.none);
            Color old = GUI.color;
            GUI.color = new Color(1f, 0.75f, 0.2f);
            GUI.DrawTexture(new Rect(bg.x + 3, bg.y + 3, (bg.width - 6) * pot.CookProgress01, bg.height - 6), Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(bg.x, bg.y + 24f, bg.width, 22f), "Cooking...", label);
        }
    }
}
