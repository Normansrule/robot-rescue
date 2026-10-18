using RobotRescue.Bots;
using UnityEngine;

namespace RobotRescue.UI
{
    /// <summary>
    /// The Bureau workshop: an IMGUI panel for dressing each player's rescue bot and the Helper Drone crew.
    /// Shared by the main menu and the pause menu. Every change saves straight away and shows on the nearest bot
    /// (the menu statue, or your own bot when paused), so there's nothing to confirm.
    /// </summary>
    public class BotWorkshop
    {
        int tab; // 0..3 = players, 4 = drone crew
        BotLook look;
        DroneLivery livery;
        BotCustomizer preview;
        Vector2 scroll;
        GUIStyle panel, header, row, value, button, tabOn, tabOff, field, hint;

        const int CrewTab = BotLookStore.MaxPlayers;

        /// <summary>Call when the panel opens.</summary>
        public void Open()
        {
            preview = Object.FindAnyObjectByType<BotCustomizer>();
            tab = preview ? Mathf.Clamp(preview.playerSlot, 0, BotLookStore.MaxPlayers - 1) : 0;
            look = BotLookStore.Load(tab);
            livery = BotLookStore.LoadDrones();
        }

        /// <summary>Call when the panel closes: the preview bot goes back to its own player's look.</summary>
        public void Close()
        {
            if (preview) preview.Apply(BotLookStore.Load(preview.playerSlot));
        }

