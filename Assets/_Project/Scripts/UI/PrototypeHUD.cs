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

        GUIStyle box, label, small, big, subtitle, ticket, button, left, tagStyle, labelRight, labelLeft;

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

        void BuildStyles()
        {
            if (box != null) return;
            box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 16, padding = new RectOffset(12, 12, 10, 10), wordWrap = true, richText = true };
            label = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, richText = true };
            small = new GUIStyle(label) { fontSize = 13 };
            left = new GUIStyle(small) { alignment = TextAnchor.MiddleLeft };
            tagStyle = new GUIStyle(small) { fontSize = 14, fontStyle = FontStyle.Bold };
            labelRight = new GUIStyle(label) { alignment = TextAnchor.MiddleRight };
            labelLeft = new GUIStyle(label) { alignment = TextAnchor.MiddleLeft };
            big = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
            subtitle = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.MiddleLeft, wordWrap = true, richText = true, padding = new RectOffset(16, 16, 10, 10) };
            ticket = new GUIStyle(GUI.skin.box) { fontSize = 15, alignment = TextAnchor.UpperCenter, richText = true, wordWrap = true, padding = new RectOffset(6, 6, 6, 6) };
            button = new GUIStyle(GUI.skin.button) { fontSize = 20, fixedHeight = 44 };
        }

        void OnGUI()
        {
            if (World && World.InTitle) return;
            BuildStyles();
            Scan();
            float w = Screen.width, h = Screen.height;
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
            if (inWorkshop && !(gm && gm.Paused)) { inWorkshop = false; workshop.Close(); }
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

        static void Bar(Rect r, float t, Color c)
        {
            Fill(r, new Color(0f, 0f, 0f, 0.55f));
            Fill(new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(t), r.height - 4), c);
        }

        void DrawWorldBars()
        {
            var cam = Camera.main;
            if (!cam) return;
            foreach (var p in indicators)
            {
                if (!Live(p) || !p.ShowProgress) continue;
                if (bot && (p.IndicatorPosition - bot.transform.position).sqrMagnitude > 900f) continue;
                Vector3 s = cam.WorldToScreenPoint(p.IndicatorPosition);
                if (s.z <= 0f) continue;
                Bar(new Rect(s.x - 40f, Screen.height - s.y - 6f, 80f, 12f), p.Progress01, p.ProgressColor);
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
                Vector3 s = cam.WorldToScreenPoint(pos);
                if (s.z <= 0f) continue;
                float a = Mathf.Clamp01((6f - dist) / 2f);
                Color old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, a);
                var drone = d.GetComponent<HelperDrone>();
                string extra = drone && drone.Charge01 < 0.99f ? $"  <size=11>{Mathf.RoundToInt(drone.Charge01 * 100f)}%</size>" : "";
                string share = drone && drone.IsSharing ? "\n<size=11><color=#7dff9a>sharing charge with " + BotLookStore.PlayerName + "</color></size>" : "";
                GUI.Label(new Rect(s.x - 110f, Screen.height - s.y - 28f, 220f, 44f), $"{d.droneName} <size=11><i>{d.Description}</i></size>{extra}{share}", tagStyle);
                GUI.color = old;
            }
        }

        void DrawCrosshair(float w, float h)
        {
            bool onTarget = tether && tether.AimTarget;
            Color c = onTarget ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 1f, 1f, 0.7f);
            float s = onTarget ? 12f : 6f;
            Fill(new Rect(w / 2 - s / 2, h / 2 - s / 2, s, s), c);
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
                    ? $"<b>{toolArm.Current.displayName} at work...</b>"
                    : $"[hold F / B] {toolArm.Current.useVerb} the {c.name.ToLowerInvariant()}");
            for (int i = 0; i < lines.Count; i++)
                GUI.Label(new Rect(0, h / 2 + 30f + i * 24f, w, 24f), lines[i], label);
        }

        void DrawTask()
        {
            var runner = Runner;
            if (!runner || !runner.task) return;
            if (World && World.Active && World.Active.IsComplete) return;
            var task = runner.task;
            var sb = new StringBuilder();
            sb.Append("<b>").Append(task.title).Append("</b>\n<i>for ").Append(task.rescuee).Append("</i>\n\n");
            foreach (var step in task.steps)
                sb.Append(runner.IsStepDone(step.id) ? "<color=#7dff9a>[x]</color> " : "[  ] ").Append(step.label).Append('\n');
            GUI.Box(new Rect(16f, 16f, 320f, 60f + task.steps.Length * 22f), sb.ToString(), box);
        }

        void DrawTool(float h)
        {
            if (!toolArm || !toolArm.Current) return;
            bool armOk = toolArm.isActiveAndEnabled;
            var tools = toolArm.Unlocked;
            float x = 16f, y = h - 70f;
            GUI.Box(new Rect(x, y, 74f + tools.Count * 44f, 54f), GUIContent.none);
            GUI.Label(new Rect(x + 6f, y + 4f, 60f, 46f), "TOOL\n<size=11>Tab / Y</size>", small);
            for (int i = 0; i < tools.Count; i++)
            {
                var t = tools[i];
                bool on = t == toolArm.Current;
                Rect r = new Rect(x + 66f + i * 44f, y + (on ? 4f : 10f), on ? 40f : 34f, on ? 46f : 34f);
                Fill(r, armOk ? (on ? t.color : t.color * 0.6f) : new Color(0.4f, 0.4f, 0.4f));
                string abbr = t.displayName.Substring(0, Mathf.Min(2, t.displayName.Length));
                GUI.Label(r, $"<b><color=black>{abbr}</color></b>", small);
            }
            GUI.Label(new Rect(x, y - 22f, 260f, 20f), armOk ? "<b>" + toolArm.Current.displayName + "</b>" : "<b><color=#ff7a6b>Tool arm missing!</color></b>", left);
        }

        void DrawBattery(float h)
        {
            if (!battery) return;
            float x = 16f, y = h - 128f;
            float c = battery.Charge01;
            bool flash = battery.IsCritical && !battery.IsCharging && Mathf.Repeat(Time.time * 3f, 1f) < 0.5f;
            Color col = c > 0.5f ? new Color(0.35f, 0.95f, 0.5f) : c > battery.lowThreshold ? new Color(1f, 0.85f, 0.25f) : new Color(1f, 0.35f, 0.3f);
            if (flash) col = Color.white;

            Fill(new Rect(x, y, 96f, 34f), new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(x + 96f, y + 10f, 6f, 14f), new Color(0f, 0f, 0f, 0.6f));
            Fill(new Rect(x + 4f, y + 4f, 88f * c, 26f), col);
            GUI.Label(new Rect(x, y, 96f, 34f), $"<b><color=black>{Mathf.CeilToInt(c * 100f)}%</color></b>", label);
            string status = battery.IsCharging ? $"<color=#7dff9a><b>Charging</b></color> <size=11>({battery.ChargingFrom})</size>"
                : battery.OnReserve ? $"<color=#ffd34d><b>RESERVE</b></color> <size=11>walk to a charger · tow in {Mathf.CeilToInt(battery.ReserveLeft)}s</size>"
                : battery.IsCritical ? "<color=#ff7a6b><b>FIND A CHARGER!</b></color>"
                : battery.IsLow ? "<color=#ffd34d>Battery low</color>" : "<size=11>BATTERY</size>";
            GUI.Label(new Rect(x + 110f, y, 320f, 34f), status, left);
        }

        void DrawParts(float h)
        {
            if (!parts) return;
            float x = 16f, y = h - 210f;
            Color ok = new Color(0.92f, 0.94f, 1f, 0.9f), lost = new Color(1f, 0.3f, 0.25f, 0.45f);
            bool any = parts.MissingCount > 0;
            if (!any && !(bot && bot.IsClimbing)) return;
            Fill(new Rect(x, y, 150f, 74f), new Color(0f, 0f, 0f, 0.45f));
            float cx = x + 36f;
            Fill(new Rect(cx - 2f, y + 6f, 4f, 10f), parts.Has(BotPart.Antenna) ? ok : lost);
            Fill(new Rect(cx - 14f, y + 16f, 28f, 18f), ok);
            Fill(new Rect(cx - 11f, y + 36f, 22f, 20f), ok);
            Fill(new Rect(cx - 21f, y + 37f, 7f, 16f), parts.Has(BotPart.ArmR) ? ok : lost);
            Fill(new Rect(cx + 14f, y + 37f, 7f, 16f), parts.Has(BotPart.ArmL) ? ok : lost);
            Fill(new Rect(cx - 12f, y + 58f, 9f, 8f), parts.Has(BotPart.FootR) ? ok : lost);
            Fill(new Rect(cx + 3f, y + 58f, 9f, 8f), parts.Has(BotPart.FootL) ? ok : lost);
            var sb = new StringBuilder(any ? "<color=#ff7a6b><b>Lost:</b></color>\n" : "");
            for (int i = 0; i < 5; i++)
                if (!parts.Has((BotPart)i)) sb.Append("<size=11>").Append(BotParts.PartName((BotPart)i)).Append("</size>\n");
            GUI.Label(new Rect(x + 66f, y + 4f, 84f, 66f), sb.ToString(), left);
        }

        void DrawStamina(float w, float h)
        {
            if (!bot || (!bot.IsClimbing && bot.Stamina01 > 0.995f)) return;
            float s = bot.Stamina01;
            Color c = s > 0.25f ? new Color(1f, 0.85f, 0.3f) : (Mathf.Repeat(Time.time * 4f, 1f) < 0.5f ? new Color(1f, 0.3f, 0.25f) : Color.white);
            Bar(new Rect(w / 2 - 120f, h - 46f, 240f, 16f), s, c);
            GUI.Label(new Rect(w / 2 - 120f, h - 68f, 240f, 20f), "<b>STAMINA</b>", small);
        }

        void DrawBolts(float w, float h)
        {
            if (Collectible.Total == 0) return;
            GUI.Label(new Rect(w - 220f, h - 40f, 200f, 28f), $"<b><color=#ffd34d>BOLTS</color></b>  {Collectible.Count} / {Collectible.Total}", labelRight);
        }

        void DrawOrders(float w)
        {
            if (!board || !board.isActiveAndEnabled) return;
            const float tw = 150f, th = 92f;
            float x = w - 16f - board.maxActive * (tw + 8f);
            GUI.Label(new Rect(x, 14f, 300f, 22f), $"<b>ORDERS</b>  {board.Served}/{board.orders.Length} served", labelLeft);
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
                if (!Live(m)) continue;
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
            Fill(new Rect(0, 0, w, h), new Color(1f, 0.4f, 0.1f, 0.25f * (1f - age / 3f)));
            GUI.Label(new Rect(0, h * 0.22f, w, 50f), "<color=#ffb347>" + chaos.LastMishap + "</color>", big);
        }

        void DrawDispatch(float w, float h)
        {
            var speaker = Speaker();
            if (!speaker || string.IsNullOrEmpty(speaker.CurrentText)) return;
            string text = speaker.CurrentText;
            if (parts && !parts.Has(BotPart.Antenna)) text = Garble(text);

            Rect r = new Rect(w * 0.2f + 86f, h - 150f, w * 0.6f - 86f, 80f);
            GUI.Box(r, "<b>DISPATCH:</b> " + text, subtitle);

            // A little monitor face that talks along with the radio voice.
            Rect p = new Rect(w * 0.2f, h - 150f, 80f, 80f);
            Fill(p, new Color(0.15f, 0.17f, 0.3f, 0.95f));
            Fill(new Rect(p.x + 5f, p.y + 5f, 70f, 70f), new Color(0.05f, 0.07f, 0.15f, 1f));
            bool blink = Mathf.Repeat(Time.time, 3.7f) < 0.12f;
            Color eye = new Color(1f, 0.75f, 0.3f);
            Fill(new Rect(p.x + 20f, p.y + (blink ? 30f : 22f), 12f, blink ? 3f : 16f), eye);
            Fill(new Rect(p.x + 48f, p.y + (blink ? 30f : 22f), 12f, blink ? 3f : 16f), eye);
            bool talking = dispatchVoice ? dispatchVoice.IsTalking : true;
            float mouth = talking ? 3f + Mathf.Abs(Mathf.Sin(Time.time * 18f)) * 10f : 3f;
            Fill(new Rect(p.x + 28f, p.y + 52f - mouth * 0.5f, 24f, mouth), eye);
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
            garbled = sb.Append("  <size=12><i>(antenna missing: bad signal)</i></size>").ToString();
            return garbled;
        }

        void DrawWorldCards(float w, float h)
        {
            var area = World.Active;
            float age = Time.time - World.EnteredTime;
            if (area && age < titleCardTime)
            {
                string t = $"{area.chapterTitle}  ·  {area.DisplayTitle}";
                if (area.IsComplete) t += "  <color=#7dff9a>(solved)</color>";
                DrawTitleCard(w, h, t, age);
            }
            float toastAge = Time.time - World.ToastTime;
            if (!string.IsNullOrEmpty(World.Toast) && toastAge < 4f)
            {
                Color old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Min(toastAge * 3f, (4f - toastAge) * 2f)));
                GUI.Label(new Rect(0, h * 0.18f, w, 60f), "<color=#ffd34d>" + World.Toast + "</color>", big);
                GUI.color = old;
            }
        }

        void DrawTitleCard(float w, float h, string text, float age)
        {
            float a = Mathf.Clamp01(Mathf.Min(age * 2f, (titleCardTime - age) * 2f));
            Color old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, a);
            GUI.Label(new Rect(0, h * 0.36f, w, 60f), text, big);
            GUI.color = old;
        }

        static void Dim(float w, float h, float alpha) => Fill(new Rect(0, 0, w, h), new Color(0f, 0f, 0f, alpha));

        void DrawFlat(float w, float h)
        {
            Dim(w, h, 0.4f);
            GUI.Label(new Rect(0, h * 0.32f, w, 60f), "<color=#ff7a6b>BATTERY FLAT</color>", big);
            GUI.Label(new Rect(0, h * 0.32f + 60f, w, 30f), "Calling the Bureau tow truck... back to the last charger.", label);
        }

        void DrawComplete(float w, float h)
        {
            Dim(w, h, 0.35f);
            var runner = Runner;
            float t = runner ? runner.Elapsed : 0f;
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
            if (inWorkshop)
            {
                // Barely dimmed so you can watch your bot change.
                Dim(w, h, 0.1f);
                if (workshop.Draw(w, h)) inWorkshop = false;
                return;
            }
            Dim(w, h, 0.5f);
            GUI.Label(new Rect(0, h * 0.22f, w, 60f), "PAUSED", big);
            float bw = 260f, x = w / 2 - bw / 2, y = h * 0.22f + 80f;
            if (GUI.Button(new Rect(x, y, bw, 44f), "Resume", button)) gm.SetPaused(false);
            if (GUI.Button(new Rect(x, y + 54f, bw, 44f), "Restart call", button)) gm.Restart();
            if ((World || CampaignProgress.Campaign) && GUI.Button(new Rect(x, y + 108f, bw, 44f), World ? "Title screen" : "Main menu", button)) gm.QuitToMenu();
            if (GUI.Button(new Rect(x, y + 162f, bw, 44f), "Customize bots", button)) { workshop.Open(); inWorkshop = true; }
            GUI.Label(new Rect(0, y + 224f, w, 24f), "Esc / P / Start to resume", small);
        }
    }
}
