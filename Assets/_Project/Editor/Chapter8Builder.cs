using System.Collections.Generic;
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
    /// Menu: Robot Rescue > Chapter 8 > ...
    /// The Founders' Festival on the summit platform: the finale.
    ///  - Call 8-1 "Stage Fright": get the festival stage ready. Two drones on the speaker plates, the curtain motor
    ///    screwed tight, and the banner freed from the top of the flagpole.
    ///  - Big Rescue "The Founders' Song": link the Founders' relay to the anchor engine so it never needs a keeper
    ///    again. Magnet the battery onto pylon A, get a drone onto pylon B, and plug in at the top of the bell tower.
    ///    The hum is just the engine's beat now, and every beat pushes the link along.
    /// </summary>
    public static class Chapter8Builder
    {
        public const string StageScene = Root + "/Scenes/Ch8_Call01_StageFright.unity";
        public const string SongScene = Root + "/Scenes/Ch8_Boss_TheFoundersSong.unity";
        const string DataDir = Root + "/Data/Ch8";

        static readonly Color TitleColor = new Color(0.55f, 0.3f, 0.65f);

        struct FestMats
        {
            public Material stone, wall, trim, stage, curtain, gold, steel, crate, bunting1, bunting2, bunting3;
        }

        static FestMats Mats() => new FestMats
        {
            stone = Mat("Festival_Stone", new Color(0.86f, 0.84f, 0.8f)),
            wall = Mat("Festival_Wall", new Color(0.95f, 0.88f, 0.75f)),
            trim = Mat("Festival_Trim", new Color(0.45f, 0.6f, 0.85f)),
            stage = Mat("Wood", new Color(0.8f, 0.6f, 0.42f)),
            curtain = Mat("Curtain_Red", new Color(0.85f, 0.25f, 0.3f)),
            gold = Mat("Funland_Gold", new Color(1f, 0.8f, 0.3f), 0.7f),
            steel = Mat("Steel", new Color(0.78f, 0.8f, 0.85f), 0.7f),
            crate = Mat("Metal_Crate", new Color(0.45f, 0.6f, 0.85f), 0.6f),
            bunting1 = Mat("Bunting_Pink", new Color(1f, 0.6f, 0.75f)),
            bunting2 = Mat("Bunting_Blue", new Color(0.45f, 0.7f, 1f)),
            bunting3 = Mat("Bunting_Yellow", new Color(1f, 0.85f, 0.3f)),
        };

        // ================================================================== Call 8-1: Stage Fright

        [MenuItem("Robot Rescue/Chapter 8/Call 8-1: Stage Fright")]
        public static void BuildStageFright()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var tools = ToolKit();
            var m = Mats();

            var task = Task("Ch8_Call01_StageFright", "ch8_call01", "Call 8-1: Stage Fright", "the Founders' Festival",
                "The festival starts tonight and the stage isn't ready. Speakers, curtain, banner: go!", 60,
                ("speakers", "Put a drone on each speaker plate"),
                ("curtain", "Screw the curtain motor tight"),
                ("banner", "Climb the flagpole and free the festival banner"));

            // Square x -13..13, z -10..10; the stage across the north end, z 4..9.6.
            var room = new GameObject("FestivalSquare").transform;
            RoomWithExit(room, 26f, 20f, 4.5f, m.stone, m.wall, m.trim, "FOUNDERS' FESTIVAL", TitleColor);
            const float stageH = 1.2f;
            Box("Stage", room, new Vector3(0f, stageH * 0.5f, 6.8f), new Vector3(16f, stageH, 5.6f), m.stage);
            // Three steps up the front of the stage.
            for (int i = 0; i < 3; i++)
                Box("StageStep", room, new Vector3(0f, 0.2f * (i + 1), 2.75f + i * 0.45f), new Vector3(3f, 0.4f * (i + 1), 0.45f), m.stage);

            // Speakers: one drone on each plate, either side of the stage.
            var speakersStep = StepSignal("Step_Speakers", room, "speakers", "speakers_on");
            var stageGate = Gate("Stage_AND", room, PowerCombiner.Mode.All, 3);
            var speakersGate = Gate("Speakers_AND", room, PowerCombiner.Mode.All, 2, speakersStep.gameObject, stageGate.gameObject);
            foreach (float x in new[] { -6f, 6f })
            {
                Box("Speaker", room, new Vector3(x, stageH + 1f, 8.6f), new Vector3(1.4f, 2f, 1f), Mat("Speaker_Black", new Color(0.15f, 0.15f, 0.2f)));
                Cylinder("Cone", room, new Vector3(x, stageH + 1.2f, 8.08f), 0.4f, 0.04f, m.steel).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                Plate("SpeakerPlate", room, new Vector3(x, stageH, 6.6f), 1.5f, 1.4f, speakersGate.gameObject);
            }
            // One drone in the café, one up on the lantern stall roof (Tether-pull it down).
            Drone("FestDrone_A", room, new Vector3(-9f, 1.1f, -5f), new Color(1f, 0.7f, 0.4f));
            Table(room, m, new Vector3(-9f, 0f, -5f));
            Box("LanternStall", room, new Vector3(9.5f, 1.3f, -5.5f), new Vector3(3f, 2.6f, 2.4f), m.bunting2);
            Box("LanternStallRoof", room, new Vector3(9.5f, 2.7f, -5.5f), new Vector3(3.4f, 0.2f, 2.8f), m.bunting1);
            Drone("FestDrone_B", room, new Vector3(9.5f, 3.3f, -5.5f), new Color(0.55f, 0.8f, 1f));

            // Curtain: two panels that slide apart once the motor is tight.
            var curtainStep = StepSignal("Step_Curtain", room, "curtain", "curtain_open");
            var curtainGate = Gate("Curtain_Relay", room, PowerCombiner.Mode.Any, 1, curtainStep.gameObject, stageGate.gameObject);
            foreach (float s in new[] { -1f, 1f })
            {
                var panel = SlidingDoor("Curtain", room, new Vector3(s * 2f, stageH + 1.6f, 8.2f), new Vector3(4f, 3.2f, 0.2f), new Vector3(s * 3.6f, 0f, 0f));
                panel.GetComponent<Renderer>().sharedMaterial = m.curtain;
                panel.GetComponent<Collider>().enabled = false;
                var list = new List<GameObject>(curtainGate.poweredObjects) { panel.gameObject };
                curtainGate.poweredObjects = list.ToArray();
            }
            Box("CurtainMotor", room, new Vector3(-7.4f, 0.5f, 3.5f), new Vector3(0.9f, 1f, 0.8f), Mat("Motor_Teal", new Color(0.3f, 0.7f, 0.7f)));
            Label("CURTAIN", room, new Vector3(-7.4f, 1.25f, 3.08f), 180f, 0.035f, new Color(0.15f, 0.3f, 0.35f));
            Chapter3Builder.Screw(room, new Vector3(-7.4f, 0.6f, 3.09f), Vector3.back, curtainGate.gameObject);

            // Banner: tangled at the top of the flagpole. Climb up and it unfurls.
            Vector3 F = new Vector3(-10.5f, 0f, -1f);
            Box("Flagpole", room, F + Vector3.up * 3.5f, new Vector3(0.6f, 7f, 0.6f), m.steel);
            // The lookout sits west of the pole, level with the top of the holds, so nothing overhangs the climb.
            GripWall("FlagpoleClimb", room, F + new Vector3(0.45f, 0f, 0f), new Vector3(0.6f, 6.65f, 0.3f), 90f, new Color(1f, 0.75f, 0.25f), 0.8f);
            Box("CrowsNest", room, F + new Vector3(-1.2f, 6.55f, 0f), new Vector3(1.8f, 0.2f, 1.8f), m.stage);
            var tangled = Sphere("TangledBanner", room, F + new Vector3(0f, 7.3f, 0f), 0.5f, m.bunting1, new Vector3(1f, 0.7f, 1f));
            var banner = Box("Banner", room, new Vector3(-4f, 4.2f, 9.7f), new Vector3(7f, 1.4f, 0.05f), m.bunting3, false);
            // Not a child of the banner: the banner box is scaled 7 x 1.4, which would stretch the text off the wall.
            Label("HAPPY FOUNDERS' DAY!", room, new Vector3(-4f, 4.2f, 9.65f), 180f, 0.07f, new Color(0.55f, 0.2f, 0.35f));
            var bannerText = room.Find("Sign_HAPPY FOUNDERS' DAY!").gameObject;
            var bannerToggle = new GameObject("BannerToggle").AddComponent<PowerToggle>();
            bannerToggle.transform.SetParent(room, false);
            bannerToggle.showWhenPowered = new[] { banner, bannerText };
            bannerToggle.hideWhenPowered = new[] { tangled };
            bannerToggle.pulse = 0f;
            var bannerZone = StepZone("Zone_Banner", room, F + new Vector3(-0.9f, 7.4f, 0f), new Vector3(2.4f, 1.6f, 1.8f), "banner", "banner_free");
            bannerZone.poweredObjects = new[] { bannerToggle.gameObject, stageGate.gameObject };
            Bolt("festival_flagpole", room, F + new Vector3(-1.4f, 7.1f, 0.5f));

            // Stage lights come on when everything's ready.
            var lights = new List<GameObject>();
            for (int i = 0; i < 9; i++)
                lights.Add(Sphere("StageLight", room, new Vector3(-7f + i * 1.75f, 4.3f, 9.4f), 0.22f, Glow("Stage_Light_" + i % 3, i % 3 == 0 ? new Color(1f, 0.5f, 0.6f) : i % 3 == 1 ? new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.85f, 0.4f), 2.5f)));
            var lightToggle = new GameObject("StageLights").AddComponent<PowerToggle>();
            lightToggle.transform.SetParent(room, false);
            lightToggle.showWhenPowered = lights.ToArray();
            stageGate.poweredObjects = new[] { lightToggle.gameObject, StepSignal("Cue_StageReady", room, null, "stage_ready").gameObject };

            Bunting(room, m, new Vector3(-12f, 3.6f, -8f), new Vector3(12f, 3.6f, -8f));
            Bunting(room, m, new Vector3(-12f, 3.6f, 0f), new Vector3(12f, 3.6f, 0f));
            Crowd(room, new Vector3(0f, 0f, -3f));

            Hum(room, new Vector3(0f, 3f, 0f), 30f, 30f, 0.05f);

            var dispatch = Script("Dispatch_Ch8_Call01",
                Line("intro", "PIP! The Founders' Festival is tonight and the stage is a mess. The speakers need a drone on each plate, the curtain motor's loose, and the banner's tangled at the top of the flagpole.", 9f),
                Line("drone_grabbed", "Got a drone. Onto a speaker plate with it.", 3f),
                Line("speakers_on", "Speakers on! Testing, testing... the drones are dancing.", 4f),
                Line("curtain_open", "Curtain's open!", 3f),
                Line("banner_free", "Banner's free! 'Happy Founders' Day!'", 4f),
                Line("stage_ready", "The stage is ready! Lights, music, everything!", 4f),
                Line("hum_tremor_first", "Hear that? It's the anchor engine's beat. Now that Zero's safe, it just sounds like a drum.", 6f),
                Line("hum_tremor", "Boom!", 2f, true),
                Line("fell", "Whoops! Back you go.", 3f, true),
                Line("exit", "Stage is set. One last job: the anchor engine still needs a keeper, and Zero has an idea.", 6f));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(-11f, 0.05f, -7f), 90f, tools.grabber, tools.whisk, tools.screwdriver, tools.magnet);
            BuildSystems(task, dispatch, bot);
            FinishScene(scene, StageScene, bot);
        }

        // ================================================================== Big Rescue: The Founders' Song

        [MenuItem("Robot Rescue/Chapter 8/Big Rescue: The Founders' Song")]
        public static void BuildFoundersSong()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var tools = ToolKit();
            var m = Mats();

            var task = Task("Ch8_Boss_TheFoundersSong", "ch8_boss", "BIG RESCUE: The Founders' Song", "Brightwater Reach",
                "Link the Founders' relay to the anchor engine, so it runs without a keeper and Zero can come home for good.", 200,
                ("battery", "Magnet the battery onto pylon A"),
                ("drone", "Get a drone onto pylon B"),
                ("bell", "Climb the bell tower and plug in at the top"),
                ("anchor_linked", "Hold the link until the Reach is anchored"));

            // Summit plaza x -13..13, z -12..12, the relay in the middle of the north half.
            var room = new GameObject("FoundersPlaza").transform;
            RoomWithExit(room, 26f, 24f, 4.5f, m.stone, m.wall, m.trim, "FOUNDERS' PLAZA", TitleColor);

            // The relay: a stone base, a mast and three rings that spin up as the link builds.
            Vector3 R = new Vector3(2f, 0f, 5f);
            Cylinder("RelayBase", room, R + Vector3.up * 0.4f, 2.2f, 0.8f, m.trim);
            Box("RelayBaseCollider", room, R + Vector3.up * 0.4f, new Vector3(3.2f, 0.8f, 3.2f), null);
            Cylinder("RelayMast", room, R + Vector3.up * 4f, 0.35f, 6.4f, m.steel);
            Box("RelayMastCollider", room, R + Vector3.up * 4f, new Vector3(0.7f, 6.4f, 0.7f), null);
            var rings = new Transform[3];
            for (int i = 0; i < 3; i++)
            {
                var ring = new GameObject("RelayRing" + i).transform;
                ring.SetParent(room, false);
                ring.position = R + Vector3.up * (2.5f + i * 1.6f);
                for (int k = 0; k < 4; k++)
                    Box("Segment", ring, ring.position + Quaternion.Euler(0f, k * 90f, 0f) * Vector3.forward * (1.6f - i * 0.3f), new Vector3(1.2f, 0.18f, 0.18f), i == 1 ? m.gold : m.trim, false)
                        .transform.rotation = Quaternion.Euler(0f, k * 90f + 90f, 0f);
                rings[i] = ring;
            }
            Sphere("RelayTop", room, R + Vector3.up * 7.4f, 0.5f, Glow("Relay_Glow", new Color(0.5f, 0.9f, 1f), 3f));
            Label("FOUNDERS' RELAY", room, R + new Vector3(0f, 1.3f, -1.7f), 180f, 0.04f, TitleColor);

            // Fireworks, hidden until the Reach is anchored.
            var fireworks = new List<GameObject>();
            var rng = new System.Random(8);
            for (int i = 0; i < 14; i++)
            {
                var p = new Vector3((float)rng.NextDouble() * 20f - 10f, 9f + (float)rng.NextDouble() * 5f, (float)rng.NextDouble() * 10f);
                fireworks.Add(Sphere("Firework", room, p, 0.4f + (float)rng.NextDouble() * 0.5f,
                    Glow("Firework_" + i % 4, i % 4 == 0 ? new Color(1f, 0.45f, 0.55f) : i % 4 == 1 ? new Color(0.45f, 0.8f, 1f) : i % 4 == 2 ? new Color(1f, 0.85f, 0.35f) : new Color(0.6f, 1f, 0.6f), 4f)));
            }

            var link = room.gameObject.AddComponent<AnchorLink>();
            link.rings = rings;
            link.showWhenLinked = fireworks.ToArray();
            var relayGate = Gate("Relay_AND", room, PowerCombiner.Mode.All, 3, link.gameObject);

            // Pylon A: the battery, a metal crate across the plaza.
            Pylon(room, m, new Vector3(-8f, 0f, -1f), "A");
            var batteryStep = StepSignal("Step_Battery", room, "battery", "pylon_a");
            var plateA = Plate("PylonPlate_A", room, new Vector3(-8f, 0f, -2.6f), 3.5f, 1.4f, relayGate.gameObject, batteryStep.gameObject);
            plateA.tooLightCue = "pylon_heavy";
            var battery = MetalCrate("RelayBattery", room, m, new Vector3(9f, 0.5f, -9f));
            battery.GetComponent<Renderer>().sharedMaterial = Mat("Battery_Green", new Color(0.4f, 0.8f, 0.5f), 0.5f);
            Wire("Wire_A", room, new Color(1f, 0.6f, 0.15f), new Vector3(-8f, 0f, -2.6f), new Vector3(-8f, 0f, 5f), R + Vector3.left * 2.2f);

            // Pylon B: a drone. The only one is up on the bakery balcony.
            Pylon(room, m, new Vector3(9f, 0f, 1f), "B");
            var droneStep = StepSignal("Step_Drone", room, "drone", "pylon_b");
            Plate("PylonPlate_B", room, new Vector3(9f, 0f, -0.6f), 1.5f, 1.4f, relayGate.gameObject, droneStep.gameObject);
            Box("Bakery", room, new Vector3(11f, 1.5f, -6f), new Vector3(3.6f, 3f, 3f), m.wall);
            Box("BakeryBalcony", room, new Vector3(9f, 3.05f, -6f), new Vector3(1.4f, 0.1f, 2.4f), m.stage);
            Label("NONNA'S BAKERY", room, new Vector3(9.15f, 2.4f, -6f), 270f, 0.04f, new Color(0.55f, 0.25f, 0.2f));
            Drone("BalconyDrone", room, new Vector3(9f, 3.6f, -6f), new Color(0.75f, 0.55f, 1f));
            Wire("Wire_B", room, new Color(1f, 0.6f, 0.15f), new Vector3(9f, 0f, -0.6f), new Vector3(9f, 0f, 5f), R + Vector3.right * 2.2f);

            // Pylon C: the bell tower. Climb it and plug in at the top.
            Vector3 B = new Vector3(-9f, 0f, 8f);
            const float towerH = 7f;
            Box("BellTower", room, B + Vector3.up * towerH * 0.5f, new Vector3(2.4f, towerH, 2.4f), m.wall);
            GripWall("BellTowerClimb", room, B + new Vector3(0f, 0f, -1.35f), new Vector3(1.8f, towerH, 0.3f), 180f, new Color(1f, 0.75f, 0.25f), 0.6f);
            Box("BellDeck", room, B + new Vector3(0f, towerH - 0.1f, 1.2f), new Vector3(3f, 0.2f, 2.8f), m.stage);
            Cylinder("Bell", room, B + new Vector3(0.6f, towerH + 1.4f, 1.2f), 0.45f, 0.7f, m.gold);
            Box("BellFrame", room, B + new Vector3(0.6f, towerH + 1.9f, 1.2f), new Vector3(1.4f, 0.12f, 0.12f), m.stage, false);
            StepZone("Zone_Bell", room, B + new Vector3(0f, towerH + 1f, 0.4f), new Vector3(3f, 2f, 3.2f), "bell", "bell_top");
            Socket("BellSocket", room, B + new Vector3(-0.8f, towerH, 1.2f), 90f, relayGate.gameObject);
            Label("PLUG IN: PYLON C", room, B + new Vector3(-0.8f, towerH + 1.6f, 1.2f), 90f, 0.03f, TitleColor);
            Wire("Wire_C", room, new Color(1f, 0.6f, 0.15f), B + new Vector3(1.3f, 0f, 0f), new Vector3(-2f, 0f, 8f), R + Vector3.back * 2.2f);
            Bolt("festival_bell", room, B + new Vector3(1f, towerH + 0.7f, -0.6f));

            // The crowd, Zero front and centre.
            var zero = Chapter7Builder.UnitZero(room, new Vector3(-2f, 0.55f, -4f), out _);
            zero.awake = true;
            zero.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            Crowd(room, new Vector3(0f, 0f, -7f));
            Bunting(room, m, new Vector3(-12f, 3.6f, -10f), new Vector3(12f, 3.6f, -10f));
            ServiceBay("ServiceBay", room, new Vector3(-11f, 0f, -10f));

            Hum(room, new Vector3(0f, 4f, 0f), 12f, 10f, 0.05f);

            var dispatch = Script("Dispatch_Ch8_Boss",
                Line("intro", "BIG RESCUE, the last one! Zero says the Founders' relay can run the anchor engine from up here, so nobody ever has to stay down there again. Power all three pylons, then hold the link.", 10f),
                Line("pylon_a", "Pylon A's on!", 2.5f),
                Line("pylon_b", "Pylon B's on!", 2.5f),
                Line("pylon_heavy", "Pylon A needs the battery. It's metal: Magnet it.", 3f, true),
                Line("bell_top", "Top of the bell tower! Plug into Pylon C.", 3f),
                Line("drone_grabbed", "Got the drone. Pylon B!", 2.5f),
                Line("link_on", "All three pylons! The relay's linking!", 3f, true),
                Line("link_off", "Lost a pylon! The link's fading!", 3f, true),
                Line("link_beat", "Feel that beat? It's pushing the link along!", 3f, true),
                Line("anchor_linked", "LINKED! The anchor engine's running on its own. The Reach is anchored for good, and Unit Zero is HOME. Happy Founders' Day, PIP!", 9f),
                Line("hum_tremor_first", "There's the beat. It's the hum, PIP, but now it's music.", 5f),
                Line("hum_tremor", "Boom!", 1.5f, true),
                Line("fell", "Oops! Back to the plaza.", 3f, true),
                Line("exit", "Zero wants to say something. 'Thank you, PIP. Thank you, Bureau. It's good to be home.' ...And DISPATCH would like everyone to know the coffee machine works now.", 10f));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(-11f, 0.05f, -6f), 90f, tools.grabber, tools.whisk, tools.screwdriver, tools.magnet);
            BuildSystems(task, dispatch, bot);
            FinishScene(scene, SongScene, bot);
        }

        // ================================================================== pieces

        static void Pylon(Transform parent, FestMats m, Vector3 floorPos, string letter)
        {
            Box("Pylon_" + letter, parent, floorPos + Vector3.up * 1.4f, new Vector3(0.8f, 2.8f, 0.8f), m.trim);
            Sphere("PylonLamp", parent, floorPos + Vector3.up * 3f, 0.3f, Glow("Relay_Glow", new Color(0.5f, 0.9f, 1f), 3f));
            Label("PYLON " + letter, parent, floorPos + new Vector3(0f, 2f, -0.42f), 180f, 0.05f, Color.white);
        }

        static GameObject MetalCrate(string name, Transform parent, FestMats m, Vector3 pos)
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

        static void Table(Transform parent, FestMats m, Vector3 floorPos)
        {
            Cylinder("TableTop", parent, floorPos + Vector3.up * 0.55f, 0.8f, 0.08f, m.stage);
            Cylinder("TableLeg", parent, floorPos + Vector3.up * 0.27f, 0.08f, 0.54f, m.steel);
            Box("TableCollider", parent, floorPos + Vector3.up * 0.3f, new Vector3(1.1f, 0.6f, 1.1f), null);
        }

        /// <summary>A string of pennants between two points.</summary>
        static void Bunting(Transform parent, FestMats m, Vector3 a, Vector3 b)
        {
            var mats = new[] { m.bunting1, m.bunting2, m.bunting3 };
            int n = Mathf.Max(2, Mathf.RoundToInt(Vector3.Distance(a, b) / 0.8f));
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                var p = Vector3.Lerp(a, b, t) + Vector3.down * Mathf.Sin(t * Mathf.PI) * 0.6f;
                var flag = Box("Pennant", parent, p + Vector3.down * 0.2f, new Vector3(0.35f, 0.4f, 0.03f), mats[i % 3], false);
                flag.transform.rotation = Quaternion.LookRotation(Vector3.Cross(b - a, Vector3.up)) * Quaternion.Euler(0f, 0f, 45f);
            }
        }

        /// <summary>Everyone PIP has helped, come to watch.</summary>
        static void Crowd(Transform parent, Vector3 center)
        {
            (string name, Color outfit, Color top, Color skin, Color hair, float h)[] people =
            {
                ("Nonna Lucia", new Color(0.4f, 0.3f, 0.55f), new Color(1f, 1f, 1f), new Color(0.88f, 0.7f, 0.58f), new Color(0.9f, 0.9f, 0.92f), 0.9f),
                ("Rosa", new Color(0.35f, 0.6f, 0.4f), new Color(1f, 0.9f, 0.6f), new Color(0.86f, 0.64f, 0.5f), new Color(0.35f, 0.2f, 0.12f), 1f),
                ("Priya", new Color(0.3f, 0.45f, 0.8f), new Color(1f, 0.95f, 0.85f), new Color(0.62f, 0.45f, 0.35f), new Color(0.1f, 0.08f, 0.08f), 0.7f),
                ("Ines", new Color(0.2f, 0.35f, 0.6f), new Color(1f, 0.85f, 0.3f), new Color(0.75f, 0.55f, 0.42f), new Color(0.3f, 0.2f, 0.15f), 1f),
                ("Captain Mo", new Color(0.95f, 0.85f, 0.3f), new Color(0.3f, 0.4f, 0.7f), new Color(0.55f, 0.38f, 0.3f), new Color(0.9f, 0.9f, 0.92f), 0.9f),
                ("Benny", new Color(0.85f, 0.3f, 0.35f), new Color(1f, 0.95f, 0.85f), new Color(0.62f, 0.45f, 0.35f), new Color(0.2f, 0.15f, 0.1f), 1f),
            };
            for (int i = 0; i < people.Length; i++)
            {
                var p = people[i];
                float x = (i - (people.Length - 1) * 0.5f) * 1.6f;
                Person(p.name, parent, center + new Vector3(x, 0f, (i % 2) * 0.8f), Mathf.Lerp(-20f, 20f, i / (people.Length - 1f)), p.outfit, p.top, p.skin, p.hair, p.h);
            }
        }

        static HumTremor Hum(Transform parent, Vector3 pos, float interval, float firstDelay, float gripLoss)
        {
            var hum = new GameObject("TheBeat");
            hum.transform.SetParent(parent, false);
            hum.transform.position = pos;
            var src = hum.AddComponent<AudioSource>();
            src.spatialBlend = 0f;
            var drone = hum.AddComponent<Babble>();
            drone.voiceHz = 110f;
            drone.radio = true;
            drone.volume = 0.4f;
            drone.syllableGap = 0.18f;
            var tremor = hum.AddComponent<HumTremor>();
            tremor.interval = interval;
            tremor.firstDelay = firstDelay;
            tremor.gripLoss = gripLoss;
            tremor.duration = 1.2f;
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
