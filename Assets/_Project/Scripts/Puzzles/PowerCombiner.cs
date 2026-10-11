using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// Logic gate for power. Wire several sources (sockets, plates) into this, and it powers its outputs when
    /// Any (OR) or All (AND) of them are on. Use one whenever a receiver has more than one input:
    /// receivers only remember the last on/off they were sent.
    /// </summary>
    public class PowerCombiner : MonoBehaviour, IPowerable
    {
        public enum Mode { Any, All }

        public Mode mode = Mode.All;
        [Tooltip("Number of sources wired in (used by All).")]
        public int inputCount = 2;
        public GameObject[] poweredObjects;

        public int ActiveInputs { get; private set; }
        public bool IsOn { get; private set; }

        public void SetPowered(bool on)
        {
            ActiveInputs = Mathf.Max(0, ActiveInputs + (on ? 1 : -1));
            bool output = mode == Mode.Any ? ActiveInputs > 0 : ActiveInputs >= inputCount;
            if (output == IsOn) return;
            IsOn = output;
            PowerUtil.Send(poweredObjects, output);
        }
    }
}
