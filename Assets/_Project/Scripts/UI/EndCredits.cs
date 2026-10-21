using RobotRescue.World;
using UnityEngine;

namespace RobotRescue.UI
{
    /// <summary>
    /// The ending: after the last Big Rescue the screen fades to night blue and the credits roll, with the people PIP
    /// helped along the way. Any key (after a moment) or the end of the roll goes back to the title.
    /// Added at runtime by <see cref="WorldDirector"/> when the final call is complete.
    /// </summary>
    public class EndCredits : MonoBehaviour
    {
        public float delay = 6f;
        public float scrollSeconds = 50f;

        static readonly string[] Lines =
        {
            "<size=64><b><color=#FF8C26>ROBOT</color> <color=#4FE3FF>RESCUE</color></b></size>",
            "",
            "The Reach is anchored. The hum is music now.",
            "Unit Zero is home.",
            "", "", "",
            "<color=#AEB8D6><b>The Bureau</b></color>",
            "PIP, rookie rescue bot",
            "DISPATCH",
            "Unit Zero, the first of us",
            "", "",
            "<color=#AEB8D6><b>Brightwater Reach</b></color>",
            "Nonna and the trattoria kitchen",
            "Mittens, who was never really stuck",
            "Priya and her seismograph",
            "Theo and the slightly-too-big volcano",
            "Captain Mo and Ines at the harbour",
            "Grandpa Sal, the twins and Benny on the pier",
            "Everyone who came to the Founders' Festival",
            "", "",
            "<color=#AEB8D6><b>Helper Drones</b></color>",
            "Cheerful, nervous, grumpy, sleepy and show-off,",
            "and every one of them carried on a plate at least once.",
            "", "",
            "<color=#AEB8D6><b>Made by</b></color>",
            "Alek",
            "with Claude",
            "", "", "",
            "Thanks for playing.",
            "<size=22>The Bureau never leaves anyone behind.</size>",
        };

        float start;
        GUIStyle style, hint;

        void OnEnable() => start = Time.unscaledTime;

        void Update()
        {
            if (Time.unscaledTime - start > delay + scrollSeconds + 4f) Finish();
        }

        void Finish()
        {
            enabled = false;
            if (WorldDirector.Instance) WorldDirector.Instance.ReturnToTitle();
        }

        void OnGUI()
        {
            float t = Time.unscaledTime - start - delay;
            if (t < -2f) return;
            if (style == null)
            {
                style = UiTheme.Style(26, UiTheme.NightText, TextAnchor.MiddleCenter, wrap: true);
                hint = UiTheme.Style(UiTheme.Caption + 2, UiTheme.NightMuted, TextAnchor.MiddleRight);
            }
            UiTheme.Begin(out float w, out float h);
            GUI.depth = -100;
            float fade = UiTheme.EaseOut((t + 2f) / 2f);
            GUI.color = new Color(0.04f, 0.07f, 0.16f, 0.94f * fade);
            GUI.DrawTexture(new Rect(0, 0, w, h), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, fade);

            // A steady, linear roll: constant motion reads as calm, and the eye can follow any line.
            const float lineH = 44f;
            float total = Lines.Length * lineH;
            float y = h - Mathf.Clamp01(t / scrollSeconds) * (h * 0.5f + total);
            for (int i = 0; i < Lines.Length; i++, y += lineH)
                if (y > -lineH && y < h) GUI.Label(new Rect(0, y, w, lineH), Lines[i], style);

            if (t > 3f) GUI.Label(new Rect(0, h - 48f, w - UiTheme.Margin, 30f), "Press any key to return to the title", hint);
            GUI.color = Color.white;
            UiTheme.End();

            var e = Event.current;
            if (t > 3f && (e.type == EventType.KeyDown || e.type == EventType.MouseDown)) Finish();
        }
    }
}
