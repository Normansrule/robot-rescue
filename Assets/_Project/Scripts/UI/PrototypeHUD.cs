using System.Collections.Generic;
using System.Text;
using RobotRescue.Bots;
using RobotRescue.Campaign;
using RobotRescue.Core;
using RobotRescue.Narrative;
using RobotRescue.Tasks;
using RobotRescue.Tasks.Cooking;
using UnityEngine;

namespace RobotRescue.UI
{
    /// <summary>
    /// Throwaway IMGUI HUD for greyboxing: crosshair, context prompts, tool, task checklist, order tickets,
    /// boss meters, world progress bars, DISPATCH subtitles, pause and level-complete screens.
    /// Replace with a UI Toolkit HUD in the polish milestone.
    /// </summary>
    public class PrototypeHUD : MonoBehaviour
    {
        public TetherCable tether;
        public Grabber grabber;
        public ToolArm toolArm;
        public TaskRunner taskRunner;
        public DispatchNarrator narrator;

        [Tooltip("Seconds the level title card stays up at the start.")]
        public float titleCardTime = 4f;

        readonly List<IProgressIndicator> indicators = new List<IProgressIndicator>();
        readonly List<IBossMeter> meters = new List<IBossMeter>();
        OrderBoard board;
        KitchenChaos chaos;
        float nextScan, startTime;
        string levelTitle;

        GUIStyle box, label, small, big, subtitle, ticket, button;

        void Start()
        {
            startTime = Time.time;
            levelTitle = CampaignProgress.CurrentTitle;
            if (!toolArm && grabber) toolArm = grabber.GetComponent<ToolArm>();
        }

        /// <summary>Things spawn and despawn (dispensed bowls), so re-scan now and then rather than every frame.</summary>
        void Scan()
        {
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 1f;
            indicators.Clear();
            meters.Clear();
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (mb is IProgressIndicator p) indicators.Add(p);
                if (mb is IBossMeter m) meters.Add(m);
            }
            board = FindAnyObjectByType<OrderBoard>();
            chaos = FindAnyObjectByType<KitchenChaos>();
        }

