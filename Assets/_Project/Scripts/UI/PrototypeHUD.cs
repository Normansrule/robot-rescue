using System.Collections.Generic;
using System.Text;
using RobotRescue.Bots;
using RobotRescue.Campaign;
using RobotRescue.Core;
using RobotRescue.Narrative;
using RobotRescue.Tasks;
using RobotRescue.Tasks.Cooking;
using RobotRescue.World;
using UnityEngine;

namespace RobotRescue.UI
{
    /// <summary>
    /// IMGUI HUD: crosshair, context prompts, task checklist, tool bar, battery and parts, stamina, bolts, order
    /// tickets, boss meters, world progress bars, drone name tags, DISPATCH (with a talking portrait and radio voice),
    /// area title cards and toasts in the open world, pause / win / lose screens.
    /// Looks and motion come from <see cref="UiTheme"/>; the pause menu takes arrows / D-pad and Enter / A via <see cref="MenuFocus"/>.
    /// Replace with a UI Toolkit HUD in the polish milestone.
    /// </summary>
    public class PrototypeHUD : MonoBehaviour
    {
        public TetherCable tether;
        public Grabber grabber;
        public ToolArm toolArm;
        public TaskRunner taskRunner;
        public DispatchNarrator narrator;
        [Tooltip("DISPATCH's radio voice (optional).")]
        public Babble dispatchVoice;

        [Tooltip("Seconds the level title card stays up at the start.")]
        public float titleCardTime = 4f;

        readonly List<IProgressIndicator> indicators = new List<IProgressIndicator>();
        readonly List<IBossMeter> meters = new List<IBossMeter>();
        readonly List<DronePersonality> drones = new List<DronePersonality>();
        OrderBoard board;
        readonly BotWorkshop workshop = new BotWorkshop();
        bool inWorkshop;
        KitchenChaos chaos;
        BotController bot;
        BotBattery battery;
        BotParts parts;
        float nextScan, startTime;
        string levelTitle;
        DispatchNarrator lastSpeaker;
        int lastLine = -1;
        string garbleSource, garbled;

        GUIStyle label, small, caption, left, labelRight, heading, meterTitle, tagStyle, big, body, subtitle, ticketStyle, button, primary;
        readonly MenuFocus pauseFocus = new MenuFocus();
        bool wasPaused;

        static WorldDirector World => WorldDirector.Instance;
        TaskRunner Runner => World && World.Active ? World.Active.runner : taskRunner;

        void Start()
        {
            startTime = Time.time;
            levelTitle = CampaignProgress.CurrentTitle;
            if (!toolArm && grabber) toolArm = grabber.GetComponent<ToolArm>();
            FindBot();
        }

        void FindBot()
        {
            if (bot) return;
            bot = grabber ? grabber.GetComponent<BotController>() : FindAnyObjectByType<BotController>();
            if (!bot) return;
            battery = bot.GetComponent<BotBattery>();
            parts = bot.GetComponent<BotParts>();
            if (!tether) tether = bot.GetComponent<TetherCable>();
            if (!grabber) grabber = bot.GetComponent<Grabber>();
            if (!toolArm) toolArm = bot.GetComponent<ToolArm>();
        }

        /// <summary>Things spawn and despawn (dispensed bowls), so re-scan now and then rather than every frame.</summary>
        void Scan()
        {
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + 1f;
            indicators.Clear();
            meters.Clear();
            drones.Clear();
            foreach (var mb in FindObjectsByType<MonoBehaviour>())
            {
                if (mb is IProgressIndicator p) indicators.Add(p);
                if (mb is IBossMeter m) meters.Add(m);
                if (mb is DronePersonality d) drones.Add(d);
            }
            board = null;
            foreach (var b in FindObjectsByType<OrderBoard>()) if (b.isActiveAndEnabled) board = b;
            chaos = null;
            foreach (var c in FindObjectsByType<KitchenChaos>()) if (c.isActiveAndEnabled) chaos = c;
            FindBot();
        }

        void Update()
        {
            // DISPATCH's radio voice starts with each new line.
            var speaker = Speaker();
            if (speaker && dispatchVoice && (speaker != lastSpeaker || speaker.LineCounter != lastLine))
            {
                if (!string.IsNullOrEmpty(speaker.CurrentText)) dispatchVoice.Say(speaker.CurrentText);
                lastSpeaker = speaker;
                lastLine = speaker.LineCounter;
            }
        }

