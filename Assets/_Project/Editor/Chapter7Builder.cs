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
    /// Menu: Robot Rescue > Chapter 7 > ...
    /// The Old Rig, half sunk at the north-west edge of the Reach, with the Bureau's first outpost inside.
    ///  - Call 7-1 "Going Down": swing across the flooded hall on the old crane hooks, fix the outpost door (two screws
    ///    and a drone on the power plate) and step inside. The gangway drops so you can get back out.
    ///  - Big Rescue "Unit Zero": Zero is slumped by the anchor engine, battery nearly flat, and every hum drains him.
    ///    Climb to the junction box and fix his charging line, plug in to charge him, then carry him across the
    ///    catwalk to the rescue lift.
    /// </summary>
    public static class Chapter7Builder
    {
        public const string DescentScene = Root + "/Scenes/Ch7_Call01_GoingDown.unity";
        public const string ZeroScene = Root + "/Scenes/Ch7_Boss_UnitZero.unity";
        const string DataDir = Root + "/Data/Ch7";

        static readonly Color TitleColor = new Color(0.35f, 0.45f, 0.6f);

        struct RigMats
        {
            public Material deck, rust, wall, water, steel, bureau, glow, dark;
        }

        static RigMats Mats() => new RigMats
        {
            deck = Mat("Rig_Deck", new Color(0.55f, 0.58f, 0.62f)),
            rust = Mat("Rig_Rust", new Color(0.72f, 0.42f, 0.28f)),
            wall = Mat("Rig_Wall", new Color(0.62f, 0.68f, 0.74f)),
            water = Mat("Rig_Water", new Color(0.2f, 0.45f, 0.7f), 0.9f),
            steel = Mat("Steel", new Color(0.78f, 0.8f, 0.85f), 0.7f),
            bureau = Mat("Bureau_Orange", new Color(1f, 0.55f, 0.2f)),
            glow = Glow("Rig_Emergency", new Color(1f, 0.35f, 0.3f), 2f),
            dark = Mat("Rig_Dark", new Color(0.18f, 0.22f, 0.3f)),
        };

        // ================================================================== Call 7-1: Going Down

        [MenuItem("Robot Rescue/Chapter 7/Call 7-1: Going Down")]
        public static void BuildGoingDown()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var tools = ToolKit();
            var m = Mats();

            var task = Task("Ch7_Call01_GoingDown", "ch7_call01", "Call 7-1: Going Down", "the old Bureau outpost",
                "The Old Rig's lower hall is flooded. Get across, get the outpost door open, and find out who's been calling.", 60,
                ("cross", "Swing across the flooded hall on the crane hooks"),
                ("power", "Open the outpost door: tighten both hinges and put a drone on the power plate"),
                ("outpost", "Go inside the Bureau outpost"));

            // Entry deck z -11..-3; flooded hall z -3..4; far deck z 4..11 with the outpost behind a wall at z 7.
            var room = new GameObject("OldRigHall").transform;
            RoomWithExit(room, 26f, 22f, 6f, m.deck, m.wall, m.rust, "THE OLD RIG", TitleColor);
            Object.DestroyImmediate(room.Find("Floor").gameObject);
            Box("Floor", room, new Vector3(0f, -0.25f, -7f), new Vector3(26f, 0.5f, 8f), m.deck);
            Box("Floor", room, new Vector3(0f, -0.25f, 7.5f), new Vector3(26f, 0.5f, 7f), m.deck);
            Flood(room, m, new Vector3(0f, 0f, 0.5f), new Vector3(26f, 0f, 7f));
            for (float x = -10f; x <= 10f; x += 5f)
                Box("BrokenStrut", room, new Vector3(x, -1.2f, 0.5f), new Vector3(0.4f, 1.2f, 0.4f), m.rust).transform.rotation = Quaternion.Euler(0f, 0f, x * 2f);

            // Crane hooks to swing on.
            Box("CraneRail", room, new Vector3(-3f, 5.6f, 0.5f), new Vector3(0.3f, 0.3f, 9f), m.rust, false);
            Hook("CraneHook_A", room, new Vector3(-3f, 4.6f, -1.2f));
            Hook("CraneHook_B", room, new Vector3(-3f, 4.6f, 2.2f));
            Label("SWING!", room, new Vector3(-3f, 2.4f, -3.1f), 180f, 0.05f, new Color(1f, 0.75f, 0.25f));
            StepZone("Zone_Cross", room, new Vector3(0f, 1f, 5.5f), new Vector3(26f, 2f, 3f), "cross", "crossed");

            // The outpost wall and its door.
            Box("OutpostWall_W", room, new Vector3(-7.75f, 3f, 7.25f), new Vector3(10.5f, 6f, 0.5f), m.wall);
            Box("OutpostWall_E", room, new Vector3(7.75f, 3f, 7.25f), new Vector3(10.5f, 6f, 0.5f), m.wall);
            Box("OutpostLintel", room, new Vector3(0f, 4.8f, 7.25f), new Vector3(5f, 2.4f, 0.5f), m.wall);
            Label("ROBOT RESCUE BUREAU\nOUTPOST ZERO", room, new Vector3(0f, 4.7f, 6.95f), 180f, 0.06f, m.bureau.color);
            var door = SlidingDoor("OutpostDoor", room, new Vector3(0f, 1.8f, 7.25f), new Vector3(5f, 3.6f, 0.3f), new Vector3(0f, 3.4f, 0f));
            door.GetComponent<Renderer>().sharedMaterial = m.bureau;

            // Return gangway: stands up until the outpost has power, then drops across the hall.
            var gangway = HingePivot("Gangway", room, new Vector3(6f, 0f, 4f));
            Box("Plank", gangway.transform, new Vector3(6f, -0.15f, 0.5f), new Vector3(2.4f, 0.3f, 7.2f), m.steel);
            Box("Rail_E", gangway.transform, new Vector3(7.15f, 0.3f, 0.5f), new Vector3(0.1f, 0.5f, 7.2f), m.rust);
            Box("Rail_W", gangway.transform, new Vector3(4.85f, 0.3f, 0.5f), new Vector3(0.1f, 0.5f, 7.2f), m.rust);
            gangway.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            gangway.degreesPerSecond = 60f;

            var powerStep = StepSignal("Step_Power", room, "power", "door_open");
            var doorGate = Gate("Door_AND", room, PowerCombiner.Mode.All, 3, door.gameObject, gangway.gameObject, powerStep.gameObject);
            foreach (float x in new[] { -2.7f, 2.7f })
            {
                Box("HingeBlock", room, new Vector3(x, 1.2f, 6.95f), new Vector3(0.35f, 0.9f, 0.1f), m.rust);
                Chapter3Builder.Screw(room, new Vector3(x, 1.2f, 6.89f), Vector3.back, doorGate.gameObject);
            }
            var wire = Wire("Wire_Plate", room, new Color(1f, 0.6f, 0.15f), new Vector3(-8f, 0f, 5.2f), new Vector3(-8f, 0f, 6.7f), new Vector3(-2.7f, 0f, 6.7f));
            Plate("PowerPlate", room, new Vector3(-8f, 0f, 5.2f), 1.5f, 1.4f, doorGate.gameObject, wire.gameObject);
            Label("EMERGENCY POWER: 1 DRONE", room, new Vector3(-8f, 1.4f, 6.9f), 180f, 0.035f, new Color(0.45f, 0.3f, 0.1f));
            Drone("RigDrone", room, new Vector3(9f, 0.5f, 5.2f), new Color(0.6f, 0.75f, 0.9f));

            // Inside the outpost: dusty consoles, an old photo board and a memory bolt.
            Box("OutpostFloorTrim", room, new Vector3(0f, 0.01f, 9.2f), new Vector3(25f, 0.02f, 3.5f), Mat("Bureau_Carpet", new Color(0.45f, 0.5f, 0.7f)), false);
            foreach (float x in new[] { -8f, -4f, 4f, 8f })
            {
                Box("Console", room, new Vector3(x, 0.55f, 10.3f), new Vector3(2f, 1.1f, 0.8f), m.dark);
                Box("ConsoleScreen", room, new Vector3(x, 1.25f, 10.55f), new Vector3(1.4f, 0.7f, 0.08f), m.glow, false).transform.rotation = Quaternion.Euler(-15f, 0f, 0f);
            }
            Box("PhotoBoard", room, new Vector3(0f, 2.2f, 10.85f), new Vector3(3f, 1.6f, 0.08f), Mat("Cork", new Color(0.8f, 0.62f, 0.42f)), false);
            Label("FIRST DAY! UNIT ZERO + THE CREW", room, new Vector3(0f, 3.2f, 10.8f), 180f, 0.03f, new Color(0.3f, 0.2f, 0.15f));
            Bolt("rig_outpost", room, new Vector3(0f, 0.6f, 9.5f));
            StepZone("Zone_Outpost", room, new Vector3(0f, 1.2f, 9.5f), new Vector3(20f, 2.4f, 2.5f), "outpost", "outpost_inside");

            ServiceBay("ServiceBay", room, new Vector3(-10f, 0f, -8f));
            EmergencyLights(room, m, 6f);
            Hum(room, new Vector3(0f, 3f, 0f), 22f, 24f, 0.3f);

            var dispatch = Script("Dispatch_Ch7_Call01",
                Line("intro", "This is it, PIP: the Old Rig. The hall's flooded, but there are crane hooks overhead. Fire the Tether at a hook to swing. The outpost door's on the far side.", 9f),
                Line("crossed", "Made it! Now that door. It looks Bureau-made. Very old Bureau-made.", 4f),
                Line("door_open", "Door's open, and the gangway came down too. That's our way back.", 5f),
                Line("outpost_inside", "An outpost. A real Bureau outpost, under the sea. Look at the photo board: that's Unit Zero on his first day. The hum's coming from below us.", 8f),
                Line("hum_tremor_first", "The hum! It's so loud down here.", 3f),
                Line("hum_tremor", "Hum!", 2f, true),
                Line("fell", "Splash! Back to the entry deck.", 3f, true),
                Line("exit", "There's a hatch to the engine room below the outpost. That's where the S.O.S. is coming from. Get ready.", 6f));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(-11f, 0.05f, -7f), 90f, tools.grabber, tools.whisk, tools.screwdriver, tools.magnet);
            BuildSystems(task, dispatch, bot);
            FinishScene(scene, DescentScene, bot);
        }

        // ================================================================== Big Rescue: Unit Zero

        [MenuItem("Robot Rescue/Chapter 7/Big Rescue: Unit Zero")]
        public static void BuildUnitZero()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var tools = ToolKit();
            var m = Mats();

            var task = Task("Ch7_Boss_UnitZero", "ch7_boss", "BIG RESCUE: Unit Zero", "Unit Zero, the Bureau's first bot",
                "Unit Zero has been minding the anchor engine for years, and his battery is almost gone. Charge him and bring him out.", 150,
                ("find_zero", "Find Unit Zero by the anchor engine"),
                ("line", "Climb to the junction box and fix Zero's charging line (2 screws)"),
                ("charged", "Plug in and charge Zero all the way"),
                ("zero_safe", "Carry Zero across the catwalk to the rescue lift"));

            // Engine room: south deck z -10..-3, a flooded trench z -3..1 with one catwalk, north deck z 1..10.
            var room = new GameObject("EngineRoom").transform;
            RoomWithExit(room, 24f, 20f, 7f, m.deck, m.wall, m.rust, "ANCHOR ENGINE ROOM", TitleColor);
            Object.DestroyImmediate(room.Find("Floor").gameObject);
            Box("Floor", room, new Vector3(0f, -0.25f, -6.5f), new Vector3(24f, 0.5f, 7f), m.deck);
            Box("Floor", room, new Vector3(0f, -0.25f, 5.5f), new Vector3(24f, 0.5f, 9f), m.deck);
            Flood(room, m, new Vector3(0f, 0f, -1f), new Vector3(24f, 0f, 4f));
            Box("Catwalk", room, new Vector3(-4f, -0.1f, -1f), new Vector3(1.4f, 0.2f, 4.2f), m.steel);
            foreach (float x in new[] { -4.65f, -3.35f })
                Box("CatwalkRail", room, new Vector3(x, 0.4f, -1f), new Vector3(0.06f, 0.8f, 4.2f), m.rust, false);

            // The anchor engine, huge and still turning slowly.
            Vector3 E = new Vector3(4f, 0f, 7f);
            var engine = new GameObject("AnchorEngine");
            engine.transform.SetParent(room, false);
            engine.transform.position = E + Vector3.up * 3f;
            var flywheel = Cylinder("Flywheel", engine.transform, E + Vector3.up * 3f, 2.4f, 0.6f, m.rust);
            flywheel.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 6; i++)
                Box("FlywheelSpoke", engine.transform, E + Vector3.up * 3f, new Vector3(4.6f, 0.3f, 0.7f), m.steel, false).transform.rotation = Quaternion.Euler(0f, 0f, i * 30f);
            var spin = engine.AddComponent<Spinner>();
            spin.axis = Vector3.forward;
            spin.degreesPerSecond = 12f;
            spin.startsOn = true;
            Box("EngineBlock", room, E + new Vector3(0f, 1.5f, 1.4f), new Vector3(6f, 3f, 2f), m.dark);
            Box("EngineBase", room, E + new Vector3(0f, 0.3f, 0f), new Vector3(6f, 0.6f, 2f), m.rust);
            Label("ANCHOR ENGINE\nKEEP RUNNING", room, E + new Vector3(0f, 3.6f, 0.35f), 180f, 0.05f, m.bureau.color);

            // Unit Zero, slumped against the engine base.
            var zero = UnitZero(room, E + new Vector3(-3.6f, 0.55f, -1.4f), out var zeroEyes);
            StepZone("Zone_FindZero", room, E + new Vector3(-3.6f, 1f, -2.4f), new Vector3(4f, 2f, 3f), "find_zero", "found_zero");
            var rescue = room.gameObject.AddComponent<ZeroRescue>();
            rescue.zero = zero;
            rescue.eyes = zeroEyes;

            // Charging line: the junction box is up on the pipe tower (west), then PIP plugs in beside Zero.
            var chargeGate = Gate("Charge_AND", room, PowerCombiner.Mode.All, 2, rescue.gameObject);
            var lineStep = StepSignal("Step_Line", room, "line", "line_fixed");
            var lineGate = Gate("Line_AND", room, PowerCombiner.Mode.All, 2, chargeGate.gameObject, lineStep.gameObject);
            Vector3 P = new Vector3(-9f, 0f, 6f);
            Box("PipeTower", room, P + Vector3.up * 2.75f, new Vector3(1.6f, 5.5f, 1.6f), m.steel);
            GripWall("PipeClimb", room, P + new Vector3(0.95f, 0f, 0f), new Vector3(1.6f, 5.5f, 0.3f), 90f, new Color(1f, 0.75f, 0.25f), 0.7f);
            // The top deck spreads west and north so nothing overhangs the climb.
            Box("TowerTop", room, P + new Vector3(-0.4f, 5.35f, 0.4f), new Vector3(2.4f, 0.3f, 2.4f), m.deck);
            Box("JunctionBox", room, P + new Vector3(0f, 6.1f, 1f), new Vector3(1.2f, 1.2f, 0.4f), m.dark);
            foreach (float x in new[] { -0.3f, 0.3f })
                Chapter3Builder.Screw(room, P + new Vector3(x, 6f, 0.79f), Vector3.back, lineGate.gameObject);
            Label("ZERO'S CHARGING LINE", room, P + new Vector3(0f, 6.95f, 0.78f), 180f, 0.035f, m.bureau.color);
            Bolt("rig_engine", room, P + new Vector3(-1f, 5.9f, -0.3f));
            Wire("Wire_Line", room, new Color(1f, 0.6f, 0.15f), P + new Vector3(1.8f, 0f, 1f), new Vector3(-2f, 0f, 7f), E + new Vector3(-2.6f, 0f, -1.4f));
            Socket("ChargeSocket", room, E + new Vector3(-1.8f, 0f, -2.6f), 180f, chargeGate.gameObject);
            Label("PLUG IN TO CHARGE ZERO", room, E + new Vector3(-1.8f, 1.6f, -2.6f), 180f, 0.03f, TitleColor);

            // The rescue lift, back across the catwalk by the entrance.
            Vector3 lift = new Vector3(8f, 0f, -7f);
            Box("LiftCage_E", room, lift + new Vector3(1.4f, 1.5f, 0f), new Vector3(0.1f, 3f, 2.8f), m.bureau, false);
            Box("LiftCage_W", room, lift + new Vector3(-1.4f, 1.5f, 0f), new Vector3(0.1f, 3f, 2.8f), m.bureau, false);
            Box("LiftCage_Back", room, lift + new Vector3(0f, 1.5f, -1.4f), new Vector3(2.8f, 3f, 0.1f), m.bureau, false);
            Box("LiftFloor", room, lift + new Vector3(0f, 0.05f, 0f), new Vector3(2.8f, 0.1f, 2.8f), Mat("Plate_Yellow", new Color(1f, 0.85f, 0.3f)), false);
            Label("RESCUE LIFT", room, lift + new Vector3(0f, 3.3f, 1.3f), 180f, 0.05f, m.bureau.color);
            var seat = new GameObject("LiftSeat").transform;
            seat.SetParent(room, false);
            seat.SetPositionAndRotation(lift + Vector3.up * 0.55f, Quaternion.Euler(0f, 180f, 0f));
            var zone = Box("LiftZone", room, lift + Vector3.up * 1f, new Vector3(2.6f, 2f, 2.6f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var drop = zone.AddComponent<RescueDropZone>();
            drop.accepts = "Unit Zero";
            drop.seat = seat;
            drop.stepId = "zero_safe";
            drop.cue = "zero_safe";

            ServiceBay("ServiceBay", room, new Vector3(-9f, 0f, -8f));
            EmergencyLights(room, m, 7f);
            Hum(room, new Vector3(0f, 4f, 0f), 18f, 15f, 0.6f);

            var dispatch = Script("Dispatch_Ch7_Boss",
                Line("intro", "BIG RESCUE! PIP, the S.O.S. is coming from right here, by the anchor engine. Find him.", 6f),
                Line("found_zero", "It's him. Unit Zero. His battery's almost gone. His charging line's broken: the junction box is up the pipe tower. Fix it, then plug in next to him.", 9f),
                Line("line_fixed", "Line's fixed! Now plug in by Zero and share some power.", 4f),
                Line("zero_charging", "He's charging! Stay plugged in!", 3f, true),
                Line("zero_unplugged", "You unplugged! His battery's draining again!", 3f, true),
                Line("zero_sos", "Every hum is Zero calling for help. It costs him power each time. Hurry!", 4f, true),
                Line("zero_low", "His battery's nearly gone, PIP!", 3f),
                Line("zero_awake", "His eyes are on! Zero's awake! Carry him across the catwalk to the rescue lift. Gently.", 6f),
                Line("zero_flat", "Oh no. His battery ran out.", 4f),
                Line("zero_safe", "Zero's in the lift! He says... 'Hello. Is the coffee machine still broken?' PIP, we did it.", 7f),
                Line("hum_tremor_first", "Hum! Hold on!", 2.5f),
                Line("hum_tremor", "Another hum.", 2f, true),
                Line("fell", "Splash! Careful on that catwalk.", 3f, true),
                Line("exit", "Unit Zero is coming home. But if he leaves, who keeps the anchor engine running? The whole Reach needs it. Back to the Bureau: we need a plan.", 8f));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(-10f, 0.05f, -6f), 90f, tools.grabber, tools.whisk, tools.screwdriver, tools.magnet);
            BuildSystems(task, dispatch, bot);
            FinishScene(scene, ZeroScene, bot);
        }

        // ================================================================== pieces

        /// <summary>A flooded section: water you can fall into between two decks.</summary>
        static void Flood(Transform parent, RigMats m, Vector3 center, Vector3 footprint)
        {
            Box("FloodBed", parent, new Vector3(center.x, -3.25f, center.z), new Vector3(footprint.x, 0.5f, footprint.z), m.rust);
            Box("PondWater", parent, new Vector3(center.x, -0.9f, center.z), new Vector3(footprint.x, 0.1f, footprint.z), m.water, false);
            foreach (float s in new[] { -1f, 1f })
                Box("DeckEdge", parent, new Vector3(center.x, -1.6f, center.z + s * (footprint.z * 0.5f - 0.15f)), new Vector3(footprint.x, 3f, 0.3f), m.wall);
            var kill = Box("KillZone", parent, new Vector3(center.x, -2.2f, center.z), new Vector3(footprint.x, 1f, footprint.z), null);
            kill.GetComponent<Collider>().isTrigger = true;
            kill.AddComponent<KillZone>();
        }

        /// <summary>Red emergency lamps along the side walls.</summary>
        static void EmergencyLights(Transform parent, RigMats m, float height)
        {
            for (float z = -8f; z <= 8f; z += 4f)
                foreach (float x in new[] { -11.7f, 11.7f })
                    Sphere("EmergencyLamp", parent, new Vector3(x, height - 1.2f, z), 0.18f, m.glow);
        }

        /// <summary>Unit Zero: boxier and older than PIP, rusty, with one bent antenna. Too flat to move until charged.</summary>
        internal static ZeroBot UnitZero(Transform parent, Vector3 pos, out Renderer[] eyes)
        {
            var m = Mats();
            var root = new GameObject("UnitZero");
            root.transform.SetParent(parent, false);
            root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, 160f, 0f));
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.7f, 1f, 0.6f);
            root.AddComponent<Rigidbody>().mass = 2f;
            var zero = root.AddComponent<ZeroBot>();
            zero.displayName = "Unit Zero";
            root.AddComponent<Respawnable>();
            Vector3 P(float x, float y, float z) => root.transform.TransformPoint(new Vector3(x, y, z));
            var shell = Mat("Zero_Shell", new Color(0.85f, 0.78f, 0.62f));
            Box("Body", root.transform, P(0f, -0.15f, 0f), new Vector3(0.62f, 0.55f, 0.5f), shell, false).transform.rotation = root.transform.rotation;
            Box("Rust", root.transform, P(0.2f, -0.3f, 0.26f), new Vector3(0.18f, 0.12f, 0.02f), m.rust, false).transform.rotation = root.transform.rotation;
            Box("Head", root.transform, P(0f, 0.3f, 0f), new Vector3(0.5f, 0.36f, 0.42f), shell, false).transform.rotation = root.transform.rotation;
            Box("Screen", root.transform, P(0f, 0.3f, 0.215f), new Vector3(0.38f, 0.24f, 0.02f), Mat("Bot_Screen", new Color(0.08f, 0.1f, 0.2f), 0.8f), false).transform.rotation = root.transform.rotation;
            var eyeMat = Glow("Zero_Eye", new Color(0.45f, 0.9f, 1f), 2f);
            var left = Sphere("Eye", root.transform, P(-0.08f, 0.31f, 0.23f), 0.04f, eyeMat);
            var right = Sphere("Eye", root.transform, P(0.08f, 0.31f, 0.23f), 0.04f, eyeMat);
            eyes = new[] { left.GetComponent<Renderer>(), right.GetComponent<Renderer>() };
            var stalk = Cylinder("Antenna", root.transform, P(0.08f, 0.62f, 0f), 0.015f, 0.28f, m.steel);
            stalk.transform.rotation = root.transform.rotation * Quaternion.Euler(0f, 0f, -25f);
            Sphere("AntennaTip", root.transform, P(0.15f, 0.76f, 0f), 0.05f, m.bureau);
            Label("UNIT 0", root.transform, P(0f, -0.1f, 0.26f), 160f, 0.018f, new Color(0.35f, 0.25f, 0.15f));
            foreach (float s in new[] { -1f, 1f })
                Box("Tread", root.transform, P(s * 0.22f, -0.47f, 0f), new Vector3(0.16f, 0.1f, 0.5f), m.dark, false).transform.rotation = root.transform.rotation;
            return zero;
        }

        static Hinge HingePivot(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.AddComponent<Rigidbody>().isKinematic = true;
            var h = go.AddComponent<Hinge>();
            h.poweredEuler = Vector3.zero;
            h.bounce = 3f;
            return h;
        }

        static HumTremor Hum(Transform parent, Vector3 pos, float interval, float firstDelay, float gripLoss)
        {
            var hum = new GameObject("TheHum");
            hum.transform.SetParent(parent, false);
            hum.transform.position = pos;
            var src = hum.AddComponent<AudioSource>();
            src.spatialBlend = 0f;
            var drone = hum.AddComponent<Babble>();
            drone.voiceHz = 80f;
            drone.radio = true;
            drone.volume = 0.6f;
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
