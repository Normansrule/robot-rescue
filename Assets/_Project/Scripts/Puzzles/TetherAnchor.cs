namespace RobotRescue.Puzzles
{
    /// <summary>A fixed hook the Tether Cable can swing from.</summary>
    public class TetherAnchor : TetherTarget
    {
        public override TetherMode Mode => TetherMode.Swing;
    }
}
