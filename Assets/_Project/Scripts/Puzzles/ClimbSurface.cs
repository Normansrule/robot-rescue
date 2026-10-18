using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// Grippy surface PIP can climb (PEAK-style): rock faces, trellises, pegboards. Put it on the collider's object
    /// or a parent. Climbing drains stamina; jump while climbing to leap higher.
    /// </summary>
    public class ClimbSurface : MonoBehaviour
    {
        [Tooltip("Multiplies stamina drain while clinging here (icy rock > 1, a ladder < 1).")]
        public float staminaMultiplier = 1f;
    }
}
