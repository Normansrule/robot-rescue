using RobotRescue.Campaign;
using RobotRescue.World;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RobotRescue.Core
{
    /// <summary>
    /// Per-scene game flow: intro cue, restart, pause, level complete and moving on to the next call.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public bool LevelComplete { get; private set; }
        public bool Paused { get; private set; }

        InputAction restartAction, pauseAction, continueAction;

        void Awake()
        {
            Instance = this;
            Time.timeScale = 1f;

            restartAction = new InputAction("Restart", InputActionType.Button);
            restartAction.AddBinding("<Keyboard>/backspace");
            restartAction.AddBinding("<Gamepad>/select");

            pauseAction = new InputAction("Pause", InputActionType.Button);
            pauseAction.AddBinding("<Keyboard>/escape");
            pauseAction.AddBinding("<Keyboard>/p");
            pauseAction.AddBinding("<Gamepad>/start");

            continueAction = new InputAction("Continue", InputActionType.Button);
            continueAction.AddBinding("<Keyboard>/enter");
            continueAction.AddBinding("<Keyboard>/numpadEnter");
            continueAction.AddBinding("<Gamepad>/buttonSouth");
        }

        void OnEnable()
        {
            restartAction.Enable();
            pauseAction.Enable();
            continueAction.Enable();
            EventBus<LevelCompleted>.Subscribe(OnLevelCompleted);
        }

        void OnDisable()
        {
            restartAction.Disable();
            pauseAction.Disable();
            continueAction.Disable();
            EventBus<LevelCompleted>.Unsubscribe(OnLevelCompleted);
        }

        void OnDestroy()
        {
            restartAction.Dispose();
            pauseAction.Dispose();
            continueAction.Dispose();
            if (Instance == this) Instance = null;
        }

        /// <summary>True in the open-world scene, where calls flow into each other without loading screens.</summary>
        public static bool InWorld => WorldDirector.Instance;

        void Start()
        {
            // In the world each area says its own intro when PIP walks in.
            if (!InWorld) EventBus<NarrationCue>.Raise(new NarrationCue("intro"));
        }

        void Update()
        {
            if (InWorld && WorldDirector.Instance.InTitle) return;
            if (restartAction.WasPressedThisFrame()) Restart();
            if (pauseAction.WasPressedThisFrame() && !LevelComplete) SetPaused(!Paused);
            if (LevelComplete && continueAction.WasPressedThisFrame()) CampaignProgress.LoadNext();
        }

        public void Restart()
        {
            if (InWorld) WorldDirector.Instance.RestartArea();
            else CampaignProgress.Load(SceneManager.GetActiveScene().name);
        }

        public void QuitToMenu()
        {
            if (InWorld) WorldDirector.Instance.ReturnToTitle();
            else CampaignProgress.LoadMenu();
        }

        public void SetPaused(bool paused)
        {
            Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }

        void OnLevelCompleted(LevelCompleted _)
        {
            if (InWorld)
            {
                WorldDirector.Instance.CompleteActive();
                return;
            }
            if (LevelComplete) return;
            LevelComplete = true;
            string scene = SceneManager.GetActiveScene().name;
            if (CampaignProgress.IsSideCall(scene)) CampaignProgress.MarkSideCallComplete(scene);
            else CampaignProgress.MarkComplete(scene);
            EventBus<NarrationCue>.Raise(new NarrationCue("exit"));
        }
    }
}