        DispatchNarrator Speaker()
        {
            if (World && World.Active && World.Active.narrator && !string.IsNullOrEmpty(World.Active.narrator.CurrentText))
                return World.Active.narrator;
            return narrator;
        }


        int styledFor = -1;
        static float cardOpenedAt;
        static int cardFrame = -10;

        void BuildStyles()
        {
            if (label != null && styledFor == UiTheme.Version) return;
            styledFor = UiTheme.Version;
            label = UiTheme.Style(UiTheme.Label, UiTheme.Text, TextAnchor.MiddleCenter);
            small = UiTheme.Style(UiTheme.Caption, UiTheme.Text, TextAnchor.MiddleCenter);
            caption = UiTheme.Style(UiTheme.Caption, UiTheme.TextMuted);
            left = UiTheme.Style(UiTheme.Label, UiTheme.Text);
            labelRight = UiTheme.Style(UiTheme.Label, UiTheme.Text, TextAnchor.MiddleRight);
            heading = UiTheme.Style(UiTheme.Body, UiTheme.Text, bold: true);
            meterTitle = UiTheme.Style(UiTheme.Label, UiTheme.Text, TextAnchor.MiddleCenter, bold: true);
            tagStyle = UiTheme.Style(UiTheme.Caption, UiTheme.Text, TextAnchor.MiddleCenter, bold: true);
            big = UiTheme.Style(UiTheme.Title, UiTheme.Text, TextAnchor.MiddleCenter, bold: true);
            body = UiTheme.Style(UiTheme.Body, UiTheme.Text, TextAnchor.MiddleCenter, wrap: true);
            subtitle = UiTheme.Style(UiTheme.Body + 1, UiTheme.Text, TextAnchor.UpperLeft, wrap: true);
            ticketStyle = UiTheme.Style(UiTheme.Caption, UiTheme.Text, TextAnchor.UpperCenter, wrap: true);
            button = UiTheme.Button(size: UiTheme.Body);
            primary = UiTheme.Button(primary: true, size: UiTheme.Body);
        }

        void OnEnable() => pauseFocus.Enable();
        void OnDisable() => pauseFocus.Disable();
        void OnDestroy() => pauseFocus.Dispose();

        void LateUpdate()
        {
            var gm = GameManager.Instance;
            if (gm && gm.Paused && !inWorkshop) pauseFocus.Tick();
        }

        void OnGUI()
        {
            if (World && World.InTitle) return;
            BuildStyles();
            Scan();
            UiTheme.Begin(out float w, out float h);
            var gm = GameManager.Instance;

            DrawWorldBars();
            DrawNameTags();
            DrawCrosshair(w, h);
            DrawPrompts(w, h);
            DrawTask();
            DrawTool(h);
            DrawBattery(h);
            DrawParts(h);
            DrawStamina(w, h);
            DrawBolts(w, h);
            DrawOrders(w);
            DrawMeters(w);
            DrawChaos(w, h);
            DrawDispatch(w, h);

            if (World) DrawWorldCards(w, h);
            else if (!string.IsNullOrEmpty(levelTitle) && Time.time - startTime < titleCardTime)
                DrawTitleCard(w, h, levelTitle, Time.time - startTime);

            if (battery && battery.IsDead) DrawFlat(w, h);

            string failure = Failure();
            if (gm && gm.LevelComplete) DrawComplete(w, h);
            else if (failure != null) DrawFailure(w, h, failure);
            else if (gm && gm.Paused) DrawPause(w, h, gm);
            else wasPaused = false;
            if (inWorkshop && !(gm && gm.Paused)) { inWorkshop = false; workshop.Close(); }
            UiTheme.End();
        }

        static bool Alive(object o) => o != null && !(o is Object u && !u);

        static bool Live(object o) => Alive(o) && !(o is Behaviour b && !b.isActiveAndEnabled);

        string Failure()
        {
            foreach (var m in meters)
                if (Live(m) && !string.IsNullOrEmpty(m.FailureText)) return m.FailureText;
            return null;
        }

        static void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        /// <summary>A warning colour that breathes between a dimmer and full version of itself, instead of strobing white.</summary>
        static Color Breathe(Color c) => Color.Lerp(c * new Color(0.6f, 0.6f, 0.6f, 1f), c, UiTheme.Pulse());

