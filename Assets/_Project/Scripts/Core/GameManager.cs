using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace RobotRescue.Core
{
    /// <summary>
    /// Per-scene game state for now: restart button and level-complete flag.
    /// Becomes the persistent bootstrapper once SaveSystem/SceneLoader exist.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public bool LevelComplete { get; private set; }

        InputAction restartAction;

        void Awake()
        {
            Instance = this;
            restartAction = new InputAction("Restart", InputActionType.Button);
            restartAction.AddBinding("<Keyboard>/backspace");
            restartAction.AddBinding("<Gamepad>/select");
        }

        void OnEnable()
        {
            restartAction.Enable();
            EventBus<LevelCompleted>.Subscribe(OnLevelCompleted);
        }

        void OnDisable()
        {
            restartAction.Disable();
            EventBus<LevelCompleted>.Unsubscribe(OnLevelCompleted);
        }

        void Start() => EventBus<NarrationCue>.Raise(new NarrationCue("intro"));

        void Update()
        {
            if (restartAction.WasPressedThisFrame())
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        void OnLevelCompleted(LevelCompleted _)
        {
            if (LevelComplete) return;
            LevelComplete = true;
            EventBus<NarrationCue>.Raise(new NarrationCue("exit"));
        }
    }
}