        void Styles()
        {
            if (panel != null) return;
            var bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0.08f, 0.1f, 0.2f, 0.82f));
            bg.Apply();
            panel = new GUIStyle(GUI.skin.box) { padding = new RectOffset(18, 18, 14, 14) };
            panel.normal.background = bg;
            header = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, richText = true };
            row = new GUIStyle(GUI.skin.label) { fontSize = 17, alignment = TextAnchor.MiddleLeft };
            value = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { fontSize = 17 };
            tabOff = new GUIStyle(GUI.skin.button) { fontSize = 15 };
            tabOn = new GUIStyle(tabOff) { fontStyle = FontStyle.Bold };
            tabOn.normal.textColor = tabOn.hover.textColor = new Color(1f, 0.62f, 0.2f);
            field = new GUIStyle(GUI.skin.textField) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            hint = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true, fontStyle = FontStyle.Italic };
        }

        /// <summary>Draws the panel down the left side of the screen. Returns true when the player presses Done.</summary>
        public bool Draw(float w, float h)
        {
            Styles();
            if (look == null) Open();
            float pw = Mathf.Min(460f, w - 40f);
            var area = new Rect(24f, 24f, pw, h - 48f);
            GUILayout.BeginArea(area, panel);
            GUILayout.Label("<color=#ff8c26>BUREAU</color> WORKSHOP", header);

            GUILayout.BeginHorizontal();
            for (int i = 0; i <= CrewTab; i++)
            {
                string label = i == CrewTab ? "Drones" : "P" + (i + 1);
                if (GUILayout.Button(label, i == tab ? tabOn : tabOff, GUILayout.Height(32f))) SwitchTab(i);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);

            scroll = GUILayout.BeginScrollView(scroll);
            if (tab == CrewTab) DrawCrew();
            else DrawPlayer();
            GUILayout.EndScrollView();

            GUILayout.Space(6f);
            bool done = GUILayout.Button("Done", button, GUILayout.Height(42f));
            GUILayout.EndArea();
            if (done) Close();
            return done;
        }

        void SwitchTab(int i)
        {
            tab = i;
            if (i < CrewTab)
            {
                look = BotLookStore.Load(i);
                Preview();
            }
        }

        void DrawPlayer()
        {
            var before = JsonUtility.ToJson(look);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Name", row, GUILayout.Width(110f), GUILayout.Height(34f));
            look.name = GUILayout.TextField(look.name ?? "", BotCatalog.MaxNameLength, field, GUILayout.Height(34f));
            GUILayout.EndHorizontal();
            GUILayout.Space(4f);

            look.chassis = Stepper("Body", look.chassis, BotCatalog.Chassis);
            look.head = Stepper("Head", look.head, BotCatalog.Heads);
            look.feet = Stepper("Feet", look.feet, BotCatalog.Feet);
            look.topper = Stepper("Antenna", look.topper, BotCatalog.Toppers);
            look.badge = Stepper("Badge", look.badge, BotCatalog.Badges);
            look.shell = Swatch("Shell", look.shell, BotCatalog.Shells);
            look.trim = Swatch("Trim", look.trim, BotCatalog.Trims);
            look.joints = Swatch("Joints", look.joints, BotCatalog.Joints);
            look.eyes = Swatch("Eyes", look.eyes, BotCatalog.Eyes, hdr: true);
            look.expression = Stepper("Smile", look.expression, Names(BotCatalog.Expressions));
            look.voice = Stepper("Voice", look.voice, Names(BotCatalog.Voices));

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Surprise me", button, GUILayout.Height(36f))) look = Random(look.name);
            if (GUILayout.Button("Starting look", button, GUILayout.Height(36f))) look = BotLookStore.Defaults(tab);
            GUILayout.EndHorizontal();
            GUILayout.Label(tab == 0
                ? "Player 1 is you. DISPATCH will call your bot by its name."
                : $"Player {tab + 1}'s bot, ready for when a friend joins.", hint);

            if (JsonUtility.ToJson(look) != before)
            {
                BotLookStore.Save(tab, look);
                Preview();
            }
        }

        void DrawCrew()
        {
            var before = JsonUtility.ToJson(livery);
            GUILayout.Label("The Helper Drone crew shares one paint job.", hint);
            livery.paint = Swatch("Paint", livery.paint, BotCatalog.DronePaints);
            livery.topper = Stepper("Topper", livery.topper, BotCatalog.DroneToppers);
            livery.eyes = Swatch("Eyes", livery.eyes, BotCatalog.DroneEyes, hdr: true);
            if (JsonUtility.ToJson(livery) != before) BotLookStore.SaveDrones(livery);
        }

        void Preview()
        {
            if (!preview) return;
            preview.Apply(look);
            if (preview.face) preview.face.SetMood(Mood.Happy, 0.8f);
            if (preview.voice) preview.voice.Say(3, Babble.Tone.Happy);
        }

        int Stepper(string label, int index, string[] options)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, row, GUILayout.Width(110f), GUILayout.Height(32f));
            if (GUILayout.Button("<", button, GUILayout.Width(40f), GUILayout.Height(32f))) index--;
            GUILayout.Label(options[BotLook.Wrap(index, options.Length)], value, GUILayout.Height(32f));
            if (GUILayout.Button(">", button, GUILayout.Width(40f), GUILayout.Height(32f))) index++;
            GUILayout.EndHorizontal();
            return BotLook.Wrap(index, options.Length);
        }

        int Swatch(string label, int index, (string name, Color color)[] options, bool hdr = false)
        {
            index = BotLook.Wrap(index, options.Length);
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, row, GUILayout.Width(110f), GUILayout.Height(32f));
            if (GUILayout.Button("<", button, GUILayout.Width(40f), GUILayout.Height(32f))) index--;
            index = BotLook.Wrap(index, options.Length);
            var c = options[index].color;
            Rect r = GUILayoutUtility.GetRect(28f, 28f, GUILayout.Width(28f), GUILayout.Height(32f));
            if (c.a > 0f)
            {
                // HDR LED colours are squashed back into 0..1 for the swatch.
                float m = hdr ? Mathf.Max(1f, Mathf.Max(c.r, Mathf.Max(c.g, c.b))) : 1f;
                var old = GUI.color;
                GUI.color = new Color(c.r / m, c.g / m, c.b / m, 1f);
                GUI.DrawTexture(new Rect(r.x, r.y + 4f, 24f, 24f), Texture2D.whiteTexture);
                GUI.color = old;
            }
            GUILayout.Label(options[index].name, value, GUILayout.Height(32f));
            if (GUILayout.Button(">", button, GUILayout.Width(40f), GUILayout.Height(32f))) index++;
            GUILayout.EndHorizontal();
            return BotLook.Wrap(index, options.Length);
        }

        static string[] Names<T>((string name, T a, T b, T c)[] items)
        {
            var n = new string[items.Length];
            for (int i = 0; i < items.Length; i++) n[i] = items[i].name;
            return n;
        }

        static string[] Names((string name, float hz)[] items)
        {
            var n = new string[items.Length];
            for (int i = 0; i < items.Length; i++) n[i] = items[i].name;
            return n;
        }

        static BotLook Random(string keepName) => new BotLook
        {
            name = keepName,
            chassis = UnityEngine.Random.Range(0, BotCatalog.Chassis.Length),
            head = UnityEngine.Random.Range(0, BotCatalog.Heads.Length),
            feet = UnityEngine.Random.Range(0, BotCatalog.Feet.Length),
            topper = UnityEngine.Random.Range(0, BotCatalog.Toppers.Length),
            badge = UnityEngine.Random.Range(0, BotCatalog.Badges.Length),
            shell = UnityEngine.Random.Range(0, BotCatalog.Shells.Length),
            trim = UnityEngine.Random.Range(0, BotCatalog.Trims.Length),
            joints = UnityEngine.Random.Range(0, BotCatalog.Joints.Length),
            eyes = UnityEngine.Random.Range(0, BotCatalog.Eyes.Length),
            expression = UnityEngine.Random.Range(0, BotCatalog.Expressions.Length),
            voice = UnityEngine.Random.Range(0, BotCatalog.Voices.Length),
        };
    }
}