        void DrawWorldBars()
        {
            var cam = Camera.main;
            if (!cam) return;
            foreach (var p in indicators)
            {
                if (!Live(p) || !p.ShowProgress) continue;
                if (bot && (p.IndicatorPosition - bot.transform.position).sqrMagnitude > 900f) continue;
                Vector3 s = UiTheme.WorldToCanvas(cam, p.IndicatorPosition);
                if (s.z <= 0f) continue;
                UiTheme.Bar(new Rect(s.x - 40f, s.y - 6f, 80f, 12f), p.Progress01, p.ProgressColor);
            }
        }

        void DrawNameTags()
        {
            var cam = Camera.main;
            if (!cam || !bot) return;
            foreach (var d in drones)
            {
                if (!Alive(d) || !d.isActiveAndEnabled) continue;
                Vector3 pos = d.transform.position + Vector3.up * 0.95f;
                float dist = Vector3.Distance(pos, bot.transform.position);
                if (dist > 6f) continue;
                Vector3 s = UiTheme.WorldToCanvas(cam, pos);
                if (s.z <= 0f) continue;
                // Tags fade in as you walk up rather than popping.
                float a = Mathf.Clamp01((6f - dist) / 2f);
                Color old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, a);
                var drone = d.GetComponent<HelperDrone>();
                string extra = drone && drone.Charge01 < 0.99f ? $"  <color={UiTheme.MutedHex}>{Mathf.RoundToInt(drone.Charge01 * 100f)}%</color>" : "";
                string share = drone && drone.IsSharing ? $"\n<size=12><color={UiTheme.GoodHex}>sharing charge with {BotLookStore.PlayerName}</color></size>" : "";
                UiTheme.Shadowed(new Rect(s.x - 120f, s.y - 30f, 240f, 46f), $"{d.droneName} <size=12><color={UiTheme.MutedHex}>{d.Description}</color></size>{extra}{share}", tagStyle);
                GUI.color = old;
            }
        }

        void DrawCrosshair(float w, float h)
        {
            bool onTarget = tether && tether.AimTarget;
            // A soft dot that grows into a cyan ring when the Tether has something to hook.
            if (onTarget)
            {
                UiTheme.Outline(new Rect(w / 2 - 9f, h / 2 - 9f, 18f, 18f), UiTheme.Signal, 2f, 9f);
                UiTheme.Panel(new Rect(w / 2 - 2f, h / 2 - 2f, 4f, 4f), UiTheme.Signal, 2f);
            }
            else
            {
                UiTheme.Panel(new Rect(w / 2 - 4f, h / 2 - 4f, 8f, 8f), new Color(0f, 0f, 0f, 0.35f), 4f);
                UiTheme.Panel(new Rect(w / 2 - 3f, h / 2 - 3f, 6f, 6f), new Color(1f, 1f, 1f, 0.8f), 3f);
            }
        }

        void DrawPrompts(float w, float h)
        {
            var lines = new List<string>();
            if (bot && bot.IsClimbing) lines.Add("[WASD] climb   [Space / A] leap up   [S] climb down");
            if (tether)
            {
                if (tether.IsSwinging) lines.Add("[Q / RB] reel in   [Z / LB] reel out   [Space / A] let go   [RMB / LT] release");
                else if (tether.IsAttached) lines.Add("[RMB / LT] unplug cable");
                else if (tether.AimTarget) lines.Add($"[RMB / LT] {tether.AimTarget.PromptVerb}");
            }
            if (grabber && grabber.isActiveAndEnabled)
            {
                if (grabber.Held) lines.Add($"[LMB / RT] throw {grabber.Held.displayName}   [E / X] put down");
                else if (grabber.Focus) lines.Add($"[E / X] pick up {grabber.Focus.displayName}");
            }
            if (toolArm && toolArm.isActiveAndEnabled && toolArm.Current && toolArm.Focus is Component c)
                lines.Add(toolArm.IsUsing
                    ? $"<b>{toolArm.Current.displayName} at work…</b>"
                    : $"[hold F / B] {toolArm.Current.useVerb} the {c.name.ToLowerInvariant()}");
            // Each prompt sits on its own small pill so it reads over snow, sky or a bright kitchen.
            for (int i = 0; i < lines.Count; i++)
            {
                string text = UiTheme.Keys(lines[i]);
                float tw = label.CalcSize(new GUIContent(text)).x + 28f;
                Rect r = new Rect(w / 2 - tw / 2, h / 2 + 34f + i * 34f, tw, 28f);
                UiTheme.Panel(r, UiTheme.Ink, 14f);
                GUI.Label(r, text, label);
            }
        }

