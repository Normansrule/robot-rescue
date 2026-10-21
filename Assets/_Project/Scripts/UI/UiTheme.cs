using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RobotRescue.UI
{
    /// <summary>
    /// The one look every IMGUI screen shares: "Live Wire", the same system as the website (site/site.css).
    /// The Tether Cable is the idea: Tether cyan is live current (focus, keys, things that are on), Bureau orange is PIP and
    /// the one main action on a screen, and panels hang off the top of the screen on a short cable and power on when they
    /// open. Unbounded for display type, Figtree for everything else. Dark (the station at night) or light (the workshop by
    /// day), saved in PlayerPrefs "rr.ui.light". Rules in docs/DESIGN.md ("Interface").
    /// Screens are laid out on a 1080-pixel-tall canvas and scaled to the real screen with <see cref="Begin"/>.
    /// </summary>
    public static class UiTheme
    {
        // ---- Mode ----------------------------------------------------------------------------------------------

        static int light = -1;

        /// <summary>Light mode. Changing it bumps <see cref="Version"/> so screens rebuild their cached styles.</summary>
        public static bool Light
        {
            get { if (light < 0) light = PlayerPrefs.GetInt("rr.ui.light", 0); return light == 1; }
            set { light = value ? 1 : 0; PlayerPrefs.SetInt("rr.ui.light", light); PlayerPrefs.Save(); Version++; }
        }

        /// <summary>Goes up whenever the mode changes. Screens that cache GUIStyles rebuild when it differs from theirs.</summary>
        public static int Version { get; private set; }

        // ---- Palette (dark / light) ----------------------------------------------------------------------------
        static Color Pick(int dark, int lightRgb, float a = 1f) => Hex(Light ? lightRgb : dark, a);

        public static Color Ink => Light ? Hex(0xFFFFFF, 0.95f) : Hex(0x0A1430, 0.92f);   // panels
        public static Color InkRaised => Pick(0x111E42, 0xECE7DB);                        // buttons, wells
        public static Color InkHover => Pick(0x1A2A57, 0xE2DCCD);
        public static Color InkPressed => Pick(0x071026, 0xD6CFBD);
        public static Color Line => Pick(0x24335F, 0xCFC8B8);                             // unpowered cable, hairlines
        public static Color Text => Pick(0xF3F0E8, 0x121A33);                             // ~15:1 on its panel
        public static Color TextMuted => Pick(0xA9B4D4, 0x545E7A);                        // ~8:1 / ~6:1
        public static readonly Color Accent = Hex(0xFF8C26);                               // Bureau orange: PIP, primary action
        public static Color AccentHover => Pick(0xFFA04D, 0xFF9A40);
        public static Color AccentPressed => Pick(0xE8731A, 0xEC7A16);
        public static readonly Color OnAccent = Hex(0x1B1206);                             // text on orange, ~8:1
        public static Color Signal => Pick(0x2FE0FF, 0x008AB0);                           // Tether cyan: live current, focus
        public static Color Good => Pick(0x6BE38F, 0x1E8F4C);
        public static Color Warn => Pick(0xFFC94D, 0xA86A00);
        public static Color Danger => Pick(0xFF7A6B, 0xC8402F);

        /// <summary>Fixed night colours for screens that are always dark (end credits, the loading frame).</summary>
        public static readonly Color NightText = Hex(0xF3F0E8), NightMuted = Hex(0xA9B4D4), NightSignal = Hex(0x2FE0FF);

        // Rich-text colours. Cyan and orange get darker in light mode so small text keeps 4.5:1 contrast.
        public static string TextHex => Light ? "#121A33" : "#F3F0E8";
        public static string MutedHex => Light ? "#545E7A" : "#A9B4D4";
        public static string AccentHex => Light ? "#B5530A" : "#FF8C26";
        public static string SignalHex => Light ? "#00718F" : "#2FE0FF";
        public static string GoodHex => Light ? "#1E8F4C" : "#6BE38F";
        public static string WarnHex => Light ? "#8A5700" : "#FFC94D";
        public static string DangerHex => Light ? "#C8402F" : "#FF7A6B";
        /// <summary>PIP's orange at full strength, for the big wordmark only (large text needs 3:1, which it has on both).</summary>
        public const string PipHex = "#FF8C26";

        // ---- Type ----------------------------------------------------------------------------------------------
        public const int Display = 76, Title = 44, Heading = 24, Body = 18, Label = 16, Caption = 14;

        static Font body, bodyBold, display;
        static bool fontsLoaded;

        static void LoadFonts()
        {
            if (fontsLoaded) return;
            fontsLoaded = true;
            // Assets/_Project/Resources/Fonts. If they're missing, styles fall back to Unity's default font.
            body = Resources.Load<Font>("Fonts/Figtree-Regular");
            bodyBold = Resources.Load<Font>("Fonts/Figtree-Bold");
            display = Resources.Load<Font>("Fonts/Unbounded-Bold");
        }

        /// <summary>Unbounded for bold headings (24 and up), Figtree Bold for other bold text, Figtree for the rest.</summary>
        static void ApplyFont(GUIStyle s, int size, bool bold)
        {
            LoadFonts();
            Font f = bold ? (size >= Heading && display ? display : bodyBold) : body;
            if (!f) return;
            s.font = f;
            s.fontStyle = FontStyle.Normal;   // the file is already the right weight; no fake bold on top
        }

        // ---- Spacing and shape ---------------------------------------------------------------------------------
        public const float Gap = 8f, Pad = 16f, Margin = 24f, Radius = 12f, ButtonHeight = 48f;

        static Color Hex(int rgb, float a = 1f) =>
            new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, a);

        // ---- Scaling -------------------------------------------------------------------------------------------

        /// <summary>UI scale for this screen: 1 at 1080p, never below 0.8 so text stays readable in a small browser window.</summary>
        public static float Scale => Mathf.Clamp(Screen.height / 1080f, 0.8f, 3f);

        /// <summary>Call first in OnGUI. Scales the GUI to the screen and returns the canvas size to lay out in.</summary>
        public static void Begin(out float w, out float h)
        {
            float s = Scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
            w = Screen.width / s;
            h = Screen.height / s;
        }

        /// <summary>Call last in OnGUI so later GUI code isn't scaled twice.</summary>
        public static void End() => GUI.matrix = Matrix4x4.identity;

        /// <summary>A world position on the canvas (for name tags and progress bars over objects). z &lt;= 0 means behind the camera.</summary>
        public static Vector3 WorldToCanvas(Camera cam, Vector3 world)
        {
            Vector3 s = cam.WorldToScreenPoint(world);
            float k = Scale;
            return new Vector3(s.x / k, (Screen.height - s.y) / k, s.z);
        }

        // ---- Motion --------------------------------------------------------------------------------------------

        static int calm = -1;

        /// <summary>
        /// Calm motion (Controls page): no screen tints, no pulsing, no sliding. State still changes colour, so nothing
        /// is lost, it just doesn't move. Saved in PlayerPrefs "rr.ui.calm".
        /// </summary>
        public static bool CalmMotion
        {
            get { if (calm < 0) calm = PlayerPrefs.GetInt("rr.ui.calm", 0); return calm == 1; }
            set { calm = value ? 1 : 0; PlayerPrefs.SetInt("rr.ui.calm", calm); PlayerPrefs.Save(); }
        }

        /// <summary>Strong ease-out (cubic): starts fast so a change reads straight away, then settles.</summary>
        public static float EaseOut(float t)
        {
            t = 1f - Mathf.Clamp01(t);
            return 1f - t * t * t;
        }

        /// <summary>
        /// Gentle 0..1 attention pulse for warnings (low battery, an order about to expire). Slow sine, never a hard
        /// on/off flash, so it stays far below the three-flashes-a-second seizure threshold. Steady 1 in calm motion.
        /// </summary>
        public static float Pulse(float hz = 1.1f) =>
            CalmMotion ? 1f : 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * hz * 2f * Mathf.PI);

        /// <summary>Opacity of a timed card: eases in over <paramref name="fadeIn"/>, leaves a little faster than it came.</summary>
        public static float CardAlpha(float age, float life, float fadeIn = 0.35f, float fadeOut = 0.25f) =>
            Mathf.Min(EaseOut(age / fadeIn), Mathf.Clamp01((life - age) / fadeOut));

        /// <summary>How far below its resting place a card starts (it rises into place as it fades in). 0 in calm motion.</summary>
        public static float Rise(float age, float fadeIn = 0.35f, float distance = 14f) =>
            CalmMotion ? 0f : (1f - EaseOut(age / fadeIn)) * distance;

        // ---- Drawing -------------------------------------------------------------------------------------------

        /// <summary>A rounded filled rectangle.</summary>
        public static void Panel(Rect r, Color c, float radius = Radius) =>
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f, radius);

        /// <summary>A rounded outline (focus rings, selected swatches).</summary>
        public static void Outline(Rect r, Color c, float width = 2f, float radius = Radius) =>
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, width, radius);

        /// <summary>A rounded track with a fill, for battery, stamina, patience and boss meters.</summary>
        public static void Bar(Rect r, float t, Color fill)
        {
            float radius = r.height / 2f;
            Panel(r, Light ? new Color(0.93f, 0.91f, 0.86f, 0.9f) : new Color(0.03f, 0.05f, 0.12f, 0.75f), radius);
            t = Mathf.Clamp01(t);
            if (t <= 0f) return;
            // Never narrower than the bar is tall, so the rounded ends stay round at low values.
            float inner = r.height - 4f;
            float fw = Mathf.Max(inner, (r.width - 4f) * t);
            Panel(new Rect(r.x + 2f, r.y + 2f, fw, inner), fill, inner / 2f);
        }

        /// <summary>Text with a soft dark shadow so it reads over a bright 3D scene with no panel behind it.</summary>
        public static void Shadowed(Rect r, string text, GUIStyle style)
        {
            var old = GUI.color;
            GUI.color = Light ? new Color(1f, 1f, 1f, 0.7f * old.a) : new Color(0f, 0f, 0f, 0.55f * old.a);
            GUI.Label(new Rect(r.x, r.y + 2f, r.width, r.height), StripColour(text), style);
            GUI.color = old;
            GUI.Label(r, text, style);
        }

        static readonly Regex ColourTag = new Regex("</?color[^>]*>");
        static readonly Regex KeyTag = new Regex(@"\[([^\]]+)\]");

        static string StripColour(string s) => ColourTag.Replace(s, "");

        /// <summary>Turns "[E / X] pick up" into a cyan, bold key hint, so every prompt reads the same way.</summary>
        public static string Keys(string s) => KeyTag.Replace(s, "<b><color=" + SignalHex + ">$1</color></b>");

        // ---- Styles --------------------------------------------------------------------------------------------

        // ---- Live Wire: cables and power-on -----------------------------------------------------------------------

        /// <summary>
        /// Hangs a panel from the top of the screen on a short cable, and powers it on: for the first
        /// 0.15 seconds a spark runs down the cable, then the panel's border glows and settles to a quiet live edge.
        /// Draw it right after the panel. <paramref name="age"/> is seconds since the panel opened. Calm motion: no spark.
        /// </summary>
        public static void PowerOn(Rect panel, float age, float radius = Radius)
        {
            if (Event.current.type != EventType.Repaint) return;
            float cx = panel.center.x, top = panel.y;
            Color live = Signal;
            // the cable and the socket it plugs into
            Panel(new Rect(cx - 1.5f, 0f, 3f, top), Line, 1.5f);
            float spark = CalmMotion ? 1f : Mathf.Clamp01(age / 0.15f);
            Panel(new Rect(cx - 1.5f, 0f, 3f, top * spark), live, 1.5f);
            if (spark < 1f) Panel(new Rect(cx - 5f, top * spark - 5f, 10f, 10f), Color.white, 5f);
            Panel(new Rect(cx - 12f, top - 6f, 24f, 10f), spark >= 1f ? live : Line, 4f);
            // the border: bright as the power arrives, then a steady live edge
            float glow = CalmMotion ? 0f : 1f - EaseOut((age - 0.15f) / 0.45f);
            if (spark >= 1f)
            {
                var c = live; c.a = 0.35f + 0.65f * Mathf.Clamp01(glow);
                Outline(panel, c, 2f, radius);
                if (glow > 0.01f) { c.a = 0.25f * glow; Outline(new Rect(panel.x - 4f, panel.y - 4f, panel.width + 8f, panel.height + 8f), c, 3f, radius + 4f); }
            }
        }

        static readonly Dictionary<Color, Texture2D> fills = new Dictionary<Color, Texture2D>();

        /// <summary>A small rounded texture for GUIStyle backgrounds (9-sliced by the style's border).</summary>
        static Texture2D RoundTex(Color c)
        {
            if (fills.TryGetValue(c, out var tex) && tex) return tex;
            const int size = 32, r = 11;
            tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Distance outside the rounded corner, with a one-pixel soft edge.
                float dx = Mathf.Max(0f, Mathf.Max(r - x - 0.5f, x + 0.5f - (size - r)));
                float dy = Mathf.Max(0f, Mathf.Max(r - y - 0.5f, y + 0.5f - (size - r)));
                float d = Mathf.Sqrt(dx * dx + dy * dy) - r;
                px[y * size + x] = new Color(c.r, c.g, c.b, c.a * Mathf.Clamp01(0.5f - d));
            }
            tex.SetPixels(px);
            tex.Apply();
            fills[c] = tex;
            return tex;
        }

        /// <summary>A label style on the theme's type scale.</summary>
        public static GUIStyle Style(int size, Color color, TextAnchor anchor = TextAnchor.MiddleLeft, bool bold = false, bool wrap = false)
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = size, alignment = anchor, richText = true, wordWrap = wrap, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal };
            ApplyFont(s, size, bold);
            s.normal.textColor = s.hover.textColor = color;
            return s;
        }

        /// <summary>
        /// Rounded button. Primary is filled orange (one per screen, the thing most people want next); secondary is a
        /// quiet raised panel. Hover lightens, press darkens, so a click is acknowledged before anything else happens.
        /// </summary>
        public static GUIStyle Button(bool primary = false, int size = Body)
        {
            var s = new GUIStyle(GUI.skin.button)
            {
                fontSize = size, fontStyle = FontStyle.Bold, richText = true, alignment = TextAnchor.MiddleCenter,
                border = new RectOffset(12, 12, 12, 12), padding = new RectOffset(16, 16, 8, 8), margin = new RectOffset(0, 0, 4, 4),
            };
            ApplyFont(s, size, true);
            Color normal = primary ? Accent : InkRaised, hover = primary ? AccentHover : InkHover, pressed = primary ? AccentPressed : InkPressed;
            Color text = primary ? OnAccent : Text;
            s.normal.background = RoundTex(normal);
            s.hover.background = s.focused.background = RoundTex(hover);
            s.active.background = RoundTex(pressed);
            s.onNormal.background = RoundTex(primary ? Accent : InkHover);
            s.normal.textColor = s.hover.textColor = s.focused.textColor = s.active.textColor = s.onNormal.textColor = text;
            return s;
        }

        /// <summary>A disabled button: same shape, no fill, muted text.</summary>
        public static GUIStyle Locked(int size = Body)
        {
            var s = Button(false, size);
            ApplyFont(s, size, false);
            s.normal.background = s.hover.background = s.active.background = RoundTex(new Color(InkRaised.r, InkRaised.g, InkRaised.b, 0.45f));
            s.normal.textColor = s.hover.textColor = s.active.textColor = TextMuted;
            return s;
        }

        /// <summary>A panel style for GUILayout areas.</summary>
        public static GUIStyle PanelStyle(float padding = Pad + 4f)
        {
            int p = Mathf.RoundToInt(padding);
            var s = new GUIStyle(GUI.skin.box) { border = new RectOffset(12, 12, 12, 12), padding = new RectOffset(p, p, p, p) };
            s.normal.background = RoundTex(Ink);
            return s;
        }

        /// <summary>A text field on a raised well.</summary>
        public static GUIStyle Field(int size = Body)
        {
            var s = new GUIStyle(GUI.skin.textField) { fontSize = size, alignment = TextAnchor.MiddleCenter, border = new RectOffset(12, 12, 12, 12), padding = new RectOffset(12, 12, 6, 6) };
            s.normal.background = s.hover.background = RoundTex(InkPressed);
            s.focused.background = RoundTex(InkRaised);
            s.normal.textColor = s.hover.textColor = s.focused.textColor = Text;
            ApplyFont(s, size, false);
            return s;
        }
    }
}
