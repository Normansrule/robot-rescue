using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.Tasks.Rescue
{
    /// <summary>
    /// Unit Zero, the Bureau's very first bot. Too flat to move until he's charged; after that PIP can carry him
    /// (or Tether-pull him) to safety.
    /// </summary>
    public class ZeroBot : Carryable
    {
        [Tooltip("False until his battery is charged enough to be moved safely.")]
        public bool awake;

        public override bool CanPickUp => base.CanPickUp && awake;
        public override bool CanAttach => base.CanAttach && awake;
        public override string PromptVerb => awake ? base.PromptVerb : "Unit Zero needs charging first";
    }
}
