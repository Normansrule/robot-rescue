using UnityEngine;

namespace RobotRescue.Tools
{
    public enum ToolKind { Grabber, Whisk, Screwdriver, Magnet }

    /// <summary>One head for PIP's Tool Arm. Unlocked over the campaign: Grabber, Whisk, Screwdriver, Magnet.</summary>
    [CreateAssetMenu(menuName = "Robot Rescue/Tool Definition", fileName = "Tool_")]
    public class ToolDefinition : ScriptableObject
    {
        public string id = "grabber";
        public string displayName = "Grabber";
        public ToolKind kind;
        public Color color = Color.white;
        [Tooltip("Verb for the HUD prompt, e.g. 'whisk'.")]
        public string useVerb = "use";
    }
}
