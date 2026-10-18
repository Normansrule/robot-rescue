using System.Collections.Generic;
using RobotRescue.Bots;
using UnityEngine;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// Cosmetic variants for customizable bots and drones. Every option in <see cref="BotCatalog"/> is built once as a
    /// child (only the first left switched on); <see cref="BotCustomizer"/> / <see cref="DroneCustomizer"/> pick and
    /// paint them at runtime. All variants share the same footprint, so colliders and puzzles never change.
    /// </summary>
    public static partial class ChamberKit
    {
        /// <summary>Renderers sorted by which paint they take, collected while building a bot.</summary>
        public class LookParts
        {
            public readonly List<Renderer> shell = new List<Renderer>(), trim = new List<Renderer>(),
                joints = new List<Renderer>(), glow = new List<Renderer>();
            public Transform propeller;
        }

        static GameObject Role(GameObject go, List<Renderer> role)
        {
            if (role != null) role.Add(go.GetComponent<Renderer>());
            return go;
        }

        static GameObject[] OnlyFirst(List<GameObject> variants)
        {
            for (int i = 0; i < variants.Count; i++) variants[i].SetActive(i == 0);
            return variants.ToArray();
        }

        static Transform Variant(string name, Transform parent, Vector3 at, List<GameObject> into)
        {
            var t = Pivot(name, parent, at);
            into.Add(t.gameObject);
            return t;
        }

        /// <summary>Torso shapes, in BotCatalog.Chassis order. <paramref name="c"/> is the torso centre.</summary>
        public static GameObject[] BotChassis(Transform parent, Vector3 c, Material shell, Material trim, LookParts parts)
        {
            var v = new List<GameObject>();
            var bean = Variant("Chassis_Bean", parent, c, v);
            Role(Sphere("Torso", bean, c, 0.36f, shell, new Vector3(1f, 0.86f, 0.92f)), parts.shell);

            var boxy = Variant("Chassis_Boxy", parent, c, v);
            Role(Box("Torso", boxy, c, new Vector3(0.72f, 0.62f, 0.64f), shell, false), parts.shell);

            var barrel = Variant("Chassis_Barrel", parent, c, v);
            Role(Cylinder("Torso", barrel, c, 0.34f, 0.6f, shell), parts.shell);
            foreach (float dy in new[] { -0.22f, 0.22f })
                Role(Cylinder("Hoop", barrel, c + new Vector3(0, dy, 0), 0.352f, 0.05f, trim), parts.trim);

            var pear = Variant("Chassis_Pear", parent, c, v);
            Role(Sphere("Belly", pear, c + new Vector3(0, -0.06f, 0), 0.38f, shell, new Vector3(1f, 0.8f, 0.86f)), parts.shell);
            Role(Sphere("Chest", pear, c + new Vector3(0, 0.14f, -0.02f), 0.27f, shell, new Vector3(1f, 0.9f, 0.95f)), parts.shell);
            return OnlyFirst(v);
        }

        /// <summary>Chest badges, in BotCatalog.Badges order. <paramref name="at"/> is the front of the chest.</summary>
        public static GameObject[] BotBadges(Transform parent, Vector3 at, Material trim, Material cream, LookParts parts)
        {
            var v = new List<GameObject>();
            GameObject Disc(Transform t)
            {
                var d = Role(Cylinder("Disc", t, at, 0.1f, 0.03f, trim), parts.trim);
                d.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                return d;
            }
            Vector3 f = at + new Vector3(0, 0, 0.02f);
            GameObject Bit(Transform t, Vector3 offset, Vector3 size, float spin)
            {
                var b = Box("Emblem", t, f + offset, size, cream, false);
                b.transform.rotation = Quaternion.Euler(0f, 0f, spin);
                return b;
            }

            var bureau = Variant("Badge_Bureau", parent, at, v);
            Disc(bureau);
            Bit(bureau, Vector3.zero, new Vector3(0.11f, 0.032f, 0.02f), 0f);
            Bit(bureau, Vector3.zero, new Vector3(0.032f, 0.11f, 0.02f), 0f);

            var heart = Variant("Badge_Heart", parent, at, v);
            Disc(heart);
            foreach (float s in new[] { -1f, 1f })
                Sphere("Lobe", heart, f + new Vector3(s * 0.024f, 0.014f, 0), 0.03f, cream, new Vector3(1f, 1f, 0.4f));
            Bit(heart, new Vector3(0, -0.012f, 0), new Vector3(0.058f, 0.058f, 0.02f), 45f);

            var bolt = Variant("Badge_Bolt", parent, at, v);
            Disc(bolt);
            Bit(bolt, new Vector3(0.014f, 0.03f, 0), new Vector3(0.026f, 0.07f, 0.02f), -25f);
            Bit(bolt, Vector3.zero, new Vector3(0.06f, 0.022f, 0.02f), 0f);
            Bit(bolt, new Vector3(-0.014f, -0.03f, 0), new Vector3(0.026f, 0.07f, 0.02f), -25f);

            var flower = Variant("Badge_Flower", parent, at, v);
            Disc(flower);
            for (int i = 0; i < 5; i++)
            {
                float a = i * Mathf.PI * 2f / 5f + Mathf.PI / 2f;
                Sphere("Petal", flower, f + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 0.042f, 0.026f, cream, new Vector3(1f, 1f, 0.4f));
            }
            Role(Sphere("Middle", flower, f + new Vector3(0, 0, 0.004f), 0.022f, trim, new Vector3(1f, 1f, 0.5f)), parts.trim);

            var plain = Variant("Badge_Plain", parent, at, v);
            Disc(plain);
            return OnlyFirst(v);
        }

        /// <summary>Head shells, in BotCatalog.Heads order. Every one frames the same face screen at <paramref name="c"/>.</summary>
        public static GameObject[] BotHeads(Transform parent, Vector3 c, Material shell, Material bezel, Material trim,
            Material joints, LookParts parts)
        {
            var v = new List<GameObject>();
            void Bezel(Transform t, Vector3 size) => Box("Bezel", t, c + new Vector3(0, 0, 0.29f), size, bezel, false);

            var monitor = Variant("Head_Monitor", parent, c, v);
            Role(Box("Shell", monitor, c, new Vector3(0.8f, 0.56f, 0.62f), shell, false), parts.shell);
            Bezel(monitor, new Vector3(0.68f, 0.44f, 0.06f));
            foreach (float side in new[] { -1f, 1f })
                Role(Cylinder("Ear", monitor, c + new Vector3(side * 0.42f, 0, 0), 0.085f, 0.07f, trim), parts.trim)
                    .transform.rotation = Quaternion.Euler(0f, 0f, 90f);

            var dome = Variant("Head_Dome", parent, c, v);
            Role(Box("Jaw", dome, c + new Vector3(0, -0.07f, 0), new Vector3(0.78f, 0.36f, 0.6f), shell, false), parts.shell);
            Role(Sphere("Dome", dome, c + new Vector3(0, 0.08f, 0), 0.39f, shell, new Vector3(1f, 0.75f, 0.77f)), parts.shell);
            Bezel(dome, new Vector3(0.68f, 0.44f, 0.06f));
            foreach (float side in new[] { -1f, 1f })
                Role(Sphere("Rivet", dome, c + new Vector3(side * 0.39f, -0.05f, 0), 0.06f, joints), parts.joints);

            var wide = Variant("Head_Wide", parent, c, v);
            Role(Box("Shell", wide, c, new Vector3(0.96f, 0.5f, 0.56f), shell, false), parts.shell);
            Bezel(wide, new Vector3(0.7f, 0.44f, 0.06f));
            foreach (float side in new[] { -1f, 1f })
                Role(Box("Vent", wide, c + new Vector3(side * 0.49f, 0, 0), new Vector3(0.05f, 0.26f, 0.3f), trim, false), parts.trim);

            var retro = Variant("Head_Retro", parent, c, v);
            Role(Box("Shell", retro, c + new Vector3(0, 0.02f, 0), new Vector3(0.84f, 0.64f, 0.58f), shell, false), parts.shell);
            Bezel(retro, new Vector3(0.72f, 0.5f, 0.07f));
            foreach (float dy in new[] { 0.08f, -0.08f })
                Role(Sphere("Knob", retro, c + new Vector3(0.44f, dy, 0.12f), 0.055f, trim), parts.trim);
            Role(Box("Stand", retro, c + new Vector3(0, -0.33f, 0), new Vector3(0.4f, 0.06f, 0.3f), joints, false), parts.joints);
            return OnlyFirst(v);
        }

        /// <summary>One foot's shapes, in BotCatalog.Feet order. <paramref name="c"/> is the foot centre (0.08 m up).</summary>
        public static GameObject[] BotFeet(Transform parent, Vector3 c, Material joints, Material trim, LookParts parts)
        {
            var v = new List<GameObject>();
            var boots = Variant("Feet_Boots", parent, c, v);
            Role(Box("Boot", boots, c, new Vector3(0.22f, 0.16f, 0.32f), joints, false), parts.joints);

            var rollers = Variant("Feet_Rollers", parent, c, v);
            Role(Box("Housing", rollers, c + new Vector3(0, 0.04f, 0), new Vector3(0.2f, 0.08f, 0.3f), trim, false), parts.trim);
            foreach (float dz in new[] { -0.09f, 0.09f })
                Role(Cylinder("Wheel", rollers, c + new Vector3(0, -0.015f, dz), 0.065f, 0.18f, joints), parts.joints)
                    .transform.rotation = Quaternion.Euler(0f, 0f, 90f);

            var clogs = Variant("Feet_Clogs", parent, c, v);
            Role(Sphere("Clog", clogs, c + new Vector3(0, 0, 0.02f), 0.13f, joints, new Vector3(0.85f, 0.6f, 1.3f)), parts.joints);
            return OnlyFirst(v);
        }

        /// <summary>
        /// A topper (antenna tip or drone hat) sitting on <paramref name="at"/>, <paramref name="k"/> times PIP size.
        /// Kinds: Bulb, Knob, Sparkle, Propeller, Sprout, Bow.
        /// </summary>
        public static GameObject Topper(string kind, Transform parent, Vector3 at, float k, Material trim, Material joints,
            Material glow, LookParts parts)
        {
            var t = Pivot("Topper_" + kind, parent, at);
            var leaf = Mat("Bot_Leaf", new Color(0.42f, 0.82f, 0.36f));
            switch (kind)
            {
                case "Bulb":
                    Role(Sphere("Bulb", t, at, 0.07f * k, glow), parts.glow);
                    break;
                case "Knob":
                    Role(Sphere("Knob", t, at, 0.07f * k, trim), parts.trim);
                    break;
                case "Sparkle":
                    foreach (float spin in new[] { 45f, -45f })
                        Role(Box("Ray", t, at + new Vector3(0, 0.04f * k, 0), new Vector3(0.2f, 0.05f, 0.05f) * k, trim, false), parts.trim)
                            .transform.rotation = Quaternion.Euler(0f, 0f, spin);
                    Role(Sphere("Core", t, at + new Vector3(0, 0.04f * k, 0), 0.045f * k, glow), parts.glow);
                    break;
                case "Propeller":
                    Role(Cylinder("Hub", t, at, 0.03f * k, 0.05f * k, joints), parts.joints);
                    var spinner = Pivot("Propeller", t, at + new Vector3(0, 0.03f * k, 0));
                    Role(Box("Blade", spinner, at + new Vector3(0, 0.03f * k, 0), new Vector3(0.34f, 0.015f, 0.06f) * k, trim, false), parts.trim);
                    Role(Box("Blade", spinner, at + new Vector3(0, 0.03f * k, 0), new Vector3(0.06f, 0.015f, 0.34f) * k, trim, false), parts.trim);
                    parts.propeller = spinner;
                    break;
                case "Sprout":
                    Cylinder("Stem", t, at + new Vector3(0, 0.03f * k, 0), 0.012f * k, 0.07f * k, leaf);
                    foreach (float side in new[] { -1f, 1f })
                        Sphere("Leaf", t, at + new Vector3(side * 0.05f, 0.07f, 0) * k, 0.06f * k, leaf, new Vector3(1f, 0.35f, 0.55f))
                            .transform.rotation = Quaternion.Euler(0f, 0f, side * 25f);
                    break;
                case "Bow":
                    foreach (float side in new[] { -1f, 1f })
                        Role(Sphere("Loop", t, at + new Vector3(side * 0.06f, 0.02f, 0) * k, 0.055f * k, trim, new Vector3(1f, 0.7f, 0.5f)), parts.trim);
                    Role(Sphere("Knot", t, at + new Vector3(0, 0.02f, 0) * k, 0.032f * k, trim), parts.trim);
                    break;
            }
            return t.gameObject;
        }

        public static GameObject[] Toppers(string[] kinds, Transform parent, Vector3 at, float k, Material trim,
            Material joints, Material glow, LookParts parts)
        {
            var v = new List<GameObject>();
            foreach (var kind in kinds) v.Add(Topper(kind, parent, at, k, trim, joints, glow, parts));
            return OnlyFirst(v);
        }
    }
}
