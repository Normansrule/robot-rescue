using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RobotRescue.Campaign
{
    /// <summary>Level order, unlocks (saved in PlayerPrefs) and scene loading for the campaign.</summary>
    public static class CampaignProgress
    {
        const string UnlockedKey = "rr.unlockedLevels";
        static CampaignDefinition campaign;

        public static CampaignDefinition Campaign => campaign ? campaign : campaign = Resources.Load<CampaignDefinition>("Campaign");

        /// <summary>Every level in play order, with its chapter index.</summary>
        public static List<(int chapter, LevelEntry level)> Levels
        {
            get
            {
                var list = new List<(int, LevelEntry)>();
                if (!Campaign) return list;
                for (int c = 0; c < Campaign.chapters.Length; c++)
                    foreach (var l in Campaign.chapters[c].levels) list.Add((c, l));
                return list;
            }
        }

        public static int UnlockedCount => Mathf.Max(1, PlayerPrefs.GetInt(UnlockedKey, 1));

        public static int IndexOf(string scene) => Levels.FindIndex(l => l.level.scene == scene);

        public static bool IsUnlocked(int levelIndex) => levelIndex < UnlockedCount;

        /// <summary>Finished in this or an earlier session (the level after it is unlocked).</summary>
        public static bool IsComplete(int levelIndex) => levelIndex >= 0 && levelIndex < UnlockedCount - 1;

        /// <summary>Index of the first level not yet finished.</summary>
        public static int Frontier => Mathf.Clamp(UnlockedCount - 1, 0, Mathf.Max(0, Levels.Count - 1));

        public static void MarkComplete(string scene)
        {
            int i = IndexOf(scene);
            if (i < 0 || i + 2 <= UnlockedCount) return;
            PlayerPrefs.SetInt(UnlockedKey, i + 2);
            PlayerPrefs.Save();
        }

        const string SideKey = "rr.side.";

        public static bool IsSideCall(string scene) =>
            Campaign && System.Array.Exists(Campaign.sideCalls ?? new LevelEntry[0], l => l.scene == scene);

        /// <summary>Side calls are remembered one by one and never touch the campaign unlock count.</summary>
        public static bool IsSideCallComplete(string scene) => PlayerPrefs.GetInt(SideKey + scene, 0) == 1;

        public static void MarkSideCallComplete(string scene)
        {
            PlayerPrefs.SetInt(SideKey + scene, 1);
            PlayerPrefs.Save();
        }

        public static void ResetProgress()
        {
            if (Campaign && Campaign.sideCalls != null)
                foreach (var s in Campaign.sideCalls) PlayerPrefs.DeleteKey(SideKey + s.scene);
            PlayerPrefs.DeleteKey(UnlockedKey);
            Collectible.ResetAll();
            Bots.ToolArm.ResetSaved();
            PlayerPrefs.Save();
        }

        public static void Load(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
        }

        public static void LoadMenu() => Load(Campaign ? Campaign.mainMenuScene : "MainMenu");

        /// <summary>The level after the current one, or the menu at the end of the campaign.</summary>
        public static void LoadNext()
        {
            var levels = Levels;
            int i = IndexOf(SceneManager.GetActiveScene().name);
            if (i >= 0 && i + 1 < levels.Count) Load(levels[i + 1].level.scene);
            else LoadMenu();
        }

        /// <summary>First level not yet completed.</summary>
        public static void Continue()
        {
            var levels = Levels;
            if (levels.Count == 0) return;
            Load(levels[Mathf.Clamp(UnlockedCount - 1, 0, levels.Count - 1)].level.scene);
        }

        public static string CurrentTitle
        {
            get
            {
                var levels = Levels;
                int i = IndexOf(SceneManager.GetActiveScene().name);
                if (i < 0) return null;
                return $"{Campaign.chapters[levels[i].chapter].title}  ·  {levels[i].level.title}";
            }
        }
    }
}
