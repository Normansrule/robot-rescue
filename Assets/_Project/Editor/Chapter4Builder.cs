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
    /// Menu: Robot Rescue > Chapter 4 > ...
    /// Brightwater School's science fair, in the gym.
    ///  - Call 4-1 "Catch the Hum": fix the wobbly stand of Priya's seismograph (two screws), power it with a drone on the
    ///    stage plate, then wait for the next hum so it records the S.O.S. on paper.
    ///  - Big Rescue "Slightly Too Big": Theo's volcano floods the gym with foam, and every hum makes it surge. Screw its
    ///    three valves shut (one up the scaffold) and park a drone on each drain plate until the foam drains away.
    /// </summary>
    public static class Chapter4Builder
    {
        public const string HumScene = Root + "/Scenes/Ch4_Call01_CatchTheHum.unity";
        public const string VolcanoScene = Root + "/Scenes/Ch4_Boss_SlightlyTooBig.unity";
        const string DataDir = Root + "/Data/Ch4";

        static readonly Color TitleColor = new Color(0.25f, 0.3f, 0.6f);
        static readonly Color Steel = new Color(0.78f, 0.8f, 0.85f);
        static readonly Color WireBlue = new Color(0.3f, 0.6f, 1f);

        struct GymMats
        {
            public Material floor, wall, trim, wood, line, table, clay, foam;
        }

        static GymMats Mats() => new GymMats
        {
            floor = Mat("Gym_Floor", new Color(0.95f, 0.8f, 0.58f)),
            wall = Mat("Gym_Wall", new Color(0.72f, 0.84f, 0.98f)),
            trim = Mat("Gym_Trim", new Color(1f, 0.6f, 0.3f)),
            wood = Mat("Wood", new Color(0.8f, 0.6f, 0.42f)),
            line = Mat("Gym_Line", new Color(1f, 1f, 1f)),
            table = Mat("Fair_Table", new Color(0.98f, 0.98f, 1f)),
            clay = Mat("Volcano_Clay", new Color(0.78f, 0.5f, 0.36f)),
            foam = Mat("Volcano_Foam", new Color(0.93f, 0.97f, 1f), 0.8f, outline: false),
        };

        // ================================================================== Call 4-1: Catch the Hum

        [MenuItem("Robot Rescue/Chapter 4/Call 4-1: Catch the Hum")]
        public static void BuildCatchTheHum()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var tools = ToolKit();
            var m = Mats();

            var task = Task("Ch4_Call01_CatchTheHum", "ch4_call01", "Call 4-1: Catch the Hum", "Priya (and her seismograph)",
                "Priya built a real seismograph for the science fair. Help her catch the hum on paper.", 45,
                ("fix_stand", "Tighten the seismograph's stand (2 screws)"),
                ("power_seismo", "Power it: weigh down the plate on the stage"),
                ("record_hum", "Wait for the hum to come through"));

            // Gym x -12..12, z -9..9, stage along the north wall.
            var room = new GameObject("Gym").transform;
            RoomWithExit(room, 24f, 18f, 6f, m.floor, m.wall, m.trim, "BRIGHTWATER SCHOOL SCIENCE FAIR", TitleColor);
            CourtLines(room, m, 24f, 18f);

            Box("Stage", room, new Vector3(0f, 0.6f, 6.5f), new Vector3(16f, 1.2f, 5f), m.wood);
            Box("StageSkirt", room, new Vector3(0f, 0.6f, 3.98f), new Vector3(16f, 1.0f, 0.04f), Mat("Stage_Skirt", new Color(0.55f, 0.35f, 0.75f)), false);
            Ramp(room, m, -5f, 1f, 4f, 1.2f);
            Bunting(room, new Vector3(-8f, 3.6f, 4.1f), new Vector3(8f, 3.6f, 4.1f));

            // Priya's seismograph on the stage.
            var seismo = SeismographProp(room, m, new Vector3(3f, 1.2f, 6.8f));
            var seismoGate = Gate("Seismo_AND", room, PowerCombiner.Mode.All, 2, seismo.gameObject);
            var standStep = StepSignal("Step_Stand", room, "fix_stand", "stand_fixed");
            var standGate = Gate("Stand_AND", room, PowerCombiner.Mode.All, 2, seismoGate.gameObject, standStep.gameObject);
            foreach (float dx in new[] { -0.6f, 0.6f })
                Chapter3Builder.Screw(room, new Vector3(3f + dx, 1.6f, 6.28f), Vector3.back, standGate.gameObject);

            var wire = Wire("Wire_Seismo", room, WireBlue, new Vector3(-1.5f, 1.2f, 6.5f), new Vector3(-1.5f, 1.2f, 7.6f), new Vector3(1.9f, 1.2f, 7.6f));
            var powerStep = StepSignal("Step_Power", room, "power_seismo", "seismo_power");
            Plate("StagePlate", room, new Vector3(-1.5f, 1.2f, 6.5f), 1.5f, 1.4f, seismoGate.gameObject, wire.gameObject, powerStep.gameObject);
            Label("POWER", room, new Vector3(-1.5f, 1.45f, 5.6f), 180f, 0.04f, new Color(0.25f, 0.4f, 0.75f));
            Person("Priya", room, new Vector3(4.8f, 1.2f, 5.6f), 210f, new Color(0.4f, 0.55f, 0.95f), new Color(1f, 1f, 1f),
                new Color(0.62f, 0.42f, 0.32f), new Color(0.15f, 0.1f, 0.12f), 0.75f, glasses: true);

            // The robotics club lends a hand (and two drones).
            Table("RoboticsTable", room, new Vector3(8f, 0f, -3f), new Vector3(2.4f, 0f, 1.2f), 0.9f, m.table);
            Drone("ClubDrone_1", room, new Vector3(8f, 1.45f, -3f), new Color(0.55f, 0.8f, 1f));
            Drone("ClubDrone_2", room, new Vector3(6.3f, 0.5f, -4.8f), new Color(1f, 0.7f, 0.4f));
            Label("ROBOTICS CLUB", room, new Vector3(8f, 2.3f, -2.3f), 180f, 0.05f, TitleColor);

            // Other exhibits.
            VolcanoMini(room, m, new Vector3(-7f, 0f, -3f));
            Person("Theo", room, new Vector3(-8.6f, 0f, -1.4f), 120f, new Color(0.95f, 0.45f, 0.35f), new Color(1f, 0.9f, 0.4f),
                new Color(0.95f, 0.75f, 0.6f), new Color(0.85f, 0.5f, 0.2f), 0.72f);
            SolarSystem(room, m, new Vector3(-1f, 0f, -5f));
            Exhibit(room, m, new Vector3(4.5f, 0f, -6.2f), "POTATO CLOCK", new Color(0.8f, 0.65f, 0.4f));
            Person("Ms. Okafor", room, new Vector3(2.6f, 0f, -7f), 20f, new Color(0.3f, 0.35f, 0.55f), new Color(0.95f, 0.6f, 0.7f),
                new Color(0.42f, 0.28f, 0.22f), new Color(0.12f, 0.08f, 0.08f));

            // Rafters: a climbing wall up the east side with a memory bolt at the top.
            GripWall("ClimbingWall", room, new Vector3(11.85f, 0f, 1.5f), new Vector3(3f, 5f, 0.3f), -90f, new Color(0.55f, 0.8f, 1f));
            Bolt("school_rafters", room, new Vector3(11.3f, 5.5f, 1.5f));

            Hum(room, new Vector3(0f, 3f, 0f), 20f, 25f, 0.25f);

            var dispatch = Script("Dispatch_Ch4_Call01",
                Line("intro", "PIP, welcome to the Brightwater School science fair! Priya built a real seismograph and wants to catch the hum on paper. Fix its wobbly stand, power it up, and wait for the next hum. The robotics club's drones can help.", 10f),
                Line("stand_fixed", "Stand's steady! Now it needs power. That plate on the stage wants a little weight on it.", 5f),
                Line("seismo_power", "Plate's down, power's flowing.", 3f),
                Line("seismo_armed", "Seismograph armed, light's green! Now we wait for the hum. Any second. Any... second.", 6f),
                Line("hum_tremor_first", "Here it comes! Feel that?", 3f),
                Line("hum_tremor", "There's the hum again.", 3f, true),
                Line("hum_recorded", "Got it on paper! Short short short, long long long. Priya's comparing it with the school's other sensors...", 7f),
                Line("drone_grabbed", "The robotics club says be gentle. That one's called 'Professor'. They're all called 'Professor'.", 5f),
                Line("plate_too_light", "Too light! Give that plate a drone.", 3f, true),
                Line("exit", "Priya says the wobbles always arrive from the north-west first. That's where the Old Rig sank. PIP... something's down there.", 7f));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(-10.5f, 0.05f, -4f), 90f, tools.grabber, tools.whisk, tools.screwdriver);
            BuildSystems(task, dispatch, bot);
            FinishScene(scene, HumScene, bot);
        }

        // ================================================================== Big Rescue: Slightly Too Big

        [MenuItem("Robot Rescue/Chapter 4/Big Rescue: Slightly Too Big")]
        public static void BuildSlightlyTooBig()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var tools = ToolKit();
            var m = Mats();

            var task = Task("Ch4_Boss_SlightlyTooBig", "ch4_boss", "BIG RESCUE: Slightly Too Big", "Theo (and the whole gym)",
                "Theo's volcano is slightly too big. The gym is filling with foam, and every hum makes it worse.", 100,
                ("valves", "Shut the volcano's 3 valves (one is up the scaffold)"),
                ("drains", "Open both drains: a drone on each plate"),
                ("foam_cleared", "Drain all the foam"));

            // Gym x -13..13, z -10..10, the volcano at (0, 3).
            var room = new GameObject("Gym").transform;
            RoomWithExit(room, 26f, 20f, 6f, m.floor, m.wall, m.trim, "SCIENCE FAIR: GRAND PRIZE ROUND", TitleColor);
            CourtLines(room, m, 26f, 20f);
            Vector3 V = new Vector3(0f, 0f, 3f);

            // The volcano: four stepped tiers of painted papier-mache, foam bubbling out of the top.
            var volcano = new GameObject("Volcano").transform;
            volcano.SetParent(room, false);
            volcano.position = V;
            var grass = Mat("Volcano_Grass", new Color(0.45f, 0.75f, 0.4f));
            (float half, float h, float y, float r)[] tiers = { (2.3f, 1.5f, 0.75f, 2.45f), (1.7f, 1.5f, 2.25f, 1.85f), (1.2f, 1.5f, 3.75f, 1.35f), (0.8f, 1f, 5f, 0.95f) };
            for (int i = 0; i < tiers.Length; i++)
            {
                var t = tiers[i];
                Box("Tier" + i, volcano, V + Vector3.up * t.y, new Vector3(t.half * 2f, t.h, t.half * 2f), null);
                Cylinder("Cone" + i, volcano, V + Vector3.up * t.y, t.r, t.h, i == 0 ? grass : m.clay);
            }
            var crater = Sphere("Crater", volcano, V + Vector3.up * 5.6f, 0.8f, m.foam, new Vector3(1f, 0.7f, 1f));
            foreach (var (x, y, z, r) in new[] { (0.9f, 4.3f, -0.6f, 0.35f), (-0.7f, 2.9f, -1.6f, 0.4f), (1.6f, 2.4f, 1f, 0.35f), (-2f, 1.5f, 1.2f, 0.45f) })
                Sphere("FoamDrip", volcano, V + new Vector3(x, y, z), r, m.foam, new Vector3(1f, 0.8f, 1f));
            Label("THEO'S VOLCANO\n(slightly too big)", volcano, V + new Vector3(-2.8f, 2.4f, -2.8f), 225f, 0.04f, new Color(0.55f, 0.3f, 0.2f));

            var foam = Box("Foam", room, new Vector3(0f, 0.5f, 0f), new Vector3(26f, 1f, 20f), m.foam, false);
            var flood = volcano.gameObject.AddComponent<FoamFlood>();
            flood.foam = foam.transform;
            flood.crater = crater.transform;
            flood.inputCount = 5;

            // Valves: two on the bottom tier, one high on the scaffold.
            var valvesStep = StepSignal("Step_Valves", room, "valves", "valves_shut");
            var valvesGate = Gate("Valves_AND", room, PowerCombiner.Mode.All, 3, valvesStep.gameObject);
            Chapter3Builder.Screw(room, V + new Vector3(2.3f, 0.8f, 0f), Vector3.right, flood.gameObject, valvesGate.gameObject);
            Chapter3Builder.Screw(room, V + new Vector3(0f, 0.8f, 2.3f), Vector3.forward, flood.gameObject, valvesGate.gameObject);
            GripWall("Scaffold", room, V + new Vector3(0f, 0f, -2.55f), new Vector3(2f, 5.6f, 0.3f), 180f, new Color(1f, 0.6f, 0.3f));
            Chapter3Builder.Screw(room, V + new Vector3(0.65f, 4.9f, -2.71f), Vector3.back, flood.gameObject, valvesGate.gameObject);
            Label("VALVE", room, V + new Vector3(0.65f, 5.3f, -2.75f), 180f, 0.03f, new Color(0.5f, 0.3f, 0.1f));

            // Drains: a plate each, needing a drone's weight.
            var drainsStep = StepSignal("Step_Drains", room, "drains", "drains_open");
            var drainsGate = Gate("Drains_AND", room, PowerCombiner.Mode.All, 2, drainsStep.gameObject);
            foreach (float x in new[] { -8f, 8f })
            {
                Plate("DrainPlate", room, new Vector3(x, 0f, -4f), 1.5f, 1.4f, flood.gameObject, drainsGate.gameObject);
                Box("DrainPost", room, new Vector3(x, 1f, -5.1f), new Vector3(0.1f, 2f, 0.1f), Mat("Steel", Steel, 0.7f), false);
                Label("DRAIN", room, new Vector3(x, 2.15f, -5.1f), 180f, 0.05f, new Color(0.2f, 0.45f, 0.8f));
            }

            // Bleachers along the east wall, a drone on the top row; another on the lab bench; one on the floor.
            Box("Bleacher1", room, new Vector3(10.75f, 0.4f, 4.5f), new Vector3(3.5f, 0.8f, 7f), Mat("Bleacher_Blue", new Color(0.45f, 0.6f, 0.95f)));
            Box("Bleacher2", room, new Vector3(11.35f, 0.8f, 4.5f), new Vector3(2.3f, 1.6f, 7f), Mat("Bleacher_Blue", new Color(0.45f, 0.6f, 0.95f)));
            Box("Bleacher3", room, new Vector3(11.95f, 1.2f, 4.5f), new Vector3(1.1f, 2.4f, 7f), Mat("Bleacher_Blue", new Color(0.45f, 0.6f, 0.95f)));
            Drone("BleacherDrone", room, new Vector3(11.95f, 2.95f, 6f), new Color(0.75f, 0.55f, 1f));
            Bolt("bleachers_top", room, new Vector3(11.95f, 3.2f, 1.6f));
            Table("LabBench", room, new Vector3(-9f, 0f, 6f), new Vector3(3f, 0f, 1.2f), 0.9f, m.table);
            Drone("BenchDrone", room, new Vector3(-9f, 1.45f, 6f), new Color(0.5f, 0.9f, 0.7f));
            Drone("FloorDrone", room, new Vector3(-3f, 0.5f, -6f), new Color(1f, 0.6f, 0.75f));

            Person("Theo", room, new Vector3(-3.8f, 0f, -0.2f), 30f, new Color(0.95f, 0.45f, 0.35f), new Color(1f, 0.9f, 0.4f),
                new Color(0.95f, 0.75f, 0.6f), new Color(0.85f, 0.5f, 0.2f), 0.72f);
            Person("Priya", room, new Vector3(8.4f, 0f, 0.6f), -120f, new Color(0.4f, 0.55f, 0.95f), new Color(1f, 1f, 1f),
                new Color(0.62f, 0.42f, 0.32f), new Color(0.15f, 0.1f, 0.12f), 0.75f, glasses: true);
            Person("Ms. Okafor", room, new Vector3(-6f, 0f, -8.2f), 15f, new Color(0.3f, 0.35f, 0.55f), new Color(0.95f, 0.6f, 0.7f),
                new Color(0.42f, 0.28f, 0.22f), new Color(0.12f, 0.08f, 0.08f));
            SolarSystem(room, m, new Vector3(5f, 0f, -7.5f));
            Exhibit(room, m, new Vector3(-10.5f, 0f, -1f), "BAKING SODA (SPARE)", new Color(1f, 1f, 1f));
            Bunting(room, new Vector3(-12.5f, 4.6f, 9.8f), new Vector3(12.5f, 4.6f, 9.8f));

            Hum(room, new Vector3(0f, 3f, 3f), 24f, 18f, 0.45f);

            var dispatch = Script("Dispatch_Ch4_Boss",
                Line("intro", "BIG RESCUE! Theo's volcano is slightly too big, and every hum sets it off. The gym is filling with foam! Screw its three valves shut (one's up the scaffold) and park a drone on each drain plate. Watch the foam meter.", 11f),
                Line("valves_shut", "All three valves shut! The volcano's just burping now.", 4f),
                Line("drains_open", "Both drains open! Glug glug glug.", 4f),
                Line("foam_surge", "The hum's shaking the volcano! More foam!", 3f, true),
                Line("foam_high", "It's up to my... well, I don't have knees. It's HIGH, PIP!", 4f),
                Line("foam_overflow", "...and the gym is a bubble bath. Theo says 'cool'. Let's try that again.", 5f, true),
                Line("foam_cleared", "Foam's gone! The floor's never been cleaner. Theo wants to do it again. Theo is NOT doing it again.", 6f),
                Line("hum_tremor_first", "Hum incoming! Hold on to that scaffold!", 3f),
                Line("hum_tremor", "Another hum!", 2.5f, true),
                Line("tether_pull", "Yoink! Bring that drone down.", 3f),
                Line("plate_too_light", "That drain plate wants a drone on it.", 3f, true),
                Line("exit", "Science fair saved! And PIP, Priya's seismograph says every hum is a little weaker than the last. Whoever's calling for help is running out of battery. We need to hurry.", 8f));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(-11.5f, 0.05f, -6f), 90f, tools.grabber, tools.whisk, tools.screwdriver);
            BuildSystems(task, dispatch, bot);
            FinishScene(scene, VolcanoScene, bot);
        }

        // ================================================================== pieces

        static Seismograph SeismographProp(Transform parent, GymMats m, Vector3 floorPos)
        {
            var root = new GameObject("Seismograph").transform;
            root.SetParent(parent, false);
            root.position = floorPos;
            Box("Table", root, floorPos + new Vector3(0f, 0.4f, 0f), new Vector3(2.2f, 0.8f, 1f), m.table);
            var paper = Mat("Seismo_Paper", new Color(1f, 0.98f, 0.92f));
            var drum = Cylinder("Drum", root, floorPos + new Vector3(0f, 1.1f, 0f), 0.25f, 1.2f, paper);
            drum.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            for (int i = 0; i < 4; i++)
                Cylinder("Trace", drum.transform, floorPos + new Vector3(-0.45f + i * 0.3f, 1.1f, 0f), 0.255f, 0.02f, Mat("Seismo_Ink", new Color(0.3f, 0.35f, 0.75f)));
            Box("DrumPost_L", root, floorPos + new Vector3(-0.7f, 1f, 0f), new Vector3(0.08f, 0.4f, 0.08f), Mat("Steel", Steel, 0.7f), false);
            Box("DrumPost_R", root, floorPos + new Vector3(0.7f, 1f, 0f), new Vector3(0.08f, 0.4f, 0.08f), Mat("Steel", Steel, 0.7f), false);
            Box("PenGantry", root, floorPos + new Vector3(0f, 1.75f, -0.1f), new Vector3(1.4f, 0.08f, 0.08f), Mat("Steel", Steel, 0.7f), false);
            var pen = new GameObject("Pen").transform;
            pen.SetParent(root, false);
            pen.position = floorPos + new Vector3(0f, 1.7f, -0.25f);
            Box("Arm", pen, floorPos + new Vector3(0f, 1.5f, -0.25f), new Vector3(0.05f, 0.4f, 0.05f), Mat("Steel", Steel, 0.7f), false);
            Sphere("Tip", pen, floorPos + new Vector3(0f, 1.3f, -0.25f), 0.04f, Mat("Seismo_Ink", new Color(0.3f, 0.35f, 0.75f)));
            var light = Sphere("ReadyLight", root, floorPos + new Vector3(-0.9f, 0.95f, -0.45f), 0.09f, Glow("Seismo_Light", new Color(1f, 0.3f, 0.3f), 2f));
            Label("PRIYA'S SEISMOGRAPH", root, floorPos + new Vector3(0f, 2.2f, 0f), 180f, 0.04f, TitleColor);
            var s = root.gameObject.AddComponent<Seismograph>();
            s.drum = drum.transform;
            s.pen = pen;
            s.readyLight = light.GetComponent<Renderer>();
            return s;
        }

        static void Ramp(Transform parent, GymMats m, float x, float zStart, float zEnd, float rise)
        {
            float run = zEnd - zStart;
            float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            var rot = Quaternion.Euler(-angle, 0f, 0f);
            Vector3 topMid = new Vector3(x, rise * 0.5f, (zStart + zEnd) * 0.5f);
            var ramp = Box("Ramp", parent, topMid - rot * Vector3.up * 0.15f, new Vector3(2.4f, 0.3f, Mathf.Sqrt(run * run + rise * rise) + 0.3f), m.wood);
            ramp.transform.rotation = rot;
        }

        static void CourtLines(Transform parent, GymMats m, float sizeX, float sizeZ)
        {
            var root = new GameObject("CourtLines").transform;
            root.SetParent(parent, false);
            Box("Line_Mid", root, new Vector3(0f, 0.005f, 0f), new Vector3(sizeX - 2f, 0.01f, 0.08f), m.line, false);
            Box("Line_W", root, new Vector3(-sizeX * 0.5f + 1f, 0.005f, 0f), new Vector3(0.08f, 0.01f, sizeZ - 2f), m.line, false);
            Box("Line_E", root, new Vector3(sizeX * 0.5f - 1f, 0.005f, 0f), new Vector3(0.08f, 0.01f, sizeZ - 2f), m.line, false);
            Cylinder("CentreCircle", root, new Vector3(0f, 0.006f, 0f), 1.8f, 0.01f, Mat("Gym_Circle", new Color(1f, 0.55f, 0.35f)));
        }

        static void Bunting(Transform parent, Vector3 from, Vector3 to)
        {
            var root = new GameObject("Bunting").transform;
            root.SetParent(parent, false);
            int n = Mathf.Max(2, Mathf.RoundToInt(Vector3.Distance(from, to) / 0.8f));
            Color[] colors = { new Color(1f, 0.6f, 0.75f), new Color(1f, 0.85f, 0.35f), new Color(0.55f, 0.8f, 1f), new Color(0.6f, 0.9f, 0.55f) };
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                var p = Vector3.Lerp(from, to, t) + Vector3.down * Mathf.Sin(t * Mathf.PI * 3f) * 0.15f;
                var c = colors[i % colors.Length];
                var flag = Box("Flag", root, p, new Vector3(0.3f, 0.3f, 0.02f), Mat("Bunting_" + ColorUtility.ToHtmlStringRGB(c), c), false);
                flag.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, (to - from).normalized)) * Quaternion.Euler(0f, 0f, 45f);
            }
        }

        static void VolcanoMini(Transform parent, GymMats m, Vector3 floorPos)
        {
            Table("VolcanoTable", parent, floorPos, new Vector3(1.6f, 0f, 1.2f), 0.9f, m.table);
            Cylinder("MiniCone0", parent, floorPos + Vector3.up * 1.1f, 0.5f, 0.4f, m.clay);
            Cylinder("MiniCone1", parent, floorPos + Vector3.up * 1.45f, 0.32f, 0.3f, m.clay);
            Sphere("MiniFoam", parent, floorPos + Vector3.up * 1.65f, 0.22f, m.foam, new Vector3(1f, 0.7f, 1f));
            Label("THEO'S VOLCANO\n(slightly too big)", parent, floorPos + new Vector3(0f, 2.2f, 0f), 135f, 0.035f, new Color(0.55f, 0.3f, 0.2f));
        }

        static void SolarSystem(Transform parent, GymMats m, Vector3 floorPos)
        {
            Table("SolarTable", parent, floorPos, new Vector3(2f, 0f, 1.1f), 0.9f, m.table);
            Sphere("Sun", parent, floorPos + new Vector3(-0.6f, 1.15f, 0f), 0.22f, Glow("Model_Sun", new Color(1f, 0.8f, 0.3f), 1.2f));
            Color[] planets = { new Color(0.7f, 0.65f, 0.6f), new Color(1f, 0.75f, 0.45f), new Color(0.35f, 0.6f, 1f), new Color(1f, 0.45f, 0.35f) };
            for (int i = 0; i < planets.Length; i++)
                Sphere("Planet", parent, floorPos + new Vector3(-0.2f + i * 0.28f, 1.05f, 0.1f * (i % 2 == 0 ? 1 : -1)), 0.06f + i * 0.015f,
                    Mat("Model_Planet" + i, planets[i]));
            Label("THE SOLAR SYSTEM", parent, floorPos + new Vector3(0f, 1.8f, 0f), 180f, 0.035f, TitleColor);
        }

        static void Exhibit(Transform parent, GymMats m, Vector3 floorPos, string title, Color prop)
        {
            Table("ExhibitTable", parent, floorPos, new Vector3(1.6f, 0f, 1f), 0.9f, m.table);
            for (int i = 0; i < 3; i++)
                Sphere("Prop", parent, floorPos + new Vector3(-0.45f + i * 0.45f, 1.02f, 0f), 0.12f, Mat("Exhibit_" + ColorUtility.ToHtmlStringRGB(prop), prop), new Vector3(1.2f, 0.9f, 1f));
            Label(title, parent, floorPos + new Vector3(0f, 1.7f, 0f), 180f, 0.035f, TitleColor);
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
