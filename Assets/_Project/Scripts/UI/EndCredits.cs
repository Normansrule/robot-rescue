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
            "<size=56><b>ROBOT RESCUE</b></size>",
            "",
            "The Reach is anchored. The hum is music now.",
            "Unit Zero is home.",
            "", "", "",
            "<b>THE BUREAU</b>",
            "PIP, rookie rescue bot",
            "DISPATCH",
            "Unit Zero, the first of us",
            "", "",
            "<b>BRIGHTWATER REACH</b>",
            "Nonna and the trattoria kitchen",
            "Mittens, who was never really stuck",
            "Priya and her seismograph",
            "Theo and the slightly-too-big volcano",
            "Captain Mo and Ines at the harbour",
            "Grandpa Sal, the twins and Benny on the pier",
            "Everyone who came to the Founders' Festival",
            "", "",
            "<b>HELPER DRONES</b>",
            "Cheerful, nervous, grumpy, sleepy and show-off,",
            "and every one of them carried on a plate at least once.",
            "", "",
            "<b>MADE BY</b>",
            "Alek",
            "with Claude",
            "", "", "",
            "Thanks for playing.",
            "<size=22>The Bureau never leaves anyone behind.</size>",
        };

        float start;
        GUIStyle style;

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
            style ??= new GUIStyle(GUI.skin.label) { fontSize = 26, alignment = TextAnchor.MiddleCenter, richText = true, wordWrap = true };
            GUI.depth = -100;
            float fade = Mathf.Clamp01((t + 2f) / 2f);
            GUI.color = new Color(0.04f, 0.07f, 0.16f, 0.92f * fade);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 1f, 1f, fade);

            const float lineH = 44f;
            float total = Lines.Length * lineH;
            float y = Screen.height - Mathf.Clamp01(t / scrollSeconds) * (Screen.height * 0.5f + total);
            for (int i = 0; i < Lines.Length; i++, y += lineH)
                if (y > -lineH && y < Screen.height) GUI.Label(new Rect(0, y, Screen.width, lineH), Lines[i], style);

            GUI.color = new Color(1f, 1f, 1f, 0.6f * fade);
            if (t > 3f) GUI.Label(new Rect(0, Screen.height - 40f, Screen.width - 20f, 30f), "<size=16>Press any key to return to the title</size>",
                new GUIStyle(style) { alignment = TextAnchor.MiddleRight });
            GUI.color = Color.white;

            var e = Event.current;
            if (t > 3f && (e.type == EventType.KeyDown || e.type == EventType.MouseDown)) Finish();
        }
    }
}
