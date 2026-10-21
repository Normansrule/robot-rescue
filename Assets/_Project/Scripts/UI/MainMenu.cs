using RobotRescue.Campaign;
using RobotRescue.World;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RobotRescue.UI
{
    /// <summary>
    /// IMGUI main menu: Continue, chapter/call select with locks, controls, reset progress, quit.
    /// Arrows / D-pad move the focus ring and Enter / A presses it; focus starts on Continue, so a gamepad player can get
    /// going with one press. In the open world it's the title screen over the island flyover, and calls are fast-travel spots.
    /// Looks come from <see cref="UiTheme"/>.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        enum Page { Main, Calls, Controls, Workshop }

        public string subtitle = "A Robot Rescue Bureau of Brightwater production";

        Page page;
        InputAction back;
        readonly MenuFocus focus = new MenuFocus();
        GUIStyle title, sub, primary, button, locked, danger, header, headerNote, small, keyCol, actionCol;
        Vector2 scroll;
        float openedAt;
        bool confirmReset;
        readonly BotWorkshop workshop = new BotWorkshop();

        static WorldDirector World => WorldDirector.Instance;

        void Continue()
        {
            if (World) World.Continue();
            else CampaignProgress.Continue();
        }

        void Play(int levelIndex, string scene)
        {
            if (World) World.Begin(levelIndex);
            else CampaignProgress.Load(scene);
        }

        void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            back = new InputAction("Back", InputActionType.Button);
            back.AddBinding("<Keyboard>/escape");
            back.AddBinding("<Keyboard>/backspace");
            back.AddBinding("<Gamepad>/buttonEast");
            openedAt = Time.unscaledTime;
        }

        void OnEnable() { focus.Enable(); back.Enable(); }
        void OnDisable() { focus.Disable(); back.Disable(); }
        void OnDestroy() { focus.Dispose(); back.Dispose(); }

        void Update()
        {
            if (page == Page.Main) focus.Tick();
            else if (page != Page.Workshop && back.WasPressedThisFrame()) Go(Page.Main);
        }

        void Go(Page p)
        {
            page = p;
            pageOpenedAt = Time.unscaledTime;
            confirmReset = false;
            focus.Reset();
        }

        int styledFor = -1;
        float pageOpenedAt;

        void Styles()
        {
            if (title != null && styledFor == UiTheme.Version) return;
            styledFor = UiTheme.Version;
            title = UiTheme.Style(UiTheme.Display, UiTheme.Text, TextAnchor.MiddleCenter, bold: true);
            sub = UiTheme.Style(UiTheme.Body, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            primary = UiTheme.Button(primary: true, size: 22);
            button = UiTheme.Button(size: 20);
            locked = UiTheme.Locked(18);
            danger = UiTheme.Button(size: 18);
            danger.normal.textColor = danger.hover.textColor = danger.active.textColor = UiTheme.Danger;
            header = UiTheme.Style(UiTheme.Heading, UiTheme.Text, bold: true);
            headerNote = UiTheme.Style(UiTheme.Caption, UiTheme.TextMuted);
            small = UiTheme.Style(UiTheme.Caption, UiTheme.TextMuted, TextAnchor.MiddleCenter);
            actionCol = UiTheme.Style(UiTheme.Label, UiTheme.Text);
            keyCol = UiTheme.Style(UiTheme.Label, UiTheme.Signal, bold: true);
        }

        void OnGUI()
        {
            Styles();
            UiTheme.Begin(out float w, out float h);
            if (page == Page.Workshop)
            {
                // The workshop panel sits on the left so the bot on the rug stays in view.
                if (workshop.Draw(w, h)) Go(Page.Main);
                UiTheme.End();
                return;
            }

            // The wordmark settles into place once when the menu opens; the buttons are there straight away.
            float age = Time.unscaledTime - openedAt;
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, UiTheme.CalmMotion ? 1f : UiTheme.EaseOut(age / 0.6f));
            float ty = h * 0.09f + UiTheme.Rise(age, 0.6f, 18f);
            UiTheme.Shadowed(new Rect(0, ty, w, 96f), $"<color={UiTheme.PipHex}>Robot</color> <color={UiTheme.TextHex}>Rescue</color>", title);
            GUI.color = old;
            UiTheme.Shadowed(new Rect(0, h * 0.09f + 96f, w, 28f), subtitle, sub);

            switch (page)
            {
                case Page.Main: DrawMain(w, h); break;
                case Page.Calls: DrawCalls(w, h); break;
                case Page.Controls: DrawControls(w, h); break;
            }
            UiTheme.End();
        }

        bool Button(ref float y, float w, string text, GUIStyle style)
        {
            const float bw = 340f;
            bool hit = focus.Button(new Rect(w / 2 - bw / 2, y, bw, UiTheme.ButtonHeight + 4f), text, style);
            y += UiTheme.ButtonHeight + 16f;
            return hit;
        }

        void DrawMain(float w, float h)
        {
            focus.BeginGUI();
            float y = h * 0.36f;
            bool hasCampaign = CampaignProgress.Campaign;
            bool started = CampaignProgress.UnlockedCount > 1;
            // One filled button: the thing most people want next.
            if (hasCampaign && Button(ref y, w, started ? "Continue" : "Start the shift", primary)) Continue();
            if (hasCampaign && Button(ref y, w, World ? "Travel to a call" : "Choose a call", button)) Go(Page.Calls);
            if (Button(ref y, w, "Customize bots", button)) { workshop.Open(); Go(Page.Workshop); }
            if (Button(ref y, w, "Controls", button)) Go(Page.Controls);
#if !UNITY_WEBGL
            if (Button(ref y, w, "Quit", button)) Application.Quit();
#endif
            if (started)
            {
                // Destructive, so it sits apart, reads in red and asks twice.
                y += 12f;
                if (Button(ref y, w, confirmReset ? "Really reset? Press again" : "Reset progress", danger))
                {
                    if (confirmReset) { CampaignProgress.ResetProgress(); confirmReset = false; }
                    else confirmReset = true;
                }
            }
            GUI.Label(new Rect(0, h - 44f, w, 28f), $"<b>↑ ↓</b> choose   ·   <b>Enter / A</b> select   ·   v{Application.version}", small);
        }

        void DrawCalls(float w, float h)
        {
            var c = CampaignProgress.Campaign;
            const float pw = 600f;
            Rect area = new Rect(w / 2 - pw / 2, h * 0.27f, pw, h * 0.66f);
            GUILayout.BeginArea(area, UiTheme.PanelStyle());
            scroll = GUILayout.BeginScrollView(scroll);
            int index = 0;
            for (int ci = 0; ci < c.chapters.Length; ci++)
            {
                var ch = c.chapters[ci];
                if (ci > 0) GUILayout.Space(14f);
                GUILayout.Label($"Chapter {ci + 1}: {ch.title}", header);
                GUILayout.Label(ch.location, headerNote);
                GUILayout.Space(4f);
                foreach (var level in ch.levels)
                {
                    bool open = CampaignProgress.IsUnlocked(index);
                    string text = !open ? "Locked"
                        : level.isBigRescue ? $"<color={UiTheme.AccentHex}>Big Rescue</color>   {level.title}" : level.title;
                    GUI.enabled = open;
                    int i = index;
                    if (GUILayout.Button(text, open ? button : locked, GUILayout.Height(44f))) Play(i, level.scene);
                    GUI.enabled = true;
                    index++;
                }
            }
            if (c.sideCalls != null && c.sideCalls.Length > 0)
            {
                GUILayout.Space(14f);
                GUILayout.Label("Side calls", header);
                GUILayout.Label("Always open", headerNote);
                GUILayout.Space(4f);
                foreach (var level in c.sideCalls)
                {
                    string done = CampaignProgress.IsSideCallComplete(level.scene) ? $"   <color={UiTheme.GoodHex}>✓ done</color>" : "";
                    if (GUILayout.Button(level.title + done, button, GUILayout.Height(44f))) CampaignProgress.Load(level.scene);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.Space(8f);
            bool lightMode = UiTheme.Light;
            if (GUILayout.Button(lightMode ? "Light mode: on" : "Light mode: off   (dark panels)", button, GUILayout.Height(44f)))
                UiTheme.Light = !lightMode;
            if (GUILayout.Button("Back", button, GUILayout.Height(UiTheme.ButtonHeight))) Go(Page.Main);
            GUILayout.EndArea();
            UiTheme.PowerOn(area, Time.unscaledTime - pageOpenedAt);
        }

        static readonly (string action, string keys, string pad)[] Controls =
        {
            ("Move / look", "WASD + mouse", "Sticks"),
            ("Jump", "Space", "A"),
            ("Pick up / put down", "E", "X"),
            ("Throw", "Left mouse", "RT"),
            ("Tether: power, pull, swing", "Right mouse", "LT"),
            ("Reel in / out", "Q / Z", "RB / LB"),
            ("Use tool (hold)", "F", "B"),
            ("Swap tool", "Tab", "Y"),
            ("Climb grippy rock", "Hold Space into it", "Hold A"),
            ("Wave", "G", "D-pad up"),
            ("Hint (goal first, answer last)", "H", "D-pad down"),
            ("Pause", "Esc / P", "Start"),
            ("Restart", "Backspace", "Select"),
        };

        void DrawControls(float w, float h)
        {
            const float pw = 720f;
            Rect area = new Rect(w / 2 - pw / 2, h * 0.24f, pw, h * 0.7f);
            GUILayout.BeginArea(area, UiTheme.PanelStyle());
            GUILayout.BeginHorizontal();
            GUILayout.Label("", headerNote, GUILayout.Width(300f));
            GUILayout.Label("Keyboard", headerNote, GUILayout.Width(190f));
            GUILayout.Label("Gamepad", headerNote);
            GUILayout.EndHorizontal();
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var (action, keys, pad) in Controls)
            {
                GUILayout.BeginHorizontal(GUILayout.Height(28f));
                GUILayout.Label(action, actionCol, GUILayout.Width(300f));
                GUILayout.Label(keys, keyCol, GUILayout.Width(190f));
                GUILayout.Label(pad, keyCol);
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(10f);
            GUILayout.Label("Red pads reset just their room. Glowing pads, chargers and Helper Drones top up your battery. " +
                            "Walk into a lost part to snap it back on.", UiTheme.Style(UiTheme.Caption, UiTheme.TextMuted, wrap: true));
            GUILayout.EndScrollView();
            GUILayout.Space(8f);
            bool calm = UiTheme.CalmMotion;
            if (GUILayout.Button(calm ? "Calm motion: on   (no flashing, tints or sliding)" : "Calm motion: off", button, GUILayout.Height(44f)))
                UiTheme.CalmMotion = !calm;
            bool lightMode = UiTheme.Light;
            if (GUILayout.Button(lightMode ? "Light mode: on" : "Light mode: off   (dark panels)", button, GUILayout.Height(44f)))
                UiTheme.Light = !lightMode;
            if (GUILayout.Button("Back", button, GUILayout.Height(UiTheme.ButtonHeight))) Go(Page.Main);
            GUILayout.EndArea();
            UiTheme.PowerOn(area, Time.unscaledTime - pageOpenedAt);
        }
    }
}
