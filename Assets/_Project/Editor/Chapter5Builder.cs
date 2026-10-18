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
    /// Menu: Robot Rescue > Chapter 5 > ...
    /// Brightwater harbour: the docks and the lighthouse.
    ///  - Call 5-1 "Heavy Lifting": pick up the Magnet, float two metal crates across the gangplank onto the listing
    ///    barge's ballast plates so it levels, and screw its mast light back on.
    ///  - Big Rescue "Lights Out": Captain Mo is lost in the fog. Magnet the fuel tank onto the generator plate, climb the
    ///    lighthouse and fix the lamp's lens, then plug in and be the beacon until her boat is home. Every hum makes the
    ///    beacon flicker on its own, in morse.
    /// </summary>
    public static class Chapter5Builder
    {
        public const string DocksScene = Root + "/Scenes/Ch5_Call01_HeavyLifting.unity";
        public const string LighthouseScene = Root + "/Scenes/Ch5_Boss_LightsOut.unity";
        const string DataDir = Root + "/Data/Ch5";

        static readonly Color TitleColor = new Color(0.15f, 0.35f, 0.55f);
        static readonly Color Steel = new Color(0.78f, 0.8f, 0.85f);

        struct DockMats
        {
            public Material boards, stone, water, rope, crate, red, white, wood;
        }

        static DockMats Mats() => new DockMats
        {
            boards = Mat("Dock_Boards", new Color(0.86f, 0.7f, 0.52f)),
            stone = Mat("Harbour_Stone", new Color(0.72f, 0.74f, 0.8f)),
            water = Mat("Harbour_Water", new Color(0.35f, 0.65f, 0.95f), 0.9f),
            rope = Mat("Rope", new Color(0.92f, 0.82f, 0.6f)),
            crate = Mat("Metal_Crate", new Color(0.45f, 0.6f, 0.85f), 0.6f),
            red = Mat("Lighthouse_Red", new Color(0.95f, 0.35f, 0.35f)),
            white = Mat("Lighthouse_White", new Color(0.98f, 0.97f, 0.95f)),
            wood = Mat("Wood", new Color(0.8f, 0.6f, 0.42f)),
        };

        // ================================================================== Call 5-1: Heavy Lifting

        [MenuItem("Robot Rescue/Chapter 5/Call 5-1: Heavy Lifting")]
        public static void BuildHeavyLifting()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var tools = ToolKit();
            var m = Mats();

            var task = Task("Ch5_Call01_HeavyLifting", "ch5_call01", "Call 5-1: Heavy Lifting", "Ines the harbourmaster",
                "The hum keeps knocking cargo about and the supply barge is listing. Level it out before it tips.", 45,
                ("get_magnet", "Pick up the Magnet"),
                ("ballast", "Float a metal crate onto each of the barge's ballast plates"),
                ("mast_light", "Screw the barge's mast light back on"));

            // Quay x -13..13, z -9..1; harbour basin z 1..9 between two side quays.
            var room = new GameObject("Docks").transform;
            RoomWithExit(room, 26f, 18f, 4.5f, m.boards, m.stone, m.rope, "BRIGHTWATER HARBOUR", TitleColor);
            Object.DestroyImmediate(room.Find("Floor").gameObject);
            Box("Floor", room, new Vector3(0f, -0.25f, -4f), new Vector3(26f, 0.5f, 10f), m.boards);
            Box("Floor", room, new Vector3(-11f, -0.25f, 5f), new Vector3(4f, 0.5f, 8f), m.boards);
            Box("Floor", room, new Vector3(11f, -0.25f, 5f), new Vector3(4f, 0.5f, 8f), m.boards);
            Basin(room, m, new Vector3(0f, 0f, 5f), new Vector3(18f, 0f, 8f));

            // The barge: listing to one side until both ballast plates are weighed down.
            var barge = HingePivot("Barge", room, new Vector3(0f, 0f, 5f));
            Box("Deck", barge.transform, new Vector3(0f, -0.2f, 5f), new Vector3(6f, 0.4f, 4.4f), m.wood);
            Box("Hull", barge.transform, new Vector3(0f, -0.9f, 5f), new Vector3(6.2f, 1f, 4.6f), m.red, false);
            Box("HullStripe", barge.transform, new Vector3(0f, -0.45f, 2.78f), new Vector3(6.2f, 0.12f, 0.05f), m.white, false);
            var mast = Box("Mast", barge.transform, new Vector3(0f, 2f, 6.5f), new Vector3(0.3f, 4f, 0.3f), m.white);
            var mastLight = Sphere("MastLight", barge.transform, new Vector3(0f, 4.15f, 6.5f), 0.25f, Glow("Mast_Light", new Color(1f, 0.85f, 0.4f), 2.5f));
            var toggle = new GameObject("MastLightSwitch").AddComponent<PowerToggle>();
            toggle.transform.SetParent(room, false);
            toggle.showWhenPowered = new[] { mastLight };
            toggle.pulse = 0f;
            var mastStep = StepSignal("Step_MastLight", room, "mast_light", "mast_light_on");
            Chapter3Builder.Screw(barge.transform, new Vector3(0f, 0.7f, 6.34f), Vector3.back, toggle.gameObject, mastStep.gameObject);

            var ballastStep = StepSignal("Step_Ballast", room, "ballast", "barge_level");
            var ballast = Gate("Ballast_AND", room, PowerCombiner.Mode.All, 2, barge.gameObject, ballastStep.gameObject);
            foreach (float x in new[] { -1.7f, 1.7f })
            {
                var plate = Plate("BallastPlate", barge.transform, new Vector3(x, 0f, 4.4f), 3.5f, 1.4f, ballast.gameObject);
                plate.tooLightCue = "ballast_light";
            }
            Label("BALLAST: 1 METAL CRATE EACH", barge.transform, new Vector3(0f, 1.6f, 6.3f), 180f, 0.04f, TitleColor);
            barge.transform.localRotation = Quaternion.Euler(0f, 0f, 5f);
            barge.degreesPerSecond = 8f;
            Box("Gangplank", room, new Vector3(0f, -0.1f, 1.9f), new Vector3(2.6f, 0.2f, 1.9f), m.wood);

            // The Magnet and two heavy crates the hum shook loose.
            ToolPedestal("MagnetStand", room, new Vector3(-9f, 0f, -6.5f), tools.magnet, "get_magnet", "got_magnet");
            MetalCrate("MetalCrate_A", room, m, new Vector3(-6f, 0.5f, -3f));
            MetalCrate("MetalCrate_B", room, m, new Vector3(7f, 0.5f, -5.5f));

            Person("Ines", room, new Vector3(-3f, 0f, -7.6f), 10f, new Color(0.2f, 0.35f, 0.6f), new Color(1f, 0.85f, 0.3f),
                new Color(0.75f, 0.55f, 0.42f), new Color(0.3f, 0.2f, 0.15f));

            // Harbour dressing, plus a crane ladder with a memory bolt at the top.
            foreach (var p in new[] { new Vector3(-8.6f, 0f, 0.6f), new Vector3(-3.5f, 0f, 0.6f), new Vector3(3.5f, 0f, 0.6f), new Vector3(8.6f, 0f, 0.6f) })
                Bollard(room, m, p);
            CargoStack(room, m, new Vector3(10.5f, 0f, -7.5f));
            CargoStack(room, m, new Vector3(-11.5f, 0f, 7f));
            RopeCoil(room, m, new Vector3(-1.5f, 0f, -1f));
            GripWall("CraneLadder", room, new Vector3(12.85f, 0f, 5.5f), new Vector3(2.5f, 4.2f, 0.3f), -90f, new Color(1f, 0.75f, 0.25f));
            Bolt("dock_crane", room, new Vector3(12.3f, 4.6f, 5.5f));

            Hum(room, new Vector3(0f, 3f, 0f), 26f, 30f, 0.2f);

            var dispatch = Script("Dispatch_Ch5_Call01",
                Line("intro", "PIP, welcome to the harbour! Ines says every hum rattles more cargo loose, and now the supply barge is listing. It needs ballast. There's a Magnet on the stand by the gate: those metal crates are far too heavy to carry.", 10f),
                Line("got_magnet", "The Magnet! Hold F (or B) next to anything metal and it floats along in front of you. Let go to drop it. Tab swaps tools.", 7f),
                Line("ballast_light", "That plate wants something heavy. A metal crate, say.", 3f, true),
                Line("barge_level", "Barge is level! Ines is giving you a very official salute.", 4f),
                Line("mast_light_on", "Mast light's on! Ships can see the barge again.", 4f),
                Line("hum_tremor_first", "There's the hum. Watch those crates!", 3f),
                Line("hum_tremor", "Hum!", 2f, true),
                Line("fell", "Splash! Harbour water is very good for the circuits. That's a lie. Out you come.", 4f, true),
                Line("exit", "Harbour sorted! Ines says the old Bureau cable runs from here straight out toward the Old Rig. Interesting.", 6f));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(-11f, 0.05f, -4f), 90f, tools.grabber, tools.whisk, tools.screwdriver);
            BuildSystems(task, dispatch, bot);
            FinishScene(scene, DocksScene, bot);
        }

        // ================================================================== Big Rescue: Lights Out

        [MenuItem("Robot Rescue/Chapter 5/Big Rescue: Lights Out")]
        public static void BuildLightsOut()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var tools = ToolKit();
            var m = Mats();

            var task = Task("Ch5_Boss_LightsOut", "ch5_boss", "BIG RESCUE: Lights Out", "Captain Mo (lost in the fog)",
                "The lighthouse is dark and Captain Mo's boat is lost. Be the beacon that brings her home.", 110,
                ("generator", "Magnet the fuel tank onto the generator plate"),
                ("lens", "Climb the lighthouse and fix the lamp's lens (3 screws)"),
                ("boat_home", "Plug in at the top and guide Captain Mo home"));

            // Land x -13..13, z -11..3; harbour basin z 3..11 with a pier; the lighthouse at (6, -3).
            var room = new GameObject("Lighthouse").transform;
            RoomWithExit(room, 26f, 22f, 4.5f, m.boards, m.stone, m.rope, "BRIGHTWATER LIGHTHOUSE", TitleColor);
            Object.DestroyImmediate(room.Find("Floor").gameObject);
            Box("Floor", room, new Vector3(0f, -0.25f, -4f), new Vector3(26f, 0.5f, 14f), m.boards);
            Basin(room, m, new Vector3(0f, 0f, 7f), new Vector3(26f, 0f, 8f));
            Box("Pier", room, new Vector3(-6f, -0.25f, 6f), new Vector3(2.5f, 0.5f, 6f), m.wood);
            for (float z = 3.8f; z < 9f; z += 1.6f)
                foreach (float x in new[] { -7.1f, -4.9f })
                    Cylinder("PierPost", room, new Vector3(x, -1.5f, z), 0.15f, 3f, m.wood);

            // The lighthouse: a ladder up the south face to the top, a deck around the lamp room.
            Vector3 L = new Vector3(6f, 0f, -3f);
            Box("Tower", room, L + Vector3.up * 6f, new Vector3(3.2f, 12f, 3.2f), null);
            for (int i = 0; i < 6; i++)
                Cylinder("TowerBand" + i, room, L + Vector3.up * (1f + i * 2f), 2f - i * 0.06f, 2f, i % 2 == 0 ? m.white : m.red);
            var ladder = GripWall("TowerLadder", room, L + new Vector3(0f, 0f, -1.75f), new Vector3(1.6f, 12f, 0.3f), 180f, new Color(0.95f, 0.95f, 1f), 0.5f);
            Cylinder("Door", room, L + new Vector3(1.3f, 1.1f, -1.62f), 0.5f, 0.05f, Mat("Door_Blue", new Color(0.3f, 0.45f, 0.75f))).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            const float top = 12f;
            Box("TopDeck_N", room, L + new Vector3(0f, top - 0.15f, 2.3f), new Vector3(6f, 0.3f, 1.4f), m.white);
            Box("TopDeck_E", room, L + new Vector3(2.3f, top - 0.15f, -0.7f), new Vector3(1.4f, 0.3f, 4.6f), m.white);
            Box("TopDeck_W", room, L + new Vector3(-2.3f, top - 0.15f, -0.7f), new Vector3(1.4f, 0.3f, 4.6f), m.white);
            Box("TopRail_N", room, L + new Vector3(0f, top + 0.35f, 2.95f), new Vector3(6f, 0.7f, 0.1f), m.red);
            Box("TopRail_E", room, L + new Vector3(2.95f, top + 0.35f, 0f), new Vector3(0.1f, 0.7f, 6f), m.red);
            Box("TopRail_W", room, L + new Vector3(-2.95f, top + 0.35f, 0f), new Vector3(0.1f, 0.7f, 6f), m.red);
            Box("LampRoom", room, L + Vector3.up * (top + 1f), new Vector3(1.6f, 2f, 1.6f), null);
            Cylinder("LampGlass", room, L + Vector3.up * (top + 1f), 1.05f, 2f, Mat("Lamp_Glass", new Color(0.75f, 0.9f, 1f), 0.95f));
            Cylinder("LampRoof", room, L + Vector3.up * (top + 2.2f), 1.3f, 0.4f, m.red);
            Sphere("RoofKnob", room, L + Vector3.up * (top + 2.55f), 0.2f, m.red);
            var lamp = Sphere("Lamp", room, L + Vector3.up * (top + 1f), 0.5f, Glow("Beacon_Lamp", new Color(1f, 0.9f, 0.55f), 3f));
            var beam = new GameObject("Beam").transform;
            beam.SetParent(room, false);
            beam.position = L + Vector3.up * (top + 1f);
            var beamMat = Glow("Beacon_Beam", new Color(1f, 0.95f, 0.7f), 1.2f);
            foreach (float side in new[] { -1f, 1f })
                Box("Ray", beam, beam.position + Vector3.right * side * 7f, new Vector3(13f, 0.5f, 0.5f), beamMat, false);
            Bolt("lighthouse_top", room, L + new Vector3(2.3f, top + 0.6f, 2.3f));

            var beacon = room.gameObject.AddComponent<BeaconRescue>();
            beacon.beam = beam;
            beacon.lamp = lamp.GetComponent<Renderer>();
            var beaconGate = Gate("Beacon_AND", room, PowerCombiner.Mode.All, 3, beacon.gameObject);

            // Lens: three screws around the lamp room.
            var lensStep = StepSignal("Step_Lens", room, "lens", "lens_fixed");
            var lensGate = Gate("Lens_AND", room, PowerCombiner.Mode.All, 3, beaconGate.gameObject, lensStep.gameObject);
            Chapter3Builder.Screw(room, L + new Vector3(0.82f, top + 0.6f, 0f), Vector3.right, lensGate.gameObject);
            Chapter3Builder.Screw(room, L + new Vector3(-0.82f, top + 0.6f, 0f), Vector3.left, lensGate.gameObject);
            Chapter3Builder.Screw(room, L + new Vector3(0f, top + 0.6f, 0.82f), Vector3.forward, lensGate.gameObject);

            // The socket PIP plugs into to be the lighthouse.
            Socket("BeaconSocket", room, L + new Vector3(-2.3f, top, 1.6f), 90f, beaconGate.gameObject);
            Label("PLUG IN HERE", room, L + new Vector3(-2.3f, top + 1.5f, 1.6f), 90f, 0.035f, TitleColor);

            // Generator at the foot of the tower, fuelled by a tank too heavy to carry.
            var genStep = StepSignal("Step_Generator", room, "generator", "generator_on");
            var wire = Wire("Wire_Generator", room, new Color(1f, 0.6f, 0.15f), new Vector3(1f, 0f, -6f), new Vector3(4.2f, 0f, -6f), new Vector3(4.2f, 0f, -4.6f));
            var plate = Plate("GeneratorPlate", room, new Vector3(1f, 0f, -6f), 3.5f, 1.4f, beaconGate.gameObject, wire.gameObject, genStep.gameObject);
            plate.tooLightCue = "ballast_light";
            Box("Generator", room, new Vector3(1f, 0.6f, -7.6f), new Vector3(1.8f, 1.2f, 1f), Mat("Generator_Yellow", new Color(1f, 0.8f, 0.25f)));
            Label("GENERATOR: FUEL TANK ON THE PLATE", room, new Vector3(1f, 1.5f, -7.05f), 180f, 0.035f, new Color(0.45f, 0.3f, 0.1f));
            var tank = MetalCrate("FuelTank", room, m, new Vector3(-8.5f, 0.5f, -1.5f));
            tank.GetComponent<Renderer>().sharedMaterial = Mat("FuelTank_Red", new Color(0.95f, 0.4f, 0.35f), 0.5f);
            ServiceBay("ServiceBay", room, L + new Vector3(-3.6f, 0f, -5f));

            // Captain Mo's boat, out in the fog, and the path the beacon brings her in on.
            var path = new[]
            {
                Waypoint(room, new Vector3(11.5f, -0.8f, 10f)),
                Waypoint(room, new Vector3(5f, -0.8f, 8.8f)),
                Waypoint(room, new Vector3(-1f, -0.8f, 8.6f)),
                Waypoint(room, new Vector3(-3.6f, -0.8f, 8.2f)),
            };
            beacon.boat = Boat(room, m, path[0].position);
            beacon.boatPath = path;

            Person("Ines", room, new Vector3(-8.2f, 0f, 1.6f), 30f, new Color(0.2f, 0.35f, 0.6f), new Color(1f, 0.85f, 0.3f),
                new Color(0.75f, 0.55f, 0.42f), new Color(0.3f, 0.2f, 0.15f));
            foreach (var p in new[] { new Vector3(-10f, 0f, 2.6f), new Vector3(-2f, 0f, 2.6f), new Vector3(10f, 0f, 2.6f) })
                Bollard(room, m, p);
            CargoStack(room, m, new Vector3(-11f, 0f, -9f));
            RopeCoil(room, m, new Vector3(-4.5f, 0f, -2f));

            Hum(room, new Vector3(0f, 4f, 0f), 22f, 20f, 0.5f);

            var dispatch = Script("Dispatch_Ch5_Boss",
                Line("intro", "BIG RESCUE! Captain Mo's fishing boat is lost in the fog and the lighthouse is dark. Fuel the generator (the tank's metal: Magnet it), fix the lamp's lens up top, then plug in and BE the lighthouse until she's home.", 11f),
                Line("generator_on", "Generator's running! Now the lamp.", 3f),
                Line("lens_fixed", "Lens fixed! Plug into the socket up there and light her up.", 4f),
                Line("beacon_on", "Beacon's lit! She's turning toward us!", 3f, true),
                Line("beacon_off", "Beacon's dark! She's drifting! Plug back in!", 3f, true),
                Line("beacon_morse", "Wait, the beacon's flickering on its own... dash dash dot dot. Dot. Dot dash dot. Dash dash dash. Z. E. R. O. ...Zero?", 8f),
                Line("boat_home", "She's in! Captain Mo's tied up at the pier and waving at the lighthouse. That's you. She's waving at you.", 6f),
                Line("ballast_light", "That plate wants something heavy. The fuel tank, say.", 3f, true),
                Line("hum_tremor_first", "Hum! Hold that ladder!", 3f),
                Line("hum_tremor", "Another hum. Hang on.", 2.5f, true),
                Line("fell", "Ooh. Long way down. Back to the bottom.", 3f, true),
                Line("exit", "Captain Mo's home safe. And PIP... the lighthouse spelled ZERO. Unit Zero, the Bureau's very first bot. He's under the Old Rig, and he's calling for help.", 8f));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(-11.5f, 0.05f, -7f), 90f, tools.grabber, tools.whisk, tools.screwdriver, tools.magnet);
            BuildSystems(task, dispatch, bot);
            FinishScene(scene, LighthouseScene, bot);
        }

        // ================================================================== pieces

        /// <summary>A harbour basin (a pit with water you can fall into) with its floor <paramref name="footprint"/> wide.</summary>
        static void Basin(Transform parent, DockMats m, Vector3 center, Vector3 footprint)
        {
            Box("BasinBed", parent, new Vector3(center.x, -3.25f, center.z), new Vector3(footprint.x, 0.5f, footprint.z), Mat("Harbour_Sand", new Color(0.9f, 0.82f, 0.62f)));
            Box("PondWater", parent, new Vector3(center.x, -0.9f, center.z), new Vector3(footprint.x, 0.1f, footprint.z), m.water, false);
            Box("QuayFace", parent, new Vector3(center.x, -1.6f, center.z - footprint.z * 0.5f + 0.15f), new Vector3(footprint.x, 3f, 0.3f), m.stone);
            if (footprint.x < 25f)
                foreach (float s in new[] { -1f, 1f })
                    Box("QuayFace", parent, new Vector3(center.x + s * (footprint.x * 0.5f - 0.15f), -1.6f, center.z), new Vector3(0.3f, 3f, footprint.z), m.stone);
            var kill = Box("KillZone", parent, new Vector3(center.x, -2.2f, center.z), new Vector3(footprint.x, 1f, footprint.z), null);
            kill.GetComponent<Collider>().isTrigger = true;
            kill.AddComponent<KillZone>();
        }

        static GameObject MetalCrate(string name, Transform parent, DockMats m, Vector3 pos)
        {
            var crate = Box(name, parent, pos, Vector3.one, m.crate);
            crate.GetComponent<Collider>().sharedMaterial = PhysMat("Crate", 0.45f, 0.45f, PhysicsMaterialCombine.Average);
            crate.AddComponent<Rigidbody>().mass = 4f;
            crate.AddComponent<MagnetBlock>();
            crate.AddComponent<Pushable>();
            crate.AddComponent<Respawnable>();
            var stripe = Mat("Crate_Stripe", new Color(1f, 0.82f, 0.3f));
            foreach (float y in new[] { -0.3f, 0.3f })
                Box("Band", crate.transform, pos + Vector3.up * y, new Vector3(1.04f, 0.1f, 1.04f), stripe, false);
            return crate;
        }

        static Hinge HingePivot(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<Rigidbody>().isKinematic = true;
            var h = go.AddComponent<Hinge>();
            h.poweredEuler = Vector3.zero;
            h.bounce = 2f;
            return h;
        }

        static Transform Waypoint(Transform parent, Vector3 pos)
        {
            var t = new GameObject("BoatWaypoint").transform;
            t.SetParent(parent, false);
            t.position = pos;
            return t;
        }

        static Transform Boat(Transform parent, DockMats m, Vector3 pos)
        {
            var root = new GameObject("CaptainMosBoat").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(pos, Quaternion.Euler(0f, -100f, 0f));
            Vector3 P(float x, float y, float z) => root.TransformPoint(new Vector3(x, y, z));
            GameObject B(string n, Vector3 p, Vector3 size, Material mat)
            {
                var go = Box(n, root, p, size, mat, false);
                go.transform.rotation = root.rotation;
                return go;
            }
            B("Hull", P(0f, 0.3f, 0f), new Vector3(1.8f, 0.8f, 4f), Mat("Boat_Teal", new Color(0.3f, 0.7f, 0.7f)));
            B("Bow", P(0f, 0.35f, 2.1f), new Vector3(1.2f, 0.7f, 0.6f), Mat("Boat_Teal", new Color(0.3f, 0.7f, 0.7f)));
            B("Trim", P(0f, 0.72f, 0f), new Vector3(1.9f, 0.08f, 4.1f), m.white);
            B("Cabin", P(0f, 1.2f, -0.8f), new Vector3(1.3f, 1f, 1.4f), m.white);
            B("CabinRoof", P(0f, 1.75f, -0.8f), new Vector3(1.5f, 0.1f, 1.6f), m.red);
            Cylinder("Mast", root, P(0f, 2.3f, 0.6f), 0.06f, 2.6f, m.wood);
            var flag = B("Flag", P(0.3f, 3.3f, 0.6f), new Vector3(0.5f, 0.3f, 0.02f), Mat("Flag_Green", new Color(0.35f, 0.85f, 0.45f)));
            flag.transform.rotation = root.rotation * Quaternion.Euler(0f, 90f, 0f);
            Person("Captain Mo", root, P(0.2f, 0.7f, 0.8f), -100f, new Color(0.95f, 0.85f, 0.3f), new Color(0.3f, 0.4f, 0.7f),
                new Color(0.55f, 0.38f, 0.3f), new Color(0.9f, 0.9f, 0.92f), 0.8f);
            foreach (var c in root.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
            return root;
        }

        static void Bollard(Transform parent, DockMats m, Vector3 floorPos)
        {
            Cylinder("Bollard", parent, floorPos + Vector3.up * 0.35f, 0.22f, 0.7f, Mat("Bollard_Dark", new Color(0.3f, 0.32f, 0.38f)));
            Sphere("BollardCap", parent, floorPos + Vector3.up * 0.72f, 0.26f, Mat("Bollard_Dark", new Color(0.3f, 0.32f, 0.38f)), new Vector3(1f, 0.5f, 1f));
            Box("BollardCollider", parent, floorPos + Vector3.up * 0.4f, new Vector3(0.4f, 0.8f, 0.4f), null);
        }

        static void CargoStack(Transform parent, DockMats m, Vector3 floorPos)
        {
            Color[] colors = { new Color(1f, 0.55f, 0.3f), new Color(0.4f, 0.7f, 0.95f), new Color(0.55f, 0.85f, 0.5f) };
            Box("Cargo", parent, floorPos + new Vector3(0f, 0.6f, 0f), new Vector3(2f, 1.2f, 1.2f), Mat("Cargo_0", colors[0]));
            Box("Cargo", parent, floorPos + new Vector3(0.2f, 1.8f, 0f), new Vector3(1.4f, 1.2f, 1.1f), Mat("Cargo_1", colors[1]));
            Box("Cargo", parent, floorPos + new Vector3(-0.8f, 0.4f, 1.1f), new Vector3(0.8f, 0.8f, 0.8f), Mat("Cargo_2", colors[2]));
        }

        static void RopeCoil(Transform parent, DockMats m, Vector3 floorPos)
        {
            for (int i = 0; i < 3; i++)
                Cylinder("RopeCoil", parent, floorPos + Vector3.up * (0.06f + i * 0.1f), 0.45f - i * 0.05f, 0.1f, m.rope);
        }

        static HumTremor Hum(Transform parent, Vector3 pos, float interval, float firstDelay, float gripLoss)
        {
            var hum = new GameObject("TheHum");
            hum.transform.SetParent(parent, false);
            hum.transform.position = pos;
            var src = hum.AddComponent<AudioSource>();
            src.spatialBlend = 0f;
            var drone = hum.AddComponent<Babble>();
            drone.voiceHz = 85f;
            drone.radio = true;
            drone.volume = 0.5f;
            drone.syllableGap = 0.22f;
            var tremor = hum.AddComponent<HumTremor>();
            tremor.interval = interval;
            tremor.firstDelay = firstDelay;
            tremor.gripLoss = gripLoss;
            tremor.voice = drone;
            return tremor;
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
