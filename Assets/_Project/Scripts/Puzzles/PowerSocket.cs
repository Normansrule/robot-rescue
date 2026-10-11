using RobotRescue.Bots;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// Plug the Tether Cable in here to power the linked objects. Power lasts while the bot stays plugged in,
    /// so the cable length becomes part of the puzzle.
    /// </summary>
    public class PowerSocket : TetherTarget
    {
        [Tooltip("Objects with IPowerable components to switch on.")]
        public GameObject[] poweredObjects;
        [Tooltip("Optional light/LED renderer that glows while powered.")]
        public Renderer indicator;
        public Color offColor = new Color(0.35f, 0.35f, 0.4f);
        public Color onColor = new Color(0.3f, 1f, 0.5f);
        public string poweredCue;

        public bool IsPowered { get; private set; }
        public override TetherMode Mode => TetherMode.Power;
        public override bool CanAttach => base.CanAttach && AttachedCable == null;

        void Start() => SetIndicator(false);

        public override void OnTetherAttached(TetherCable cable)
        {
            base.OnTetherAttached(cable);
            IsPowered = true;
            SetIndicator(true);
            PowerUtil.Send(poweredObjects, true);
            if (!string.IsNullOrEmpty(poweredCue)) EventBus<NarrationCue>.Raise(new NarrationCue(poweredCue));
        }

        public override void OnTetherDetached(TetherCable cable)
        {
            base.OnTetherDetached(cable);
            IsPowered = false;
            SetIndicator(false);
            PowerUtil.Send(poweredObjects, false);
        }

        void SetIndicator(bool on)
        {
            if (indicator) indicator.material.color = on ? onColor : offColor;
        }
    }
}
