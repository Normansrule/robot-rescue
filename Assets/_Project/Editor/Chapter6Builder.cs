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
    /// Menu: Robot Rescue > Chapter 6 > ...
    /// Funland Pier, Brightwater's seaside fair.
    ///  - Call 6-1 "Carousel Chaos": the hum shook three carousel animals loose. Climb the ticket booth for the horse,
    ///    Tether-pull the swan off its raft, Magnet the crate off the prize stall for the seal, put each back on its pole,
    ///    then screw the carousel motor tight and set it spinning.
    ///  - Big Rescue "Stuck at the Top": the Ferris wheel froze with Grandpa Sal and the twins in the top gondola.
    ///    Climb the service tower and tighten the hub, Magnet the counterweight onto the brake plate, then plug in at the
    ///    control booth and turn the wheel until they're at the bottom. Every hum jams it. From the tower you can see the
    ///    Old Rig's shadow under the water.
    /// </summary>
    public static class Chapter6Builder
    {
        public const string CarouselScene = Root + "/Scenes/Ch6_Call01_CarouselChaos.unity";
        public const string WheelScene = Root + "/Scenes/Ch6_Boss_StuckAtTheTop.unity";
        const string DataDir = Root + "/Data/Ch6";

        static readonly Color TitleColor = new Color(0.75f, 0.25f, 0.45f);

        struct PierMats
        {
            public Material boards, rail, water, stripeA, stripeB, gold, white, steel, crate;
        }

        static PierMats Mats() => new PierMats
        {
            boards = Mat("Pier_Boards", new Color(0.88f, 0.74f, 0.56f)),
            rail = Mat("Pier_Rail", new Color(0.4f, 0.75f, 0.85f)),
            water = Mat("Harbour_Water", new Color(0.35f, 0.65f, 0.95f), 0.9f),
            stripeA = Mat("Funland_Pink", new Color(1f, 0.55f, 0.7f)),
            stripeB = Mat("Funland_Cream", new Color(1f, 0.95f, 0.85f)),
            gold = Mat("Funland_Gold", new Color(1f, 0.8f, 0.3f), 0.7f),
            white = Mat("Lighthouse_White", new Color(0.98f, 0.97f, 0.95f)),
            steel = Mat("Steel", new Color(0.78f, 0.8f, 0.85f), 0.7f),
            crate = Mat("Metal_Crate", new Color(0.45f, 0.6f, 0.85f), 0.6f),
        };

        // ================================================================== Call 6-1: Carousel Chaos

        [MenuItem("Robot Rescue/Chapter 6/Call 6-1: Carousel Chaos")]
        public static void BuildCarouselChaos()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var tools = ToolKit();
            var m = Mats();

            var task = Task("Ch6_Call01_CarouselChaos", "ch6_call01", "Call 6-1: Carousel Chaos", "Benny the ride operator",
                "The hum shook the carousel animals right off their poles. Put all three back and get it spinning before the gates open.", 50,
                ("animals", "Put the horse, the swan and the seal back on the carousel"),
                ("motor", "Screw the carousel motor tight"));

            // Pier x -13..13, z -10..6; the sea beyond it, z 6..10.
            var room = new GameObject("FunlandPier").transform;
            RoomWithExit(room, 26f, 20f, 4.5f, m.boards, m.rail, m.stripeB, "FUNLAND PIER", TitleColor);
            Object.DestroyImmediate(room.Find("Floor").gameObject);
            Box("Floor", room, new Vector3(0f, -0.25f, -2f), new Vector3(26f, 0.5f, 16f), m.boards);
            Sea(room, m, new Vector3(0f, 0f, 8f), new Vector3(26f, 0f, 4f));

            // The carousel. It only turns once all three animals are home and the motor is tight.
            Vector3 C = new Vector3(0f, 0f, -1f);
            var carousel = new GameObject("Carousel");
            carousel.transform.SetParent(room, false);
            carousel.transform.position = C;
            var body = carousel.AddComponent<Rigidbody>();
            body.isKinematic = true;
            var spinner = carousel.AddComponent<Spinner>();
            spinner.degreesPerSecond = 24f;
            var ct = carousel.transform;
            Cylinder("Deck", ct, C + Vector3.up * 0.15f, 3.2f, 0.3f, m.stripeB);
            Cylinder("DeckTrim", ct, C + Vector3.up * 0.16f, 3.3f, 0.26f, m.stripeA);
            Box("DeckCollider", ct, C + Vector3.up * 0.15f, new Vector3(4.5f, 0.3f, 4.5f), null);
            Cylinder("Column", ct, C + Vector3.up * 1.9f, 0.55f, 3.5f, m.gold);
            Box("ColumnCollider", ct, C + Vector3.up * 1.9f, new Vector3(0.9f, 3.5f, 0.9f), null);
            Cylinder("Canopy", ct, C + Vector3.up * 3.75f, 3.4f, 0.3f, m.stripeA);
            Cylinder("CanopyTop", ct, C + Vector3.up * 4.1f, 2.2f, 0.4f, m.stripeB);
            Sphere("Finial", ct, C + Vector3.up * 4.5f, 0.3f, m.gold);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                Sphere("Bulb", ct, C + new Vector3(Mathf.Cos(a) * 3.35f, 3.6f, Mathf.Sin(a) * 3.35f), 0.1f, Glow("Bulb_Warm", new Color(1f, 0.85f, 0.5f), 2f));
            }

            var animalsStep = StepSignal("Step_Animals", room, "animals", "animals_home");
            var animalsGate = Gate("Animals_AND", room, PowerCombiner.Mode.All, 3, animalsStep.gameObject);
            var spinGate = Gate("Carousel_AND", room, PowerCombiner.Mode.All, 2, spinner.gameObject);
            animalsGate.poweredObjects = new[] { animalsStep.gameObject, spinGate.gameObject };

            string[] names = { "Horse", "Swan", "Seal" };
            for (int i = 0; i < names.Length; i++)
            {
                float a = (90f + i * 120f) * Mathf.Deg2Rad;
                var pole = C + new Vector3(Mathf.Cos(a) * 2.2f, 0f, Mathf.Sin(a) * 2.2f);
                CarouselPole(ct, m, pole, names[i], animalsGate.gameObject);
            }

            // Motor housing beside the carousel (it doesn't turn), wired in.
            var motorStep = StepSignal("Step_Motor", room, "motor", "motor_tight");
            Box("Motor", room, new Vector3(0f, 0.5f, -5.2f), new Vector3(1.4f, 1f, 0.9f), Mat("Motor_Teal", new Color(0.3f, 0.7f, 0.7f)));
            Label("CAROUSEL MOTOR", room, new Vector3(0f, 1.25f, -5.66f), 180f, 0.035f, new Color(0.15f, 0.3f, 0.35f));
            Wire("Wire_Motor", room, new Color(1f, 0.6f, 0.15f), new Vector3(0f, 0f, -4.75f), new Vector3(0f, 0f, -4.3f));
            Chapter3Builder.Screw(room, new Vector3(0f, 0.6f, -5.66f), Vector3.back, spinGate.gameObject, motorStep.gameObject);

            // The horse ended up on the ticket booth roof: climb the yellow holds.
            Vector3 booth = new Vector3(-9f, 0f, -5f);
            Box("TicketBooth", room, booth + Vector3.up * 1.5f, new Vector3(3f, 3f, 2.4f), m.stripeA);
            Box("BoothRoof", room, booth + Vector3.up * 3.1f, new Vector3(3.4f, 0.2f, 2.8f), m.stripeB);
            Box("BoothWindow", room, booth + new Vector3(0f, 1.6f, -1.22f), new Vector3(1.6f, 0.9f, 0.05f), Mat("Lamp_Glass", new Color(0.75f, 0.9f, 1f), 0.95f), false);
            Label("TICKETS", room, booth + new Vector3(0f, 2.4f, -1.25f), 180f, 0.05f, new Color(0.5f, 0.15f, 0.3f));
            GripWall("BoothClimb", room, booth + new Vector3(1.65f, 0f, 0f), new Vector3(2f, 3.1f, 0.3f), 90f, new Color(1f, 0.75f, 0.25f));
            Animal("Horse", room, booth + new Vector3(-0.6f, 3.6f, 0f));
            Bolt("pier_booth", room, booth + new Vector3(0.8f, 3.7f, 0.6f));

            // The swan drifted out to sea on a raft: Tether-pull it in.
            var raft = Box("Raft", room, new Vector3(7f, -0.75f, 8.2f), new Vector3(1.8f, 0.3f, 1.8f), Mat("Wood", new Color(0.8f, 0.6f, 0.42f)));
            raft.name = "SwanRaft";
            Animal("Swan", room, new Vector3(7f, -0.2f, 8.2f));

            // The seal is shut in the prize stall behind a crate too heavy to carry: Magnet it away.
            Vector3 stall = new Vector3(9.5f, 0f, -6f);
            Box("Stall_Back", room, stall + new Vector3(0f, 1.3f, -1.4f), new Vector3(3f, 2.6f, 0.2f), m.stripeB);
            Box("Stall_E", room, stall + new Vector3(1.4f, 1.3f, 0f), new Vector3(0.2f, 2.6f, 3f), m.stripeA);
            Box("Stall_W", room, stall + new Vector3(-1.4f, 1.3f, 0f), new Vector3(0.2f, 2.6f, 3f), m.stripeA);
            Box("Stall_Roof", room, stall + new Vector3(0f, 2.7f, 0f), new Vector3(3.2f, 0.2f, 3.2f), m.stripeA);
            Label("RING TOSS: PRIZES", room, stall + new Vector3(0f, 2.3f, 1.55f), 180f, 0.04f, new Color(0.5f, 0.15f, 0.3f));
            Animal("Seal", room, stall + new Vector3(0f, 0.6f, -0.4f));
            // The front is closed apart from a doorway just wide enough for the crate that's blocking it.
            foreach (float x in new[] { -0.95f, 0.95f })
                Box("Stall_Front", room, stall + new Vector3(x, 1.3f, 1.4f), new Vector3(0.9f, 2.6f, 0.2f), m.stripeB);
            Box("Stall_Lintel", room, stall + new Vector3(0f, 1.85f, 1.4f), new Vector3(1.1f, 1.5f, 0.2f), m.stripeB);
            MetalCrate("StallCrate", room, m, stall + new Vector3(0f, 0.5f, 1.4f));
            ToolPedestal("MagnetStand", room, new Vector3(5f, 0f, -8.5f), tools.magnet, null, "got_magnet");

            Person("Benny", room, new Vector3(-4f, 0f, -8f), 20f, new Color(0.85f, 0.3f, 0.35f), new Color(1f, 0.95f, 0.85f),
                new Color(0.62f, 0.45f, 0.35f), new Color(0.2f, 0.15f, 0.1f));

            // Fairground dressing.
            foreach (var p in new[] { new Vector3(-12f, 0f, 2f), new Vector3(12f, 0f, 2f), new Vector3(-6f, 0f, 5.3f), new Vector3(6f, 0f, 5.3f) })
                Lamppost(room, m, p);
            Balloons(room, new Vector3(-11.5f, 0f, -8.5f));
            Balloons(room, new Vector3(12f, 0f, -1f));

            Hum(room, new Vector3(0f, 3f, 0f), 26f, 28f, 0.25f);

            var dispatch = Script("Dispatch_Ch6_Call01",
                Line("intro", "Funland Pier! Benny says the hum shook the carousel animals right off their poles. The horse is on the ticket booth roof, the swan's floated out to sea, and the seal's stuck in the prize stall behind a big metal crate.", 10f),
                Line("got_magnet", "Got the Magnet. Hold F (or B) next to metal and it floats along with you.", 4f),
                Line("animal_home", "Back on its pole!", 2.5f, true),
                Line("animals_home", "All three animals home! Now tighten the motor by the gate.", 4f),
                Line("motor_tight", "Motor's tight!", 2.5f),
                Line("hum_tremor_first", "Hum! Grab something!", 3f),
                Line("hum_tremor", "Hum!", 2f, true),
                Line("fell", "Splash! Back to the pier you go.", 3f, true),
                Line("exit", "The carousel's turning and the music's playing! Benny says the Ferris wheel's been acting up too. He'd better check on it.", 6f));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(-11f, 0.05f, -1f), 90f, tools.grabber, tools.whisk, tools.screwdriver);
            BuildSystems(task, dispatch, bot);
            FinishScene(scene, CarouselScene, bot);
        }

        // ================================================================== Big Rescue: Stuck at the Top

        [MenuItem("Robot Rescue/Chapter 6/Big Rescue: Stuck at the Top")]
        public static void BuildStuckAtTheTop()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var tools = ToolKit();
            var m = Mats();

            var task = Task("Ch6_Boss_StuckAtTheTop", "ch6_boss", "BIG RESCUE: Stuck at the Top", "Grandpa Sal and the twins",
                "The Ferris wheel froze with three riders in the top gondola. Fix it and bring them down.", 120,
                ("hub", "Climb the service tower and tighten the hub (2 screws)"),
                ("brake", "Magnet the counterweight onto the brake plate"),
                ("wheel_down", "Plug in at the control booth and turn the wheel until they're at the bottom"));

            // Pier x -13..13, z -12..8; sea z 8..12, where the Old Rig's shadow lies.
            var room = new GameObject("FerrisWheel").transform;
            RoomWithExit(room, 26f, 24f, 4.5f, m.boards, m.rail, m.stripeB, "FUNLAND FERRIS WHEEL", TitleColor);
            Object.DestroyImmediate(room.Find("Floor").gameObject);
            Box("Floor", room, new Vector3(0f, -0.25f, -2f), new Vector3(26f, 0.5f, 20f), m.boards);
            Sea(room, m, new Vector3(0f, 0f, 10f), new Vector3(26f, 0f, 4f));
            var shadow = Box("OldRigShadow", room, new Vector3(-3f, -2.4f, 10.5f), new Vector3(9f, 0.6f, 2.6f), Mat("Rig_Shadow", new Color(0.12f, 0.2f, 0.32f)), false);
            shadow.transform.rotation = Quaternion.Euler(0f, 12f, 0f);
            Sphere("RigLight", room, new Vector3(-1f, -2f, 10.6f), 0.15f, Glow("Rig_Blink", new Color(1f, 0.35f, 0.3f), 3f));

            // The wheel: hub at (0, 10.5, 4), radius 7.5, turning in the XY plane.
            Vector3 H = new Vector3(0f, 10.5f, 4f);
            const float R = 7.5f;
            var wheelGo = new GameObject("Wheel");
            wheelGo.transform.SetParent(room, false);
            wheelGo.transform.position = H;
            var wheel = wheelGo.transform;
            var hubCap = Cylinder("Hub", wheel, H, 0.8f, 1.4f, m.gold);
            hubCap.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 16; i++)
            {
                float a = (i + 0.5f) * 22.5f;
                Vector3 mid = H + Quaternion.Euler(0f, 0f, a) * Vector3.right * R;
                var rim = Box("Rim", wheel, mid, new Vector3(0.25f, 2.95f, 0.25f), i % 2 == 0 ? m.stripeA : m.stripeB, false);
                rim.transform.rotation = Quaternion.Euler(0f, 0f, a);
            }
            for (int i = 0; i < 8; i++)
            {
                float a = i * 45f;
                var spoke = Box("Spoke", wheel, H + Quaternion.Euler(0f, 0f, a) * Vector3.right * R * 0.5f, new Vector3(R, 0.15f, 0.15f), m.white, false);
                spoke.transform.rotation = Quaternion.Euler(0f, 0f, a);
            }

            var gondolas = new Transform[8];
            for (int i = 0; i < 8; i++)
            {
                float a = 90f + i * 45f;
                Vector3 hang = H + Quaternion.Euler(0f, 0f, a) * Vector3.right * R;
                gondolas[i] = Gondola(wheel, m, hang, i);
            }
            // The stuck gondola, right at the top.
            var top = gondolas[0];
            var gp = top.position + Vector3.down * 1.75f;
            Person("Grandpa Sal", top, gp + new Vector3(-0.35f, 0f, 0f), 180f, new Color(0.45f, 0.4f, 0.7f), new Color(0.95f, 0.9f, 0.8f),
                new Color(0.8f, 0.6f, 0.48f), new Color(0.92f, 0.92f, 0.95f), 0.75f, true);
            Person("Twin Lou", top, gp + new Vector3(0.25f, 0f, 0.2f), 170f, new Color(0.95f, 0.6f, 0.3f), new Color(1f, 0.9f, 0.4f),
                new Color(0.6f, 0.42f, 0.32f), new Color(0.2f, 0.12f, 0.08f), 0.5f);
            Person("Twin Lu", top, gp + new Vector3(0.55f, 0f, -0.15f), 190f, new Color(0.35f, 0.75f, 0.55f), new Color(1f, 0.9f, 0.4f),
                new Color(0.6f, 0.42f, 0.32f), new Color(0.2f, 0.12f, 0.08f), 0.5f);

            // A-frame legs either side of the wheel (visual; the riders hang between them).
            foreach (float zs in new[] { -1f, 1f })
                foreach (float xs in new[] { -1f, 1f })
                {
                    Vector3 foot = new Vector3(xs * 4.5f, 0f, H.z + zs * 1.2f);
                    Vector3 mid = (foot + H + Vector3.forward * zs * 0.9f) * 0.5f;
                    var leg = Box("Leg", room, mid, new Vector3(0.4f, Vector3.Distance(foot, H) , 0.4f), m.steel, false);
                    leg.transform.rotation = Quaternion.FromToRotation(Vector3.up, (H + Vector3.forward * zs * 0.9f - foot).normalized);
                }

            var rescue = room.gameObject.AddComponent<FerrisRescue>();
            rescue.wheel = wheel;
            rescue.gondolas = gondolas;
            rescue.axis = Vector3.forward;
            var wheelGate = Gate("Wheel_AND", room, PowerCombiner.Mode.All, 3, rescue.gameObject);

            // Service tower up to a platform under the hub. The hub needs two screws.
            Vector3 T = new Vector3(0f, 0f, 1.6f);
            const float deckY = 9.7f;
            Box("ServiceTower", room, T + Vector3.up * deckY * 0.5f, new Vector3(1.4f, deckY, 1.4f), m.steel);
            GripWall("TowerClimb", room, T + new Vector3(0f, 0f, -0.85f), new Vector3(1.4f, deckY, 0.3f), 180f, new Color(1f, 0.75f, 0.25f), 0.6f);
            Box("TowerDeck", room, new Vector3(0f, deckY - 0.15f, 2.4f), new Vector3(3f, 0.3f, 3f), m.boards);
            Box("TowerRail_W", room, new Vector3(-1.45f, deckY + 0.4f, 2.4f), new Vector3(0.1f, 0.8f, 3f), m.rail);
            Box("TowerRail_E", room, new Vector3(1.45f, deckY + 0.4f, 2.4f), new Vector3(0.1f, 0.8f, 3f), m.rail);
            Box("HubPlate", room, new Vector3(0f, H.y, 3.25f), new Vector3(1.6f, 1.6f, 0.1f), m.steel);
            var hubStep = StepSignal("Step_Hub", room, "hub", "hub_tight");
            var hubGate = Gate("Hub_AND", room, PowerCombiner.Mode.All, 2, wheelGate.gameObject, hubStep.gameObject);
            foreach (float x in new[] { -0.45f, 0.45f })
                Chapter3Builder.Screw(room, new Vector3(x, H.y - 0.3f, 3.19f), Vector3.back, hubGate.gameObject);
            CheckpointZone("TowerTop", room, new Vector3(0f, deckY + 1f, 2.4f), new Vector3(2.8f, 2f, 2.8f), "see_rig");
            Bolt("ferris_tower", room, new Vector3(1f, deckY + 0.6f, 1.4f));

            // Brake: the counterweight plate, west side.
            var brakeStep = StepSignal("Step_Brake", room, "brake", "brake_off");
            var plate = Plate("BrakePlate", room, new Vector3(-7f, 0f, -2f), 3.5f, 1.4f, wheelGate.gameObject, brakeStep.gameObject);
            plate.tooLightCue = "brake_light";
            Label("BRAKE: COUNTERWEIGHT HERE", room, new Vector3(-7f, 1.3f, -0.9f), 180f, 0.035f, new Color(0.45f, 0.3f, 0.1f));
            var weight = MetalCrate("Counterweight", room, m, new Vector3(9f, 0.5f, -9f));
            weight.GetComponent<Renderer>().sharedMaterial = Mat("Counterweight_Grey", new Color(0.45f, 0.48f, 0.55f), 0.5f);
            Wire("Wire_Brake", room, new Color(1f, 0.6f, 0.15f), new Vector3(-7f, 0f, -2f), new Vector3(-4.5f, 0f, -2f), new Vector3(-4.5f, 0f, 2.8f));

            // Control booth: PIP plugs in and stays plugged in while the wheel turns.
            Vector3 B = new Vector3(6.5f, 0f, -1.5f);
            Box("ControlBooth", room, B + new Vector3(0f, 1.3f, 0.9f), new Vector3(2.2f, 2.6f, 1.2f), m.stripeA);
            Box("BoothRoof", room, B + new Vector3(0f, 2.7f, 0.9f), new Vector3(2.6f, 0.2f, 1.6f), m.stripeB);
            Label("WHEEL CONTROL", room, B + new Vector3(0f, 2.2f, 0.27f), 180f, 0.04f, new Color(0.5f, 0.15f, 0.3f));
            Socket("WheelSocket", room, B + new Vector3(0f, 0f, -0.6f), 180f, wheelGate.gameObject);
            Wire("Wire_Booth", room, new Color(1f, 0.6f, 0.15f), B, new Vector3(4.5f, 0f, -1.5f), new Vector3(4.5f, 0f, 2.8f));

            ServiceBay("ServiceBay", room, new Vector3(-10f, 0f, -9f));
            Person("Benny", room, new Vector3(3.5f, 0f, -4f), 160f, new Color(0.85f, 0.3f, 0.35f), new Color(1f, 0.95f, 0.85f),
                new Color(0.62f, 0.45f, 0.35f), new Color(0.2f, 0.15f, 0.1f));
            foreach (var p in new[] { new Vector3(-12f, 0f, 6f), new Vector3(12f, 0f, 6f), new Vector3(-12f, 0f, -6f), new Vector3(12f, 0f, -11f) })
                Lamppost(room, m, p);
            Balloons(room, new Vector3(11f, 0f, -10.5f));

            Hum(room, new Vector3(0f, 5f, 0f), 20f, 18f, 0.5f);

            var dispatch = Script("Dispatch_Ch6_Boss",
                Line("intro", "BIG RESCUE! The Ferris wheel froze with Grandpa Sal and the twins right at the top. Tighten the hub up the service tower, Magnet the counterweight onto the brake plate, then plug in at the control booth and bring them down.", 11f),
                Line("hub_tight", "Hub's tight!", 3f),
                Line("brake_off", "Brake's off!", 3f),
                Line("brake_light", "The brake wants the counterweight. It's metal: Magnet it.", 3f, true),
                Line("see_rig", "PIP, look out to sea. That dark shape under the water... that's the Old Rig. And something's blinking down there.", 7f),
                Line("wheel_on", "It's turning! Stay plugged in!", 3f, true),
                Line("wheel_off", "The wheel stopped! Plug back in!", 3f, true),
                Line("wheel_jammed", "The hum jammed it! Hang on, it'll shake loose.", 3f, true),
                Line("wheel_down", "They're down! Grandpa Sal says it was the best ride of his life. The twins want to go again.", 6f),
                Line("hum_tremor_first", "Hum! Hold on tight up there!", 3f),
                Line("hum_tremor", "Another hum.", 2f, true),
                Line("fell", "Oof. Back to the pier.", 3f, true),
                Line("exit", "Everybody's safe! And now we know exactly where the Old Rig is. Next stop: down there.", 6f));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(-11.5f, 0.05f, -6f), 90f, tools.grabber, tools.whisk, tools.screwdriver, tools.magnet);
            BuildSystems(task, dispatch, bot);
            FinishScene(scene, WheelScene, bot);
        }

        // ================================================================== pieces

        /// <summary>Open sea past the end of the pier: water you can fall into, with a pier edge.</summary>
        static void Sea(Transform parent, PierMats m, Vector3 center, Vector3 footprint)
        {
            Box("SeaBed", parent, new Vector3(center.x, -3.25f, center.z), new Vector3(footprint.x, 0.5f, footprint.z), Mat("Harbour_Sand", new Color(0.9f, 0.82f, 0.62f)));
            Box("PondWater", parent, new Vector3(center.x, -0.9f, center.z), new Vector3(footprint.x, 0.1f, footprint.z), m.water, false);
            Box("PierEdge", parent, new Vector3(center.x, -1.6f, center.z - footprint.z * 0.5f + 0.15f), new Vector3(footprint.x, 3f, 0.3f), m.boards);
            for (float x = -footprint.x * 0.5f + 1f; x < footprint.x * 0.5f; x += 3f)
                Cylinder("PierLeg", parent, new Vector3(center.x + x, -1.8f, center.z - footprint.z * 0.5f + 0.4f), 0.2f, 3f, Mat("Wood", new Color(0.8f, 0.6f, 0.42f)));
            var kill = Box("KillZone", parent, new Vector3(center.x, -2.2f, center.z), new Vector3(footprint.x, 1f, footprint.z), null);
            kill.GetComponent<Collider>().isTrigger = true;
            kill.AddComponent<KillZone>();
        }

        /// <summary>A carousel pole with a saddle where one named animal goes back.</summary>
        static void CarouselPole(Transform carousel, PierMats m, Vector3 floorPos, string animal, GameObject powered)
        {
            Cylinder("Pole", carousel, floorPos + Vector3.up * 2f, 0.06f, 3.5f, m.gold);
            var seat = new GameObject("Seat_" + animal).transform;
            seat.SetParent(carousel, false);
            seat.SetPositionAndRotation(floorPos + Vector3.up * 1.2f, Quaternion.LookRotation(Vector3.Cross(Vector3.up, floorPos - carousel.position)));
            Label(animal.ToUpperInvariant(), carousel, floorPos + Vector3.up * 0.55f, Mathf.Atan2(floorPos.x - carousel.position.x, floorPos.z - carousel.position.z) * Mathf.Rad2Deg + 180f, 0.03f, new Color(0.5f, 0.15f, 0.3f));
            var zone = Box("PoleZone_" + animal, carousel, floorPos + Vector3.up * 1f, new Vector3(1.4f, 1.6f, 1.4f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var drop = zone.AddComponent<RescueDropZone>();
            drop.accepts = animal;
            drop.seat = seat;
            drop.cue = "animal_home";
            drop.poweredObjects = new[] { powered };
        }

        /// <summary>A painted carousel animal PIP can carry (and Tether-pull).</summary>
        static Carryable Animal(string animal, Transform parent, Vector3 pos)
        {
            var root = new GameObject(animal);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.5f, 0.7f, 0.9f);
            root.AddComponent<Rigidbody>().mass = 1.4f;
            var carry = root.AddComponent<Carryable>();
            carry.displayName = animal;
            root.AddComponent<Respawnable>();
            Vector3 P(float x, float y, float z) => pos + new Vector3(x, y, z);
            var gold = Mat("Funland_Gold", new Color(1f, 0.8f, 0.3f), 0.7f);
            switch (animal)
            {
                case "Horse":
                    var horse = Mat("Carousel_Horse", new Color(0.98f, 0.96f, 0.92f));
                    Sphere("Body", root.transform, P(0f, 0f, 0f), 0.3f, horse, new Vector3(0.9f, 0.9f, 1.5f));
                    Sphere("Head", root.transform, P(0f, 0.3f, 0.4f), 0.17f, horse, new Vector3(0.8f, 1f, 1.3f));
                    Sphere("Mane", root.transform, P(0f, 0.32f, 0.25f), 0.12f, Mat("Carousel_Mane", new Color(1f, 0.55f, 0.7f)), new Vector3(0.6f, 1.2f, 1.4f));
                    Sphere("Saddle", root.transform, P(0f, 0.24f, -0.05f), 0.2f, Mat("Carousel_Red", new Color(0.9f, 0.3f, 0.35f)), new Vector3(1.2f, 0.4f, 1f));
                    break;
                case "Swan":
                    var swan = Mat("Carousel_Swan", new Color(1f, 1f, 1f));
                    Sphere("Body", root.transform, P(0f, 0f, 0f), 0.3f, swan, new Vector3(1f, 0.8f, 1.4f));
                    Cylinder("Neck", root.transform, P(0f, 0.3f, 0.3f), 0.06f, 0.5f, swan);
                    Sphere("Head", root.transform, P(0f, 0.55f, 0.36f), 0.1f, swan);
                    Sphere("Beak", root.transform, P(0f, 0.54f, 0.47f), 0.05f, Mat("Beak_Orange", new Color(1f, 0.6f, 0.2f)), new Vector3(0.8f, 0.6f, 1.4f));
                    break;
                default:
                    var seal = Mat("Carousel_Seal", new Color(0.55f, 0.65f, 0.8f));
                    Sphere("Body", root.transform, P(0f, 0f, 0f), 0.3f, seal, new Vector3(0.9f, 0.85f, 1.5f));
                    Sphere("Head", root.transform, P(0f, 0.25f, 0.38f), 0.16f, seal);
                    Sphere("Ball", root.transform, P(0f, 0.48f, 0.42f), 0.1f, Mat("Carousel_Red", new Color(0.9f, 0.3f, 0.35f)));
                    break;
            }
            Sphere("Crown", root.transform, P(0f, -0.28f, 0f), 0.08f, gold);
            return carry;
        }

        static GameObject MetalCrate(string name, Transform parent, PierMats m, Vector3 pos)
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

        /// <summary>A gondola hanging from the rim at <paramref name="hang"/>; its pivot is the hanger point.</summary>
        static Transform Gondola(Transform wheel, PierMats m, Vector3 hang, int index)
        {
            var root = new GameObject("Gondola_" + index).transform;
            root.SetParent(wheel, false);
            root.position = hang;
            var colour = index % 3 == 0 ? m.stripeA : index % 3 == 1 ? Mat("Gondola_Blue", new Color(0.45f, 0.7f, 0.95f)) : Mat("Gondola_Mint", new Color(0.55f, 0.88f, 0.7f));
            Box("Hanger", root, hang + Vector3.down * 0.45f, new Vector3(0.08f, 0.9f, 0.08f), m.steel, false);
            Box("Roof", root, hang + Vector3.down * 0.9f, new Vector3(1.6f, 0.1f, 1.3f), colour, false);
            Box("Tub", root, hang + Vector3.down * 2.1f, new Vector3(1.6f, 0.7f, 1.3f), colour, false);
            foreach (float x in new[] { -0.75f, 0.75f })
                Box("Post", root, hang + new Vector3(x, -1.35f, 0f), new Vector3(0.06f, 0.9f, 0.06f), m.steel, false);
            return root;
        }

        static void Lamppost(Transform parent, PierMats m, Vector3 floorPos)
        {
            Cylinder("Lamppost", parent, floorPos + Vector3.up * 1.6f, 0.08f, 3.2f, m.rail);
            Sphere("Lamp", parent, floorPos + Vector3.up * 3.3f, 0.22f, Glow("Bulb_Warm", new Color(1f, 0.85f, 0.5f), 2f));
            Box("LamppostCollider", parent, floorPos + Vector3.up * 1.6f, new Vector3(0.2f, 3.2f, 0.2f), null);
        }

        static void Balloons(Transform parent, Vector3 floorPos)
        {
            Color[] colors = { new Color(1f, 0.4f, 0.45f), new Color(0.45f, 0.7f, 1f), new Color(1f, 0.85f, 0.3f), new Color(0.55f, 0.9f, 0.6f) };
            for (int i = 0; i < colors.Length; i++)
            {
                var top = floorPos + new Vector3((i - 1.5f) * 0.35f, 2.4f + (i % 2) * 0.3f, (i % 2) * 0.2f);
                Box("String", parent, new Vector3(top.x, (floorPos.y + top.y) * 0.5f, top.z), new Vector3(0.015f, top.y - floorPos.y, 0.015f), Mat("Lighthouse_White", new Color(0.98f, 0.97f, 0.95f)), false);
                Sphere("Balloon", parent, top, 0.25f, Mat("Balloon_" + i, colors[i], 0.6f), new Vector3(1f, 1.2f, 1f));
            }
        }

        /// <summary>The hum tremor, set up the same way as Chapter 5's.</summary>
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
