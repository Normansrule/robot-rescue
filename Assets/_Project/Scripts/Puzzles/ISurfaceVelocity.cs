using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>A surface that carries whatever stands on it (conveyors now, moving platforms later).</summary>
    public interface ISurfaceVelocity
    {
        Vector3 SurfaceVelocity { get; }
    }
}
