using System.Collections.Generic;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// Dresses a Helper Drone in the crew's saved <see cref="DroneLivery"/>: shared paint (or its own colour), a topper
    /// and LED colour. Cosmetic only; the drone's size and physics never change.
    /// </summary>
    public class DroneCustomizer : MonoBehaviour
    {
        [Tooltip("The colour this drone was built with (used when the crew keeps their own colours).")]
        public Color ownColor = Color.white;
        public Renderer[] shell;
        public GameObject[] toppers;
        public BotFace face;
        public Transform propeller;

        readonly List<Material> owned = new List<Material>();
        Material shellMat;
        Color ownEyes;
        bool eyesRead;

        void Awake() => Apply(BotLookStore.LoadDrones());

        void OnEnable() => EventBus<BotLookChanged>.Subscribe(OnChanged);
        void OnDisable() => EventBus<BotLookChanged>.Unsubscribe(OnChanged);

        void OnDestroy()
        {
            foreach (var m in owned)
                if (m) Destroy(m);
        }

        void OnChanged(BotLookChanged e)
        {
            if (e.Slot == BotLookStore.DroneSlot) Apply(BotLookStore.LoadDrones());
        }

        void Update()
        {
            if (propeller && propeller.gameObject.activeInHierarchy)
                propeller.Rotate(0f, 480f * Time.deltaTime, 0f, Space.Self);
        }

        public void Apply(DroneLivery livery)
        {
            livery = (livery ?? new DroneLivery()).Sanitized();

            if (toppers != null)
                for (int i = 0; i < toppers.Length; i++)
                    if (toppers[i]) toppers[i].SetActive(i == livery.topper);

            if (shell != null && shell.Length > 0)
            {
                if (!shellMat)
                {
                    Material source = null;
                    foreach (var r in shell)
                        if (r && r.sharedMaterial) { source = r.sharedMaterial; break; }
                    if (source)
                    {
                        shellMat = new Material(source) { name = source.name + " (" + name + ")" };
                        owned.Add(shellMat);
                        foreach (var r in shell)
                            if (r) r.sharedMaterial = shellMat;
                    }
                }
                BotCatalog.Paint(shellMat, livery.paint == 0 ? ownColor : BotCatalog.DronePaints[livery.paint].color);
            }

            if (face)
            {
                if (!eyesRead) { ownEyes = face.eyeColor; eyesRead = true; }
                face.eyeColor = livery.eyes == 0 ? ownEyes : BotCatalog.DroneEyes[livery.eyes].color;
            }
        }
    }
}
