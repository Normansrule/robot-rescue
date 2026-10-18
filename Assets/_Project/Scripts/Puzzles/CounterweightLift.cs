using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// A gate or drawbridge hung on a pulley with a weighing pan on the other end. Put weight in the pan and it opens
    /// (a gate slides up, a drawbridge swings down); it stays open for as long as the weight stays in. The mechanism
    /// holds the state, so you don't have to stand there. Every bit of weight moves it a little, so you can see the
    /// rule working, but only the full weight opens it all the way. A motor input (a cradle with a full cell) also
    /// opens it fully: that's the second way to solve it. It never closes on something in its safety zone.
    /// </summary>
    public class CounterweightLift : MonoBehaviour, IPowerable
    {
        [Tooltip("What moves: a gate or a drawbridge pivot (kinematic body).")]
        public Rigidbody load;
        [Tooltip("Slide when open (a gate).")]
        public Vector3 openOffset = new Vector3(0f, 3f, 0f);
        [Tooltip("Turn when open, local degrees (a drawbridge). Leave zero for a sliding gate.")]
        public Vector3 openEuler;
        [Tooltip("The weighing pan (kinematic body) and its trigger.")]
        public Rigidbody panBody;
        public WeighZone pan;
        [Tooltip("How far the pan sinks at full weight.")]
        public float panDrop = 0.4f;
        public float requiredMass = 3f;
        [Tooltip("Opening follows (weight / required) ^ curve: a little weight shows movement, only full weight opens it.")]
        public float curve = 2f;
        [Tooltip("Metres per second when sliding.")]
        public float speed = 1.1f;
        [Tooltip("Degrees per second when turning.")]
        public float degreesPerSecond = 35f;
        [Tooltip("While anything is in here (under a gate, on a bridge) it won't close.")]
        public WeighZone safety;
        [Tooltip("Gauge needle: turns with the weight in the pan (or all the way when the motor runs).")]
        public Transform needle;
        public float needleSweep = 150f;
        [Tooltip("Powered on while fully open (step signals, lights).")]
        public GameObject[] poweredObjects;
        public string openCue;
        [Tooltip("Said once, the first time there's some weight but not enough.")]
        public string partialCue = "counterweight_partial";

        public bool IsOpen { get; private set; }
        public bool MotorOn { get; private set; }
        public float Open01 { get; private set; }
        public float Weight01 => pan ? Mathf.Clamp01(pan.Mass / Mathf.Max(0.01f, requiredMass)) : 0f;

        Vector3 loadRest, panRest;
        Quaternion loadRestRot, needleRest;
        bool partialSaid;

        bool Turns => openEuler != Vector3.zero;

        void Awake()
        {
            if (load)
            {
                loadRest = load.position;
                loadRestRot = load.rotation;
            }
            if (panBody) panRest = panBody.position;
            if (needle) needleRest = needle.localRotation;
        }

        public void SetPowered(bool on) => MotorOn = on;

        void FixedUpdate()
        {
            float w = Weight01;
            float target = MotorOn ? 1f : Mathf.Pow(w, curve);
            if (target < Open01 && safety && safety.AnythingInside) target = Open01;

            float rate = Turns ? degreesPerSecond / Mathf.Max(1f, MaxAngle()) : speed / Mathf.Max(0.01f, openOffset.magnitude);
            Open01 = Mathf.MoveTowards(Open01, target, rate * Time.fixedDeltaTime);
            if (load)
            {
                if (Turns) load.MoveRotation(loadRestRot * Quaternion.Euler(openEuler * Open01));
                else load.MovePosition(loadRest + openOffset * Open01);
            }
            if (panBody) panBody.MovePosition(panRest + Vector3.down * panDrop * w);
            if (needle) needle.localRotation = needleRest * Quaternion.Euler(0f, 0f, -needleSweep * (MotorOn ? 1f : w));

            if (!partialSaid && !MotorOn && w > 0.05f && w < 0.999f)
            {
                partialSaid = true;
                if (!string.IsNullOrEmpty(partialCue)) EventBus<NarrationCue>.Raise(new NarrationCue(partialCue));
            }

            bool open = Open01 >= 0.99f;
            if (open == IsOpen) return;
            IsOpen = open;
            PowerUtil.Send(poweredObjects, open);
            if (open && !string.IsNullOrEmpty(openCue)) EventBus<NarrationCue>.Raise(new NarrationCue(openCue));
        }

        float MaxAngle() => Mathf.Max(Mathf.Abs(openEuler.x), Mathf.Max(Mathf.Abs(openEuler.y), Mathf.Abs(openEuler.z)));
    }
}
