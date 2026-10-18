using System.Collections.Generic;
using RobotRescue.Bots;
using RobotRescue.Campaign;
using RobotRescue.Narrative;
using RobotRescue.Puzzles;
using RobotRescue.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RobotRescue.EditorTools
{
    /// <summary>Puzzle-toolkit pieces for chamber builders. Every position is world space; floors are at y = 0 unless noted.</summary>
    public static partial class ChamberKit
    {
        // ------------------------------------------------------------------ scene lifecycle

        /// <summary>Asks to save the open scene, then starts a fresh lit scene. Returns false if the user cancelled.</summary>
        public static bool BeginScene(out Scene scene, params string[] folders)
        {
            scene = default;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(MatDir);
            foreach (var f in folders) EnsureFolder(f);
            ResetCache();
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLighting();
            return true;
        }

        public static void FinishScene(Scene scene, string path, BotController bot)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
            AddSceneToBuildSettings(path);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = bot.gameObject;
            Debug.Log($"[Robot Rescue] Built {path}. Press Play!");
        }

        // ------------------------------------------------------------------ pieces

        static int droneCount;

        static readonly (string name, Temperament temperament)[] DroneCrew =
        {
            ("Bolt", Temperament.Cheerful), ("Nugget", Temperament.Nervous), ("Grumbles", Temperament.Grumpy),
            ("Snooze", Temperament.Sleepy), ("Sparkle", Temperament.ShowOff), ("Widget", Temperament.Cheerful),
            ("Jitters", Temperament.Nervous), ("Rusty", Temperament.Grumpy), ("Dozer", Temperament.Sleepy),
            ("Dazzle", Temperament.ShowOff), ("Pickle", Temperament.Cheerful), ("Fizz", Temperament.Nervous),
        };

        /// <summary>
        /// Wind-up Helper Drone: a 1 m rounded toy you can carry, stack, throw and Tether-pull. Each one gets a name,
        /// a temperament, an LED face and a little voice (picked in build order, so rebuilding gives the same crew).
        /// The physics body is the root; every mesh sits under a Visual child so personality quirks never move it.
        /// </summary>
        public static HelperDrone Drone(string name, Transform parent, Vector3 pos, Color color)
        {
            var (droneName, temperament) = DroneCrew[droneCount++ % DroneCrew.Length];
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var box = go.AddComponent<BoxCollider>();
            box.size = Vector3.one;
            box.sharedMaterial = PhysMat("Drone", 0.8f, 0.9f, PhysicsMaterialCombine.Maximum);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1.5f;
            var carry = go.AddComponent<Carryable>();
            carry.displayName = droneName;
            carry.firstPickupCue = "drone_grabbed";
            go.AddComponent<Respawnable>();
            var drone = go.AddComponent<HelperDrone>();

            var shell = Mat("Drone_" + ColorUtility.ToHtmlStringRGB(color), color, 0.5f);
            var bezel = Mat("Bot_Screen", new Color(0.08f, 0.1f, 0.2f), 0.8f);
            var metal = Mat("Bot_Grey", new Color(0.55f, 0.58f, 0.66f));
            var cream = Mat("Bot_White", new Color(0.97f, 0.97f, 1f), 0.6f);
            var accent = Mat("Bot_Orange", new Color(1f, 0.55f, 0.15f));

            var visual = Pivot("Visual", go.transform, pos);
            var looks = new LookParts();
            var body = Box("Body", visual, pos + new Vector3(0, 0.02f, 0), new Vector3(0.96f, 0.9f, 0.92f), shell, false);
            Box("Bezel", visual, pos + new Vector3(0, 0.08f, 0.45f), new Vector3(0.74f, 0.5f, 0.06f), bezel, false);
            var screen = FaceQuad("Face", visual, pos + new Vector3(0, 0.08f, 0.485f), new Vector2(0.64f, 0.42f));
            Box("TopPlate", visual, pos + new Vector3(0, 0.49f, 0), new Vector3(0.8f, 0.06f, 0.78f), cream, false);
            var toppers = Toppers(BotCatalog.DroneToppers, visual, pos + new Vector3(0, 0.56f, 0), 1.3f, accent, metal, accent, looks);
            foreach (float side in new[] { -1f, 1f })
            {
                Box("Foot", visual, pos + new Vector3(side * 0.26f, -0.46f, 0.05f), new Vector3(0.26f, 0.1f, 0.34f), metal, false);
                var cheek = Cylinder("Bolt", visual, pos + new Vector3(side * 0.49f, 0.08f, 0), 0.08f, 0.04f, metal);
                cheek.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }

            // Wind-up key on the back: a pivot that spins around the drone's local Z.
            var key = Pivot("WindUpKey", visual, pos + new Vector3(0, 0.1f, -0.52f));
            Cylinder("Shaft", key, pos + new Vector3(0, 0.1f, -0.54f), 0.04f, 0.12f, metal).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Box("Bow", key, pos + new Vector3(0, 0.1f, -0.62f), new Vector3(0.46f, 0.16f, 0.05f), accent, false);
            drone.windUpKey = key;

            var face = go.AddComponent<BotFace>();
            face.screen = screen;
            face.orientation = go.transform;
            face.aspect = 0.64f / 0.42f;
            face.eyeColor = new Color(2.6f, 2.0f, 0.6f);

            var src = go.AddComponent<AudioSource>();
            src.spatialBlend = 1f;
            src.minDistance = 2f;
            src.maxDistance = 25f;
            var voice = go.AddComponent<Babble>();
            voice.voiceHz = temperament switch
            {
                Temperament.Grumpy => 380f,
                Temperament.Sleepy => 430f,
                Temperament.Nervous => 980f,
                Temperament.ShowOff => 820f,
                _ => 880f,
            };
            voice.volume = 0.32f;

            var personality = go.AddComponent<DronePersonality>();
            personality.droneName = droneName;
            personality.temperament = temperament;
            personality.visual = visual;
            personality.face = face;
            personality.voice = voice;

            var custom = go.AddComponent<DroneCustomizer>();
            custom.ownColor = color;
            custom.shell = new[] { body.GetComponent<Renderer>() };
            custom.toppers = toppers;
            custom.face = face;
            custom.propeller = looks.propeller;
            return drone;
        }

        /// <summary>
        /// Service bay: a glowing floor ring that recharges PIP's battery (and any drone parked on it) and, when
        /// <paramref name="repairs"/> is set, snaps every lost part back on. It also becomes PIP's tow point and respawn.
        /// </summary>
        public static ChargePad ServiceBay(string name, Transform parent, Vector3 floorPos, bool repairs = true)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = floorPos;
            Cylinder("Base", root, floorPos + Vector3.up * 0.04f, 1.05f, 0.08f, Mat("Bay_Base", new Color(0.3f, 0.34f, 0.46f)));
            var ring = Cylinder("GlowRing", root, floorPos + Vector3.up * 0.085f, 0.9f, 0.02f, Glow("Bay_Glow", new Color(0.3f, 0.7f, 1f), 1.6f));
            Cylinder("Inner", root, floorPos + Vector3.up * 0.095f, 0.72f, 0.02f, Mat("Bay_Inner", new Color(0.86f, 0.9f, 1f)));
            if (repairs)
            {
                // A little wrench-and-bolt sign so the bay reads as "fix me here" from across the room.
                var post = Box("SignPost", root, floorPos + new Vector3(0.95f, 0.75f, 0.6f), new Vector3(0.08f, 1.5f, 0.08f), Mat("Bot_Grey", new Color(0.55f, 0.58f, 0.66f)), false);
                Sphere("SignBulb", root, post.transform.position + Vector3.up * 0.82f, 0.13f, Glow("Bay_Bulb", new Color(0.4f, 1f, 0.55f), 2f));
            }
            var zone = Box("ChargeZone", root, floorPos + Vector3.up * 0.8f, new Vector3(1.8f, 1.6f, 1.8f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var pad = zone.AddComponent<ChargePad>();
            pad.repairsParts = repairs;
            pad.glow = ring.GetComponent<Renderer>();
            return pad;
        }

        /// <summary>Wall socket PIP can plug its Tether into to recharge slowly (and power whatever it is wired to).</summary>
        public static ChargerSocket ChargerPost(string name, Transform parent, Vector3 floorPos, float yaw, params GameObject[] powered)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(floorPos, Quaternion.Euler(0, yaw, 0));
            Box("Post", root, floorPos + Vector3.up * 0.6f, new Vector3(0.45f, 1.2f, 0.45f), Mat("Charger_Post", new Color(0.4f, 0.85f, 0.55f)));
            Vector3 face = root.rotation * new Vector3(0, 0, 0.235f);
            var socket = Box("Socket", root, floorPos + Vector3.up * 0.85f + face, new Vector3(0.3f, 0.3f, 0.04f), Mat("Socket", new Color(0.2f, 0.2f, 0.25f)));
            socket.transform.rotation = root.rotation;
            var bolt = Box("Bolt", root, floorPos + Vector3.up * 1.12f + face, new Vector3(0.1f, 0.16f, 0.03f), Glow("Charger_Bolt", new Color(1f, 0.9f, 0.3f), 1.8f), false);
            bolt.transform.rotation = root.rotation * Quaternion.Euler(0, 0, 20f);
            var cs = socket.AddComponent<ChargerSocket>();
            cs.poweredObjects = powered;
            cs.indicator = bolt.GetComponent<Renderer>();
            return cs;
        }

        /// <summary>A climbable wall: chunky handholds on a slab with a ClimbSurface, so PIP can grab on and scramble up.</summary>
        public static GameObject GripWall(string name, Transform parent, Vector3 baseCenter, Vector3 size, float yaw, Color color, float stamina = 1f)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var rot = Quaternion.Euler(0, yaw, 0);
            root.SetPositionAndRotation(baseCenter, rot);
            var slab = Box("Slab", root, baseCenter + Vector3.up * size.y * 0.5f, size, Mat("Grip_" + ColorUtility.ToHtmlStringRGB(color), color));
            slab.transform.rotation = rot;
            slab.AddComponent<ClimbSurface>().staminaMultiplier = stamina;
            var holdMat = Mat("Grip_Hold", new Color(1f, 0.82f, 0.3f));
            var rng = new System.Random(name.GetHashCode());
            for (float y = 0.6f; y < size.y - 0.2f; y += 0.75f)
                for (float x = -size.x * 0.5f + 0.4f; x < size.x * 0.5f - 0.2f; x += 0.9f)
                {
                    float jx = (float)rng.NextDouble() * 0.4f - 0.2f, jy = (float)rng.NextDouble() * 0.3f;
                    var hold = Sphere("Hold", root, baseCenter + rot * new Vector3(x + jx, y + jy, size.z * 0.5f + 0.04f), 0.11f, holdMat, new Vector3(1.3f, 0.8f, 0.7f));
                    hold.transform.rotation = rot;
                }
            return slab;
        }

        /// <summary>A spinning golden bolt pickup. <paramref name="id"/> must be unique across the whole game.</summary>
        public static Collectible Bolt(string id, Transform parent, Vector3 pos)
        {
            var go = new GameObject("Bolt_" + id);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var col = go.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.7f;
            var c = go.AddComponent<Collectible>();
            c.id = id;
            var visual = Pivot("Visual", go.transform, pos);
            var gold = Glow("Bolt_Gold", new Color(1f, 0.78f, 0.2f), 0.9f);
            var head = Cylinder("Head", visual, pos + Vector3.up * 0.18f, 0.22f, 0.12f, gold);
            Cylinder("Shaft", visual, pos - Vector3.up * 0.08f, 0.08f, 0.4f, gold);
            head.transform.rotation = Quaternion.identity;
            c.visual = visual;
            return c;
        }

        // ------------------------------------------------------------------ shared DISPATCH lines

        /// <summary>Lines every level shares: battery, broken parts, recharging, bolts and climbing.</summary>
        public static DispatchScript CommonDispatch()
        {
            EnsureFolder(Root + "/Data");
            var d = LoadOrCreate<DispatchScript>(Root + "/Data/Dispatch_Common.asset");
            d.lines = new[]
            {
                CL("battery_low", "PIP, your battery's down to a quarter. Find a glowing charge ring, a green charger post, or hug a Helper Drone.", 5f, true),
                CL("battery_critical", "Ten percent! You're running on fumes and good intentions. Charger. Now.", 4f, true),
                CL("battery_reserve", "You're on reserve power. You can still walk, carry and fix yourself up, just slowly. No climbing or tools. Get to a charger!", 5f, true),
                CL("battery_reserve_ok", "Off reserve. Phew. Tools and climbing are back.", 3f, true),
                CL("battery_dead", "Reserve's gone. Sending the tow truck. It's a very small tow truck.", 4f, true),
                CL("battery_towed", "Towed back to the last charger and topped up halfway. Let's keep an eye on that meter.", 4f, true),
                CL("charging", "Ahh, charging. That's the good stuff. Stand on any ring like this to top up.", 4f),
                CL("part_lost_arml", "Your left arm just went for a walk! Bump into it to click it back on.", 4f, true),
                CL("part_lost_armr", "There goes your tool arm! No tools until you pick it back up.", 4f, true),
                CL("part_lost_antenna", "Antenna's off! You're getting me all crackly. Grab it before you lose signal.", 4f, true),
                CL("part_lost_footl", "You've lost a foot. Hop to it. Literally. Go get it.", 4f, true),
                CL("part_lost_footr", "Foot's off! Walking will be wobbly until you snap it back on.", 4f, true),
                CL("part_fixed", "Click! Good as new. Well, good as slightly used.", 3f, true),
                CL("part_floating", "Your part's bobbing in the water. Hang on, the service tray will fish it out.", 4f, true),
                CL("part_returned", "The service tray found your part and sent it home. Click!", 3f, true),
                CL("parts_repaired", "The service bay patched you up. Every part present and accounted for.", 4f, true),
                CL("bolt_first", "A golden bolt! Wait, it's humming. That's a memory bolt. Hold still, I'm reading it.", 4f),
                CL("screw_first", "Righty-tighty! Keep holding Use until the screw sinks all the way in.", 4f),
                CL("screw_done", "Screw's tight!", 2.5f, true),
                CL("magnet_first", "Hold Use and it floats along in front of you. Let go to drop it.", 4f),
                CL("grip", "You can climb anything with chunky yellow handholds. Watch your grip meter though!", 5f),
                CL("fell", "Whoops. Back you go.", 3f, true),
                CL("next_area", "The bridge to the next platform is powered up! Follow it to the gate.", 4f, true),
                CL("pod_launch", "Strap in! Mission pods are perfectly safe. Mostly. Pretty much. Have fun!", 4f, true),
                CL("mission_unlocked", "New mission! Brightwater Reach needs the Bureau. Head for the Launch Bay at the end of the walkway.", 5f, true),

                // The Hum: after each call, a little more of the mystery.
                CL("hum_1", "Did... the station just hum? Probably the vending machine. Probably.", 4f),
                CL("hum_2", "There it is again. A low hum on the long-range radio. It's coming from the planet.", 5f),
                CL("hum_3", "The hum has a pattern. Short, short, short. Long, long, long. Hmm.", 5f),
                CL("hum_4", "That coffee machine only blew because of a power surge, right when the hum spiked. It's coming from Brightwater Reach. Mission pod's unlocked. Go see.", 7f),
                CL("hum_5", "The fuses popped during the hum again. Nonna says it's been happening for weeks, always from under the sea.", 6f),
                CL("hum_6", "It's louder out here, near the Old Rig. Nobody goes near the Old Rig. It sank before you were built.", 6f),
                CL("hum_7", "Kitchen saved! And PIP, I decoded the hum. Short short short, long long long... it's an S.O.S. Somebody down there needs rescuing.", 7f),
                CL("hum_8", "Rosa says the park's screws always work loose in the same direction: toward the sea. The hum is shaking the whole Reach apart, one screw at a time.", 7f),
                CL("hum_9", "The school's science fair has a real seismograph. If we record the hum, we can work out exactly where it's coming from. That's the next call. The school's just over the bridge.", 8f),
                CL("hum_10", "Priya ran the numbers: the hum always comes from the north-west, right where the Old Rig went down. That's where we're headed, once the bridges reach it.", 8f),
                CL("hum_11", "Weaker every time. If that's a bot down there, its battery's nearly flat. PIP, the Bureau has never left anyone behind. We're not starting now.", 8f),
                CL("hum_12", "Ines found an old Bureau cable on the harbour floor. It runs north-west, all the way to the Old Rig. Someone from the Bureau was down there.", 8f),
                CL("hum_13", "Captain Mo's chart had blueprints tucked inside: Unit Zero, the Bureau's first bot. His battery was only meant to last a week. PIP... he's been down there for years.", 9f),
                CL("hum_14", "Benny remembers the Old Rig going down. Everyone got off safe, he says, except the little Bureau bot who stayed to keep the anchor engine running.", 8f),
                CL("hum_15", "We saw it from the top of the wheel: the Old Rig, and a light blinking under it. PIP, it's time to bring Unit Zero home.", 8f),
                CL("hum_16", "The outpost logs say Zero volunteered to stay. 'Someone has to keep the anchor engine running,' he wrote. 'Back in a week.'", 8f),
                CL("hum_17", "The hum stopped. For the first time in years, it's quiet. Zero's safe, but the anchor engine still needs a keeper. The Founders' Festival is tomorrow. Maybe the whole Reach can help.", 9f),
                CL("hum_18", "Stage's ready. Tonight the whole Reach plays the Founders' Song, and every beat goes into the anchor engine. Zero says it only needs one good link.", 8f),
                CL("hum_19", "Listen. No hum, just music. The anchor's linked, the Reach is steady, and Unit Zero is home. Welcome back to the Bureau, Zero.", 10f),

                // Memory bolts: the Nth bolt you find tells the Nth part of Unit Zero's story.
                CL("lore_1", "Memory bolt, log one: 'Unit Zero. First day at the Robot Rescue Bureau. Everybody is so nice.' Unit ZERO? That's the Bureau's very first bot!", 7f),
                CL("lore_2", "Log two: 'Built my first bridge today. Brightwater Reach is three platforms and a lot of hope.'", 6f),
                CL("lore_3", "Log three: 'The anchor engine under the Old Rig keeps the whole Reach from drifting off. Somebody has to mind it.'", 6f),
                CL("lore_4", "Log four: 'Storm coming. The Old Rig is leaning. Getting everyone off.'", 5f),
                CL("lore_5", "Log five: 'Everyone's safe. I stayed to hold the engine steady. Water's rising.' ...PIP, I never knew.", 6f),
                CL("lore_6", "Log six: 'The rig sank halfway. The engine's still turning. So am I. Just slower.'", 6f),
                CL("lore_7", "Log seven: 'I hum when my battery runs low. I hope someone hears.' ...The Hum. PIP, the Hum is HIM.", 7f),
                CL("lore_8", "Log eight: 'I've been sending memory bolts up the old cables. If you're reading this: hello, new friend.'", 6f),
                CL("lore_9", "Log nine: 'The power grid shakes when I hum. Sorry about the coffee machines.'", 5f),
                CL("lore_10", "Log ten: 'You can see the station's lights from down here, through the water. I miss it.'", 6f),
                CL("lore_11", "Log eleven: 'Find all my bolts and you'll know the way down.'", 5f),
                CL("lore_12", "Log twelve: 'Unit Zero. Still on duty.' ...PIP. We're bringing Zero home.", 6f),
            };
            EditorUtility.SetDirty(d);
            return d;
        }

        static NarrationLine CL(string cue, string text, float duration, bool repeatable = false) =>
            new NarrationLine { cue = cue, text = text, duration = duration, repeatable = repeatable };

        /// <summary>Freestanding socket post. The socket faces <paramref name="yaw"/> (0 = +Z).</summary>
        public static PowerSocket Socket(string name, Transform parent, Vector3 floorPos, float yaw, params GameObject[] powered)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(floorPos, Quaternion.Euler(0, yaw, 0));
            Box("Post", root, floorPos + Vector3.up * 0.55f, new Vector3(0.4f, 1.1f, 0.4f), Mat("Socket_Post", new Color(0.85f, 0.85f, 0.92f)));
            Vector3 face = root.rotation * new Vector3(0, 0, 0.21f);
            var socket = Box("Socket", root, floorPos + Vector3.up * 0.8f + face, new Vector3(0.3f, 0.3f, 0.04f), Mat("Socket", new Color(0.2f, 0.2f, 0.25f)));
            socket.transform.rotation = root.rotation;
            var led = Box("LED", root, floorPos + Vector3.up * 1.05f + face, new Vector3(0.1f, 0.06f, 0.03f), Mat("LED", Color.gray), false);
            led.transform.rotation = root.rotation;
            var ps = socket.AddComponent<PowerSocket>();
            ps.poweredObjects = powered;
            ps.indicator = led.GetComponent<Renderer>();
            return ps;
        }

        public static PressurePlate Plate(string name, Transform parent, Vector3 floorPos, float requiredMass, float triggerHeight, params GameObject[] powered)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var visual = Box("Plate", root, floorPos + Vector3.up * 0.05f, new Vector3(1.6f, 0.1f, 1.6f), Mat("Plate_Yellow", new Color(1f, 0.85f, 0.3f)));
            var zone = Box("Trigger", root, floorPos + Vector3.up * (0.1f + triggerHeight * 0.5f), new Vector3(1.4f, triggerHeight, 1.4f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var plate = zone.AddComponent<PressurePlate>();
            plate.requiredMass = requiredMass;
            plate.plateVisual = visual.transform;
            plate.poweredObjects = powered;
            plate.tooLightCue = "plate_too_light";
            return plate;
        }

        /// <summary>Kinematic sliding door. Add it to a source's powered list, or set openOnTaskComplete.</summary>
        public static Door SlidingDoor(string name, Transform parent, Vector3 center, Vector3 size, Vector3 openOffset)
        {
            var go = Box(name, parent, center, size, Mat("Trim_Coral", new Color(1f, 0.55f, 0.5f)));
            go.AddComponent<Rigidbody>().isKinematic = true;
            var door = go.AddComponent<Door>();
            door.openOffset = openOffset;
            return door;
        }

        /// <summary>Floor wire through the given points (y is taken from the points). Lights up when powered.</summary>
        public static PowerWire Wire(string name, Transform parent, Color color, params Vector3[] points)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var segments = new List<Renderer>();
            var mat = Mat("Wire_" + ColorUtility.ToHtmlStringRGB(color), color);
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 a = points[i], b = points[i + 1];
                Vector3 d = b - a;
                var seg = Box("Seg" + i, root.transform, (a + b) * 0.5f + Vector3.up * 0.015f, new Vector3(0.18f, 0.03f, d.magnitude + 0.18f), mat, false);
                seg.transform.rotation = Quaternion.LookRotation(new Vector3(d.x, 0, d.z));
                segments.Add(seg.GetComponent<Renderer>());
            }
            var wire = root.AddComponent<PowerWire>();
            wire.segments = segments.ToArray();
            wire.color = color;
            return wire;
        }

        /// <summary>Belt whose top surface is at <paramref name="center"/>.y, running toward <paramref name="yaw"/> (0 = +Z).</summary>
        public static Conveyor Belt(string name, Transform parent, Vector3 center, float length, float width, float yaw, float speed, bool startsOn)
        {
            var rot = Quaternion.Euler(0, yaw, 0);
            var belt = Box(name, parent, center + Vector3.down * 0.15f, new Vector3(width, 0.3f, length), Mat("Belt_Dark", new Color(0.25f, 0.26f, 0.3f)));
            belt.transform.rotation = rot;
            belt.GetComponent<Collider>().sharedMaterial = PhysMat("Belt", 0.6f, 0.6f, PhysicsMaterialCombine.Average);

            var stripesRoot = new GameObject(name + "_Stripes").transform;
            stripesRoot.SetParent(parent, false);
            var stripeMat = Mat("Belt_Stripe", new Color(1f, 0.8f, 0.3f));
            var stripes = new List<Transform>();
            for (float z = -length * 0.5f + 0.25f; z < length * 0.5f; z += 0.6f)
            {
                var s = Box("Stripe", stripesRoot, center + rot * new Vector3(0, 0.01f, z), new Vector3(width * 0.85f, 0.02f, 0.12f), stripeMat, false);
                s.transform.rotation = rot;
                stripes.Add(s.transform);
            }
            var rail = Mat("Steel", new Color(0.78f, 0.8f, 0.85f), 0.7f);
            foreach (float side in new[] { -1f, 1f })
            {
                var r = Box("Rail", stripesRoot, center + rot * new Vector3(side * (width * 0.5f + 0.05f), 0.02f, 0), new Vector3(0.1f, 0.12f, length), rail, false);
                r.transform.rotation = rot;
            }

            var c = belt.AddComponent<Conveyor>();
            c.speed = speed;
            c.startsOn = startsOn;
            c.length = length;
            c.stripes = stripes.ToArray();
            return c;
        }

        public static PowerCombiner Gate(string name, Transform parent, PowerCombiner.Mode mode, int inputs, params GameObject[] powered)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var g = go.AddComponent<PowerCombiner>();
            g.mode = mode;
            g.inputCount = inputs;
            g.poweredObjects = powered;
            return g;
        }

        public static TaskStepSignal StepSignal(string name, Transform parent, string stepId, string cue = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<TaskStepSignal>();
            s.stepId = stepId;
            s.cue = cue;
            return s;
        }

        public static TaskStepTrigger StepZone(string name, Transform parent, Vector3 center, Vector3 size, string stepId, string cue = null)
        {
            var go = Box(name, parent, center, size, null);
            go.GetComponent<Collider>().isTrigger = true;
            var t = go.AddComponent<TaskStepTrigger>();
            t.stepId = stepId;
            t.cue = cue;
            return t;
        }

        public static Checkpoint CheckpointZone(string name, Transform parent, Vector3 center, Vector3 size, string cue = null)
        {
            var go = Box(name, parent, center, size, null);
            go.GetComponent<Collider>().isTrigger = true;
            var cp = go.AddComponent<Checkpoint>();
            cp.cue = cue;
            return cp;
        }

        public static void KillPlane(Transform parent, float y)
        {
            var go = Box("KillZone", parent, new Vector3(0, y, 0), new Vector3(200f, 1f, 200f), null);
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<KillZone>();
        }

        public static void ExitFlag(Transform parent, Vector3 floorPos, Vector3 triggerSize)
        {
            var exit = Box("LevelExit", parent, floorPos + Vector3.up * (triggerSize.y * 0.5f), triggerSize, null);
            exit.GetComponent<Collider>().isTrigger = true;
            exit.AddComponent<LevelExit>();
            Box("FlagPole", parent, floorPos + new Vector3(1.2f, 1.5f, 0), new Vector3(0.1f, 3f, 0.1f), Mat("Steel", new Color(0.78f, 0.8f, 0.85f), 0.7f), false);
            Box("Flag", parent, floorPos + new Vector3(1.65f, 2.7f, 0), new Vector3(0.8f, 0.5f, 0.04f), Mat("Flag_Green", new Color(0.35f, 0.85f, 0.45f)), false);
        }

        /// <summary>Swing hook hanging from a short rope.</summary>
        public static TetherAnchor Hook(string name, Transform parent, Vector3 pos)
        {
            Box(name + "_Rope", parent, pos + Vector3.up * 0.6f, new Vector3(0.05f, 0.9f, 0.05f), Mat("Post_Blue", new Color(0.3f, 0.45f, 0.75f)), false);
            var hook = Sphere(name, parent, pos, 0.35f, Mat("Hook_Yellow", new Color(1f, 0.85f, 0.2f)), collider: true);
            return hook.AddComponent<TetherAnchor>();
        }
    }
}
