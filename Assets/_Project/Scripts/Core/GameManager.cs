using RobotRescue.Campaign;
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

        void Start() => EventBus<NarrationCue>.Raise(new NarrationCue("intro"));

        void Update()
        {
            if (restartAction.WasPressedThisFrame()) Restart();
            if (pauseAction.WasPressedThisFrame() && !LevelComplete) SetPaused(!Paused);
            if (LevelComplete && continueAction.WasPressedThisFrame()) CampaignProgress.LoadNext();
        }

        public void Restart() => CampaignProgress.Load(SceneManager.GetActiveScene().name);

        public void SetPaused(bool paused)
        {
            Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = paused;
        }

        void OnLevelCompleted(LevelCompleted _)
        {
            if (LevelComplete) return;
            LevelComplete = true;
            CampaignProgress.MarkComplete(SceneManager.GetActiveScene().name);
            EventBus<NarrationCue>.Raise(new NarrationCue("exit"));
        }
    }
}
