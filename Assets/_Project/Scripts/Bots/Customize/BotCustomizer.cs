using System.Collections.Generic;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// Dresses a rescue bot in its player's saved <see cref="BotLook"/>. The builder makes every variant (chassis, head,
    /// feet, topper, badge) as an inactive child and lists which renderers take which paint; this switches the chosen
    /// ones on and paints per-bot material copies. Lost parts are copies of the visuals, so they keep the paint job.
    /// Purely cosmetic: colliders, reach and the Tool Arm head colour never change.
    /// </summary>
    public class BotCustomizer : MonoBehaviour
    {
        [Tooltip("Which player's saved look to wear (0 = player 1).")]
        public int playerSlot;
        [Tooltip("Off for statues that a menu dresses directly.")]
        public bool followSaved = true;

        [Header("Variants, in BotCatalog order")]
        public GameObject[] chassis;
        public GameObject[] heads;
        public GameObject[] feetL;
        public GameObject[] feetR;
        public GameObject[] toppers;
        public GameObject[] badges;

        [Header("Paint roles")]
        public Renderer[] shell;
        public Renderer[] trim;
        public Renderer[] joints;
        [Tooltip("Self-lit bits (antenna bulb) that glow in the trim colour.")]
        public Renderer[] glow;

        [Header("Other parts")]
        public BotFace face;
        public Babble voice;
        public TetherCable tether;
        [Tooltip("Spins while the Propeller topper is on.")]
        public Transform propeller;

        public BotLook Look { get; private set; }
        public string BotName => Look != null ? Look.name : "PIP";

        readonly List<Material> owned = new List<Material>();
        Material shellMat, trimMat, jointMat, glowMat, cableMat;

        void Awake() => Apply(followSaved ? BotLookStore.Load(playerSlot) : Look ?? new BotLook());

        void OnEnable() => EventBus<BotLookChanged>.Subscribe(OnChanged);
        void OnDisable() => EventBus<BotLookChanged>.Unsubscribe(OnChanged);

        void OnDestroy()
        {
            foreach (var m in owned)
                if (m) Destroy(m);
        }

        void OnChanged(BotLookChanged e)
        {
            if (followSaved && e.Slot == playerSlot) Apply(BotLookStore.Load(playerSlot));
        }

        void Update()
        {
            if (propeller && propeller.gameObject.activeInHierarchy)
                propeller.Rotate(0f, 540f * Time.deltaTime, 0f, Space.Self);
        }

        /// <summary>Wears <paramref name="look"/> right now (the workshop calls this for live previews).</summary>
        public void Apply(BotLook look)
        {
            Look = (look ?? new BotLook()).Sanitized();

            Pick(chassis, Look.chassis);
            Pick(heads, Look.head);
            Pick(feetL, Look.feet);
            Pick(feetR, Look.feet);
            Pick(toppers, Look.topper);
            Pick(badges, Look.badge);

            Color trimColor = BotCatalog.Trims[Look.trim].color;
            shellMat = Painted(shell, shellMat, BotCatalog.Shells[Look.shell].color);
            trimMat = Painted(trim, trimMat, trimColor);
            jointMat = Painted(joints, jointMat, BotCatalog.Joints[Look.joints].color);
            glowMat = Painted(glow, glowMat, trimColor, 1.5f);

            if (tether && tether.cableMaterial)
            {
                if (!cableMat) cableMat = Own(tether.cableMaterial);
                BotCatalog.Paint(cableMat, trimColor);
                tether.cableMaterial = cableMat;
            }

            if (face)
            {
                var ex = BotCatalog.Expressions[Look.expression];
                face.eyeColor = BotCatalog.Eyes[Look.eyes].color;
                face.restingSmile = ex.smile;
                face.restingAngry = ex.angry;
                face.restingSleepy = ex.sleepy;
            }
            if (voice) voice.SetVoice(BotCatalog.Voices[Look.voice].hz);
        }

        static void Pick(GameObject[] variants, int index)
        {
            if (variants == null) return;
            for (int i = 0; i < variants.Length; i++)
                if (variants[i]) variants[i].SetActive(i == index);
        }

        /// <summary>Gives the role its own material copy (made once) and paints it.</summary>
        Material Painted(Renderer[] renderers, Material mat, Color color, float glowAmount = 0f)
        {
            if (renderers == null || renderers.Length == 0) return mat;
            if (!mat)
            {
                Material source = null;
                foreach (var r in renderers)
                    if (r && r.sharedMaterial) { source = r.sharedMaterial; break; }
                if (!source) return null;
                mat = Own(source);
                foreach (var r in renderers)
                    if (r) r.sharedMaterial = mat;
            }
            BotCatalog.Paint(mat, color, glowAmount);
            return mat;
        }

        Material Own(Material source)
        {
            var m = new Material(source) { name = source.name + " (" + name + ")" };
            owned.Add(m);
            return m;
        }
    }
}
