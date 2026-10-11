using UnityEngine;
using UnityEngine.InputSystem;

namespace RobotRescue.Bots
{
    /// <summary>
    /// Keyboard/mouse + gamepad bindings, built in code so the prototype has no asset dependencies.
    /// Swap for a generated .inputactions asset when remapping UI lands.
    /// Default layout (pad): A jump, X grab/drop, RT throw, LT tether, RB/LB reel in/out.
    /// </summary>
    public class BotInput : MonoBehaviour
    {
        InputActionMap map;
        InputAction move, lookMouse, lookStick, jump, interact, throwAction, tether, reel;

        public Vector2 Move => move.ReadValue<Vector2>();
        /// <summary>Mouse look in pixels this frame.</summary>
        public Vector2 LookMouse => lookMouse.ReadValue<Vector2>();
        /// <summary>Stick look, -1..1, multiply by deltaTime.</summary>
        public Vector2 LookStick => lookStick.ReadValue<Vector2>();
        public bool JumpPressed => jump.WasPressedThisFrame();
        public bool JumpHeld => jump.IsPressed();
        public bool InteractPressed => interact.WasPressedThisFrame();
        public bool ThrowPressed => throwAction.WasPressedThisFrame();
        public bool TetherPressed => tether.WasPressedThisFrame();
        /// <summary>+1 reel in, -1 reel out.</summary>
        public float Reel => reel.ReadValue<float>();

        void Awake()
        {
            map = new InputActionMap("Bot");

            move = map.AddAction("Move", InputActionType.Value);
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            move.AddBinding("<Gamepad>/leftStick");

            lookMouse = map.AddAction("LookMouse", InputActionType.Value, "<Mouse>/delta");
            lookStick = map.AddAction("LookStick", InputActionType.Value, "<Gamepad>/rightStick");

            jump = map.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
            jump.AddBinding("<Gamepad>/buttonSouth");

            interact = map.AddAction("Interact", InputActionType.Button, "<Keyboard>/e");
            interact.AddBinding("<Gamepad>/buttonWest");

            throwAction = map.AddAction("Throw", InputActionType.Button, "<Mouse>/leftButton");
            throwAction.AddBinding("<Gamepad>/rightTrigger");

            tether = map.AddAction("Tether", InputActionType.Button, "<Mouse>/rightButton");
            tether.AddBinding("<Gamepad>/leftTrigger");

            reel = map.AddAction("Reel", InputActionType.Value);
            reel.AddCompositeBinding("1DAxis").With("Positive", "<Keyboard>/q").With("Negative", "<Keyboard>/z");
            reel.AddCompositeBinding("1DAxis").With("Positive", "<Gamepad>/rightShoulder").With("Negative", "<Gamepad>/leftShoulder");
        }

        void OnEnable() => map.Enable();
        void OnDisable() => map.Disable();
        void OnDestroy() => map.Dispose();
    }
}
