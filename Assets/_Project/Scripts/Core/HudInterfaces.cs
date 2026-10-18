using UnityEngine;

namespace RobotRescue.Core
{
    /// <summary>Anything that shows a little progress bar over itself in the world (pots cooking, bowls whisking).</summary>
    public interface IProgressIndicator
    {
        bool ShowProgress { get; }
        Vector3 IndicatorPosition { get; }
        float Progress01 { get; }
        Color ProgressColor { get; }
    }

    /// <summary>A big top-of-screen meter for boss levels (pressure, critic mood...).</summary>
    public interface IBossMeter
    {
        string MeterTitle { get; }
        float Meter01 { get; }
        Color MeterColor { get; }
        /// <summary>Extra line under the bar, e.g. "Hold all three vents open!".</summary>
        string MeterStatus { get; }
        /// <summary>Non-empty once the player has lost (shown big, with the restart hint).</summary>
        string FailureText { get; }
    }
}

namespace RobotRescue.Core
{
    /// <summary>
    /// Marks behaviours that should only run while the player is in their area of the open world (boss timers,
    /// order boards, task runners, narrators). <see cref="World.Area"/> switches them on and off.
    /// Outside the world (a single level scene) they simply run.
    /// </summary>
    public interface IAreaGated { }
}