        void BuildStyles()
        {
            if (box != null) return;
            box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 16, padding = new RectOffset(12, 12, 10, 10), wordWrap = true, richText = true };
            label = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, richText = true };
            small = new GUIStyle(label) { fontSize = 13 };
            big = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
            subtitle = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = true, padding = new RectOffset(16, 16, 10, 10) };
            ticket = new GUIStyle(GUI.skin.box) { fontSize = 15, alignment = TextAnchor.UpperCenter, richText = true, wordWrap = true, padding = new RectOffset(6, 6, 6, 6) };
            button = new GUIStyle(GUI.skin.button) { fontSize = 20, fixedHeight = 44 };
        }

        void OnGUI()
        {
            BuildStyles();
            Scan();
            float w = Screen.width, h = Screen.height;
            var gm = GameManager.Instance;

            DrawWorldBars();
            DrawCrosshair(w, h);
            DrawPrompts(w, h);
            DrawTask();
            DrawTool(h);
            DrawOrders(w);
            DrawMeters(w);
            DrawChaos(w, h);

            if (narrator && !string.IsNullOrEmpty(narrator.CurrentText))
                GUI.Box(new Rect(w * 0.2f, h - 140f, w * 0.6f, 70f), "<b>DISPATCH:</b> " + narrator.CurrentText, subtitle);

            if (!string.IsNullOrEmpty(levelTitle) && Time.time - startTime < titleCardTime)
                DrawTitleCard(w, h);

            string failure = Failure();
            if (gm && gm.LevelComplete) DrawComplete(w, h);
            else if (failure != null) DrawFailure(w, h, failure);
            else if (gm && gm.Paused) DrawPause(w, h, gm);
        }

        static bool Alive(object o) => o != null && !(o is Object u && !u);

        string Failure()
        {
            foreach (var m in meters)
                if (Alive(m) && !string.IsNullOrEmpty(m.FailureText)) return m.FailureText;
            return null;
        }

        static void Bar(Rect r, float t, Color c)
        {
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = c;
            GUI.DrawTexture(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(t), r.height - 4), Texture2D.whiteTexture);
            GUI.color = old;
        }

        void DrawWorldBars()
        {
            var cam = Camera.main;
            if (!cam) return;
            foreach (var p in indicators)
            {
                if (!Alive(p) || !p.ShowProgress) continue;
                Vector3 s = cam.WorldToScreenPoint(p.IndicatorPosition);
                if (s.z <= 0f) continue;
                Bar(new Rect(s.x - 40f, Screen.height - s.y - 6f, 80f, 12f), p.Progress01, p.ProgressColor);
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
            var lines = new List<string>();
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
            if (toolArm && toolArm.Current && toolArm.Focus is Component c)
                lines.Add(toolArm.IsUsing
                    ? $"<b>{toolArm.Current.displayName} at work...</b>"
                    : $"[hold F / B] {toolArm.Current.useVerb} the {c.name.ToLowerInvariant()}");
            for (int i = 0; i < lines.Count; i++)
                GUI.Label(new Rect(0, h / 2 + 30f + i * 24f, w, 24f), lines[i], label);
        }

        void DrawTask()
        {
            if (!taskRunner || !taskRunner.task) return;
            var task = taskRunner.task;
            var sb = new StringBuilder();
            sb.Append("<b>").Append(task.title).Append("</b>\n<i>for ").Append(task.rescuee).Append("</i>\n\n");
            foreach (var step in task.steps)
                sb.Append(taskRunner.IsStepDone(step.id) ? "[x] " : "[  ] ").Append(step.label).Append('\n');
            GUI.Box(new Rect(16f, 16f, 320f, 60f + task.steps.Length * 22f), sb.ToString(), box);
        }

        void DrawTool(float h)
        {
            if (!toolArm || !toolArm.Current) return;
            var tools = toolArm.Unlocked;
            float x = 16f, y = h - 70f;
            GUI.Box(new Rect(x, y, 74f + tools.Count * 44f, 54f), GUIContent.none);
            GUI.Label(new Rect(x + 6f, y + 4f, 60f, 46f), "TOOL\n<size=11>Tab / Y</size>", small);
            for (int i = 0; i < tools.Count; i++)
            {
                var t = tools[i];
                bool on = t == toolArm.Current;
                Rect r = new Rect(x + 66f + i * 44f, y + (on ? 4f : 10f), on ? 40f : 34f, on ? 46f : 34f);
                Color old = GUI.color;
                GUI.color = on ? t.color : t.color * 0.6f;
                GUI.DrawTexture(r, Texture2D.whiteTexture);
                GUI.color = old;
                string abbr = t.displayName.Substring(0, Mathf.Min(2, t.displayName.Length));
                GUI.Label(r, $"<b><color=black>{abbr}</color></b>", small);
            }
            var left = new GUIStyle(small) { alignment = TextAnchor.MiddleLeft };
            GUI.Label(new Rect(x, y - 22f, 240f, 20f), "<b>" + toolArm.Current.displayName + "</b>", left);
        }

        void DrawOrders(float w)
        {
            if (!board || !board.isActiveAndEnabled) return;
            const float tw = 150f, th = 92f;
            float x = w - 16f - board.maxActive * (tw + 8f);
            var left = new GUIStyle(label) { alignment = TextAnchor.MiddleLeft };
            GUI.Label(new Rect(x, 14f, 300f, 22f), $"<b>ORDERS</b>  {board.Served}/{board.orders.Length} served", left);
            for (int i = 0; i < board.Active.Count; i++)
            {
                var o = board.Active[i];
                var recipe = o.spec.recipe;
                if (!recipe) continue;
                Rect r = new Rect(x + i * (tw + 8f), 40f, tw, th);
                var sb = new StringBuilder();
                sb.Append("<b>").Append(recipe.dishName).Append("</b>\n<size=12>");
                sb.Append(recipe.method == CookMethod.Whisk ? "whisk: " : "boil: ");
                for (int k = 0; k < recipe.ingredients.Length; k++)
                    sb.Append(k > 0 ? " + " : "").Append(recipe.ingredients[k] ? recipe.ingredients[k].displayName : "?");
                sb.Append("</size>");
                GUI.Box(r, sb.ToString(), ticket);
                Color c = Color.Lerp(new Color(1f, 0.3f, 0.25f), new Color(0.45f, 0.9f, 0.45f), o.Patience01);
                if (o.Patience01 < 0.25f && Mathf.Repeat(Time.time * 4f, 1f) < 0.5f) c = Color.white;
                Bar(new Rect(r.x + 8f, r.yMax - 20f, r.width - 16f, 12f), o.Patience01, c);
            }
        }

        void DrawMeters(float w)
        {
            // The order board's tickets sit top-right; the boss meters stack top-centre.
            float y = 16f;
            foreach (var m in meters)
            {
                if (!Alive(m) || (m is Behaviour b && !b.isActiveAndEnabled)) continue;
                Rect r = new Rect(w / 2 - 180f, y, 360f, 22f);
                GUI.Label(new Rect(r.x, r.y, r.width, 20f), "<b>" + m.MeterTitle + "</b>", label);
                Bar(new Rect(r.x, r.y + 22f, r.width, 18f), m.Meter01, m.MeterColor);
                if (!string.IsNullOrEmpty(m.MeterStatus))
                    GUI.Label(new Rect(r.x - 60f, r.y + 42f, r.width + 120f, 20f), m.MeterStatus, small);
                y += 70f;
            }
        }

        void DrawChaos(float w, float h)
        {
            if (!chaos || string.IsNullOrEmpty(chaos.LastMishap)) return;
            float age = Time.time - chaos.LastMishapTime;
            if (age > 3f) return;
            Color old = GUI.color;
            GUI.color = new Color(1f, 0.4f, 0.1f, 0.25f * (1f - age / 3f));
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(0, h * 0.22f, w, 50f), "<color=#ffb347>" + chaos.LastMishap + "</color>", big);
        }

        void DrawTitleCard(float w, float h)
        {
            float age = Time.time - startTime;
            float a = Mathf.Clamp01(Mathf.Min(age * 2f, (titleCardTime - age) * 2f));
            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(0, h * 0.36f, w, 60f), levelTitle, big);
            GUI.color = old;
        }

        static void Dim(float w, float h, float alpha)
        {
            Color old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, alpha);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = old;
        }

        void DrawComplete(float w, float h)
        {
            Dim(w, h, 0.35f);
            float t = taskRunner ? taskRunner.Elapsed : 0f;
            GUI.Label(new Rect(0, h * 0.3f, w, 60f), "CALL COMPLETE!", big);
            string extra = board ? $"   ·   Hero moments {board.HeroMoments}" : "";
            GUI.Label(new Rect(0, h * 0.3f + 60f, w, 30f), $"Rescue time {(int)t / 60}:{(int)t % 60:00}{extra}", label);
            GUI.Label(new Rect(0, h * 0.3f + 92f, w, 30f), "<b>Enter / A</b>: next call   ·   <b>Backspace / Select</b>: replay", label);
        }

        void DrawFailure(float w, float h, string text)
        {
            Dim(w, h, 0.45f);
            GUI.Label(new Rect(0, h * 0.3f, w, 60f), "<color=#ff6b5b>" + text + "</color>", big);
            GUI.Label(new Rect(0, h * 0.3f + 64f, w, 30f), "<b>Backspace / Select</b> to try again", label);
        }

        void DrawPause(float w, float h, GameManager gm)
        {
            Dim(w, h, 0.5f);
            GUI.Label(new Rect(0, h * 0.22f, w, 60f), "PAUSED", big);
            float bw = 260f, x = w / 2 - bw / 2, y = h * 0.22f + 80f;
            if (GUI.Button(new Rect(x, y, bw, 44f), "Resume", button)) gm.SetPaused(false);
            if (GUI.Button(new Rect(x, y + 54f, bw, 44f), "Restart call", button)) gm.Restart();
            if (CampaignProgress.Campaign && GUI.Button(new Rect(x, y + 108f, bw, 44f), "Main menu", button)) CampaignProgress.LoadMenu();
            GUI.Label(new Rect(0, y + 170f, w, 24f), "Esc / P / Start to resume", small);
        }
    }
}
