using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// Procedural animation for PIP: stompy feet, swinging arms, body lean, carrying pose, climbing reach,
    /// tool-arm buzz, waving, a happy spin, and a sad slump when the battery dies. No animator controller needed.
    /// </summary>
    public class BotAnimator : MonoBehaviour
    {
        public Transform body, head, armL, armR, footL, footR;

        BotController bot;
        Grabber grabber;
        ToolArm toolArm;
        TetherCable tether;
        BotBattery battery;
        Vector3 footLPos, footRPos, headPos;
        Quaternion armLRot, armRRot, bodyRot, headRot;
        float phase, waveUntil, spinStart = -10f, spinDuration, spinTurns, hopUntil;

        void Awake()
        {
            bot = GetComponent<BotController>();
            grabber = GetComponent<Grabber>();
            toolArm = GetComponent<ToolArm>();
            tether = GetComponent<TetherCable>();
            battery = GetComponent<BotBattery>();
            if (footL) footLPos = footL.localPosition;
            if (footR) footRPos = footR.localPosition;
            if (armL) armLRot = armL.localRotation;
            if (armR) armRRot = armR.localRotation;
            if (body) bodyRot = body.localRotation;
            if (head) { headRot = head.localRotation; headPos = head.localPosition; }
        }

        public void Wave(float seconds = 1.6f) => waveUntil = Time.time + seconds;

        public void Spin(float seconds = 1f, float turns = 1f)
        {
            spinStart = Time.time;
            spinDuration = seconds;
            spinTurns = turns;
        }

        public void Hop() => hopUntil = Time.time + 0.35f;

        void LateUpdate()
        {
            float t = Time.time, dt = Time.deltaTime;
            bool grounded = bot && bot.IsGrounded;
            bool climbing = bot && bot.IsClimbing;
            bool dead = battery && battery.IsDead;
            float speed = bot ? bot.Speed01 : 0f;
            bool carrying = grabber && grabber.Held;
            bool swinging = tether && tether.IsSwinging;
            bool usingTool = toolArm && toolArm.isActiveAndEnabled && toolArm.IsUsing;

            if (climbing && bot.Body.linearVelocity.sqrMagnitude > 0.05f) phase += dt * 9f;
            else if (grounded && speed > 0.05f) phase += dt * (7f + 7f * speed);
            float stride = climbing ? 0.6f : grounded ? speed : 0.25f;
            float s = Mathf.Sin(phase), c = Mathf.Cos(phase);

            if (footL) footL.localPosition = footLPos + new Vector3(0f, Mathf.Max(0f, s) * 0.13f * stride, c * 0.16f * stride);
            if (footR) footR.localPosition = footRPos + new Vector3(0f, Mathf.Max(0f, -s) * 0.13f * stride, -c * 0.16f * stride);

            // Arms: swing while walking, up overhead to carry or swing, alternate reaching while climbing.
            float swingL = s * 40f * speed, swingR = -s * 40f * speed;
            Quaternion targetL = armLRot * Quaternion.Euler(swingL, 0f, 0f);
            Quaternion targetR = armRRot * Quaternion.Euler(swingR, 0f, 0f);
            if (carrying || swinging)
            {
                targetL = armLRot * Quaternion.Euler(-160f, 0f, -10f);
                targetR = armRRot * Quaternion.Euler(-160f, 0f, 10f);
            }
            if (climbing)
            {
                targetL = armLRot * Quaternion.Euler(-150f + s * 30f, 0f, 0f);
                targetR = armRRot * Quaternion.Euler(-150f - s * 30f, 0f, 0f);
            }
            if (usingTool) targetR = armRRot * Quaternion.Euler(-80f + Mathf.Sin(t * 40f) * 8f, 0f, 0f);
            if (t < waveUntil) targetR = armRRot * Quaternion.Euler(0f, 0f, 150f + Mathf.Sin(t * 14f) * 25f);
            if (dead)
            {
                targetL = armLRot * Quaternion.Euler(10f, 0f, -20f);
                targetR = armRRot * Quaternion.Euler(10f, 0f, 20f);
            }
            float k = 1f - Mathf.Exp(-16f * dt);
            if (armL) armL.localRotation = Quaternion.Slerp(armL.localRotation, targetL, k);
            if (armR) armR.localRotation = Quaternion.Slerp(armR.localRotation, targetR, k);

            // Body: lean into the run, bob with each step, spin for joy.
            float spinU = spinDuration > 0f ? Mathf.Clamp01((t - spinStart) / spinDuration) : 1f;
            float spin = spinU < 1f ? Mathf.SmoothStep(0f, 360f * spinTurns, spinU) : 0f;
            if (body) body.localRotation = bodyRot * Quaternion.Euler(speed * 10f + (dead ? 18f : 0f), spin, 0f);

            float bob = grounded ? Mathf.Abs(s) * 0.04f * speed + Mathf.Sin(t * 2.2f) * 0.012f : 0f;
            float hop = t < hopUntil ? Mathf.Sin((hopUntil - t) / 0.35f * Mathf.PI) * 0.25f : 0f;
            if (head)
            {
                head.localPosition = headPos + Vector3.up * (bob + hop);
                Quaternion headTarget = headRot * Quaternion.Euler(dead ? 30f : 0f, spin, 0f);
                head.localRotation = Quaternion.Slerp(head.localRotation, headTarget, k);
            }
        }
    }
}
