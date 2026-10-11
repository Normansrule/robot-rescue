using RobotRescue.Core;
using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>Heats any pot sitting in its burner zone while powered (plug the Tether into its socket).</summary>
    public class Stove : MonoBehaviour, IPowerable
    {
        public Renderer burnerVisual;
        public Color offColor = new Color(0.25f, 0.25f, 0.28f);
        public Color onColor = new Color(1f, 0.35f, 0.1f);

        public bool IsOn { get; private set; }
        bool everPowered;

        void Start() => SetColor();

        public void SetPowered(bool on)
        {
            IsOn = on;
            SetColor();
            if (on && !everPowered)
            {
                everPowered = true;
                EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted("power_stove"));
            }
        }

        void SetColor()
        {
            if (burnerVisual) burnerVisual.material.color = IsOn ? onColor : offColor;
        }
    }
}
