using RobotRescue.Narrative;
using RobotRescue.Puzzles;
using RobotRescue.Tasks;
using UnityEditor;
using UnityEngine;
using static RobotRescue.EditorTools.ChamberKit;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// Milestone 2 puzzle-toolkit test chambers (the Bureau HQ training wing). Menu: Robot Rescue > Test Chambers.
    ///  01 Stack 'Em Up   - Helper Drones: build stairs, use a drone as a counterweight, Tether-pull one upstairs.
    ///  02 Conveyor Caper - Conveyors, colour-coded wires and an AND gate you hold open with your own power.
    ///  03 Swing Shift    - Chained swings over a pit, then Tether-pull a stranded drone across to a plate.
    /// </summary>
    public static class TestChamberBuilder
    {
        const string SceneDir = Root + "/Scenes/TestChambers";
        const string DataDir = Root + "/Data/TestChambers";

        static readonly Color DroneLilac = new Color(0.72f, 0.62f, 1f);
        static readonly Color DroneMint = new Color(0.5f, 0.9f, 0.7f);
        static readonly Color DronePink = new Color(1f, 0.6f, 0.75f);
        static readonly Color WireBlue = new Color(0.3f, 0.6f, 1f);
        static readonly Color WireOrange = new Color(1f, 0.6f, 0.15f);
        static readonly Color WireGreen = new Color(0.3f, 0.85f, 0.45f);

        [MenuItem("Robot Rescue/Test Chambers/Build All")]
        public static void BuildAll()
        {
            BuildStack();
            BuildConveyor();
            BuildSwing();
        }

        // ================================================================== 01 STACK 'EM UP

        [MenuItem("Robot Rescue/Test Chambers/01 Stack 'Em Up")]
        public static void BuildStack()
        {
            if (!BeginScene(out var scene, SceneDir, DataDir)) return;
            var room = new GameObject("Chamber").transform;
            var (floor, wall, accent) = TrainingWingMats();

            // Room x -6..6, z -8..8. Upper floor (y = 3) covers z 3..8.
            Box("Floor", room, new Vector3(0, -0.25f, 0), new Vector3(12, 0.5f, 16), floor);
            Walls(room, wall, 12, 16, 7);
            Box("UpperFloor", room, new Vector3(0, 1.5f, 5.5f), new Vector3(12, 3, 5), wall);
            Box("UpperFloorTrim", room, new Vector3(0, 3.02f, 3.05f), new Vector3(12, 0.04f, 0.1f), accent, false);
            Box("StackHere", room, new Vector3(0, 0.01f, 2f), new Vector3(1.2f, 0.02f, 2.2f), Mat("Hint_Stripe", new Color(1f, 0.85f, 0.3f)), false);
            Label("STACK HERE", room, new Vector3(0, 1.6f, 2.95f), 180f, 0.08f, new Color(0.45f, 0.35f, 0.7f));
            Label("TEST 01: STACK 'EM UP", room, new Vector3(0, 5.5f, -7.7f), 0f, 0.12f, new Color(0.45f, 0.35f, 0.7f));

            // Three drones on dispenser pads.
            var pad = Mat("Dispenser_Pad", new Color(0.55f, 0.75f, 1f));
            float[] xs = { -3.5f, 0f, 3.5f };
            Color[] colors = { DroneLilac, DroneMint, DronePink };
            for (int i = 0; i < 3; i++)
            {
                Cylinder("DispenserPad" + i, room, new Vector3(xs[i], 0.01f, -2.5f), 0.7f, 0.02f, pad);
                Drone("HelperDrone" + (i + 1), room, new Vector3(xs[i], 0.52f, -2.5f), colors[i]);
            }

            // Upstairs: a gate wall at z = 6.5 with a door, opened by a plate PIP is too light for.
            Box("GateWall_L", room, new Vector3(-3.75f, 5f, 6.5f), new Vector3(4.5f, 4f, 0.4f), wall);
            Box("GateWall_R", room, new Vector3(3.75f, 5f, 6.5f), new Vector3(4.5f, 4f, 0.4f), wall);
            Box("GateLintel", room, new Vector3(0, 6.25f, 6.5f), new Vector3(3f, 1.5f, 0.4f), wall);
            var door = SlidingDoor("ExitDoor", room, new Vector3(0, 4.25f, 6.5f), new Vector3(3f, 2.5f, 0.3f), new Vector3(3.2f, 0, 0));
            var wire = Wire("Wire_Plate", room, WireBlue, new Vector3(3, 3, 4.5f), new Vector3(3, 3, 6.1f), new Vector3(1.5f, 3, 6.1f));
            var step = StepSignal("Step_DoorOpen", room, "door_open", "door_open");
            Plate("UpperPlate", room, new Vector3(3, 3, 4.5f), 1.5f, 2.5f, door.gameObject, wire.gameObject, step.gameObject);
            StepZone("Step_Climb", room, new Vector3(0, 4f, 4.5f), new Vector3(12f, 2f, 3f), "climb", "climb");
            ExitFlag(room, new Vector3(0, 3, 7.3f), new Vector3(3f, 2f, 1.2f));

            KillPlane(room, -10f);
            var data = Data("Test01_StackEmUp", "Test 01: Stack 'Em Up", "Bureau Training", new[]
            {
                ("climb", "Stack drones to reach the upper floor"),
                ("door_open", "Weigh down the plate upstairs (PIP's too light!)"),
            }, new[]
            {
                L("intro", "Welcome to the Bureau training wing! These are Helper Drones. Wind-up, very loyal, and they cost more than you, so be nice.", 7f),
                L("drone_grabbed", "Ooh, it likes you. The key spins faster when it's happy."),
                L("climb", "Look at you, up on the big-bot floor! Stairs made of coworkers. Very team-building.", 5f),
                L("plate_too_light", "Too light again. Drones weigh more than you. Don't take it personally.", 5f),
                L("tether_pull", "Pulling a drone up with your Tether. Remember that one, it's on the test. This is the test.", 5f),
                L("door_open", "Door's open! Drones make excellent paperweights."),
                L("exit", "Test one: passed! I'm writing 'competent' on your file. In pencil.", 5f),
            });

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(0, 0.05f, -6f));
            BuildSystems(data.task, data.dispatch, bot);
            FinishScene(scene, SceneDir + "/Test01_StackEmUp.unity", bot);
        }

        // ================================================================== 02 CONVEYOR CAPER

        [MenuItem("Robot Rescue/Test Chambers/02 Conveyor Caper")]
        public static void BuildConveyor()
        {
            if (!BeginScene(out var scene, SceneDir, DataDir)) return;
            var room = new GameObject("Chamber").transform;
            var (floor, wall, accent) = TrainingWingMats();

            // Room x -7..7, z -8..8, exit corridor through the south wall at x = -4.
            Box("Floor", room, new Vector3(0, -0.25f, 0), new Vector3(14, 0.5f, 16), floor);
            Box("Wall_N", room, new Vector3(0, 3.5f, 8.25f), new Vector3(14.5f, 7, 0.5f), wall);
            Box("Wall_E", room, new Vector3(7.25f, 3.5f, 0), new Vector3(0.5f, 7, 16), wall);
            Box("Wall_W", room, new Vector3(-7.25f, 3.5f, 0), new Vector3(0.5f, 7, 16), wall);
            Box("Wall_S_L", room, new Vector3(-6.25f, 3.5f, -8.25f), new Vector3(1.5f, 7, 0.5f), wall);
            Box("Wall_S_R", room, new Vector3(2.375f, 3.5f, -8.25f), new Vector3(9.75f, 7, 0.5f), wall);
            Box("Wall_S_Lintel", room, new Vector3(-4f, 5.25f, -8.25f), new Vector3(3f, 3.5f, 0.5f), wall);
            Label("TEST 02: CONVEYOR CAPER", room, new Vector3(0, 5.5f, 7.95f), 180f, 0.12f, new Color(0.45f, 0.35f, 0.7f));

            // Fence at z = 2: bars you can see through, one invisible blocker, and a low slot for the belt.
            var bar = Mat("Fence_Bar", new Color(0.55f, 0.6f, 0.75f), 0.6f);
            for (float x = -6.7f; x <= 6.7f; x += 0.55f)
                if (x < 3.1f || x > 4.9f)
                    Box("Bar", room, new Vector3(x, 1.75f, 2f), new Vector3(0.12f, 3.5f, 0.12f), bar, false);
            Box("FenceTop", room, new Vector3(0, 3.5f, 2f), new Vector3(14f, 0.15f, 0.2f), bar, false);
            Box("SlotLintel", room, new Vector3(4f, 2.4f, 2f), new Vector3(1.8f, 2.2f, 0.2f), bar);
            Box("FenceBlock_L", room, new Vector3(-1.95f, 3.5f, 2f), new Vector3(10.1f, 7f, 0.2f), null);
            Box("FenceBlock_R", room, new Vector3(5.95f, 3.5f, 2f), new Vector3(2.1f, 7f, 0.2f), null);
            Box("FenceBlock_Top", room, new Vector3(4f, 4.4f, 2f), new Vector3(1.8f, 5.2f, 0.2f), null);

            // Belt along x = 4 from z 7 to z -1, running toward PIP (-Z) when the blue socket is powered.
            var belt = Belt("SupplyBelt", room, new Vector3(4f, 0.3f, 3f), 8f, 1.4f, 180f, 2.5f, false);
            var crate = Box("Crate", room, new Vector3(4f, 0.72f, 6f), new Vector3(0.8f, 0.8f, 0.8f), Mat("Crate_Orange", new Color(1f, 0.62f, 0.25f)));
            crate.GetComponent<Collider>().sharedMaterial = PhysMat("Crate", 0.45f, 0.45f, PhysicsMaterialCombine.Average);
            crate.AddComponent<Rigidbody>().mass = 2.4f;
            crate.AddComponent<Pushable>();
            crate.AddComponent<Respawnable>();

            var beltWire = Wire("Wire_Belt", room, WireBlue, new Vector3(2.2f, 0, -1.5f), new Vector3(3.2f, 0, -1.5f), new Vector3(3.2f, 0, -0.9f));
            var beltStep = StepSignal("Step_BeltOn", room, "belt_on", "belt_on");
            Socket("Socket_Belt", room, new Vector3(2.2f, 0, -1.5f), 90f, belt.gameObject, beltWire.gameObject, beltStep.gameObject);

            // Exit door needs BOTH: the crate on the plate AND PIP plugged into the orange socket by the door.
            var door = SlidingDoor("ExitDoor", room, new Vector3(-4f, 1.75f, -8.25f), new Vector3(3f, 3.5f, 0.3f), new Vector3(-3.3f, 0, 0));
            var doorWire = Wire("Wire_Door", room, WireGreen, new Vector3(0, 0, -6.3f), new Vector3(0, 0, -7.6f), new Vector3(-2.5f, 0, -7.6f));
            var doorStep = StepSignal("Step_DoorOpen", room, "door_open", "door_open");
            var gate = Gate("AND_Door", room, PowerCombiner.Mode.All, 2, door.gameObject, doorWire.gameObject, doorStep.gameObject);
            Box("GateBox", room, new Vector3(0, 0.2f, -6f), new Vector3(0.6f, 0.4f, 0.6f), Mat("Gate_Box", new Color(0.3f, 0.32f, 0.4f)));
            Label("AND", room, new Vector3(0, 0.6f, -6f), 180f, 0.06f, Color.white);

            var plateWire = Wire("Wire_Plate", room, WireOrange, new Vector3(2f, 0, -4f), new Vector3(0, 0, -4f), new Vector3(0, 0, -5.7f));
            var plateStep = StepSignal("Step_Crate", room, "crate_on_plate", "crate_on_plate");
            Plate("CratePlate", room, new Vector3(2f, 0, -4f), 2f, 1.5f, gate.gameObject, plateWire.gameObject, plateStep.gameObject);

            // Close enough to the exit that PIP can walk through the door without unplugging (7 m cable).
            var socketWire = Wire("Wire_DoorSocket", room, WireOrange, new Vector3(-2.2f, 0, -6f), new Vector3(-0.3f, 0, -6f));
            Socket("Socket_Door", room, new Vector3(-2.5f, 0, -6f), 90f, gate.gameObject, socketWire.gameObject);

            // Exit corridor with a treadmill belt running back toward the room: walk against it.
            Box("Corridor", room, new Vector3(-4f, -0.25f, -10.75f), new Vector3(3f, 0.5f, 5.5f), floor);
            Belt("Treadmill", room, new Vector3(-4f, 0.02f, -10.5f), 2.5f, 2.6f, 0f, 2.5f, true);
            ExitFlag(room, new Vector3(-4f, 0, -12.8f), new Vector3(3f, 2f, 1f));
            KillPlane(room, -10f);

            var data = Data("Test02_ConveyorCaper", "Test 02: Conveyor Caper", "Bureau Training", new[]
            {
                ("belt_on", "Power the conveyor"),
                ("crate_on_plate", "Push the crate onto the plate"),
                ("door_open", "Hold the door open with your own power"),
            }, new[]
            {
                L("intro", "Test two: conveyor belts! The crate you need is behind that fence. Belts go under fences. Crates go on belts. You see where I'm going.", 8f),
                L("belt_on", "Belt's running! Let the crate come to you. Patience is a rescue skill.", 5f),
                L("crate_on_plate", "Plate's down. That's half the door happy. The other half wants you.", 5f),
                L("tether_power", "Plugged in. Look at the wires: green means go. Orange means 'PIP, do something'.", 5f),
                L("door_open", "Door's open! You're holding it open, so walk through and stay plugged in.", 5f),
                L("cable_taut", "End of the cable! If you unplug, the door shuts. Physics. And also me.", 5f),
                L("exit", "Test two: passed. Free treadmill workout included.", 5f),
            });

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(-3f, 0.05f, -2f));
            BuildSystems(data.task, data.dispatch, bot);
            FinishScene(scene, SceneDir + "/Test02_ConveyorCaper.unity", bot);
        }

        // ================================================================== 03 SWING SHIFT

        [MenuItem("Robot Rescue/Test Chambers/03 Swing Shift")]
        public static void BuildSwing()
        {
            if (!BeginScene(out var scene, SceneDir, DataDir)) return;
            var room = new GameObject("Chamber").transform;
            var (floor, wall, accent) = TrainingWingMats();

            // Start z -10..-4, pit -4..8 with an island at z 0.5..3.5, far side z 7.5..14.
            Box("StartFloor", room, new Vector3(0, -0.25f, -7f), new Vector3(12, 0.5f, 6), floor);
            Box("Island", room, new Vector3(0, -2.25f, 2f), new Vector3(3, 4.5f, 3), floor);
            Box("FarFloor", room, new Vector3(0, -0.25f, 10.75f), new Vector3(12, 0.5f, 6.5f), floor);
            Box("PitWater", room, new Vector3(0, -4f, 2f), new Vector3(12, 0.1f, 24), Mat("Harbour", new Color(0.35f, 0.65f, 0.95f), 0.9f), false);
            Box("Wall_E", room, new Vector3(6.25f, 4f, 2f), new Vector3(0.5f, 12, 24), wall);
            Box("Wall_W", room, new Vector3(-6.25f, 4f, 2f), new Vector3(0.5f, 12, 24), wall);
            Box("Wall_S", room, new Vector3(0, 4f, -10.25f), new Vector3(13, 12, 0.5f), wall);
            Box("Wall_N", room, new Vector3(0, 4f, 14.25f), new Vector3(13, 12, 0.5f), wall);
            Label("TEST 03: SWING SHIFT", room, new Vector3(0, 6f, -9.95f), 0f, 0.12f, new Color(0.45f, 0.35f, 0.7f));

            // Gantry beams across the pit with a hook under each.
            var post = Mat("Post_Blue", new Color(0.3f, 0.45f, 0.75f));
            foreach (float z in new[] { -1.25f, 6f })
            {
                Box("Beam", room, new Vector3(0, 8f, z), new Vector3(12f, 0.3f, 0.3f), post);
                Hook("SwingHook", room, new Vector3(0, 7f, z));
            }

            Drone("StrandedDrone", room, new Vector3(0, 0.52f, 2.3f), DroneMint);
            CheckpointZone("Checkpoint_Start", room, new Vector3(0, 1f, -7f), new Vector3(12f, 2f, 6f));
            CheckpointZone("Checkpoint_Island", room, new Vector3(0, 1f, 2f), new Vector3(3f, 2f, 3f));
            StepZone("Step_Island", room, new Vector3(0, 1f, 2f), new Vector3(3f, 2f, 3f), "island", "island");
            CheckpointZone("Checkpoint_Far", room, new Vector3(0, 1f, 9.5f), new Vector3(12f, 2f, 3f));
            StepZone("Step_Far", room, new Vector3(0, 1f, 9.5f), new Vector3(12f, 2f, 3f), "far_side", "far_side");

            // Far side: gate wall at z = 12.5 with a door, opened by a plate that needs a drone on it.
            Box("GateWall_L", room, new Vector3(-3.75f, 3.5f, 12.5f), new Vector3(4.5f, 7f, 0.4f), wall);
            Box("GateWall_R", room, new Vector3(3.75f, 3.5f, 12.5f), new Vector3(4.5f, 7f, 0.4f), wall);
            Box("GateLintel", room, new Vector3(0, 5.25f, 12.5f), new Vector3(3f, 3.5f, 0.4f), wall);
            var door = SlidingDoor("ExitDoor", room, new Vector3(0, 1.75f, 12.5f), new Vector3(3f, 3.5f, 0.3f), new Vector3(3.2f, 0, 0));
            var wire = Wire("Wire_Plate", room, WireBlue, new Vector3(-3f, 0, 10f), new Vector3(-3f, 0, 12f), new Vector3(-1.5f, 0, 12f));
            var step = StepSignal("Step_DoorOpen", room, "door_open", "door_open");
            Plate("DronePlate", room, new Vector3(-3f, 0, 10f), 1.5f, 2.5f, door.gameObject, wire.gameObject, step.gameObject);
            Box("PlateArrow", room, new Vector3(-3f, 0.01f, 8.6f), new Vector3(0.3f, 0.02f, 0.8f), accent, false);
            ExitFlag(room, new Vector3(0, 0, 13.4f), new Vector3(3f, 2f, 1.4f));

            KillPlane(room, -8f);
            var data = Data("Test03_SwingShift", "Test 03: Swing Shift", "A stranded Helper Drone", new[]
            {
                ("island", "Swing to the island"),
                ("far_side", "Swing to the far side"),
                ("door_open", "Get the drone onto the plate"),
            }, new[]
            {
                L("intro", "Test three: the Swing Shift. Hooks, a big pit, and a drone stuck on an island. Rescue him. That's literally the job.", 7f),
                L("tether_swing", "Wheee! Sorry. Professional wheee. Q and Z reel the cable in and out.", 5f),
                L("island", "Made it! Say hi to the drone. Then think about how you're both getting across.", 5f),
                L("far_side", "Other side! Now, about our little friend. You can pull things from a distance, remember?", 6f),
                L("tether_pull", "Incoming drone!", 3f),
                L("plate_too_light", "You're not heavy enough for that plate. The drone is. Just saying.", 5f),
                L("door_open", "Plate's down, door's open, drone's rescued. Textbook.", 5f),
                L("fell", "Splash! The pit's full of very soft water. Mostly soft. Try again.", 4f, true),
                L("fell", "Bureau policy: if at first you don't succeed, respawn.", 4f, true),
                L("exit", "Training complete! You're officially a Rescue Bot. Provisionally. Pending paperwork.", 6f),
            });

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(0, 0.05f, -8.5f));
            BuildSystems(data.task, data.dispatch, bot);
            FinishScene(scene, SceneDir + "/Test03_SwingShift.unity", bot);
        }

        // ================================================================== shared

        static (Material floor, Material wall, Material accent) TrainingWingMats() => (
            Mat("TW_Floor", new Color(0.95f, 0.95f, 0.98f)),
            Mat("TW_Wall", new Color(0.82f, 0.78f, 0.98f)),
            Mat("TW_Accent", new Color(0.45f, 0.75f, 1f)));

        /// <summary>Four walls around a room centred on the origin.</summary>
        static void Walls(Transform p, Material m, float sizeX, float sizeZ, float height)
        {
            float hx = sizeX * 0.5f, hz = sizeZ * 0.5f, y = height * 0.5f;
            Box("Wall_N", p, new Vector3(0, y, hz + 0.25f), new Vector3(sizeX + 1, height, 0.5f), m);
            Box("Wall_S", p, new Vector3(0, y, -hz - 0.25f), new Vector3(sizeX + 1, height, 0.5f), m);
            Box("Wall_E", p, new Vector3(hx + 0.25f, y, 0), new Vector3(0.5f, height, sizeZ), m);
            Box("Wall_W", p, new Vector3(-hx - 0.25f, y, 0), new Vector3(0.5f, height, sizeZ), m);
        }

        struct ChamberData
        {
            public TaskDefinition task;
            public DispatchScript dispatch;
        }

        static ChamberData Data(string id, string title, string rescuee, (string id, string label)[] steps, NarrationLine[] lines)
        {
            var task = LoadOrCreate<TaskDefinition>($"{DataDir}/Task_{id}.asset");
            task.taskId = id;
            task.title = title;
            task.rescuee = rescuee;
            task.steps = new TaskStep[steps.Length];
            for (int i = 0; i < steps.Length; i++) task.steps[i] = new TaskStep { id = steps[i].id, label = steps[i].label };

            var dispatch = LoadOrCreate<DispatchScript>($"{DataDir}/Dispatch_{id}.asset");
            dispatch.lines = lines;

            EditorUtility.SetDirty(task);
            EditorUtility.SetDirty(dispatch);
            return new ChamberData { task = task, dispatch = dispatch };
        }

        static NarrationLine L(string cue, string text, float duration = 4f, bool repeatable = false) =>
            new NarrationLine { cue = cue, text = text, duration = duration, repeatable = repeatable };
    }
}