        void DrawTask()
        {
            var runner = Runner;
            if (!runner || !runner.task) return;
            if (World && World.Active && World.Active.IsComplete) return;
            var task = runner.task;
            const float x = UiTheme.Margin, y = UiTheme.Margin, pw = 340f, row = 26f;
            float ph = 72f + task.steps.Length * row;
            UiTheme.Panel(new Rect(x, y, pw, ph), UiTheme.Ink);
            GUI.Label(new Rect(x + UiTheme.Pad, y + 12f, pw - 32f, 24f), task.title, heading);
            GUI.Label(new Rect(x + UiTheme.Pad, y + 36f, pw - 32f, 18f), "for " + task.rescuee, caption);
            float sy = y + 62f;
            foreach (var step in task.steps)
            {
                bool done = runner.IsStepDone(step.id);
                Rect box = new Rect(x + UiTheme.Pad, sy + 5f, 16f, 16f);
                if (done) UiTheme.Panel(box, UiTheme.Good, 5f);
                else UiTheme.Outline(box, UiTheme.TextMuted, 2f, 5f);
                // Finished steps step back so the next job is the brightest line on the card.
                GUI.Label(new Rect(x + UiTheme.Pad + 26f, sy, pw - 58f, row), done ? $"<color={UiTheme.MutedHex}>{step.label}</color>" : step.label, left);
                sy += row;
            }
        }

        void DrawTool(float h)
        {
            if (!toolArm || !toolArm.Current) return;
            bool armOk = toolArm.isActiveAndEnabled;
            var tools = toolArm.Unlocked;
            float x = UiTheme.Margin, y = h - 82f;
            UiTheme.Panel(new Rect(x, y, 84f + tools.Count * 46f, 58f), UiTheme.Ink);
            GUI.Label(new Rect(x + 12f, y + 8f, 64f, 20f), "Tool", caption);
            GUI.Label(new Rect(x + 12f, y + 28f, 64f, 20f), $"<b><color={UiTheme.SignalHex}>Tab / Y</color></b>", caption);
            for (int i = 0; i < tools.Count; i++)
            {
                var t = tools[i];
                bool on = t == toolArm.Current;
                Rect r = new Rect(x + 76f + i * 46f, y + (on ? 7f : 12f), on ? 40f : 34f, on ? 44f : 34f);
                // Tool heads keep their own colours (DESIGN.md), the current one is full size with a cyan ring.
                UiTheme.Panel(r, armOk ? (on ? t.color : t.color * new Color(0.6f, 0.6f, 0.6f, 1f)) : new Color(0.4f, 0.4f, 0.4f), 8f);
                if (on && armOk) UiTheme.Outline(new Rect(r.x - 3f, r.y - 3f, r.width + 6f, r.height + 6f), UiTheme.Signal, 2f, 10f);
                string abbr = t.displayName.Substring(0, Mathf.Min(2, t.displayName.Length));
                GUI.Label(r, $"<b><color=#141414>{abbr}</color></b>", small);
            }
            UiTheme.Shadowed(new Rect(x + 2f, y - 26f, 280f, 22f),
                armOk ? "<b>" + toolArm.Current.displayName + "</b>" : $"<b><color={UiTheme.DangerHex}>Tool arm missing</color></b>  <size=13>find it or visit a service bay</size>", left);
        }

