using System;
using UnityEngine;

namespace RobotRescue.Campaign
{
    /// <summary>The single-player campaign: chapters of calls, in play order. Lives in a Resources folder as "Campaign".</summary>
    [CreateAssetMenu(menuName = "Robot Rescue/Campaign", fileName = "Campaign")]
    public class CampaignDefinition : ScriptableObject
    {
        public string mainMenuScene = "MainMenu";
        public ChapterEntry[] chapters = Array.Empty<ChapterEntry>();
    }

    [Serializable]
    public class ChapterEntry
    {
        public string title;
        public string location;
        public LevelEntry[] levels = Array.Empty<LevelEntry>();
    }

    [Serializable]
    public class LevelEntry
    {
        [Tooltip("Scene file name without .unity; must be in Build Settings.")]
        public string scene;
        public string title;
        public bool isBigRescue;
    }
}
