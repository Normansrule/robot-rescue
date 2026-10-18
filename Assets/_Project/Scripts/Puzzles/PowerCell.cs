using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Puzzles
{
    /// <summary>
    /// A portable battery. Charge it in a charging cradle by plugging the Tether into that cradle's socket (the bot is
    /// the power), then carry it anywhere. A full cell sitting in a <see cref="CellCradle"/> powers that cradle's targets.
    /// Cells keep their charge, so power can travel further than the cable reaches. They're heavy, too: a cell counts on
    /// a counterweight pan like any crate.
    /// </summary>
    public class PowerCell : Carryable, IProgressIndicator
    {
        [Range(0f, 1f)] public float startCharge;
        [Tooltip("Charge bars, bottom to top. How many are lit shows the charge, so it reads without colour too.")]
        public Renderer[] bars;
        [Tooltip("Charge levels the bars show. With more bars than levels, bars are grouped (e.g. front and back face).")]
        public int levels = 4;
        public Color barOff = new Color(0.22f, 0.24f, 0.3f);
        public Color barOn = new Color(1f, 0.8f, 0.25f);
        public Color barFull = new Color(0.35f, 1f, 0.5f);
        [Tooltip("Task step completed the first time this cell is filled.")]
        public string chargedStepId;
        public string chargedCue = "cell_full";

        public float Charge01 { get; private set; }
        public bool IsFull => Charge01 >= 0.999f;

        MaterialPropertyBlock mpb;
        float lastChargedAt = -10f;
        bool filledOnce;

        protected override void Awake()
        {
            base.Awake();
            Charge01 = startCharge;
            RefreshBars();
        }

        /// <summary>Adds charge (a cradle calls this while the bot is plugged into it).</summary>
        public void AddCharge(float amount)
        {
            if (IsFull || amount <= 0f) return;
            lastChargedAt = Time.time;
            Charge01 = Mathf.Min(1f, Charge01 + amount);
            if (IsFull)
            {
                Charge01 = 1f;
                if (!filledOnce && !string.IsNullOrEmpty(chargedStepId)) EventBus<TaskStepCompleted>.Raise(new TaskStepCompleted(chargedStepId));
                filledOnce = true;
                if (!string.IsNullOrEmpty(chargedCue)) EventBus<NarrationCue>.Raise(new NarrationCue(chargedCue));
            }
            RefreshBars();
        }

        /// <summary>Back to the charge it started the level with (puzzle reset).</summary>
        public void ResetCharge()
        {
            Charge01 = startCharge;
            RefreshBars();
        }

        void RefreshBars()
        {
            if (bars == null) return;
            mpb ??= new MaterialPropertyBlock();
            for (int i = 0; i < bars.Length; i++)
            {
                if (!bars[i]) continue;
                int n = Mathf.Clamp(levels, 1, bars.Length);
                int level = i * n / bars.Length;
                bool lit = Charge01 >= (level + 1f) / n - 0.001f;
                Color c = !lit ? barOff : IsFull ? barFull : barOn;
                mpb.SetColor("_BaseColor", c);
                mpb.SetColor("_Color", c);
                mpb.SetColor("_EmissionColor", lit ? c * 1.6f : Color.black);
                bars[i].SetPropertyBlock(mpb);
            }
        }

        public bool ShowProgress => Time.time - lastChargedAt < 0.3f;
        public Vector3 IndicatorPosition => transform.position + Vector3.up * 0.9f;
        public float Progress01 => Charge01;
        public Color ProgressColor => IsFull ? barFull : barOn;
    }
}
