using RobotRescue.Bots;
using RobotRescue.Narrative;
using RobotRescue.Puzzles;
using RobotRescue.Tasks;
using RobotRescue.Tasks.Rescue;
using UnityEditor;
using UnityEngine;
using static RobotRescue.EditorTools.ChamberKit;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// Menu: Robot Rescue > Side Calls > The Pump House.
    /// A three-room rescue puzzle slice (see docs/slices/pump-house/SLICE_PLAN.md):
    ///  1. Charge Room: power transfer. Plug the Tether into the charge station to fill a power cell, then carry the cell
    ///     to the door's cradle, further than the cable reaches.
    ///  2. Gate Room: a held state. Weight in a pan lifts the gate and keeps it up without you.
    ///  3. Pump Hall: both rules, two valid answers. Lower the drawbridge with a full cell in the winch cradle OR with
    ///     enough weight in its pan (the crates alone are short: the cell is heavy too), fetch Pebble, carry Pebble home.
    /// Every room has a red reset pad (puzzle-local reset) and layered hints on H / D-pad down. Everything is solo.
    /// </summary>
    public static class PumpHouseBuilder
    {
        public const string ScenePath = Root + "/Scenes/Side_PumpHouse.unity";
        const string DataDir = Root + "/Data/Side";

        static readonly Color TitleColor = new Color(0.12f, 0.36f, 0.42f);
        static readonly Color PulleyColor = new Color(0.95f, 0.5f, 0.35f);   // gate, pan, bridge and their wires share this
        static readonly Color PowerColor = new Color(0.35f, 0.7f, 1f);        // cell power wires

        struct PumpMats
        {
            public Material floor, wall, wallLow, trim, pipe, brass, crate, water, dark, cream;
        }

        static PumpMats Mats() => new PumpMats
        {
            floor = Mat("Pump_Floor", new Color(0.86f, 0.66f, 0.46f)),          // warm boards
            wall = Mat("Pump_Wall", new Color(0.42f, 0.66f, 0.66f)),            // soft teal
            wallLow = Mat("Pump_Wainscot", new Color(0.98f, 0.9f, 0.76f)),      // cream band
            trim = Mat("Pump_Trim", new Color(0.95f, 0.62f, 0.35f)),
            pipe = Mat("Pump_Pipe", new Color(0.62f, 0.75f, 0.8f), 0.6f),
            brass = Mat("Pump_Brass", new Color(0.95f, 0.75f, 0.35f), 0.7f),
            crate = Mat("Crate_Orange", new Color(1f, 0.62f, 0.25f)),
            water = Mat("Pump_Water", new Color(0.3f, 0.62f, 0.92f), 0.9f, outline: false),
            dark = Mat("Pump_Dark", new Color(0.18f, 0.2f, 0.26f)),
            cream = Mat("Bot_Cream", new Color(1f, 0.98f, 0.92f), 0.5f),
        };

        [MenuItem("Robot Rescue/Side Calls/The Pump House")]
        public static void Build()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var m = Mats();
            WarmInterior();

            var task = Task("Side_PumpHouse", "side_pumphouse", "Side Call: The Pump House", "Pebble, the tide-gauge bot",
                "The Brightwater pump house flooded and its little tide-gauge bot is stranded across the sump. Get the pumps going.", 30,
                ("cell_charged", "Charge a power cell from your Tether"),
                ("door1", "Power the Gate Room door with a full cell"),
                ("gate", "Hold the gate open with the counterweight"),
                ("bridge", "Lower the drawbridge (weight or winch)"),
                ("pebble", "Carry Pebble home to the tide gauge"));

            var root = new GameObject("PumpHouse").transform;
            Shell(root, m);
            ChargeRoom(root, m);
            GateRoom(root, m);
            PumpHall(root, m);

            KillPlane(root, -12f);

            var dispatch = Script("Dispatch_Side_PumpHouse",
                Line("intro", "PIP, the Brightwater pump house flooded and Pebble, the little tide-gauge bot, is stuck across the sump. Three rooms. Take your time: your battery only drains while you move. Stuck? Press H for a hint. Messed up a room? Stand on its red reset pad.", 12f),
                Line("cell_grabbed", "Power cell! It's heavy, it's portable, and it keeps whatever charge you give it.", 4f),
                Line("cell_full", "All four bars! That cell's full.", 3f, true),
                Line("cell_not_full", "That cradle wants a FULL cell. Count the bars: this one isn't full yet.", 4f, true),
                Line("door1_open", "The cradle's powering the door. You moved power further than your cable reaches. That's the whole trick.", 6f),
                Line("gate_partial", "The gate moved! A bit. The needle shows how much weight is in the pan. It wants more.", 5f),
                Line("gate_open", "Gate's up, and it stays up while the weight stays in. You don't have to hold it.", 5f),
                Line("bridge_partial", "The drawbridge is coming down... and stopping halfway. Not enough weight yet.", 5f),
                Line("bridge_open", "Bridge is down! Go get Pebble.", 3f),
                Line("pebble_grabbed", "Pebble's clinging on. Gently, gently. The tide gauge is by the exit.", 4f),
                Line("pebble_home", "Pebble's home! Listen to those pumps. The water's going down already.", 6f),
                Line("room_reset", "Room reset. Everything in here is back where it started. Nothing else changed.", 4f, true),
                Line("hint_available", "Hmm, tricky one. If you want a nudge, press H. Each press tells you a little more.", 5f),
                Line("hint_solved", "You've already solved this room! Head through the open door.", 3f, true),
                Line("fell", "Splash! Sump water is not a spa. Out you come.", 3f, true),
                Line("exit", "Pumps running, floor drying, Pebble ticking happily on the gauge. Lovely work, PIP.", 6f),

                Line("r1_hint_1", "Hint 1 of 4, the goal: open the door to the Gate Room. Its power comes from the cradle next to it, not from a socket.", 7f, true),
                Line("r1_hint_2", "Hint 2 of 4, the system: cradles take power cells. A cell only powers its cradle when all four bars are lit.", 7f, true),
                Line("r1_hint_3", "Hint 3 of 4, how they connect: you're a power supply. Plug your Tether into the charge station and the cell in the station's cradle fills up.", 8f, true),
                Line("r1_hint_4", "Hint 4 of 4, the action: plug in (right mouse / LT) until every bar glows green, unplug, carry the cell (E / X) to the door's cradle and set it down.", 9f, true),
                Line("r2_hint_1", "Hint 1 of 4, the goal: open the gate and keep it open long enough to walk through.", 6f, true),
                Line("r2_hint_2", "Hint 2 of 4, the system: the gate hangs on a pulley. Weight in the pan lifts it, and the needle shows how much is in.", 7f, true),
                Line("r2_hint_3", "Hint 3 of 4, how they connect: anything you're holding weighs nothing, and if you stand in the pan you're not at the gate. It has to stay up without you.", 8f, true),
                Line("r2_hint_4", "Hint 4 of 4, the action: put both crates in the pan. The one on the high shelf comes down with a Tether pull.", 7f, true),
                Line("r3_hint_1", "Hint 1 of 4, the goal: lower the drawbridge, fetch Pebble from the far side, and carry Pebble to the tide gauge by the exit.", 8f, true),
                Line("r3_hint_2", "Hint 2 of 4, the system: the bridge works like the gate, with a pan. It also has a winch, and the winch cradle is a motor when it holds a full cell.", 8f, true),
                Line("r3_hint_3", "Hint 3 of 4, how they connect: there are two answers. A full cell in the winch cradle, or enough weight in the pan. The two crates alone aren't quite enough. What else in here is heavy?", 9f, true),
                Line("r3_hint_4", "Hint 4 of 4, the action: either charge the cell at the station and set it in the winch cradle, or put both crates AND the cell in the pan. Then cross, pick Pebble up, and set Pebble on the gauge.", 10f, true));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(0f, 0.05f, 2.2f), 0f);
            BuildSystems(task, dispatch, bot);
            FinishScene(scene, ScenePath, bot);
        }

        // ================================================================== shell

        // Rooms run north along +Z, all 16 m wide (x -8..8):
        //   Charge Room z 0..14 · Gate Room z 14..28 · Pump Hall z 28..46 (near floor 28..35, sump 35..42, far deck 42..46)
        const float HalfX = 8f;

        static void Shell(Transform root, PumpMats m)
        {
            // Floors (the Gate Room's floor has a pit for the pan; the Pump Hall's has the sump).
            Box("Floor_Charge", root, new Vector3(0f, -0.25f, 7f), new Vector3(16f, 0.5f, 14f), m.floor);
            FloorWithPit(root, m, 14f, 28f, new Vector3(4.5f, 0f, 24.5f), 2.4f);
            FloorWithPit(root, m, 28f, 35f, new Vector3(5f, 0f, 33.4f), 2.4f);
            Box("Floor_FarDeck", root, new Vector3(0f, -0.25f, 44f), new Vector3(16f, 0.5f, 4f), m.floor);

            // Outer walls: a cream wainscot under soft teal, like a lived-in workshop.
            OuterWall(root, m, "Wall_S", new Vector3(0f, 0f, -0.25f), new Vector3(17f, 7f, 0.5f));
            OuterWall(root, m, "Wall_E_Low", new Vector3(HalfX + 0.25f, 0f, 14f), new Vector3(0.5f, 7f, 28f));
            OuterWall(root, m, "Wall_W_Low", new Vector3(-HalfX - 0.25f, 0f, 14f), new Vector3(0.5f, 7f, 28f));
            OuterWall(root, m, "Wall_E_Hall", new Vector3(HalfX + 0.25f, 0f, 37f), new Vector3(0.5f, 10f, 18f));
            // West wall of the hall has the exit doorway at z 29.5..32.5.
            OuterWall(root, m, "Wall_W_Hall_N", new Vector3(-HalfX - 0.25f, 0f, 39.25f), new Vector3(0.5f, 10f, 13.5f));
            OuterWall(root, m, "Wall_W_Hall_S", new Vector3(-HalfX - 0.25f, 0f, 28.75f), new Vector3(0.5f, 10f, 1.5f));
            Box("Wall_W_Hall_Lintel", root, new Vector3(-HalfX - 0.25f, 6.75f, 31f), new Vector3(0.5f, 6.5f, 3f), m.wall);
            OuterWall(root, m, "Wall_N", new Vector3(0f, 0f, 46.25f), new Vector3(17f, 10f, 0.5f));

            Divider(root, m, 14f, 7f);
            Divider(root, m, 28f, 10f);

            // The sump between the near floor and the far deck: walls down to a bottom, water, and a kill zone.
            Box("Sump_NearFace", root, new Vector3(0f, -1.75f, 35.1f), new Vector3(16f, 2.5f, 0.2f), m.dark);
            Box("Sump_FarFace", root, new Vector3(0f, -1.75f, 41.9f), new Vector3(16f, 2.5f, 0.2f), m.dark);
            Box("Sump_Bottom", root, new Vector3(0f, -3.1f, 38.5f), new Vector3(16f, 0.2f, 7f), m.dark);
            Box("Sump_Water", root, new Vector3(0f, -1.3f, 38.5f), new Vector3(16f, 0.1f, 6.6f), m.water, false);
            var kill = Box("Sump_KillZone", root, new Vector3(0f, -1.9f, 38.5f), new Vector3(16f, 1f, 6.8f), null);
            kill.GetComponent<Collider>().isTrigger = true;
            kill.AddComponent<KillZone>();

            // Exit corridor west of the hall.
            Box("Corridor", root, new Vector3(-12f, -0.25f, 31f), new Vector3(7.5f, 0.5f, 4f), m.floor);
            Box("Corridor_WallN", root, new Vector3(-12f, 1.5f, 33.25f), new Vector3(7.5f, 3f, 0.5f), m.wall);
            Box("Corridor_WallS", root, new Vector3(-12f, 1.5f, 28.75f), new Vector3(7.5f, 3f, 0.5f), m.wall);
            Box("Corridor_End", root, new Vector3(-16f, 1.5f, 31f), new Vector3(0.5f, 3f, 4.5f), m.wall);
            ExitFlag(root, new Vector3(-13.5f, 0f, 31f), new Vector3(2f, 2f, 3f));
        }

        static void OuterWall(Transform root, PumpMats m, string name, Vector3 floorCenter, Vector3 size)
        {
            Box(name, root, floorCenter + Vector3.up * size.y * 0.5f, size, m.wall);
            bool alongX = size.x > size.z;
            var band = alongX ? new Vector3(size.x - 0.6f, 1.2f, size.z + 0.06f) : new Vector3(size.x + 0.06f, 1.2f, size.z - 0.6f);
            Box(name + "_Wainscot", root, floorCenter + Vector3.up * 0.6f, band, m.wallLow, false);
        }

        /// <summary>Wall across the rooms at <paramref name="z"/> with a 3 m doorway (3.5 m tall) at x = 0.</summary>
        static void Divider(Transform root, PumpMats m, float z, float height)
        {
            const float door = 3f, doorHeight = 3.5f;
            float side = HalfX - door * 0.5f;
            Box($"Divider{z}_W", root, new Vector3(-(door * 0.5f + side * 0.5f), height * 0.5f, z), new Vector3(side, height, 0.5f), m.wall);
            Box($"Divider{z}_E", root, new Vector3(door * 0.5f + side * 0.5f, height * 0.5f, z), new Vector3(side, height, 0.5f), m.wall);
            Box($"Divider{z}_Lintel", root, new Vector3(0f, (height + doorHeight) * 0.5f, z), new Vector3(door, height - doorHeight, 0.5f), m.wall);
            Box($"Divider{z}_Frame", root, new Vector3(0f, doorHeight + 0.1f, z - 0.3f), new Vector3(door + 0.4f, 0.2f, 0.1f), m.trim, false);
        }

        /// <summary>Floor between <paramref name="z0"/> and <paramref name="z1"/> with a square pit (0.7 m deep) for a weighing pan.</summary>
        static void FloorWithPit(Transform root, PumpMats m, float z0, float z1, Vector3 pit, float size)
        {
            float h = size * 0.5f;
            float zs = pit.z - h, zn = pit.z + h, xw = pit.x - h, xe = pit.x + h;
            Box("Floor_S", root, new Vector3(0f, -0.25f, (z0 + zs) * 0.5f), new Vector3(16f, 0.5f, zs - z0), m.floor);
            if (z1 - zn > 0.01f) Box("Floor_N", root, new Vector3(0f, -0.25f, (zn + z1) * 0.5f), new Vector3(16f, 0.5f, z1 - zn), m.floor);
            Box("Floor_W", root, new Vector3((-HalfX + xw) * 0.5f, -0.25f, pit.z), new Vector3(xw + HalfX, 0.5f, size), m.floor);
            Box("Floor_E", root, new Vector3((xe + HalfX) * 0.5f, -0.25f, pit.z), new Vector3(HalfX - xe, 0.5f, size), m.floor);
            Box("Pit_Bottom", root, new Vector3(pit.x, -0.85f, pit.z), new Vector3(size, 0.3f, size), m.dark);
        }

        static void WarmInterior()
        {
            // Warm workshop light: a lower, golden sun and a peachy ambient, after the big-warm-room studies.
            RenderSettings.ambientSkyColor = new Color(0.8f, 0.78f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.95f, 0.82f, 0.7f);
            RenderSettings.ambientGroundColor = new Color(0.62f, 0.5f, 0.45f);
            if (RenderSettings.sun)
            {
                RenderSettings.sun.color = new Color(1f, 0.88f, 0.7f);
                RenderSettings.sun.transform.rotation = Quaternion.Euler(55f, -20f, 0f);
            }
        }

        static void Lamp(Transform root, Vector3 pos, float range)
        {
            Sphere("LampBulb", root, pos, 0.18f, Glow("Pump_LampGlow", new Color(1f, 0.8f, 0.5f), 2.2f));
            var light = new GameObject("LampLight").AddComponent<Light>();
            light.transform.SetParent(root, false);
            light.transform.position = pos + Vector3.down * 0.3f;
            light.type = LightType.Point;
            light.color = new Color(1f, 0.78f, 0.5f);
            light.range = range;
            light.intensity = 1.6f;
            light.shadows = LightShadows.None;
        }

        // ================================================================== room 1: power transfer

        static Transform ChargeRoom(Transform root, PumpMats m)
        {
            var room = new GameObject("Room1_ChargeRoom").transform;
            room.SetParent(root, false);
            Label("THE PUMP HOUSE", room, new Vector3(0f, 6.2f, 13.7f), 180f, 0.1f, TitleColor);
            Label("1 · CHARGE ROOM", room, new Vector3(0f, 5.4f, 13.7f), 180f, 0.06f, TitleColor);

            // Charge station on the west wall: a socket you plug into, a cradle beside it with an empty cell.
            Station(room, m, new Vector3(-7.3f, 0f, 6f), 90f, new Vector3(-5.6f, 0f, 6f), out _);
            var cell = Cell("PowerCell_A", room, new Vector3(-5.6f, 0.55f, 6f));
            cell.chargedStepId = "cell_charged";

            // The door to the Gate Room only listens to the cradle beside it: 12 m from the socket, out of cable reach.
            var door = SlidingDoor("Door_GateRoom", room, new Vector3(0f, 1.75f, 13.85f), new Vector3(3f, 3.5f, 0.3f), new Vector3(3.3f, 0f, 0f));
            door.latch = true;
            var step = StepSignal("Step_Door1", room, "door1");
            var wire = Wire("Wire_DoorCradle", room, PowerColor, new Vector3(4.6f, 0f, 11.8f), new Vector3(2.2f, 0f, 11.8f), new Vector3(2.2f, 0f, 13.4f));
            var doorCradle = Cradle("DoorCradle", room, m, new Vector3(5.4f, 0f, 11.8f), door.gameObject, wire.gameObject, step.gameObject);
            doorCradle.poweredCue = "door1_open";
            Label("DOOR POWER", room, new Vector3(5.4f, 2.1f, 12.6f), 180f, 0.045f, TitleColor);
            Label("needs a FULL cell", room, new Vector3(5.4f, 1.7f, 12.6f), 180f, 0.03f, TitleColor);

            // A crate to play with (and to prove carrying a cell is no different from carrying anything else).
            var crate = Crate("Crate_R1", room, m, new Vector3(3f, 0.5f, 4.5f));

            Person("Marlow", room, new Vector3(-4.5f, 0f, 11f), 140f, new Color(0.25f, 0.45f, 0.5f), new Color(1f, 0.75f, 0.35f),
                new Color(0.85f, 0.65f, 0.5f), new Color(0.55f, 0.35f, 0.25f));
            Label("Marlow, pump keeper", room, new Vector3(-4.5f, 2.3f, 11f), 140f, 0.025f, new Color(0.3f, 0.25f, 0.2f));

            Pipes(room, m, new Vector3(7.6f, 0f, 1f), 12f, 4.6f);
            Lamp(room, new Vector3(0f, 6.2f, 7f), 14f);

            ResetPad(room, new Vector3(6.4f, 0f, 2.2f), 270f, cell.GetComponent<Respawnable>(), crate.GetComponent<Respawnable>());
            Hints(room, new Vector3(0f, 2f, 7f), new Vector3(16f, 4f, 13.6f), "door1", "r1_hint_1", "r1_hint_2", "r1_hint_3", "r1_hint_4");
            return room;
        }

        // ================================================================== room 2: a held state

        static Transform GateRoom(Transform root, PumpMats m)
        {
            var room = new GameObject("Room2_GateRoom").transform;
            room.SetParent(root, false);
            Label("2 · GATE ROOM", room, new Vector3(0f, 5.4f, 27.7f), 180f, 0.06f, TitleColor);
            CheckpointZone("Checkpoint_GateRoom", room, new Vector3(0f, 1f, 15.5f), new Vector3(3f, 2f, 1.5f));
            ServiceBay("ServiceBay_GateRoom", room, new Vector3(-6f, 0f, 16.5f));

            // The gate hangs in front of the doorway to the Pump Hall; the pan sits in a pit by the east wall.
            var gate = Box("Gate", room, new Vector3(0f, 1.75f, 27.55f), new Vector3(3.2f, 3.5f, 0.3f), Mat("Pump_Gate", PulleyColor));
            gate.AddComponent<Rigidbody>().isKinematic = true;
            for (float y = 0.5f; y < 3.4f; y += 0.7f)
                Box("Slat", gate.transform, new Vector3(0f, y, 27.38f), new Vector3(3.0f, 0.12f, 0.06f), m.dark, false);

            var lift = room.gameObject.AddComponent<CounterweightLift>();
            lift.load = gate.GetComponent<Rigidbody>();
            lift.openOffset = new Vector3(0f, 3.3f, 0f);
            lift.requiredMass = 3f;
            lift.curve = 2f;
            lift.partialCue = "gate_partial";
            lift.openCue = "gate_open";
            Pan(room, m, new Vector3(4.5f, 0f, 24.5f), lift);
            lift.safety = Safety("GateSafety", room, new Vector3(0f, 1.6f, 27.4f), new Vector3(3f, 3.2f, 1f));
            lift.needle = Gauge(room, m, new Vector3(7.74f, 2.2f, 24.5f), Vector3.left, "GATE");
            var wire = Wire("Wire_Pulley", room, PulleyColor, new Vector3(4.5f, 0f, 25.9f), new Vector3(4.5f, 0f, 26.6f), new Vector3(1.8f, 0f, 26.6f), new Vector3(1.8f, 0f, 27.2f));
            lift.poweredObjects = new[] { wire.gameObject, StepSignal("Step_Gate", room, "gate").gameObject };
            Pulley(room, m, new Vector3(0f, 6.2f, 27.2f), new Vector3(4.5f, 6.2f, 24.5f));
            Label("WEIGHT IN THE PAN LIFTS THE GATE", room, new Vector3(4.5f, 3.4f, 27.7f), 180f, 0.035f, TitleColor);

            // Two crates: one on the floor, one on a high shelf (Tether-pull it down).
            var crateA = Crate("Crate_R2_Floor", room, m, new Vector3(-3.5f, 0.5f, 19f));
            Box("HighShelf", room, new Vector3(-7.1f, 3f, 23f), new Vector3(1.8f, 0.2f, 2f), m.trim);
            Box("HighShelf_Brace", room, new Vector3(-7.6f, 2.5f, 23f), new Vector3(0.6f, 0.8f, 0.2f), m.trim, false);
            var crateB = Crate("Crate_R2_Shelf", room, m, new Vector3(-7.1f, 3.6f, 23f));

            Pipes(room, m, new Vector3(-7.6f, 0f, 15f), 5f, 3.8f);
            Lamp(room, new Vector3(0f, 6.2f, 21f), 14f);

            ResetPad(room, new Vector3(6.4f, 0f, 16f), 270f, crateA.GetComponent<Respawnable>(), crateB.GetComponent<Respawnable>());
            Hints(room, new Vector3(0f, 2f, 21f), new Vector3(16f, 4f, 13.4f), "gate", "r2_hint_1", "r2_hint_2", "r2_hint_3", "r2_hint_4");
            return room;
        }

        // ================================================================== room 3: both rules, two answers

        static Transform PumpHall(Transform root, PumpMats m)
        {
            var room = new GameObject("Room3_PumpHall").transform;
            room.SetParent(root, false);
            Label("3 · PUMP HALL", room, new Vector3(0f, 8.6f, 45.95f), 180f, 0.08f, TitleColor);
            CheckpointZone("Checkpoint_PumpHall", room, new Vector3(0f, 1f, 29.5f), new Vector3(3f, 2f, 1.5f));
            ServiceBay("ServiceBay_PumpHall", room, new Vector3(-3.6f, 0f, 29.7f));

            // Second charge station with an empty cell (east wall).
            Station(room, m, new Vector3(7.3f, 0f, 31f), -90f, new Vector3(5.9f, 0f, 31f), out _);
            var cell = Cell("PowerCell_B", room, new Vector3(5.9f, 0.55f, 31f));
            cell.chargedStepId = "cell_charged";

            // The drawbridge: hinged at the far deck's edge, standing up until weight or the winch brings it down.
            var pivot = new GameObject("Drawbridge");
            pivot.transform.SetParent(room, false);
            pivot.transform.position = new Vector3(0f, 0f, 42f);
            pivot.AddComponent<Rigidbody>().isKinematic = true;
            var deckMat = Mat("Pump_Gate", PulleyColor);
            Box("Deck", pivot.transform, new Vector3(0f, -0.15f, 38.3f), new Vector3(3f, 0.3f, 7.4f), m.floor);
            foreach (float side in new[] { -1f, 1f })
                Box("Rail", pivot.transform, new Vector3(side * 1.45f, 0.2f, 38.3f), new Vector3(0.12f, 0.4f, 7.4f), deckMat);
            Box("TipStripe", pivot.transform, new Vector3(0f, 0.01f, 34.9f), new Vector3(3f, 0.03f, 0.4f), deckMat, false);
            pivot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var bridge = room.gameObject.AddComponent<CounterweightLift>();
            bridge.load = pivot.GetComponent<Rigidbody>();
            bridge.openEuler = new Vector3(-90f, 0f, 0f);
            bridge.degreesPerSecond = 30f;
            bridge.requiredMass = 4.5f;
            bridge.curve = 2f;
            bridge.partialCue = "bridge_partial";
            bridge.openCue = "bridge_open";
            Pan(room, m, new Vector3(5f, 0f, 33.4f), bridge);
            bridge.safety = Safety("BridgeSafety", room, new Vector3(0f, 1f, 38.5f), new Vector3(3f, 2f, 7f));
            bridge.needle = Gauge(room, m, new Vector3(7.74f, 2.2f, 33.4f), Vector3.left, "BRIDGE");
            var pulleyWire = Wire("Wire_BridgePulley", room, PulleyColor, new Vector3(5f, 0f, 34.7f), new Vector3(1.8f, 0f, 34.7f));
            bridge.poweredObjects = new[] { pulleyWire.gameObject, StepSignal("Step_Bridge", room, "bridge").gameObject };
            Pulley(room, m, new Vector3(0f, 8.2f, 42.2f), new Vector3(5f, 8.2f, 33.4f));
            Label("BRIDGE PAN", room, new Vector3(5f, 2.6f, 35.05f), 180f, 0.04f, TitleColor);

            // The winch: a cradle that drives the bridge as a motor when it holds a full cell. 11 m from the socket.
            var winchWire = Wire("Wire_Winch", room, PowerColor, new Vector3(-3f, 0f, 33.2f), new Vector3(-2.2f, 0f, 33.2f), new Vector3(-2.2f, 0f, 34.3f));
            Cradle("WinchCradle", room, m, new Vector3(-3.4f, 0f, 33.4f), bridge.gameObject, winchWire.gameObject);
            Box("WinchHousing", room, new Vector3(-2.2f, 0.55f, 34.55f), new Vector3(0.9f, 1.1f, 0.7f), m.brass);
            Cylinder("WinchDrum", room, new Vector3(-2.2f, 1.25f, 34.55f), 0.3f, 0.7f, m.dark).transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            Label("WINCH", room, new Vector3(-3.4f, 2.1f, 34.2f), 180f, 0.045f, TitleColor);
            Label("needs a FULL cell", room, new Vector3(-3.4f, 1.7f, 34.2f), 180f, 0.03f, TitleColor);

            var crateC = Crate("Crate_R3_A", room, m, new Vector3(-1.2f, 0.5f, 31.4f));
            var crateD = Crate("Crate_R3_B", room, m, new Vector3(2f, 0.5f, 32.4f));

            // Pebble, stranded on the far deck. Too frightened to be Tether-yanked across: someone has to walk over.
            var pebble = Drone("Pebble", room, new Vector3(-2.5f, 0.55f, 44.2f), new Color(0.55f, 0.85f, 0.95f));
            var pebbleCarry = pebble.GetComponent<Carryable>();
            pebbleCarry.displayName = "Pebble";
            pebbleCarry.tetherPull = false;
            pebbleCarry.firstPickupCue = "pebble_grabbed";
            if (pebble.TryGetComponent(out DronePersonality pebblePersonality)) pebblePersonality.droneName = "Pebble";
            Label("PEBBLE", room, new Vector3(-2.5f, 1.6f, 45.9f), 180f, 0.04f, TitleColor);

            // What changes when Pebble is home: pumps spin, warm lamps come on, warning beacons go off, the puddles
            // drain away, and the exit opens.
            var pumps = new System.Collections.Generic.List<GameObject>();
            foreach (float x in new[] { -5f, 0f, 5f })
            {
                var wheel = new GameObject("PumpWheel");
                wheel.transform.SetParent(room, false);
                wheel.transform.position = new Vector3(x, 5.5f, 45.7f);
                Cylinder("Rim", wheel.transform, wheel.transform.position, 1.2f, 0.2f, m.brass).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                for (int i = 0; i < 3; i++)
                {
                    var spoke = Box("Spoke", wheel.transform, wheel.transform.position + Vector3.back * 0.12f, new Vector3(2.2f, 0.18f, 0.08f), m.dark, false);
                    spoke.transform.rotation = Quaternion.Euler(0f, 0f, i * 60f);
                }
                var spinner = wheel.AddComponent<Spinner>();
                spinner.axis = Vector3.forward;
                spinner.degreesPerSecond = x == 0f ? -90f : 90f;
                pumps.Add(wheel);
                Box("PumpPipe", room, new Vector3(x, 2.25f, 45.7f), new Vector3(0.5f, 4.5f, 0.5f), m.pipe, false);
            }
            var warm = new System.Collections.Generic.List<GameObject>();
            var warning = new System.Collections.Generic.List<GameObject>();
            foreach (float x in new[] { -6f, 6f })
            {
                warm.Add(Sphere("HallLamp", room, new Vector3(x, 7.5f, 45.6f), 0.35f, Glow("Pump_LampGlow", new Color(1f, 0.8f, 0.5f), 2.2f)));
                warning.Add(Sphere("WarningBeacon", room, new Vector3(x, 3.8f, 45.6f), 0.25f, Glow("Pump_Warning", new Color(1f, 0.3f, 0.25f), 2.5f)));
            }
            var lights = new GameObject("PumpLights").AddComponent<PowerToggle>();
            lights.transform.SetParent(room, false);
            lights.showWhenPowered = warm.ToArray();
            lights.hideWhenPowered = warning.ToArray();
            lights.pulse = 0f;

            var puddles = new GameObject("Puddles");
            puddles.transform.SetParent(room, false);
            puddles.AddComponent<Rigidbody>().isKinematic = true;
            foreach (var p in new[] { new Vector3(-5.5f, 0.02f, 31.5f), new Vector3(1f, 0.02f, 29.6f), new Vector3(3f, 0.02f, 34.2f) })
            {
                var puddle = Cylinder("Puddle", puddles.transform, p, 1.1f, 0.04f, m.water);
                puddle.transform.localScale = new Vector3(2.6f, 0.02f, 1.6f);
            }
            var drain = puddles.AddComponent<Door>();
            drain.openOffset = new Vector3(0f, -0.2f, 0f);
            drain.speed = 0.04f;
            drain.latch = true;

            var exit = SlidingDoor("ExitDoor", room, new Vector3(-8.25f, 1.75f, 31f), new Vector3(0.3f, 3.5f, 3f), new Vector3(0f, 0f, 3.3f));
            exit.openOnTaskComplete = true;
            exit.latch = true;
            Label("EXIT", room, new Vector3(-7.9f, 3.9f, 31f), 90f, 0.07f, TitleColor);

            // Pebble's post: the tide gauge by the exit. Pebble goes home here.
            Box("TideGauge", room, new Vector3(-6f, 0.45f, 33.6f), new Vector3(1.2f, 0.9f, 1.2f), m.cream);
            for (int i = 0; i < 5; i++)
                Box("GaugeMark", room, new Vector3(-6f, 0.15f + i * 0.15f, 32.98f), new Vector3(i % 2 == 0 ? 0.5f : 0.3f, 0.04f, 0.02f), m.dark, false);
            var seat = new GameObject("Seat").transform;
            seat.SetParent(room, false);
            seat.position = new Vector3(-6f, 1.4f, 33.6f);
            seat.rotation = Quaternion.Euler(0f, 180f, 0f);
            var dropZone = Box("TideGauge_Drop", room, new Vector3(-6f, 1.5f, 33.6f), new Vector3(1.8f, 1.6f, 1.8f), null);
            dropZone.GetComponent<Collider>().isTrigger = true;
            var home = dropZone.AddComponent<RescueDropZone>();
            home.accepts = "Pebble";
            home.seat = seat;
            home.stepId = "pebble";
            home.cue = "pebble_home";
            var homePowered = new System.Collections.Generic.List<GameObject>(pumps) { lights.gameObject, puddles, exit.gameObject };
            home.poweredObjects = homePowered.ToArray();
            Label("PEBBLE'S TIDE GAUGE", room, new Vector3(-6f, 2.6f, 32.9f), 180f, 0.035f, TitleColor);

            Person("Marlow", room, new Vector3(-7f, 0f, 29.1f), 45f, new Color(0.25f, 0.45f, 0.5f), new Color(1f, 0.75f, 0.35f),
                new Color(0.85f, 0.65f, 0.5f), new Color(0.55f, 0.35f, 0.25f));

            Pipes(room, m, new Vector3(-7.6f, 0f, 36f), 5.5f, 6f);
            Lamp(room, new Vector3(0f, 8.6f, 31.5f), 16f);

            ResetPad(room, new Vector3(3.2f, 0f, 29.4f), 270f, cell.GetComponent<Respawnable>(), crateC.GetComponent<Respawnable>(),
                crateD.GetComponent<Respawnable>(), pebble.GetComponent<Respawnable>());
            Hints(room, new Vector3(0f, 2.5f, 37f), new Vector3(16f, 5f, 17.6f), "pebble", "r3_hint_1", "r3_hint_2", "r3_hint_3", "r3_hint_4");
            return room;
        }

        // ================================================================== pieces

        /// <summary>
        /// Portable power cell (1.5 kg: light enough to carry, heavy enough to count on a pan). Four charge bars on each
        /// face, a brass cap, and a handle, so its state reads from any side.
        /// </summary>
        static PowerCell Cell(string name, Transform parent, Vector3 pos)
        {
            var body = Box(name, parent, pos, new Vector3(0.5f, 0.75f, 0.5f), Mat("Cell_Body", new Color(0.3f, 0.36f, 0.5f), 0.5f));
            body.GetComponent<Collider>().sharedMaterial = PhysMat("Crate", 0.45f, 0.45f, PhysicsMaterialCombine.Average);
            body.AddComponent<Rigidbody>().mass = 1.5f;
            var cell = body.AddComponent<PowerCell>();
            cell.displayName = "Power cell";
            cell.firstPickupCue = "cell_grabbed";
            body.AddComponent<Respawnable>();
            Box("Cap", body.transform, pos + Vector3.up * 0.42f, new Vector3(0.36f, 0.1f, 0.36f), Mat("Pump_Brass", new Color(0.95f, 0.75f, 0.35f), 0.7f), false);
            Box("Terminal", body.transform, pos + Vector3.up * 0.5f, new Vector3(0.12f, 0.08f, 0.12f), Mat("Pump_Dark", new Color(0.18f, 0.2f, 0.26f)), false);
            // Four levels, a bar on the front and back face for each, listed bottom to top.
            var barMat = Mat("Cell_Bar", new Color(0.22f, 0.24f, 0.3f), 0.5f);
            var bars = new System.Collections.Generic.List<Renderer>();
            for (int i = 0; i < 4; i++)
            {
                float y = -0.24f + i * 0.155f;
                foreach (float side in new[] { 0.26f, -0.26f })
                    bars.Add(Box("Bar" + i, body.transform, pos + new Vector3(0f, y, side), new Vector3(0.32f, 0.1f, 0.03f), barMat, false).GetComponent<Renderer>());
            }
            cell.bars = bars.ToArray();
            cell.levels = 4;
            return cell;
        }

        /// <summary>A cup for one power cell, with a status lamp on a post. Powers <paramref name="powered"/> while it holds a full cell.</summary>
        static CellCradle Cradle(string name, Transform parent, PumpMats m, Vector3 floorPos, params GameObject[] powered)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = floorPos;
            Box("Base", root, floorPos + Vector3.up * 0.06f, new Vector3(1.1f, 0.12f, 1.1f), m.dark);
            foreach (var (offset, size) in new[]
            {
                (new Vector3(0f, 0.25f, 0.5f), new Vector3(1.1f, 0.3f, 0.1f)), (new Vector3(0f, 0.25f, -0.5f), new Vector3(1.1f, 0.3f, 0.1f)),
                (new Vector3(0.5f, 0.25f, 0f), new Vector3(0.1f, 0.3f, 0.9f)), (new Vector3(-0.5f, 0.25f, 0f), new Vector3(0.1f, 0.3f, 0.9f)),
            })
                Box("Lip", root, floorPos + offset, size, m.brass);
            Box("LampPost", root, floorPos + new Vector3(0.62f, 0.6f, 0.62f), new Vector3(0.08f, 1.2f, 0.08f), m.dark, false);
            var lamp = Sphere("Lamp", root, floorPos + new Vector3(0.62f, 1.25f, 0.62f), 0.14f, Glow("Cradle_Lamp", new Color(0.4f, 0.4f, 0.45f), 0.5f));
            var seat = new GameObject("Seat").transform;
            seat.SetParent(root, false);
            seat.position = floorPos + Vector3.up * 0.5f;
            var zone = Box("CradleZone", root, floorPos + Vector3.up * 0.6f, new Vector3(0.85f, 0.9f, 0.85f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var cradle = zone.AddComponent<CellCradle>();
            cradle.poweredObjects = powered;
            cradle.seat = seat;
            cradle.lamp = lamp.GetComponent<Renderer>();
            return cradle;
        }

        /// <summary>
        /// Charge station: a socket on a post at <paramref name="socketPos"/> wired to a charging cradle at
        /// <paramref name="cradlePos"/>. While the bot is plugged in, the cell in the cradle fills.
        /// </summary>
        static PowerSocket Station(Transform parent, PumpMats m, Vector3 socketPos, float yaw, Vector3 cradlePos, out CellCradle cradle)
        {
            cradle = Cradle("ChargeCradle", parent, m, cradlePos);
            var wire = Wire("Wire_Charge", parent, PowerColor, socketPos, cradlePos);
            var socket = Socket("ChargeSocket", parent, socketPos, yaw, cradle.gameObject, wire.gameObject);
            Vector3 toCradle = (cradlePos - socketPos).normalized;
            Label("CHARGE STATION", parent, socketPos + Vector3.up * 2.4f + toCradle * 0.8f, yaw, 0.045f, TitleColor);
            Label("plug in to fill the cell", parent, socketPos + Vector3.up * 2.0f + toCradle * 0.8f, yaw, 0.03f, TitleColor);
            // Exposed workings behind the station: a back panel with coils, so it reads as a machine you can understand.
            Vector3 back = socketPos - toCradle * 0.35f;
            var panel = Box("StationPanel", parent, back + Vector3.up * 1.6f, new Vector3(0.1f, 1.6f, 2.6f), m.cream, false);
            panel.transform.rotation = Quaternion.Euler(0f, yaw - 90f, 0f);
            for (int i = 0; i < 3; i++)
            {
                var coil = Cylinder("Coil", parent, back + Vector3.up * (1.1f + i * 0.45f) + toCradle * 0.08f, 0.16f, 0.9f, m.brass);
                coil.transform.rotation = Quaternion.Euler(0f, yaw, 90f);
            }
            return socket;
        }

        /// <summary>Weighing pan in a pit at <paramref name="pitCenter"/> (top flush with the floor), wired into <paramref name="lift"/>.</summary>
        static void Pan(Transform parent, PumpMats m, Vector3 pitCenter, CounterweightLift lift)
        {
            var pan = new GameObject("Pan");
            pan.transform.SetParent(parent, false);
            pan.transform.position = pitCenter;
            pan.AddComponent<Rigidbody>().isKinematic = true;
            var tray = Mat("Pump_Gate", PulleyColor);
            Box("Tray", pan.transform, pitCenter + Vector3.down * 0.1f, new Vector3(2.3f, 0.2f, 2.3f), tray);
            foreach (var (offset, size) in new[]
            {
                (new Vector3(0f, 0.12f, 1.1f), new Vector3(2.3f, 0.25f, 0.1f)), (new Vector3(0f, 0.12f, -1.1f), new Vector3(2.3f, 0.25f, 0.1f)),
                (new Vector3(1.1f, 0.12f, 0f), new Vector3(0.1f, 0.25f, 2.1f)), (new Vector3(-1.1f, 0.12f, 0f), new Vector3(0.1f, 0.25f, 2.1f)),
            })
                Box("Rim", pan.transform, pitCenter + offset, size, m.brass);
            var zone = Box("Weigh", pan.transform, pitCenter + Vector3.up * 0.7f, new Vector3(2.1f, 1.3f, 2.1f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            lift.pan = zone.AddComponent<WeighZone>();
            lift.panBody = pan.GetComponent<Rigidbody>();
            lift.panDrop = 0.4f;
        }

        static WeighZone Safety(string name, Transform parent, Vector3 center, Vector3 size)
        {
            var go = Box(name, parent, center, size, null);
            go.GetComponent<Collider>().isTrigger = true;
            return go.AddComponent<WeighZone>();
        }

        /// <summary>Wall dial facing <paramref name="facing"/>: a needle sweeps from empty to a green FULL tick as weight goes in.</summary>
        static Transform Gauge(Transform parent, PumpMats m, Vector3 pos, Vector3 facing, string title)
        {
            var dialRot = Quaternion.LookRotation(facing);
            var face = Cylinder("GaugeFace", parent, pos, 0.6f, 0.06f, m.cream);
            face.transform.rotation = dialRot * Quaternion.Euler(90f, 0f, 0f);
            var rim = Cylinder("GaugeRim", parent, pos - facing * 0.02f, 0.66f, 0.05f, m.brass);
            rim.transform.rotation = face.transform.rotation;
            var pivot = new GameObject("Needle").transform;
            pivot.SetParent(parent, false);
            // Needle starts pointing to "empty" (75 degrees left of up) and sweeps 150 degrees clockwise to "full".
            pivot.SetPositionAndRotation(pos + facing * 0.05f, dialRot * Quaternion.Euler(0f, 0f, 75f));
            Box("Hand", pivot, pivot.position + pivot.up * 0.25f, new Vector3(0.06f, 0.5f, 0.03f), m.dark, false).transform.rotation = pivot.rotation;
            var full = Box("FullTick", parent, pos + facing * 0.04f + dialRot * Quaternion.Euler(0f, 0f, -75f) * Vector3.up * 0.5f,
                new Vector3(0.1f, 0.18f, 0.03f), Glow("Gauge_Full", new Color(0.35f, 1f, 0.5f), 1.5f), false);
            full.transform.rotation = dialRot * Quaternion.Euler(0f, 0f, -75f);
            float yaw = Quaternion.LookRotation(facing).eulerAngles.y;
            Label(title + " WEIGHT", parent, pos + Vector3.up * 0.95f + facing * 0.05f, yaw, 0.035f, TitleColor);
            Label("FULL", parent, pos + facing * 0.05f + dialRot * Quaternion.Euler(0f, 0f, -75f) * Vector3.up * 0.8f, yaw, 0.025f, new Color(0.15f, 0.5f, 0.25f));
            return pivot;
        }

        /// <summary>Decorative pulley: a wheel over the load, a wheel over the pan, a beam and a rope down to the pan.</summary>
        static void Pulley(Transform parent, PumpMats m, Vector3 overLoad, Vector3 overPan)
        {
            foreach (var p in new[] { overLoad, overPan })
            {
                var wheel = Cylinder("PulleyWheel", parent, p, 0.4f, 0.15f, m.brass);
                wheel.transform.rotation = Quaternion.LookRotation(overPan - overLoad) * Quaternion.Euler(0f, 0f, 90f) * Quaternion.Euler(90f, 0f, 0f);
            }
            Vector3 d = overPan - overLoad;
            var beam = Box("PulleyBeam", parent, (overLoad + overPan) * 0.5f + Vector3.up * 0.45f, new Vector3(0.15f, 0.15f, d.magnitude), m.dark, false);
            beam.transform.rotation = Quaternion.LookRotation(d);
            var rope = Mat("Pump_Gate", PulleyColor);
            Box("PulleyRope", parent, new Vector3(overPan.x, (overPan.y + 1.4f) * 0.5f, overPan.z), new Vector3(0.06f, overPan.y - 1.4f, 0.06f), rope, false);
            Box("PanBar", parent, new Vector3(overPan.x, 1.4f, overPan.z), new Vector3(1.6f, 0.08f, 0.08f), m.brass, false);
        }

        static void Pipes(Transform parent, PumpMats m, Vector3 floorStart, float lengthZ, float height)
        {
            Box("PipeRun", parent, floorStart + new Vector3(0f, height, lengthZ * 0.5f), new Vector3(0.3f, 0.3f, lengthZ), m.pipe, false);
            for (float z = 0.8f; z < lengthZ; z += 2.4f)
                Box("PipeClamp", parent, floorStart + new Vector3(0f, height, z), new Vector3(0.38f, 0.38f, 0.12f), m.brass, false);
            Box("PipeDown", parent, floorStart + new Vector3(0f, height * 0.5f, 0.2f), new Vector3(0.3f, height, 0.3f), m.pipe, false);
            Cylinder("Valve", parent, floorStart + new Vector3(-0.25f, 1.3f, 0.2f), 0.22f, 0.06f, Mat("Trim_Coral", new Color(1f, 0.55f, 0.5f)))
                .transform.rotation = Quaternion.Euler(0f, 0f, 90f);
        }

        static GameObject Crate(string name, Transform parent, PumpMats m, Vector3 pos)
        {
            var crate = Box(name, parent, pos, Vector3.one * 0.8f, m.crate);
            crate.GetComponent<Collider>().sharedMaterial = PhysMat("Crate", 0.45f, 0.45f, PhysicsMaterialCombine.Average);
            crate.AddComponent<Rigidbody>().mass = 1.5f;
            var c = crate.AddComponent<Carryable>();
            c.displayName = "Crate";
            crate.AddComponent<Respawnable>();
            Box("Band", crate.transform, pos, new Vector3(0.84f, 0.12f, 0.84f), Mat("Crate_Stripe", new Color(1f, 0.82f, 0.3f)), false);
            Label("1.5", crate.transform, pos + new Vector3(0f, 0f, -0.43f), 180f, 0.03f, new Color(0.35f, 0.2f, 0.1f));
            return crate;
        }

        /// <summary>Red floor pad: stand on it for a moment to put this room's objects back.</summary>
        static PuzzleReset ResetPad(Transform parent, Vector3 floorPos, float facing, params Respawnable[] objects)
        {
            var root = new GameObject("ResetPad").transform;
            root.SetParent(parent, false);
            root.position = floorPos;
            Cylinder("Pad", root, floorPos + Vector3.up * 0.04f, 0.75f, 0.08f, Mat("Reset_Red", new Color(1f, 0.42f, 0.38f)));
            // Circular-arrow mark on the pad: shape as well as colour.
            for (int i = 0; i < 6; i++)
            {
                float a = i * 45f * Mathf.Deg2Rad;
                Box("Arrow", root, floorPos + new Vector3(Mathf.Cos(a) * 0.45f, 0.09f, Mathf.Sin(a) * 0.45f), new Vector3(0.12f, 0.02f, 0.25f), Mat("Bot_Cream", new Color(1f, 0.98f, 0.92f), 0.5f), false)
                    .transform.rotation = Quaternion.Euler(0f, -i * 45f, 0f);
            }
            Label("RESET ROOM", root, floorPos + Vector3.up * 1.4f, facing, 0.035f, new Color(0.6f, 0.15f, 0.12f));
            Label("stand here", root, floorPos + Vector3.up * 1.1f, facing, 0.025f, new Color(0.6f, 0.15f, 0.12f));
            var zone = Box("ResetZone", root, floorPos + Vector3.up * 0.7f, new Vector3(1.2f, 1.4f, 1.2f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var reset = zone.AddComponent<PuzzleReset>();
            reset.objects = objects;
            return reset;
        }

        static LayeredHints Hints(Transform parent, Vector3 center, Vector3 size, string solvedStep, params string[] layers)
        {
            var go = Box("Hints", parent, center, size, null);
            go.GetComponent<Collider>().isTrigger = true;
            var h = go.AddComponent<LayeredHints>();
            h.layers = layers;
            h.solvedStep = solvedStep;
            return h;
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
