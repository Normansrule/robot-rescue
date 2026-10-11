using RobotRescue.Campaign;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RobotRescue.UI
{
    /// <summary>
    /// IMGUI main menu: Continue, chapter/call select with locks, controls, reset progress, quit.
    /// Enter / A starts Continue so a gamepad player can get going without a mouse.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        enum Page { Main, Calls, Controls }

        public string subtitle = "A Robot Rescue Bureau of Brightwater production";

        Page page;
        InputAction confirm;
        GUIStyle title, sub, button, header, small, locked;
        Vector2 scroll;

        void Awake()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            confirm = new InputAction("Confirm", InputActionType.Button);
            confirm.AddBinding("<Keyboard>/enter");
            confirm.AddBinding("<Gamepad>/buttonSouth");
            confirm.AddBinding("<Gamepad>/start");
        }

        void OnEnable() => confirm.Enable();
        void OnDisable() => confirm.Disable();
        void OnDestroy() => confirm.Dispose();

        void Update()
        {
            if (page == Page.Main && confirm.WasPressedThisFrame()) CampaignProgress.Continue();
        }

        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 72, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
            sub = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Italic };
            button = new GUIStyle(GUI.skin.button) { fontSize = 22 };
            header = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, richText = true };
            small = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, richText = true, alignment = TextAnchor.MiddleCenter };
            locked = new GUIStyle(button) { fontStyle = FontStyle.Italic };
        }

        void OnGUI()
        {
            Styles();
            float w = Screen.width, h = Screen.height;
            GUI.Label(new Rect(0, h * 0.08f, w, 90f), "<color=#ff8c26>ROBOT</color> <color=#4fe3ff>RESCUE</color>", title);
            GUI.Label(new Rect(0, h * 0.08f + 86f, w, 30f), subtitle, sub);

            switch (page)
            {
                case Page.Main: DrawMain(w, h); break;
                case Page.Calls: DrawCalls(w, h); break;
                case Page.Controls: DrawControls(w, h); break;
            }
        }

        bool Button(ref float y, float w, string text, GUIStyle style = null)
        {
            const float bw = 320f;
            bool hit = GUI.Button(new Rect(w / 2 - bw / 2, y, bw, 50f), text, style ?? button);
            y += 60f;
            return hit;
        }

        void DrawMain(float w, float h)
        {
            float y = h * 0.36f;
            bool hasCampaign = CampaignProgress.Campaign;
            bool started = CampaignProgress.UnlockedCount > 1;
            if (hasCampaign && Button(ref y, w, started ? "Continue" : "Start the shift")) CampaignProgress.Continue();
            if (hasCampaign && Button(ref y, w, "Choose a call")) page = Page.Calls;
            if (Button(ref y, w, "Controls")) page = Page.Controls;
            if (started && Button(ref y, w, "Reset progress")) CampaignProgress.ResetProgress();
#if !UNITY_WEBGL
            if (Button(ref y, w, "Quit")) Application.Quit();
#endif
            GUI.Label(new Rect(0, h - 40f, w, 30f), "Enter / A to continue   ·   v" + Application.version, small);
        }

        void DrawCalls(float w, float h)
        {
            var c = CampaignProgress.Campaign;
            const float pw = 560f;
            Rect area = new Rect(w / 2 - pw / 2, h * 0.3f, pw, h * 0.6f);
            GUILayout.BeginArea(area);
            scroll = GUILayout.BeginScrollView(scroll);
            int index = 0;
            for (int ci = 0; ci < c.chapters.Length; ci++)
            {
                var ch = c.chapters[ci];
                GUILayout.Label($"Chapter {ci + 1}: {ch.title}  <size=15><i>{ch.location}</i></size>", header);
                foreach (var level in ch.levels)
                {
                    bool open = CampaignProgress.IsUnlocked(index);
                    string text = open ? (level.isBigRescue ? "BIG RESCUE: " : "") + level.title : "Locked";
                    GUI.enabled = open;
                    if (GUILayout.Button(text, open ? button : locked, GUILayout.Height(44f))) CampaignProgress.Load(level.scene);
                    GUI.enabled = true;
                    index++;
                }
                GUILayout.Space(10f);
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Back", button, GUILayout.Height(44f))) page = Page.Main;
            GUILayout.EndArea();
        }

        void DrawControls(float w, float h)
        {
            const string text =
                "<b>Move / look</b>  WASD + mouse  ·  sticks\n" +
                "<b>Jump</b>  Space  ·  A\n" +
                "<b>Pick up / put down</b>  E  ·  X\n" +
                "<b>Throw</b>  Left mouse  ·  RT\n" +
                "<b>Tether (power / pull / swing)</b>  Right mouse  ·  LT\n" +
                "<b>Reel in / out</b>  Q / Z  ·  RB / LB\n" +
                "<b>Use tool (hold)</b>  F  ·  B\n" +
                "<b>Swap tool</b>  Tab  ·  Y\n" +
                "<b>Pause</b>  Esc / P  ·  Start\n" +
                "<b>Restart</b>  Backspace  ·  Select";
            GUI.Label(new Rect(w / 2 - 300f, h * 0.3f, 600f, 300f), text, small);
            float y = h * 0.3f + 320f;
            if (Button(ref y, w, "Back")) page = Page.Main;
        }
    }
}