        void DrawBattery(float h)
        {
            if (!battery) return;
            float x = UiTheme.Margin, y = h - 140f;
            float c = battery.Charge01;
            // The fill keeps its bright colours in both modes: it sits behind dark text.
            Color col = c > 0.5f ? new Color(0.42f, 0.89f, 0.56f) : c > battery.lowThreshold ? new Color(1f, 0.79f, 0.3f) : new Color(1f, 0.48f, 0.42f);
            if (battery.IsCritical && !battery.IsCharging) col = Breathe(col);

            UiTheme.Panel(new Rect(x, y, 104f, 38f), UiTheme.Ink, 10f);
            UiTheme.Panel(new Rect(x + 104f, y + 12f, 6f, 14f), UiTheme.Ink, 3f);
            if (c > 0f) UiTheme.Panel(new Rect(x + 4f, y + 4f, Mathf.Max(10f, 96f * c), 30f), col, 7f);
            if (battery.IsCharging)
            {
                // Live Wire: a lead plugs into the battery's nub and current runs along it into the cell.
                UiTheme.Panel(new Rect(x + 110f, y + 17.5f, 30f, 3f), UiTheme.Signal, 1.5f);
                float run = UiTheme.CalmMotion ? 0.5f : 1f - Mathf.Repeat(Time.unscaledTime * 1.6f, 1f);
                UiTheme.Panel(new Rect(x + 110f + 26f * run, y + 15f, 8f, 8f), Color.white, 4f);
                UiTheme.Outline(new Rect(x - 2f, y - 2f, 108f, 42f), UiTheme.Signal, 2f, 11f);
            }
            GUI.Label(new Rect(x, y, 104f, 38f), $"<b><color=#141414>{Mathf.CeilToInt(c * 100f)}%</color></b>", label);
            string status = battery.IsCharging ? $"<color={UiTheme.GoodHex}><b>Charging</b></color>  <color={UiTheme.MutedHex}>{battery.ChargingFrom}</color>"
                : battery.OnReserve ? $"<color={UiTheme.WarnHex}><b>On reserve</b></color>  walk to a charger · tow in {Mathf.CeilToInt(battery.ReserveLeft)}s"
                : battery.IsCritical ? $"<color={UiTheme.DangerHex}><b>Find a charger</b></color>"
                : battery.IsLow ? $"<color={UiTheme.WarnHex}><b>Battery low</b></color>"
                : $"<color={UiTheme.MutedHex}>Battery</color>";
            UiTheme.Shadowed(new Rect(x + 122f, y, 360f, 38f), status, left);
        }

        void DrawParts(float h)
        {
            if (!parts) return;
            float x = UiTheme.Margin, y = h - 230f;
            Color ok = new Color(UiTheme.Text.r, UiTheme.Text.g, UiTheme.Text.b, 0.9f), lost = new Color(UiTheme.Danger.r, UiTheme.Danger.g, UiTheme.Danger.b, 0.5f);
            bool any = parts.MissingCount > 0;
            if (!any && !(bot && bot.IsClimbing)) return;
            UiTheme.Panel(new Rect(x, y, 170f, 78f), UiTheme.Ink);
            float cx = x + 38f;
            Fill(new Rect(cx - 2f, y + 8f, 4f, 10f), parts.Has(BotPart.Antenna) ? ok : lost);
            UiTheme.Panel(new Rect(cx - 14f, y + 18f, 28f, 18f), ok, 5f);
            UiTheme.Panel(new Rect(cx - 11f, y + 38f, 22f, 20f), ok, 4f);
            UiTheme.Panel(new Rect(cx - 21f, y + 39f, 7f, 16f), parts.Has(BotPart.ArmR) ? ok : lost, 3f);
            UiTheme.Panel(new Rect(cx + 14f, y + 39f, 7f, 16f), parts.Has(BotPart.ArmL) ? ok : lost, 3f);
            UiTheme.Panel(new Rect(cx - 12f, y + 60f, 9f, 8f), parts.Has(BotPart.FootR) ? ok : lost, 3f);
            UiTheme.Panel(new Rect(cx + 3f, y + 60f, 9f, 8f), parts.Has(BotPart.FootL) ? ok : lost, 3f);
            var sb = new StringBuilder(any ? $"<color={UiTheme.DangerHex}><b>Lost</b></color>\n" : "");
            for (int i = 0; i < 5; i++)
                if (!parts.Has((BotPart)i)) sb.Append("<size=13>").Append(BotParts.PartName((BotPart)i)).Append("</size>\n");
            GUI.Label(new Rect(x + 72f, y + 6f, 94f, 66f), sb.ToString(), left);
        }

