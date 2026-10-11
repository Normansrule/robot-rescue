using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>Anything a socket, plate or switch can turn on and off (stoves, taps, doors, conveyors...).</summary>
    public interface IPowerable
    {
        void SetPowered(bool powered);
    }

    public static class PowerUtil
    {
        /// <summary>Sends power to every IPowerable on each listed object.</summary>
        public static void Send(GameObject[] targets, bool powered)
        {
            if (targets == null) return;
            foreach (var go in targets)
            {
                if (!go) continue;
                foreach (var p in go.GetComponents<IPowerable>()) p.SetPowered(powered);
            }
        }
    }
}
