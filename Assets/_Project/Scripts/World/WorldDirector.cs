using RobotRescue.Bots;
using RobotRescue.Campaign;
using RobotRescue.Core;
using RobotRescue.Narrative;
using RobotRescue.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RobotRescue.World
{
    /// <summary>
    /// Runs the open world: every call lives in one scene on Brightwater hill. Starts on a title flyover; Continue
    /// drops PIP at the first unfinished call. Tracks which area PIP is in, unlocks the next one when a call is done,
    /// and handles "restart this call" by reloading the world straight into that area.
    /// </summary>
    public class WorldDirector : MonoBehaviour
    {
        public static WorldDirector Instance { get; private set; }

        /// <summary>Another destination's scene and the calls (campaign indices) that live there.</summary>
        [System.Serializable]
        public struct WorldLink
        {
            public string scene;
            public int firstLevel, lastLevel;
        }

        /// <summary>Spawn key meaning "arrive by mission pod" instead of at a call.</summary>
        public const int PodKey = -2;

        [Tooltip("Shown on the title card and in toasts, e.g. 'Bureau Station'.")]
        public string displayName = "Brightwater";
        [Tooltip("Scene with the title screen (the Bureau Station).")]
        public string titleScene;
        [Tooltip("Where PIP stands after a mission pod lands here.")]
        public Transform podSpawn;
        public WorldLink[] links;

        [Tooltip("In campaign order.")]
        public Area[] areas;
        public BotController bot;
        public BotCamera botCamera;
        public Camera titleCamera;
        public MainMenu titleMenu;
        [Tooltip("Shared DISPATCH lines for the spaces between calls (battery, bolts, parts...).")]
        public DispatchNarrator worldNarrator;

        public Area Active { get; private set; }
        public bool InTitle { get; private set; }
        public float EnteredTime { get; private set; } = -100f;
        public string Toast { get; private set; }
        public float ToastTime { get; private set; } = -100f;

        const string SpawnKey = "rr.worldSpawn";

        void Awake() => Instance = this;
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            int spawn = PlayerPrefs.GetInt(SpawnKey, -1);
            PlayerPrefs.DeleteKey(SpawnKey);
            if (spawn == PodKey && podSpawn) Arrive(podSpawn, "Welcome to " + displayName + "!");
            else if (spawn >= 0) Begin(spawn);
            else ShowTitle();
        }

        public void ShowTitle()
        {
            InTitle = true;
            if (bot) bot.gameObject.SetActive(false);
            if (botCamera) botCamera.gameObject.SetActive(false);
            if (titleCamera) titleCamera.gameObject.SetActive(true);
            if (titleMenu) titleMenu.enabled = true;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// <summary>
        /// Puts PIP on the porch of the call with campaign index <paramref name="levelIndex"/>. If that call is in
        /// another destination, flies there first.
        /// </summary>
        public void Begin(int levelIndex)
        {
            if (areas == null || areas.Length == 0) return;
            var area = System.Array.Find(areas, a => a && a.levelIndex == levelIndex);
            if (!area && links != null)
                foreach (var link in links)
                    if (levelIndex >= link.firstLevel && levelIndex <= link.lastLevel && link.scene != SceneManager.GetActiveScene().name)
                    {
                        Load(link.scene, levelIndex);
                        return;
                    }
            if (!area) area = areas[levelIndex < areas[0].levelIndex ? 0 : areas.Length - 1];
            Arrive(area.spawnPoint, area.DisplayTitle + "  -  walk in through the gate");
        }

        /// <summary>Hands control to PIP standing at <paramref name="spot"/>.</summary>
        public void Arrive(Transform spot, string toast)
        {
            InTitle = false;
            if (titleMenu) titleMenu.enabled = false;
            if (titleCamera) titleCamera.gameObject.SetActive(false);
            if (bot)
            {
                bot.gameObject.SetActive(true);
                if (spot)
                {
                    bot.Teleport(spot.position, spot.eulerAngles.y);
                    bot.RespawnPoint = spot.position;
                }
            }
            if (botCamera)
            {
                botCamera.gameObject.SetActive(true);
                botCamera.Snap();
            }
            // PIP starts outside the call's gate; walking in through the gate starts the call.
            if (Active) Active.Deactivate();
            Active = null;
            ShowToast(toast);
        }

        /// <summary>Mission pod: PIP climbs in, the pod lifts off, and the destination loads.</summary>
        public void Launch(string scene, int spawnKey, string destination)
        {
            if (launching) return;
            launching = true;
            ShowToast("Mission pod launching: " + destination + "!");
            EventBus<NarrationCue>.Raise(new NarrationCue("pod_launch"));
            if (bot) bot.gameObject.SetActive(false);
            StartCoroutine(LaunchAfter(scene, spawnKey));
        }

        bool launching;

        System.Collections.IEnumerator LaunchAfter(string scene, int spawnKey)
        {
            yield return new WaitForSeconds(2.2f);
            Load(scene, spawnKey);
        }

        void Load(string scene, int spawnKey)
        {
            PlayerPrefs.SetInt(SpawnKey, spawnKey);
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
        }

        public void Continue() => Begin(CampaignProgress.Frontier);

        public void Enter(Area area)
        {
            if (!area || area == Active || InTitle) return;
            if (Active) Active.Deactivate();
            Active = area;
            EnteredTime = Time.time;
            if (bot && area.spawnPoint) bot.RespawnPoint = area.spawnPoint.position;
            area.Activate();
        }

        /// <summary>Called when PIP reaches the active area's exit flag.</summary>
        public void CompleteActive()
        {
            if (!Active || Active.IsComplete) return;
            Active.MarkComplete();
            EventBus<NarrationCue>.Raise(new NarrationCue("exit"));
            ShowToast("CALL COMPLETE!  " + Active.DisplayTitle);
            EventBus<NarrationCue>.Raise(new NarrationCue("hum_" + (Active.levelIndex + 1)));
            int next = System.Array.IndexOf(areas, Active) + 1;
            if (next < areas.Length)
            {
                areas[next].Unlock();
                EventBus<NarrationCue>.Raise(new NarrationCue("next_area"));
            }
            else if (links != null && System.Array.Exists(links, l => l.firstLevel == Active.levelIndex + 1))
            {
                ShowToast("New mission unlocked! Head to the mission pod at the end of the walkway.");
                EventBus<NarrationCue>.Raise(new NarrationCue("mission_unlocked"));
            }
            else if (Active.levelIndex >= CampaignProgress.Levels.Count - 1)
            {
                // The finale: let the fireworks and the last DISPATCH line play, then roll the credits.
                ShowToast("THE REACH IS ANCHORED!");
                EventBus<NarrationCue>.Raise(new NarrationCue("the_end"));
                if (!GetComponent<EndCredits>()) gameObject.AddComponent<EndCredits>().delay = 12f;
            }
            else ShowToast("Every call here answered! More missions coming soon.");
        }

        public void ShowToast(string text)
        {
            Toast = text;
            ToastTime = Time.time;
        }

        /// <summary>Reloads the world and drops PIP back at the start of the current call (fresh puzzle).</summary>
        public void RestartArea()
        {
            int index = Active ? Active.levelIndex : (areas != null && areas.Length > 0 ? areas[0].levelIndex : 0);
            Load(SceneManager.GetActiveScene().name, Mathf.Max(0, index));
        }

        public void ReturnToTitle()
        {
            PlayerPrefs.DeleteKey(SpawnKey);
            Time.timeScale = 1f;
            SceneManager.LoadScene(string.IsNullOrEmpty(titleScene) ? SceneManager.GetActiveScene().name : titleScene);
        }
    }
}
