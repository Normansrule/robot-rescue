using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// Colour-coded wire painted on the floor/walls between a source and what it powers. Lights up when live,
    /// so players can read the circuit at a glance. Add it to the source's poweredObjects list.
    /// </summary>
    public class PowerWire : MonoBehaviour, IPowerable
    {
        public Renderer[] segments;
        public Color color = new Color(0.35f, 0.6f, 1f);
        [Range(0f, 1f)] public float offBrightness = 0.3f;

        void Start() => SetPowered(false);

        public void SetPowered(bool on)
        {
            Color c = on ? color : Color.Lerp(new Color(0.2f, 0.2f, 0.22f), color, offBrightness);
            foreach (var r in segments)
                if (r) r.material.color = c;
        }
    }
}
