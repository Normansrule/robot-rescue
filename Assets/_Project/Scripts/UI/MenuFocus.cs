using UnityEngine;
using UnityEngine.InputSystem;

namespace RobotRescue.UI
{
    /// <summary>
    /// Keyboard and gamepad focus for a vertical IMGUI menu: up / down (arrows, W / S, D-pad, left stick) move a cyan
    /// focus ring, Enter / A presses the focused button. The mouse still works, and hovering moves the ring too, so
    /// there is only ever one highlighted button. Focus starts on the first button, which is always the primary one.
    /// Call <see cref="Tick"/> from Update and draw buttons with <see cref="Button"/> inside OnGUI.
    /// </summary>
    public class MenuFocus
    {
        readonly InputAction up, down, confirm;
        int index, count, drawn;
        bool pressed;
        // The focus ring springs to the next button (slightly under-damped, so it lands with one small settle).
        Rect ring; Vector4 ringVel; bool ringPlaced; int ringFrame;

        public MenuFocus()
        {
            up = new InputAction("MenuUp", InputActionType.Button);
            up.AddBinding("<Keyboard>/upArrow");
            up.AddBinding("<Keyboard>/w");
            up.AddBinding("<Gamepad>/dpad/up");
            up.AddBinding("<Gamepad>/leftStick/up");
            down = new InputAction("MenuDown", InputActionType.Button);
            down.AddBinding("<Keyboard>/downArrow");
            down.AddBinding("<Keyboard>/s");
            down.AddBinding("<Gamepad>/dpad/down");
            down.AddBinding("<Gamepad>/leftStick/down");
            confirm = new InputAction("MenuConfirm", InputActionType.Button);
            confirm.AddBinding("<Keyboard>/enter");
            confirm.AddBinding("<Keyboard>/numpadEnter");
            confirm.AddBinding("<Gamepad>/buttonSouth");
        }

        public void Enable() { up.Enable(); down.Enable(); confirm.Enable(); }
        public void Disable() { up.Disable(); down.Disable(); confirm.Disable(); }
        public void Dispose() { up.Dispose(); down.Dispose(); confirm.Dispose(); }

        /// <summary>Back to the first (primary) button, e.g. when a page opens.</summary>
        public void Reset() { index = 0; pressed = false; ringPlaced = false; }

        /// <summary>Read input once per frame.</summary>
        public void Tick()
        {
            if (count == 0) return;
            if (up.WasPressedThisFrame()) index = (index - 1 + count) % count;
            if (down.WasPressedThisFrame()) index = (index + 1) % count;
            if (confirm.WasPressedThisFrame()) pressed = true;
        }

        /// <summary>Call at the top of each OnGUI pass, before the first <see cref="Button"/>.</summary>
        public void BeginGUI()
        {
            // Every pass draws the same buttons, so the last pass's total is this menu's size.
            if (Event.current.type == EventType.Layout) count = drawn;
            drawn = 0;
            if (count > 0) index = Mathf.Clamp(index, 0, count - 1);
        }

        void DrawRing(Rect target)
        {
            if (!ringPlaced || UiTheme.CalmMotion) { ring = target; ringVel = Vector4.zero; ringPlaced = true; }
            else if (ringFrame != Time.frameCount)
            {
                // A damped spring per edge: stiffness 600, damping 40 (about 0.8 of critical), in 1080-canvas pixels.
                float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
                var cur = new Vector4(ring.x, ring.y, ring.width, ring.height);
                var to = new Vector4(target.x, target.y, target.width, target.height);
                ringVel += (-(600f) * (cur - to) - 40f * ringVel) * dt;
                cur += ringVel * dt;
                ring = new Rect(cur.x, cur.y, cur.z, cur.w);
            }
            ringFrame = Time.frameCount;
            var live = UiTheme.Signal;
            UiTheme.Outline(ring, live, 2f, UiTheme.Radius + 4f);
            // The plug: a short live lead into the left edge, so focus reads as "this one is powered".
            UiTheme.Panel(new Rect(ring.x - 14f, ring.center.y - 1.5f, 12f, 3f), live, 1.5f);
            UiTheme.Panel(new Rect(ring.x - 20f, ring.center.y - 7f, 8f, 14f), live, 3f);
        }

        /// <summary>A focusable button. Returns true when clicked, or when Enter / A is pressed while it has focus.</summary>
        public bool Button(Rect r, string text, GUIStyle style)
        {
            int i = drawn++;
            var e = Event.current;
            if (e.type == EventType.MouseMove && r.Contains(e.mousePosition)) index = i;
            bool focused = i == index;
            bool hit = GUI.Button(r, text, style);
            if (focused && e.type == EventType.Repaint) DrawRing(new Rect(r.x - 4f, r.y - 4f, r.width + 8f, r.height + 8f));
            if (hit) { index = i; pressed = false; return true; }
            if (focused && pressed && e.type == EventType.Repaint) { pressed = false; return true; }
            return false;
        }
    }
}