        void DrawStamina(float w, float h)
        {
            if (!bot || (!bot.IsClimbing && bot.Stamina01 > 0.995f)) return;
            float s = bot.Stamina01;
            Color c = s > 0.25f ? UiTheme.Warn : Breathe(UiTheme.Danger);
            UiTheme.Bar(new Rect(w / 2 - 130f, h - 52f, 260f, 16f), s, c);
            UiTheme.Shadowed(new Rect(w / 2 - 130f, h - 76f, 260f, 20f), "<b>Grip</b>", small);
        }

        void DrawBolts(float w, float h)
        {
            if (Collectible.Total == 0) return;
            Rect r = new Rect(w - UiTheme.Margin - 176f, h - UiTheme.Margin - 40f, 176f, 40f);
            UiTheme.Panel(r, UiTheme.Ink);
            GUI.Label(new Rect(r.x + 14f, r.y, 80f, r.height), $"<color={UiTheme.WarnHex}><b>Bolts</b></color>", left);
            GUI.Label(new Rect(r.x, r.y, r.width - 14f, r.height), $"<b>{Collectible.Count}</b> <color={UiTheme.MutedHex}>/ {Collectible.Total}</color>", labelRight);
        }

        void DrawOrders(float w)
        {
            if (!board || !board.isActiveAndEnabled) return;
            const float tw = 156f, th = 98f;
            float x = w - UiTheme.Margin - board.maxActive * (tw + UiTheme.Gap) + UiTheme.Gap;
            UiTheme.Shadowed(new Rect(x, UiTheme.Margin - 4f, 320f, 24f), $"<b>Orders</b>  <color={UiTheme.MutedHex}>{board.Served} of {board.orders.Length} served</color>", left);
            for (int i = 0; i < board.Active.Count; i++)
            {
                var o = board.Active[i];
                var recipe = o.spec.recipe;
                if (!recipe) continue;
                Rect r = new Rect(x + i * (tw + UiTheme.Gap), UiTheme.Margin + 26f, tw, th);
                var sb = new StringBuilder();
                sb.Append("<b>").Append(recipe.dishName).Append("</b>\n<color=").Append(UiTheme.MutedHex).Append(">");
                sb.Append(recipe.method == CookMethod.Whisk ? "whisk: " : "boil: ");
                for (int k = 0; k < recipe.ingredients.Length; k++)
                    sb.Append(k > 0 ? " + " : "").Append(recipe.ingredients[k] ? recipe.ingredients[k].displayName : "?");
                sb.Append("</color>");
                UiTheme.Panel(r, UiTheme.Ink);
                GUI.Label(new Rect(r.x + 8f, r.y + 8f, r.width - 16f, r.height - 30f), sb.ToString(), ticketStyle);
                Color c = Color.Lerp(UiTheme.Danger, UiTheme.Good, o.Patience01);
                if (o.Patience01 < 0.25f) c = Breathe(c);
                UiTheme.Bar(new Rect(r.x + 10f, r.yMax - 20f, r.width - 20f, 10f), o.Patience01, c);
            }
        }

        void DrawMeters(float w)
        {
            // The order board's tickets sit top-right; the boss meters stack top-centre.
            float y = UiTheme.Margin;
            foreach (var m in meters)
            {
                if (!Live(m)) continue;
                Rect r = new Rect(w / 2 - 190f, y, 380f, 22f);
                UiTheme.Shadowed(new Rect(r.x, r.y, r.width, 22f), m.MeterTitle, meterTitle);
                UiTheme.Bar(new Rect(r.x, r.y + 26f, r.width, 18f), m.Meter01, m.MeterColor);
                if (!string.IsNullOrEmpty(m.MeterStatus))
                    UiTheme.Shadowed(new Rect(r.x - 60f, r.y + 48f, r.width + 120f, 20f), m.MeterStatus, small);
                y += 76f;
            }
        }

