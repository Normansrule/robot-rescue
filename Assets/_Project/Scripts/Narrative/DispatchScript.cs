using System;
using UnityEngine;

namespace RobotRescue.Narrative
{
    /// <summary>
    /// DISPATCH's lines for one chamber, keyed by cue id. Several lines with the same cue play in turn.
    /// Stand-in until Yarn Spinner is wired up; cue ids map straight onto Yarn node names.
    /// </summary>
    [CreateAssetMenu(menuName = "Robot Rescue/Dispatch Script", fileName = "Dispatch_")]
    public class DispatchScript : ScriptableObject
    {
        public NarrationLine[] lines = Array.Empty<NarrationLine>();
    }

    [Serializable]
    public class NarrationLine
    {
        public string cue;
        [TextArea(2, 4)] public string text;
        public float duration = 4f;
        [Tooltip("Can play again every time the cue fires (otherwise once per scene).")]
        public bool repeatable;
    }
}
