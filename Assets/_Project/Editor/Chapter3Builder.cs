using RobotRescue.Bots;
using RobotRescue.Narrative;
using RobotRescue.Puzzles;
using RobotRescue.Tasks;
using RobotRescue.Tasks.Rescue;
using RobotRescue.World;
using UnityEditor;
using UnityEngine;
using static RobotRescue.EditorTools.ChamberKit;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// Menu: Robot Rescue > Chapter 3 > ...
    /// Brightwater Park, a walled garden platform on the Reach. Every hum shakes another screw loose.
    ///  - Call 3-1 "Loose Screws": pick up the Screwdriver, tighten the drawbridge brackets so it drops across the duck
    ///    pond, then fix the fountain's pump panel (three screws).
    ///  - Big Rescue "Up a Tree": Mittens the cat is up the giant oak and climbs higher whenever PIP gets close.
    ///    Screw the fallen ladder back up, climb branch to branch (the Hum shakes the tree and costs grip), catch her
    ///    on the top branch and bring her down to her basket.
    /// </summary>
    public static class Chapter3Builder
    {
        public const string LooseScene = Root + "/Scenes/Ch3_Call01_LooseScrews.unity";
        public const string OakScene = Root + "/Scenes/Ch3_Boss_UpATree.unity";
        const string DataDir = Root + "/Data/Ch3";

        static readonly Color TitleColor = new Color(0.2f, 0.42f, 0.25f);
        static readonly Color Steel = new Color(0.78f, 0.8f, 0.85f);   // same as the shared "Steel" material

        struct ParkMats
        {
            public Material grass, hedge, trim, path, stone, water, wood, bark, leaves, soil;
        }

        static ParkMats Mats() => new ParkMats
        {
            grass = Mat("Park_Grass", new Color(0.56f, 0.84f, 0.45f)),
            hedge = Mat("Park_Hedge", new Color(0.3f, 0.62f, 0.36f)),
            trim = Mat("Park_Flowers", new Color(1f, 0.62f, 0.75f)),
            path = Mat("Park_Path", new Color(0.95f, 0.87f, 0.72f)),
            stone = Mat("Park_Stone", new Color(0.82f, 0.82f, 0.88f)),
            water = Mat("Park_Water", new Color(0.4f, 0.75f, 1f), 0.9f),
            wood = Mat("Wood", new Color(0.8f, 0.6f, 0.42f)),
            bark = Mat("Oak_Bark", new Color(0.55f, 0.4f, 0.3f)),
            leaves = Mat("Oak_Leaves", new Color(0.45f, 0.78f, 0.38f)),
            soil = Mat("Park_Soil", new Color(0.55f, 0.4f, 0.3f)),
        };

        // ================================================================== Call 3-1: Loose Screws

        [MenuItem("Robot Rescue/Chapter 3/Call 3-1: Loose Screws")]
        public static void BuildLooseScrews()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var tools = ToolKit();
            var m = Mats();

            var task = Task("Ch3_Call01_LooseScrews", "ch3_call01", "Call 3-1: Loose Screws", "Rosa the park keeper",
                "Every hum shakes more screws out of Brightwater Park. The drawbridge is stuck up and the fountain's dry.", 40,
                ("get_screwdriver", "Pick up the Screwdriver"),
                ("bridge", "Tighten both drawbridge brackets"),
                ("fountain", "Fix the fountain's pump panel (3 screws)"));

            // Garden x -13..13, z -9..9. Duck pond x 2..8 splits it; the gate (in the world) is in the east wall.
            var room = new GameObject("Park").transform;
            RoomWithExit(room, 26f, 18f, 4.5f, m.grass, m.hedge, m.trim, "BRIGHTWATER PARK", TitleColor);
            Object.DestroyImmediate(room.Find("Floor").gameObject);
            Box("Floor", room, new Vector3(-5.5f, -0.25f, 0f), new Vector3(15f, 0.5f, 18f), m.grass);
            Box("Floor", room, new Vector3(10.5f, -0.25f, 0f), new Vector3(5f, 0.5f, 18f), m.grass);
            HedgeTops(room, m, 26f, 18f, 4.5f);

            // Pond: too wide to jump, deep enough to fall in (the pit kill zone sits just under the water).
            Box("PondBed", room, new Vector3(5f, -3.25f, 0f), new Vector3(6f, 0.5f, 18f), m.soil);
            Box("PondBank_W", room, new Vector3(2.15f, -1.5f, 0f), new Vector3(0.3f, 3f, 18f), m.stone);
            Box("PondBank_E", room, new Vector3(7.85f, -1.5f, 0f), new Vector3(0.3f, 3f, 18f), m.stone);
            Box("PondWater", room, new Vector3(5f, -0.9f, 0f), new Vector3(5.4f, 0.1f, 18f), m.water, false);
            var kill = Box("KillZone", room, new Vector3(5f, -2.2f, 0f), new Vector3(5.4f, 1f, 18f), null);
            kill.GetComponent<Collider>().isTrigger = true;
            kill.AddComponent<KillZone>();
            foreach (var (x, z) in new[] { (3.4f, 5.5f), (6.3f, -4.5f), (4.2f, -7f), (6.6f, 6.8f) })
                Cylinder("LilyPad", room, new Vector3(x, -0.83f, z), 0.45f, 0.02f, Mat("Lily_Pad", new Color(0.35f, 0.75f, 0.4f)));
            Duck(room, new Vector3(4f, -0.85f, 4f), 20f);
            Duck(room, new Vector3(5.6f, -0.85f, 3.2f), -30f);
            Duck(room, new Vector3(4.8f, -0.85f, -5.5f), 160f);

            // Drawbridge: hinged on the east bank, standing up until both brackets are tight.
            var bridge = HingePivot("Drawbridge", room, new Vector3(8f, 0f, 0f));
            Box("Plank", bridge.transform, new Vector3(4.7f, -0.15f, 0f), new Vector3(6.6f, 0.3f, 2.4f), m.wood);
            Box("Rail_N", bridge.transform, new Vector3(4.7f, 0.3f, 1.15f), new Vector3(6.6f, 0.5f, 0.1f), m.wood);
            Box("Rail_S", bridge.transform, new Vector3(4.7f, 0.3f, -1.15f), new Vector3(6.6f, 0.5f, 0.1f), m.wood);
            for (float x = 2f; x < 8f; x += 0.75f)
                Box("Slat", bridge.transform, new Vector3(x, 0.005f, 0f), new Vector3(0.08f, 0.02f, 2.3f), Mat("Wood_Dark", new Color(0.62f, 0.45f, 0.32f)), false);
            bridge.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            bridge.degreesPerSecond = 70f;

            var bridgeStep = StepSignal("Step_Bridge", room, "bridge", "bridge_down");
            var bridgeGate = Gate("Bridge_AND", room, PowerCombiner.Mode.All, 2, bridge.gameObject, bridgeStep.gameObject);
            foreach (float z in new[] { -1.7f, 1.7f })
            {
                Box("BracketPost", room, new Vector3(8.5f, 0.45f, z), new Vector3(0.4f, 0.9f, 0.4f), Mat("Steel", Steel, 0.7f));
                Screw(room, new Vector3(8.7f, 0.6f, z), Vector3.right, bridgeGate.gameObject);
            }
            Label("DRAWBRIDGE\nTIGHTEN BOTH BRACKETS", room, new Vector3(8.6f, 2.3f, 0f), 90f, 0.045f, TitleColor);

            // East bank: the Screwdriver, Rosa, and (in the world) the gate.
            ToolPedestal("ScrewdriverStand", room, new Vector3(11f, 0f, -2.5f), tools.screwdriver, "get_screwdriver", "got_screwdriver");
            Keeper(room, new Vector3(12f, 0f, -6.5f), -90f);
            ParkTree(room, m, new Vector3(11.6f, 0f, 7.6f), 5f);

            // West bank: the fountain with its pump panel.
            Vector3 F = new Vector3(-5f, 0f, 2.5f);
            Cylinder("Basin", room, F + Vector3.up * 0.35f, 2.2f, 0.7f, m.stone);
            Box("BasinCollider", room, F + Vector3.up * 0.35f, new Vector3(3.6f, 0.7f, 3.6f), null);
            var dry = Cylinder("BasinDry", room, F + Vector3.up * 0.62f, 1.95f, 0.06f, Mat("Park_Mud", new Color(0.62f, 0.5f, 0.4f)));
            var full = Cylinder("BasinWater", room, F + Vector3.up * 0.64f, 1.95f, 0.06f, m.water);
            Cylinder("Column", room, F + Vector3.up * 1.1f, 0.3f, 1.6f, m.stone);
            Box("ColumnCollider", room, F + Vector3.up * 1.1f, new Vector3(0.5f, 1.6f, 0.5f), null);
            Cylinder("Bowl", room, F + Vector3.up * 1.95f, 0.9f, 0.2f, m.stone);
            var waterGlow = Glow("Fountain_Water", new Color(0.55f, 0.85f, 1f), 0.6f);
            var jets = new System.Collections.Generic.List<GameObject> { full };
            jets.Add(Cylinder("Jet", room, F + Vector3.up * 2.7f, 0.12f, 1.4f, waterGlow));
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f * Mathf.Deg2Rad;
                jets.Add(Sphere("Drip", room, F + new Vector3(Mathf.Cos(a) * 0.95f, 1.7f, Mathf.Sin(a) * 0.95f), 0.1f, waterGlow, new Vector3(1f, 2.4f, 1f)));
            }
            var pump = new GameObject("FountainPump");
            pump.transform.SetParent(room, false);
            var toggle = pump.AddComponent<PowerToggle>();
            toggle.showWhenPowered = jets.ToArray();
            toggle.hideWhenPowered = new[] { dry };
            Box("PumpPanel", room, new Vector3(F.x, 0.5f, 0.15f), new Vector3(1.8f, 1f, 0.25f), Mat("Steel", Steel, 0.7f));
            Label("PUMP", room, new Vector3(F.x, 0.88f, 0.01f), 180f, 0.035f, new Color(0.25f, 0.3f, 0.4f));
            var fountainStep = StepSignal("Step_Fountain", room, "fountain", "fountain_on");
            var fountainGate = Gate("Fountain_AND", room, PowerCombiner.Mode.All, 3, pump, fountainStep.gameObject);
            foreach (float dx in new[] { -0.6f, 0f, 0.6f })
                Screw(room, new Vector3(F.x + dx, 0.5f, 0.02f), Vector3.back, fountainGate.gameObject);

            // A trellis up the north hedge with a memory bolt at the top.
            GripWall("Trellis", room, new Vector3(-10f, 0f, 8.8f), new Vector3(3f, 4f, 0.3f), 180f, new Color(0.95f, 0.8f, 0.9f));
            Bolt("park_trellis", room, new Vector3(-10f, 4.55f, 8.3f));

            // Paths, beds, benches, lamps, trees.
            Box("Path_Bridge", room, new Vector3(-1.5f, 0.01f, 0f), new Vector3(7f, 0.02f, 2.2f), m.path, false);
            Box("Path_Exit", room, new Vector3(0f, 0.01f, -5f), new Vector3(2.2f, 0.02f, 8f), m.path, false);
            Box("Path_East", room, new Vector3(10.5f, 0.01f, 0f), new Vector3(5f, 0.02f, 2.2f), m.path, false);
            FlowerBed(room, m, new Vector3(-7f, 0f, 8.1f), new Vector3(3f, 0f, 1.2f));
            FlowerBed(room, m, new Vector3(-5f, 0f, -7.9f), new Vector3(4f, 0f, 1.2f));
            FlowerBed(room, m, new Vector3(11.8f, 0f, 3.8f), new Vector3(1.2f, 0f, 2f));
            Bench(room, m, new Vector3(-9.5f, 0f, -1.5f), 90f);
            Bench(room, m, new Vector3(-1f, 0f, 5.5f), 180f);
            foreach (var p in new[] { new Vector3(1f, 0f, 2.2f), new Vector3(1f, 0f, -2.2f), new Vector3(9f, 0f, 2.3f), new Vector3(9f, 0f, -2.3f), new Vector3(-10f, 0f, -7.5f) })
                ParkLamp(room, p);
            ParkTree(room, m, new Vector3(-11.2f, 0f, -6.6f), 5.5f);
            ParkTree(room, m, new Vector3(-11.6f, 0f, 4.5f), 4.6f);
            ParkTree(room, m, new Vector3(-1.5f, 0f, 7.6f), 4.2f);

            var dispatch = Script("Dispatch_Ch3_Call01",
                Line("intro", "PIP, welcome to Brightwater Park! Rosa the park keeper says every time the hum comes through, more screws wriggle loose. The drawbridge is stuck up and the fountain's dry. There's a Screwdriver on the stand.", 9f),
                Line("got_screwdriver", "The Screwdriver! Hold F (or B) in front of a loose screw. The glowing yellow rings are the loose ones. Tab swaps tools.", 7f),
                Line("bridge_down", "Drawbridge down! The ducks are very relieved. They were tired of swimming around it.", 5f),
                Line("fountain_on", "The fountain's back! Rosa's doing a little dance. Don't look, she's shy.", 5f),
                Line("fell", "Splash! The ducks will remember this.", 3f, true),
                Line("fell", "Fell in the pond. Ducks: one. PIP: zero.", 3f, true),
                Line("exit", "Park fixed! Rosa says thank you, and also 'nobody climbs my oak'. We'll see about that.", 6f));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(11f, 0.05f, 4f), -90f, tools.grabber, tools.whisk);
            BuildSystems(task, dispatch, bot);
            FinishScene(scene, LooseScene, bot);
        }

        // ================================================================== Big Rescue: Up a Tree

        [MenuItem("Robot Rescue/Chapter 3/Big Rescue: Up a Tree")]
        public static void BuildUpATree()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var tools = ToolKit();
            var m = Mats();

            var task = Task("Ch3_Boss_UpATree", "ch3_boss", "BIG RESCUE: Up a Tree", "Mittens the cat (and Rosa)",
                "Mittens is up the giant oak, and every hum scares her higher. Bring her home.", 90,
                ("ladder", "Screw the fallen ladder's brackets back on"),
                ("got_cat", "Climb the oak and catch Mittens"),
                ("cat_home", "Bring Mittens down to her basket"));

            // Garden x -13..13, z -11..11, the oak at (0, 3).
            var room = new GameObject("OakGarden").transform;
            RoomWithExit(room, 26f, 22f, 4.5f, m.grass, m.hedge, m.trim, "THE OLD OAK", TitleColor);
            HedgeTops(room, m, 26f, 22f, 4.5f);
            Vector3 T = new Vector3(0f, 0f, 3f);

            // Trunk: the bottom is too smooth to climb (that's what the ladder is for); the upper bark is grippy.
            Cylinder("Trunk", room, T + Vector3.up * 8f, 1.05f, 16f, m.bark);
            Box("TrunkLower", room, T + Vector3.up * 2.25f, new Vector3(2f, 4.5f, 2f), null);
            Box("TrunkUpper", room, T + Vector3.up * 10.25f, new Vector3(2f, 11.5f, 2f), null).AddComponent<ClimbSurface>();
            var knot = Mat("Grip_Hold", new Color(1f, 0.82f, 0.3f));
            var rng = new System.Random(3);
            for (float y = 5.2f; y < 15.6f; y += 0.75f)
                for (int i = 0; i < 6; i++)
                {
                    float a = (i * 60f + (float)rng.NextDouble() * 30f + y * 40f) * Mathf.Deg2Rad;
                    Sphere("Knot", room, T + new Vector3(Mathf.Cos(a) * 1.05f, y, Mathf.Sin(a) * 1.05f), 0.11f, knot, new Vector3(1.3f, 0.8f, 1.3f));
                }
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f + 20f;
                var root = Sphere("Root", room, T + Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.15f, 1.6f), 0.6f, m.bark, new Vector3(1f, 0.6f, 2.4f));
                root.transform.rotation = Quaternion.Euler(0f, a, 0f);
            }

            // Treehouse deck at 4.5 m: a ring around the trunk, open on the east where the ladder comes up.
            const float deckY = 4.5f;
            Box("Deck_N", room, T + new Vector3(0f, deckY - 0.15f, 2f), new Vector3(6f, 0.3f, 2f), m.wood);
            Box("Deck_S", room, T + new Vector3(0f, deckY - 0.15f, -2f), new Vector3(6f, 0.3f, 2f), m.wood);
            Box("Deck_E", room, T + new Vector3(2f, deckY - 0.15f, 0f), new Vector3(2f, 0.3f, 2f), m.wood);
            Box("Deck_W", room, T + new Vector3(-2f, deckY - 0.15f, 0f), new Vector3(2f, 0.3f, 2f), m.wood);
            Box("DeckRail_N", room, T + new Vector3(0f, deckY + 0.3f, 2.95f), new Vector3(6f, 0.6f, 0.1f), m.wood);
            Box("DeckRail_S", room, T + new Vector3(0f, deckY + 0.3f, -2.95f), new Vector3(6f, 0.6f, 0.1f), m.wood);
            Box("DeckRail_W", room, T + new Vector3(-2.95f, deckY + 0.3f, 0f), new Vector3(0.1f, 0.6f, 6f), m.wood);
            foreach (var (x, z) in new[] { (-2.7f, -2.7f), (2.7f, -2.7f), (-2.7f, 2.7f), (2.7f, 2.7f) })
                Cylinder("DeckPost", room, T + new Vector3(x, deckY * 0.5f - 0.15f, z), 0.12f, deckY - 0.3f, m.wood);
            Label("TREEHOUSE", room, T + new Vector3(0f, deckY + 0.75f, -3.02f), 180f, 0.04f, new Color(0.45f, 0.3f, 0.2f));
            for (int i = 0; i < 7; i++)
            {
                var flag = Box("Bunting", room, T + new Vector3(-2.6f + i * 0.85f, deckY + 1.05f, -2.95f), new Vector3(0.28f, 0.28f, 0.02f),
                    Mat(i % 2 == 0 ? "Bunting_Pink" : "Bunting_Yellow", i % 2 == 0 ? new Color(1f, 0.6f, 0.75f) : new Color(1f, 0.85f, 0.35f)), false);
                flag.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
            }

            // The fallen ladder: lying in the grass until both brackets are screwed tight, then it stands up.
            Vector3 L0 = T + new Vector3(3.25f, 0.12f, 0f);
            var ladder = HingePivot("Ladder", room, L0);
            Box("Rails", ladder.transform, L0 + Vector3.up * 2.45f, new Vector3(0.2f, 4.9f, 1.1f), m.wood).AddComponent<ClimbSurface>().staminaMultiplier = 0.35f;
            for (float y = 0.4f; y < 4.8f; y += 0.45f)
                Box("Rung", ladder.transform, L0 + new Vector3(0.13f, y, 0f), new Vector3(0.07f, 0.08f, 1f), Mat("Grip_Hold", new Color(1f, 0.82f, 0.3f)), false);
            ladder.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
            ladder.degreesPerSecond = 60f;
            var ladderStep = StepSignal("Step_Ladder", room, "ladder", "ladder_up");
            var ladderGate = Gate("Ladder_AND", room, PowerCombiner.Mode.All, 2, ladder.gameObject, ladderStep.gameObject);
            foreach (float dz in new[] { -0.95f, 0.95f })
            {
                Box("LadderBracket", room, T + new Vector3(3f, 0.25f, dz), new Vector3(0.4f, 0.5f, 0.3f), Mat("Steel", Steel, 0.7f));
                Screw(room, T + new Vector3(3.2f, 0.3f, dz), Vector3.right, ladderGate.gameObject);
            }

            // Branches: west at 9 m, north at 13.5 m. Their solid part starts a little way out from the trunk (the twig
            // joining them is just for show), so PIP can climb past and then leap off the trunk onto them.
            Box("Branch_W", room, T + new Vector3(-3.8f, 8.8f, 0f), new Vector3(4f, 0.4f, 1.4f), m.bark);
            Cylinder("Twig_W", room, T + new Vector3(-1.4f, 8.8f, 0f), 0.2f, 0.9f, m.bark).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            Box("Branch_N", room, T + new Vector3(0f, 13.3f, 3.8f), new Vector3(1.4f, 0.4f, 4f), m.bark);
            Cylinder("Twig_N", room, T + new Vector3(0f, 13.3f, 1.4f), 0.2f, 0.9f, m.bark).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var wobbles = new System.Collections.Generic.List<Wobble>
            {
                LeafCluster(room, m, T + new Vector3(-6.3f, 10f, 0f), 1.2f, true),
                LeafCluster(room, m, T + new Vector3(0.3f, 15.2f, 7.4f), 1.2f, true),
                LeafCluster(room, m, T + new Vector3(2.6f, 7.4f, -1.4f), 1.1f, true),
            };
            // The crown: high above the top of the trunk, no outline so the camera can pass through it cleanly.
            var crownLeaves = Mat("Oak_Crown", new Color(0.4f, 0.74f, 0.36f), 0.2f, outline: false);
            foreach (var (x, y, z, r) in new[] { (-3f, 19.5f, 0f, 2.6f), (3f, 19.8f, 1f, 2.4f), (0f, 20.5f, -3f, 2.5f), (0.5f, 21.5f, 2.5f, 2.6f), (-2f, 18.8f, 4f, 2f) })
            {
                var pivot = new GameObject("Crown").transform;
                pivot.SetParent(room, false);
                pivot.position = T + new Vector3(0f, 16f, 0f);
                Sphere("Leaves", pivot, T + new Vector3(x, y, z), r, crownLeaves);
                var w = pivot.gameObject.AddComponent<Wobble>();
                w.response = 0f; w.stiffness = 40f; w.damping = 2.5f; w.maxAngle = 6f; w.idleSway = 0.6f;
                wobbles.Add(w);
            }
            Bolt("oak_crown", room, T + new Vector3(0f, 16.7f, 0f));

            // Mittens, and where she runs to.
            var perches = new[]
            {
                Perch(room, T + new Vector3(2.3f, deckY, 2.2f), 200f),
                Perch(room, T + new Vector3(-4.9f, 9f, 0f), 90f),
                Perch(room, T + new Vector3(0f, 13.5f, 4.9f), 180f),
            };
            var cat = Cat(room, perches);

            // Rosa and the basket by the path.
            Keeper(room, new Vector3(-8.6f, 0f, -6.4f), 60f);
            Basket(room, new Vector3(-7f, 0f, -5.5f));

            // The Hum: every so often it shakes the whole garden.
            var hum = new GameObject("TheHum");
            hum.transform.SetParent(room, false);
            hum.transform.position = T + Vector3.up * 6f;
            var src = hum.AddComponent<AudioSource>();
            src.spatialBlend = 0f;
            var drone = hum.AddComponent<Babble>();
            drone.voiceHz = 85f;
            drone.radio = true;
            drone.volume = 0.5f;
            drone.syllableGap = 0.22f;
            var tremor = hum.AddComponent<HumTremor>();
            tremor.interval = 24f;
            tremor.firstDelay = 14f;
            tremor.shakeWobbles = wobbles.ToArray();
            tremor.cats = new[] { cat };
            tremor.voice = drone;

            // Garden dressing.
            Box("Path_Exit", room, new Vector3(0f, 0.01f, -7f), new Vector3(2.2f, 0.02f, 8f), m.path, false);
            Box("Path_East", room, new Vector3(7f, 0.01f, -5.5f), new Vector3(12f, 0.02f, 2.2f), m.path, false);
            Box("PicnicBlanket", room, new Vector3(-6.5f, 0.015f, 6.5f), new Vector3(2.4f, 0.02f, 1.8f), Mat("Picnic_Red", new Color(1f, 0.45f, 0.45f)), false);
            FlowerBed(room, m, new Vector3(-10f, 0f, 10.1f), new Vector3(4f, 0f, 1.2f));
            FlowerBed(room, m, new Vector3(9f, 0f, 10.1f), new Vector3(5f, 0f, 1.2f));
            FlowerBed(room, m, new Vector3(-11.9f, 0f, -2f), new Vector3(1.2f, 0f, 4f));
            Bench(room, m, new Vector3(-4f, 0f, -8.6f), 0f);
            Bench(room, m, new Vector3(8f, 0f, 2f), -90f);
            foreach (var p in new[] { new Vector3(2f, 0f, -8.5f), new Vector3(-2f, 0f, -8.5f), new Vector3(11f, 0f, -3.5f), new Vector3(-10f, 0f, 3f) })
                ParkLamp(room, p);
            ParkTree(room, m, new Vector3(-11.3f, 0f, -9.3f), 4.5f);
            ParkTree(room, m, new Vector3(11.4f, 0f, 9.2f), 5f);
            Box("RulesPost", room, new Vector3(9.8f, 0.9f, -8.6f), new Vector3(0.12f, 1.8f, 0.12f), m.wood, false);
            Box("RulesBoard", room, new Vector3(9.8f, 2f, -8.6f), new Vector3(0.08f, 0.9f, 1.6f), Mat("Sign_Cream", new Color(1f, 0.96f, 0.86f)), false);
            Label("PARK RULES\nNO CLIMBING THE OAK\n(rescue bots excepted)", room, new Vector3(9.85f, 2f, -8.6f), 90f, 0.028f, new Color(0.35f, 0.25f, 0.2f));

            var dispatch = Script("Dispatch_Ch3_Boss",
                Line("intro", "BIG RESCUE! Mittens the cat is up the old oak, and every hum scares her higher. Rosa's ladder fell in the last tremor. Screw its brackets back on, climb up, and bring Mittens home to her basket. And PIP: hold on tight when it hums.", 11f),
                Line("ladder_up", "Ladder's up! The bottom of the trunk is too smooth to climb, but the knobbly bark higher up is grippy. Climb past a branch, then jump off the trunk onto it.", 7f),
                Line("cat_flee_1", "She ran higher! Cats. Honestly.", 3f),
                Line("cat_top", "She's on the very top branch now, and she's just realised she can't get down. Classic cat. Climb up the north side past her branch and jump across, or go right to the top and drop down.", 7f),
                Line("cat_grabbed", "Got her! You can't climb with your hands full, so get down carefully. Cats land on their feet. You don't.", 6f),
                Line("throw", "Did you just THROW the cat? ...She landed on her feet. Rosa would like a word.", 5f),
                Line("cat_home", "Mittens is home! Purring and everything. Rosa's crying a little. Good tears.", 5f),
                Line("hum_tremor_first", "Hang on! THAT's the hum! Feel the oak shaking? It shakes the roots loose every time. It's coming from under the water.", 7f),
                Line("hum_tremor", "Here it comes again! Grip!", 2.5f, true),
                Line("hum_tremor", "Hum! Hold on to something!", 2.5f, true),
                Line("fell", "Ooh, long way down. Back to the bottom.", 3f, true),
                Line("exit", "Rescue complete! And PIP, the oak was shaking in time with the hum. Short short short, long long long. Whoever's sending it, they're getting weaker.", 8f));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(11f, 0.05f, -5.5f), -90f, tools.grabber, tools.whisk, tools.screwdriver);
            BuildSystems(task, dispatch, bot);
            FinishScene(scene, OakScene, bot);
        }

        // ================================================================== pieces

        /// <summary>A loose screw sticking out of a surface, with a glowing ring. Tightening it powers <paramref name="powered"/>.</summary>
        internal static ScrewSocket Screw(Transform parent, Vector3 surface, Vector3 facing, params GameObject[] powered)
        {
            var root = new GameObject("Loose screw").transform;
            root.SetParent(parent, false);
            root.position = surface;
            var col = root.gameObject.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius = 0.35f;
            var rot = Quaternion.FromToRotation(Vector3.up, facing);
            var steel = Mat("Steel", Steel, 0.7f);
            var marker = Cylinder("LooseMarker", root, surface + facing * 0.012f, 0.2f, 0.02f, Glow("Screw_Loose", new Color(1f, 0.8f, 0.2f), 1.6f));
            marker.transform.rotation = rot;
            var head = Cylinder("ScrewHead", root, surface + facing * 0.16f, 0.1f, 0.05f, steel);
            head.transform.rotation = rot;
            Box("Slot", head.transform, surface + facing * 0.186f, new Vector3(0.14f, 0.012f, 0.03f), Mat("Screw_Slot", new Color(0.25f, 0.26f, 0.3f)), false);
            Cylinder("Shaft", head.transform, surface + facing * 0.06f, 0.035f, 0.18f, steel);
            var s = root.gameObject.AddComponent<ScrewSocket>();
            s.screw = head.transform;
            s.looseMarker = marker;
            s.poweredObjects = powered;
            return s;
        }

        static Hinge HingePivot(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<Rigidbody>().isKinematic = true;
            var h = go.AddComponent<Hinge>();
            h.poweredEuler = Vector3.zero;
            h.bounce = 4f;
            return h;
        }

        static Transform Perch(Transform parent, Vector3 pos, float yaw)
        {
            var t = new GameObject("CatPerch").transform;
            t.SetParent(parent, false);
            t.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            return t;
        }

        /// <summary>Mittens: an orange tabby with very strong opinions.</summary>
        static CatNPC Cat(Transform parent, Transform[] perches)
        {
            var go = new GameObject("Mittens");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(perches[0].position, perches[0].rotation);
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.25f, 0.05f);
            col.size = new Vector3(0.45f, 0.45f, 0.8f);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

            var fur = Mat("Cat_Orange", new Color(1f, 0.62f, 0.3f));
            var white = Mat("Cat_White", new Color(1f, 0.97f, 0.93f));
            var stripe = Mat("Cat_Stripe", new Color(0.85f, 0.42f, 0.16f));
            var tr = go.transform;
            Vector3 P(float x, float y, float z) => tr.TransformPoint(new Vector3(x, y, z));

            var body = new GameObject("Body").transform;
            body.SetParent(tr, false);
            body.localPosition = Vector3.zero;
            Sphere("Torso", body, P(0f, 0.26f, 0f), 0.23f, fur, new Vector3(1f, 0.92f, 1.55f));
            Sphere("Chest", body, P(0f, 0.24f, 0.2f), 0.15f, white, new Vector3(1f, 1.1f, 0.8f));
            for (int i = 0; i < 3; i++)
                Sphere("Stripe", body, P(0f, 0.46f, -0.18f + i * 0.13f), 0.06f, stripe, new Vector3(3.2f, 0.5f, 0.9f));
            Sphere("Head", body, P(0f, 0.5f, 0.32f), 0.19f, fur, new Vector3(1.1f, 1f, 1f));
            Sphere("Muzzle", body, P(0f, 0.45f, 0.47f), 0.08f, white, new Vector3(1.4f, 0.9f, 0.8f));
            Sphere("Nose", body, P(0f, 0.49f, 0.52f), 0.025f, Mat("Cat_Nose", new Color(1f, 0.55f, 0.6f)));
            var eye = Glow("Cat_Eye", new Color(0.45f, 1f, 0.5f), 1.2f);
            foreach (float s in new[] { -1f, 1f })
            {
                Sphere("Eye", body, P(s * 0.075f, 0.54f, 0.48f), 0.04f, eye, new Vector3(1f, 1.3f, 0.6f));
                var ear = Sphere("Ear", body, P(s * 0.11f, 0.67f, 0.3f), 0.07f, fur, new Vector3(0.9f, 1.6f, 0.5f));
                ear.transform.rotation = tr.rotation * Quaternion.Euler(0f, 0f, -s * 18f);
                Sphere("PawF", body, P(s * 0.11f, 0.05f, 0.24f), 0.06f, white, new Vector3(1f, 0.7f, 1.3f));
                Sphere("PawB", body, P(s * 0.12f, 0.05f, -0.22f), 0.065f, white, new Vector3(1f, 0.7f, 1.3f));
            }
            var tail = new GameObject("Tail").transform;
            tail.SetParent(body, false);
            tail.position = P(0f, 0.3f, -0.33f);
            tail.rotation = tr.rotation;
            Sphere("TailFluff", tail, P(0f, 0.3f, -0.6f), 0.07f, fur, new Vector3(1f, 1f, 4.2f));
            Sphere("TailTip", tail, P(0f, 0.3f, -0.88f), 0.075f, white);

            var src = go.AddComponent<AudioSource>();
            src.spatialBlend = 1f;
            var voice = go.AddComponent<Babble>();
            voice.voiceHz = 1150f;
            voice.volume = 0.4f;

            var cat = go.AddComponent<CatNPC>();
            cat.displayName = "Mittens";
            cat.firstPickupCue = "cat_grabbed";
            cat.perches = perches;
            cat.body = body;
            cat.tail = tail;
            cat.voice = voice;
            go.AddComponent<Respawnable>();
            return cat;
        }

        static void Basket(Transform parent, Vector3 floorPos)
        {
            var root = new GameObject("CatBasket").transform;
            root.SetParent(parent, false);
            root.position = floorPos;
            var wicker = Mat("Wicker", new Color(0.9f, 0.72f, 0.45f));
            Cylinder("Basket", root, floorPos + Vector3.up * 0.18f, 0.5f, 0.36f, wicker);
            Cylinder("Cushion", root, floorPos + Vector3.up * 0.37f, 0.42f, 0.04f, Mat("Cushion_Red", new Color(1f, 0.45f, 0.5f)));
            Cylinder("Rim", root, floorPos + Vector3.up * 0.38f, 0.53f, 0.06f, Mat("Wicker_Dark", new Color(0.75f, 0.55f, 0.32f)));
            Label("MITTENS", root, floorPos + new Vector3(0f, 0.75f, -0.55f), 180f, 0.03f, new Color(0.5f, 0.3f, 0.2f));
            var seat = new GameObject("Seat").transform;
            seat.SetParent(root, false);
            seat.SetPositionAndRotation(floorPos + Vector3.up * 0.39f, Quaternion.Euler(0f, 160f, 0f));
            var zone = Box("BasketZone", root, floorPos + Vector3.up * 0.6f, new Vector3(1.5f, 1.2f, 1.5f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var drop = zone.AddComponent<RescueDropZone>();
            drop.accepts = "Mittens";
            drop.seat = seat;
            drop.stepId = "cat_home";
            drop.cue = "cat_home";
        }

        /// <summary>Rosa the park keeper: green overalls, a straw hat and rosy cheeks.</summary>
        static void Keeper(Transform parent, Vector3 floorPos, float yaw)
        {
            var root = new GameObject("Rosa").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(floorPos, Quaternion.Euler(0f, yaw, 0f));
            Vector3 P(float x, float y, float z) => root.TransformPoint(new Vector3(x, y, z));
            var overalls = Mat("Rosa_Overalls", new Color(0.35f, 0.6f, 0.4f));
            var skin = Mat("Rosa_Skin", new Color(0.86f, 0.64f, 0.5f));
            Sphere("Body", root, P(0f, 0.78f, 0f), 0.42f, overalls, new Vector3(1f, 1.55f, 0.85f));
            Sphere("Shirt", root, P(0f, 1.15f, 0.12f), 0.24f, Mat("Rosa_Shirt", new Color(1f, 0.9f, 0.6f)), new Vector3(1.3f, 0.8f, 0.8f));
            Sphere("Head", root, P(0f, 1.62f, 0f), 0.3f, skin);
            Cylinder("HatBrim", root, P(0f, 1.86f, 0f), 0.46f, 0.04f, Mat("Straw", new Color(1f, 0.88f, 0.5f)));
            Cylinder("HatCrown", root, P(0f, 1.98f, 0f), 0.25f, 0.22f, Mat("Straw", new Color(1f, 0.88f, 0.5f)));
            Cylinder("HatBand", root, P(0f, 1.91f, 0f), 0.26f, 0.06f, Mat("Bunting_Pink", new Color(1f, 0.6f, 0.75f)));
            var eye = Mat("Eye_Black", new Color(0.12f, 0.1f, 0.15f));
            foreach (float s in new[] { -1f, 1f })
            {
                Sphere("Eye", root, P(s * 0.1f, 1.66f, 0.26f), 0.04f, eye);
                Sphere("Cheek", root, P(s * 0.17f, 1.56f, 0.22f), 0.055f, Mat("Cheek", new Color(1f, 0.55f, 0.55f)), new Vector3(1f, 0.7f, 0.5f));
                Sphere("Arm", root, P(s * 0.45f, 0.95f, 0.05f), 0.11f, Mat("Rosa_Shirt", new Color(1f, 0.9f, 0.6f)), new Vector3(1f, 2.6f, 1f));
                Sphere("Boot", root, P(s * 0.16f, 0.08f, 0.05f), 0.13f, Mat("Boot_Brown", new Color(0.45f, 0.32f, 0.25f)), new Vector3(1f, 0.7f, 1.4f));
            }
            Box("Collider", root, P(0f, 0.95f, 0f), new Vector3(0.8f, 1.9f, 0.8f), null);
            Label("ROSA", root, P(0f, 2.45f, 0f), yaw, 0.045f, new Color(0.25f, 0.45f, 0.3f));
        }

        static void Duck(Transform parent, Vector3 pos, float yaw)
        {
            var root = new GameObject("Duck").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var yellow = Mat("Duck_Yellow", new Color(1f, 0.88f, 0.3f));
            Sphere("Body", root, root.TransformPoint(new Vector3(0f, 0.12f, 0f)), 0.2f, yellow, new Vector3(1f, 0.75f, 1.3f));
            Sphere("Head", root, root.TransformPoint(new Vector3(0f, 0.33f, 0.16f)), 0.12f, yellow);
            Sphere("Beak", root, root.TransformPoint(new Vector3(0f, 0.31f, 0.28f)), 0.05f, Mat("Duck_Beak", new Color(1f, 0.55f, 0.15f)), new Vector3(1.3f, 0.5f, 1.4f));
            var eye = Mat("Eye_Black", new Color(0.12f, 0.1f, 0.15f));
            foreach (float s in new[] { -1f, 1f })
                Sphere("Eye", root, root.TransformPoint(new Vector3(s * 0.07f, 0.37f, 0.24f)), 0.022f, eye);
        }

        static Wobble LeafCluster(Transform parent, ParkMats m, Vector3 center, float radius, bool sway)
        {
            var pivot = new GameObject("Leaves").transform;
            pivot.SetParent(parent, false);
            pivot.position = center - Vector3.up * radius;
            Sphere("Leaves", pivot, center, radius, m.leaves);
            Sphere("Leaves", pivot, center + new Vector3(radius * 0.6f, -radius * 0.2f, radius * 0.3f), radius * 0.7f, m.leaves);
            Sphere("Leaves", pivot, center + new Vector3(-radius * 0.5f, -radius * 0.1f, -radius * 0.4f), radius * 0.65f, m.leaves);
            var w = pivot.gameObject.AddComponent<Wobble>();
            w.response = 0f;
            w.stiffness = 60f;
            w.damping = 3f;
            w.maxAngle = 10f;
            w.idleSway = sway ? 1f : 0f;
            return w;
        }

        static void ParkTree(Transform parent, ParkMats m, Vector3 floorPos, float height)
        {
            var root = new GameObject("Tree").transform;
            root.SetParent(parent, false);
            root.position = floorPos;
            Cylinder("Trunk", root, floorPos + Vector3.up * height * 0.3f, 0.25f, height * 0.6f, m.bark);
            Box("TrunkCollider", root, floorPos + Vector3.up * height * 0.3f, new Vector3(0.45f, height * 0.6f, 0.45f), null);
            LeafCluster(root, m, floorPos + Vector3.up * height * 0.75f, height * 0.28f, true);
        }

        static void FlowerBed(Transform parent, ParkMats m, Vector3 floorPos, Vector3 footprint)
        {
            var root = new GameObject("FlowerBed").transform;
            root.SetParent(parent, false);
            root.position = floorPos;
            Box("Soil", root, floorPos + Vector3.up * 0.1f, new Vector3(footprint.x, 0.2f, footprint.z), m.soil, false);
            Color[] colors = { new Color(1f, 0.55f, 0.7f), new Color(1f, 0.85f, 0.3f), new Color(0.75f, 0.6f, 1f), new Color(1f, 0.5f, 0.4f) };
            var rng = new System.Random(Mathf.RoundToInt(floorPos.x * 31f + floorPos.z * 17f));
            for (float x = -footprint.x * 0.5f + 0.25f; x < footprint.x * 0.5f; x += 0.42f)
                for (float z = -footprint.z * 0.5f + 0.25f; z < footprint.z * 0.5f; z += 0.42f)
                {
                    var c = colors[rng.Next(colors.Length)];
                    var p = floorPos + new Vector3(x + (float)rng.NextDouble() * 0.12f, 0.28f + (float)rng.NextDouble() * 0.08f, z);
                    Sphere("Flower", root, p, 0.09f, Mat("Flower_" + ColorUtility.ToHtmlStringRGB(c), c));
                }
        }

        static void Bench(Transform parent, ParkMats m, Vector3 floorPos, float yaw)
        {
            var root = new GameObject("Bench").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(floorPos, Quaternion.Euler(0f, yaw, 0f));
            GameObject B(string n, Vector3 local, Vector3 size, Material mat, bool col = true)
            {
                var go = Box(n, root, root.TransformPoint(local), size, mat, col);
                go.transform.rotation = root.rotation;
                return go;
            }
            B("Seat", new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.1f, 0.5f), m.wood);
            B("Back", new Vector3(0f, 0.8f, -0.24f), new Vector3(1.8f, 0.4f, 0.08f), m.wood);
            foreach (float s in new[] { -0.75f, 0.75f })
                B("Leg", new Vector3(s, 0.2f, 0f), new Vector3(0.1f, 0.4f, 0.45f), Mat("Bench_Iron", new Color(0.3f, 0.32f, 0.38f)), false);
        }

        static void ParkLamp(Transform parent, Vector3 floorPos)
        {
            var root = new GameObject("ParkLamp").transform;
            root.SetParent(parent, false);
            root.position = floorPos;
            Cylinder("Pole", root, floorPos + Vector3.up * 1.5f, 0.06f, 3f, Mat("Bench_Iron", new Color(0.3f, 0.32f, 0.38f)));
            Sphere("Bulb", root, floorPos + Vector3.up * 3.15f, 0.22f, Glow("Park_LampGlow", new Color(1f, 0.9f, 0.6f), 1.4f));
            Cylinder("Cap", root, floorPos + Vector3.up * 3.38f, 0.2f, 0.06f, Mat("Bench_Iron", new Color(0.3f, 0.32f, 0.38f)));
        }

        /// <summary>Bumpy rounded tops on the hedge walls, so they read as hedges rather than green boxes.</summary>
        static void HedgeTops(Transform parent, ParkMats m, float sizeX, float sizeZ, float height)
        {
            var root = new GameObject("HedgeTops").transform;
            root.SetParent(parent, false);
            float hx = sizeX * 0.5f + 0.25f, hz = sizeZ * 0.5f + 0.25f;
            for (float x = -hx + 0.6f; x < hx; x += 1.2f)
            {
                Sphere("Bump", root, new Vector3(x, height, hz), 0.45f, m.hedge, new Vector3(1.2f, 0.7f, 1f));
                if (Mathf.Abs(x) > 2f) Sphere("Bump", root, new Vector3(x, height, -hz), 0.45f, m.hedge, new Vector3(1.2f, 0.7f, 1f));
            }
            for (float z = -hz + 0.6f; z < hz; z += 1.2f)
                foreach (float x in new[] { -hx, hx })
                    Sphere("Bump", root, new Vector3(x, height, z), 0.45f, m.hedge, new Vector3(1f, 0.7f, 1.2f));
        }

        // ================================================================== data

        static TaskDefinition Task(string file, string id, string title, string rescuee, string description, int bolts, params (string id, string label)[] steps)
        {
            var task = LoadOrCreate<TaskDefinition>($"{DataDir}/Task_{file}.asset");
            task.taskId = id;
            task.title = title;
            task.rescuee = rescuee;
            task.description = description;
            task.boltsReward = bolts;
            task.steps = new TaskStep[steps.Length];
            for (int i = 0; i < steps.Length; i++) task.steps[i] = new TaskStep { id = steps[i].id, label = steps[i].label };
            EditorUtility.SetDirty(task);
            return task;
        }

        static DispatchScript Script(string file, params NarrationLine[] lines)
        {
            var d = LoadOrCreate<DispatchScript>($"{DataDir}/{file}.asset");
            d.lines = lines;
            EditorUtility.SetDirty(d);
            return d;
        }

        static NarrationLine Line(string cue, string text, float duration = 4f, bool repeatable = false) =>
            new NarrationLine { cue = cue, text = text, duration = duration, repeatable = repeatable };
    }
}
