using System;
using System.Text.RegularExpressions;
using RobotRescue.Core;
using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// How one player's rescue bot looks and sounds. Every choice is an index into <see cref="BotCatalog"/>, so saves
    /// stay tiny and every combination stays on the curated (readable, cute) palette. Looks are purely cosmetic: the
    /// collider, reach and tool heads are the same on every bot, and tool heads keep their tool colours.
    /// </summary>
    [Serializable]
    public class BotLook
    {
        public string name = "PIP";
        public int chassis, head, feet, topper, badge;
        public int shell, trim, joints, eyes;
        public int expression, voice = 1;

        public BotLook Clone() => (BotLook)MemberwiseClone();

        /// <summary>Clamps every index into range (old saves, edited prefs).</summary>
        public BotLook Sanitized()
        {
            var l = Clone();
            l.name = BotCatalog.CleanName(l.name);
            l.chassis = Wrap(l.chassis, BotCatalog.Chassis.Length);
            l.head = Wrap(l.head, BotCatalog.Heads.Length);
            l.feet = Wrap(l.feet, BotCatalog.Feet.Length);
            l.topper = Wrap(l.topper, BotCatalog.Toppers.Length);
            l.badge = Wrap(l.badge, BotCatalog.Badges.Length);
            l.shell = Wrap(l.shell, BotCatalog.Shells.Length);
            l.trim = Wrap(l.trim, BotCatalog.Trims.Length);
            l.joints = Wrap(l.joints, BotCatalog.Joints.Length);
            l.eyes = Wrap(l.eyes, BotCatalog.Eyes.Length);
            l.expression = Wrap(l.expression, BotCatalog.Expressions.Length);
            l.voice = Wrap(l.voice, BotCatalog.Voices.Length);
            return l;
        }

        public static int Wrap(int i, int n) => n <= 0 ? 0 : ((i % n) + n) % n;
    }

    /// <summary>The Helper Drone crew's shared paint job. Index 0 of paint and eyes means "each drone's own".</summary>
    [Serializable]
    public class DroneLivery
    {
        public int paint, topper, eyes;

        public DroneLivery Clone() => (DroneLivery)MemberwiseClone();

        public DroneLivery Sanitized()
        {
            var l = Clone();
            l.paint = BotLook.Wrap(l.paint, BotCatalog.DronePaints.Length);
            l.topper = BotLook.Wrap(l.topper, BotCatalog.DroneToppers.Length);
            l.eyes = BotLook.Wrap(l.eyes, BotCatalog.DroneEyes.Length);
            return l;
        }
    }

    /// <summary>Every option the Bureau workshop offers. Builders make one variant per entry, in this order.</summary>
    public static class BotCatalog
    {
        public static readonly string[] Chassis = { "Bean", "Boxy", "Barrel", "Pear" };
        public static readonly string[] Heads = { "Monitor", "Dome", "Wide-screen", "Retro TV" };
        public static readonly string[] Feet = { "Boots", "Rollers", "Clogs" };
        public static readonly string[] Toppers = { "Bulb", "Sparkle", "Propeller", "Sprout", "Bow" };
        public static readonly string[] Badges = { "Bureau", "Heart", "Bolt", "Flower", "Plain" };

        public static readonly (string name, Color color)[] Shells =
        {
            ("Cloud", new Color(0.97f, 0.97f, 1f)),
            ("Sunny", new Color(1f, 0.88f, 0.42f)),
            ("Coral", new Color(1f, 0.6f, 0.52f)),
            ("Mint", new Color(0.62f, 0.93f, 0.78f)),
            ("Sky", new Color(0.6f, 0.8f, 1f)),
            ("Lilac", new Color(0.8f, 0.7f, 1f)),
            ("Peach", new Color(1f, 0.8f, 0.66f)),
            ("Bubblegum", new Color(1f, 0.7f, 0.85f)),
            ("Graphite", new Color(0.36f, 0.38f, 0.45f)),
            ("Sand", new Color(0.9f, 0.84f, 0.7f)),
        };

        public static readonly (string name, Color color)[] Trims =
        {
            ("Bureau orange", new Color(1f, 0.55f, 0.15f)),
            ("Teal", new Color(0.1f, 0.75f, 0.75f)),
            ("Tomato", new Color(0.95f, 0.3f, 0.25f)),
            ("Lemon", new Color(1f, 0.85f, 0.15f)),
            ("Grape", new Color(0.55f, 0.35f, 0.9f)),
            ("Leaf", new Color(0.35f, 0.8f, 0.3f)),
            ("Ocean", new Color(0.2f, 0.5f, 0.95f)),
            ("Pink", new Color(1f, 0.45f, 0.7f)),
            ("Navy", new Color(0.18f, 0.24f, 0.5f)),
            ("Silver", new Color(0.82f, 0.85f, 0.9f)),
        };

        public static readonly (string name, Color color)[] Joints =
        {
            ("Steel", new Color(0.55f, 0.58f, 0.66f)),
            ("Gunmetal", new Color(0.3f, 0.32f, 0.38f)),
            ("Brass", new Color(0.85f, 0.68f, 0.3f)),
            ("Copper", new Color(0.8f, 0.48f, 0.3f)),
            ("Chalk", new Color(0.92f, 0.92f, 0.9f)),
        };

        /// <summary>LED colours (HDR so they bloom). Index 0 is PIP's classic cyan.</summary>
        public static readonly (string name, Color color)[] Eyes =
        {
            ("Cyan", new Color(0.6f, 2.4f, 2.8f)),
            ("Amber", new Color(2.6f, 1.7f, 0.4f)),
            ("Mint", new Color(0.6f, 2.6f, 1.2f)),
            ("Pink", new Color(2.8f, 0.9f, 1.8f)),
            ("Violet", new Color(1.6f, 0.9f, 2.8f)),
            ("Lime", new Color(1.6f, 2.8f, 0.4f)),
            ("White", new Color(2.4f, 2.4f, 2.4f)),
        };

        /// <summary>Resting face. smile / angry / sleepy feed BotFace's resting personality.</summary>
        public static readonly (string name, float smile, float angry, float sleepy)[] Expressions =
        {
            ("Cheery", 0.45f, 0f, 0f),
            ("Calm", 0.2f, 0f, 0.1f),
            ("Focused", 0.05f, 0.3f, 0f),
            ("Dreamy", 0.3f, 0f, 0.3f),
            ("Beaming", 0.85f, 0f, 0f),
        };

        public static readonly (string name, float hz)[] Voices =
        {
            ("Low", 520f), ("Medium", 720f), ("High", 940f), ("Squeaky", 1150f),
        };

        public static readonly string[] DroneToppers = { "Knob", "Bow", "Propeller", "Sprout" };

        /// <summary>Crew paint. Index 0 keeps each drone's own colour (they're easier to tell apart that way).</summary>
        public static readonly (string name, Color color)[] DronePaints =
        {
            ("Each their own", Color.clear),
            ("Sky", new Color(0.55f, 0.8f, 1f)),
            ("Tangerine", new Color(1f, 0.7f, 0.4f)),
            ("Lilac", new Color(0.75f, 0.55f, 1f)),
            ("Mint", new Color(0.5f, 0.9f, 0.7f)),
            ("Bubblegum", new Color(1f, 0.6f, 0.75f)),
            ("Lemon", new Color(1f, 0.88f, 0.4f)),
        };

        /// <summary>Drone LEDs. Index 0 keeps the drones' classic warm amber.</summary>
        public static readonly (string name, Color color)[] DroneEyes =
        {
            ("Classic", Color.clear),
            ("Cyan", new Color(0.6f, 2.4f, 2.8f)),
            ("Mint", new Color(0.6f, 2.6f, 1.2f)),
            ("Pink", new Color(2.8f, 0.9f, 1.8f)),
            ("White", new Color(2.4f, 2.4f, 2.4f)),
        };

        public const int MaxNameLength = 12;

        public static string CleanName(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length > MaxNameLength) name = name.Substring(0, MaxNameLength);
            return name.Length == 0 ? "PIP" : name;
        }

        /// <summary>
        /// Toon outline colour for a paint colour (same rule the builders use), so recoloured parts keep a matching ink line.
        /// </summary>
        public static Color OutlineFor(Color c)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            return Color.HSVToRGB(h, Mathf.Clamp01(s * 1.1f + 0.25f), Mathf.Clamp01(v * 0.32f));
        }

        static readonly int IdBase = Shader.PropertyToID("_BaseColor"), IdColor = Shader.PropertyToID("_Color"),
            IdOutline = Shader.PropertyToID("_OutlineColor"), IdEmission = Shader.PropertyToID("_EmissionColor");

        /// <summary>Recolours a material that works with both the toon shader and the URP Lit fallback.</summary>
        public static void Paint(Material m, Color c, float glow = 0f)
        {
            if (!m) return;
            if (m.HasProperty(IdBase)) m.SetColor(IdBase, c);
            if (m.HasProperty(IdColor)) m.SetColor(IdColor, c);
            if (m.HasProperty(IdOutline)) m.SetColor(IdOutline, OutlineFor(c));
            if (glow > 0f && m.HasProperty(IdEmission)) m.SetColor(IdEmission, c * glow);
        }
    }

    /// <summary>Raised when a saved look changes. Slot is the player (0-3), or <see cref="BotLookStore.DroneSlot"/>.</summary>
    public struct BotLookChanged
    {
        public int Slot;
        public BotLookChanged(int slot) { Slot = slot; }
    }

    /// <summary>
    /// Saved looks, one per player slot (PlayerPrefs "rr.look.p1".."p4") plus the drone crew's livery. Slot 0 is the
    /// local player; slots 1-3 are ready for co-op friends on the same machine.
    /// </summary>
    public static class BotLookStore
    {
        public const int MaxPlayers = 4;
        public const int DroneSlot = -1;
        const string DroneKey = "rr.look.drones";

        static readonly BotLook[] cache = new BotLook[MaxPlayers];
        static DroneLivery droneCache;

        static string Key(int slot) => "rr.look.p" + (slot + 1);

        /// <summary>Starting looks: player 1 is classic PIP; friends get their own colours and names.</summary>
        public static BotLook Defaults(int slot) => slot switch
        {
            1 => new BotLook { name = "DOT", chassis = 2, head = 1, topper = 3, badge = 1, shell = 3, trim = 1, eyes = 2, voice = 2 },
            2 => new BotLook { name = "BIX", chassis = 1, head = 2, feet = 1, topper = 2, badge = 2, shell = 4, trim = 2, joints = 1, eyes = 1, expression = 2, voice = 0 },
            3 => new BotLook { name = "LOU", chassis = 3, head = 3, feet = 2, topper = 4, badge = 3, shell = 5, trim = 7, joints = 2, eyes = 3, expression = 3, voice = 3 },
            _ => new BotLook(),
        };

        public static BotLook Load(int slot)
        {
            slot = Mathf.Clamp(slot, 0, MaxPlayers - 1);
            if (cache[slot] != null) return cache[slot].Clone();
            BotLook look = null;
            string json = PlayerPrefs.GetString(Key(slot), "");
            if (!string.IsNullOrEmpty(json))
            {
                try { look = JsonUtility.FromJson<BotLook>(json); }
                catch (Exception) { look = null; }
            }
            cache[slot] = (look ?? Defaults(slot)).Sanitized();
            return cache[slot].Clone();
        }

        public static void Save(int slot, BotLook look)
        {
            slot = Mathf.Clamp(slot, 0, MaxPlayers - 1);
            cache[slot] = look.Sanitized();
            PlayerPrefs.SetString(Key(slot), JsonUtility.ToJson(cache[slot]));
            PlayerPrefs.Save();
            EventBus<BotLookChanged>.Raise(new BotLookChanged(slot));
        }

        public static DroneLivery LoadDrones()
        {
            if (droneCache != null) return droneCache.Clone();
            DroneLivery livery = null;
            string json = PlayerPrefs.GetString(DroneKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                try { livery = JsonUtility.FromJson<DroneLivery>(json); }
                catch (Exception) { livery = null; }
            }
            droneCache = (livery ?? new DroneLivery()).Sanitized();
            return droneCache.Clone();
        }

        public static void SaveDrones(DroneLivery livery)
        {
            droneCache = livery.Sanitized();
            PlayerPrefs.SetString(DroneKey, JsonUtility.ToJson(droneCache));
            PlayerPrefs.Save();
            EventBus<BotLookChanged>.Raise(new BotLookChanged(DroneSlot));
        }

        /// <summary>Back to the starting looks (Reset progress leaves looks alone; this is its own button).</summary>
        public static void ResetAll()
        {
            for (int i = 0; i < MaxPlayers; i++)
            {
                PlayerPrefs.DeleteKey(Key(i));
                cache[i] = null;
                EventBus<BotLookChanged>.Raise(new BotLookChanged(i));
            }
            PlayerPrefs.DeleteKey(DroneKey);
            droneCache = null;
            PlayerPrefs.Save();
            EventBus<BotLookChanged>.Raise(new BotLookChanged(DroneSlot));
        }

        /// <summary>The local player's bot name ("PIP" unless they renamed it).</summary>
        public static string PlayerName => Load(0).name;

        static readonly Regex PipWord = new Regex(@"\bPIP\b");

        /// <summary>
        /// DISPATCH's script calls the rookie "PIP"; this swaps in the player's chosen name so the radio talks to
        /// their bot.
        /// </summary>
        public static string Personalize(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string n = PlayerName;
            return n == "PIP" ? text : PipWord.Replace(text, n);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearCache()
        {
            for (int i = 0; i < MaxPlayers; i++) cache[i] = null;
            droneCache = null;
        }
    }
}