        void DrawChaos(float w, float h)
        {
            if (!chaos || string.IsNullOrEmpty(chaos.LastMishap)) return;
            float age = Time.time - chaos.LastMishapTime;
            if (age > 3f) return;
            // A brief warm tint says "something just went wrong in the kitchen"; calm motion keeps only the words.
            if (!UiTheme.CalmMotion) Fill(new Rect(0, 0, w, h), new Color(1f, 0.45f, 0.1f, 0.16f * (1f - age / 3f)));
            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, UiTheme.CardAlpha(age, 3f, 0.2f, 0.5f));
            UiTheme.Shadowed(new Rect(0, h * 0.22f + UiTheme.Rise(age, 0.2f, 10f), w, 56f), $"<color={UiTheme.WarnHex}>{chaos.LastMishap}</color>", big);
            GUI.color = old;
        }

        void DrawDispatch(float w, float h)
        {
            var speaker = Speaker();
            if (!speaker || string.IsNullOrEmpty(speaker.CurrentText)) return;
            string text = speaker.CurrentText;
            if (parts && !parts.Has(BotPart.Antenna)) text = Garble(text);

            float pw = Mathf.Min(860f, w - 2f * UiTheme.Margin);
            Rect r = new Rect(w / 2 - pw / 2, h - 176f, pw, 104f);
            UiTheme.Panel(r, UiTheme.Ink);

            // A little monitor face that talks along with the radio voice.
            Rect p = new Rect(r.x + 12f, r.y + 12f, 80f, 80f);
            UiTheme.Panel(p, new Color(0.15f, 0.17f, 0.3f, 1f), 10f);
            UiTheme.Panel(new Rect(p.x + 5f, p.y + 5f, 70f, 70f), new Color(0.04f, 0.06f, 0.14f, 1f), 7f);
            bool blink = Mathf.Repeat(Time.time, 3.7f) < 0.12f;
            Color eye = new Color(1f, 0.75f, 0.3f);
            UiTheme.Panel(new Rect(p.x + 20f, p.y + (blink ? 30f : 22f), 12f, blink ? 3f : 16f), eye, blink ? 1f : 4f);
            UiTheme.Panel(new Rect(p.x + 48f, p.y + (blink ? 30f : 22f), 12f, blink ? 3f : 16f), eye, blink ? 1f : 4f);
            bool talking = dispatchVoice ? dispatchVoice.IsTalking : true;
            float mouth = talking ? 3f + Mathf.Abs(Mathf.Sin(Time.time * 18f)) * 10f : 3f;
            UiTheme.Panel(new Rect(p.x + 28f, p.y + 52f - mouth * 0.5f, 24f, mouth), eye, 1.5f);

            // Speaker name on its own line, so the words themselves start clean.
            float tx = p.xMax + 16f, tw = r.xMax - tx - UiTheme.Pad;
            GUI.Label(new Rect(tx, r.y + 10f, tw, 20f), $"<b><color={UiTheme.AccentHex}>DISPATCH</color></b>", caption);
            GUI.Label(new Rect(tx, r.y + 32f, tw, r.height - 40f), text, subtitle);
        }

        string Garble(string text)
        {
            if (text == garbleSource) return garbled;
            garbleSource = text;
            const string noise = "#%&*~?/";
            var sb = new StringBuilder(text.Length);
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                bool scramble = ch != ' ' && ((i * 7919 + text.Length * 31) % 100) < 22;
                sb.Append(scramble ? noise[(i * 13) % noise.Length] : ch);
            }
            garbled = sb.Append($"  <size=13><color={UiTheme.MutedHex}>(antenna missing: bad signal)</color></size>").ToString();
            return garbled;
        }

        void DrawWorldCards(float w, float h)
        {
            var area = World.Active;
            float age = Time.time - World.EnteredTime;
            if (area && age < titleCardTime)
            {
                string t = $"{area.chapterTitle}  ·  {area.DisplayTitle}";
                if (area.IsComplete) t += $"  <color={UiTheme.GoodHex}>solved</color>";
                DrawTitleCard(w, h, t, age);
            }
            float toastAge = Time.time - World.ToastTime;
            if (!string.IsNullOrEmpty(World.Toast) && toastAge < 4f)
            {
                Color old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, UiTheme.CardAlpha(toastAge, 4f));
                UiTheme.Shadowed(new Rect(0, h * 0.18f + UiTheme.Rise(toastAge), w, 60f), $"<color={UiTheme.WarnHex}>{World.Toast}</color>", big);
                GUI.color = old;
            }
        }

        void DrawTitleCard(float w, float h, string text, float age)
        {
            // Rises a few pixels into place as it fades in, then leaves a little faster than it came.
            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, UiTheme.CardAlpha(age, titleCardTime, 0.45f, 0.35f));
            UiTheme.Shadowed(new Rect(0, h * 0.36f + UiTheme.Rise(age, 0.45f), w, 60f), text, big);
            GUI.color = old;
        }

        static void Dim(float w, float h, float alpha) => Fill(new Rect(0, 0, w, h), new Color(0.02f, 0.03f, 0.08f, alpha));

        /// <summary>A centred card for the flat / complete / failed / paused screens.</summary>
        static Rect Card(float w, float h, float cw, float ch)
        {
            var r = new Rect(w / 2 - cw / 2, h * 0.42f - ch / 2, cw, ch);
            // A card that wasn't showing a moment ago has just opened: it powers on along its cable.
            if (Time.frameCount - cardFrame > 2) cardOpenedAt = Time.unscaledTime;
            cardFrame = Time.frameCount;
            UiTheme.Panel(r, UiTheme.Ink, 18f);
            UiTheme.PowerOn(r, Time.unscaledTime - cardOpenedAt, 18f);
            return r;
        }

        void DrawFlat(float w, float h)
        {
            Dim(w, h, 0.4f);
            var r = Card(w, h, 560f, 150f);
            GUI.Label(new Rect(r.x, r.y + 26f, r.width, 54f), $"<color={UiTheme.DangerHex}>Battery flat</color>", big);
            GUI.Label(new Rect(r.x + 24f, r.y + 88f, r.width - 48f, 40f), "The Bureau tow truck is on its way. Back to the last charger.", body);
        }

        void DrawComplete(float w, float h)
        {
            Dim(w, h, 0.35f);
            var r = Card(w, h, 600f, 190f);
            var runner = Runner;
            float t = runner ? runner.Elapsed : 0f;
            GUI.Label(new Rect(r.x, r.y + 26f, r.width, 54f), $"<color={UiTheme.GoodHex}>Call complete</color>", big);
            string extra = board ? $"   ·   Hero moments {board.HeroMoments}" : "";
            GUI.Label(new Rect(r.x, r.y + 86f, r.width, 28f), $"Rescue time <b>{(int)t / 60}:{(int)t % 60:00}</b>{extra}", body);
            GUI.Label(new Rect(r.x, r.y + 132f, r.width, 28f), UiTheme.Keys("[Enter / A] next call      [Backspace / Select] replay"), label);
        }

        void DrawFailure(float w, float h, string text)
        {
            Dim(w, h, 0.45f);
            var r = Card(w, h, 640f, 160f);
            GUI.Label(new Rect(r.x + 20f, r.y + 26f, r.width - 40f, 54f), $"<color={UiTheme.DangerHex}>{text}</color>", big);
            GUI.Label(new Rect(r.x, r.y + 100f, r.width, 28f), UiTheme.Keys("[Backspace / Select] try again"), label);
        }

        void DrawPause(float w, float h, GameManager gm)
        {
            if (!wasPaused) { pauseFocus.Reset(); wasPaused = true; }
            if (inWorkshop)
            {
                // Barely dimmed so you can watch your bot change.
                Dim(w, h, 0.1f);
                if (workshop.Draw(w, h)) inWorkshop = false;
                return;
            }
            Dim(w, h, 0.5f);
            bool menu = World || CampaignProgress.Campaign;
            float bw = 300f, bh = UiTheme.ButtonHeight, step = bh + 14f;
            int count = menu ? 4 : 3;
            var r = Card(w, h, bw + 80f, 110f + count * step + 30f);
            GUI.Label(new Rect(r.x, r.y + 22f, r.width, 54f), "Paused", big);
            float x = r.x + 40f, y = r.y + 96f;
            pauseFocus.BeginGUI();
            if (pauseFocus.Button(new Rect(x, y, bw, bh), "Resume", primary)) gm.SetPaused(false);
            if (pauseFocus.Button(new Rect(x, y += step, bw, bh), "Restart call", button)) gm.Restart();
            if (menu && pauseFocus.Button(new Rect(x, y += step, bw, bh), World ? "Title screen" : "Main menu", button)) gm.QuitToMenu();
            if (pauseFocus.Button(new Rect(x, y += step, bw, bh), "Customize bots", button)) { workshop.Open(); inWorkshop = true; }
            GUI.Label(new Rect(r.x, r.yMax - 40f, r.width, 24f), UiTheme.Keys("[Esc / P / Start] resume"), small);
        }
    }
}
