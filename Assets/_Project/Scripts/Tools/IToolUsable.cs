using RobotRescue.Bots;

namespace RobotRescue.Tools
{
    /// <summary>Something PIP works on by holding Use with the right tool equipped (a bowl to whisk, a bolt to screw...).</summary>
    public interface IToolUsable
    {
        ToolKind RequiredTool { get; }
        bool CanUse { get; }
        /// <summary>Called every frame Use is held while this is in front of PIP.</summary>
        void UseTool(ToolArm arm, float deltaTime);
    }
}
