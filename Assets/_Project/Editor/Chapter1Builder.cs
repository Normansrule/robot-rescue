using RobotRescue.Narrative;
using RobotRescue.Tasks;
using UnityEditor;
using UnityEngine;
using static RobotRescue.EditorTools.ChamberKit;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// Menu: Robot Rescue > Chapter 1 > Big Rescue: Coffee Crisis.
    ///
    /// DISPATCH's coffee machine in the Bureau break room is about to blow. Pressure climbs on its own; three
    /// colour-coded vents bleed it off while powered. Hold all three open at once until it stabilises:
    ///  - Blue vent: a socket. PIP plugs in (and has to stay within cable range).
    ///  - Green vent: a pressure plate that needs one Helper Drone (PIP alone is too light).
    ///  - Yellow vent: a heavy plate that needs TWO drones stacked. Two of the three drones sit on a high shelf:
    ///    Tether-pull them down.
    /// Sensible order: drones onto the plates first (each vent slows the pressure), then plug in last.
    /// </summary>
    public static class Chapter1Builder
    {
        public const string ScenePath = Root + "/Scenes/Ch1_Boss_CoffeeCrisis.unity";
        const string DataDir = Root + "/Data/Ch1";

        static readonly Color Blue = new Color(0.3f, 0.6f, 1f);
        static readonly Color Green = new Color(0.35f, 0.85f, 0.45f);
        static readonly Color Yellow = new Color(1f, 0.82f, 0.25f);

        [MenuItem("Robot Rescue/Chapter 1/Big Rescue: Coffee Crisis")]
        public static void Build()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var (task, dispatch) = BuildData();

            var room = new GameObject("BreakRoom").transform;
            var wall = Mat("Wall_Bureau", new Color(0.72f, 0.8f, 0.95f));
            RoomWithExit(room, 20f, 16f, 6f, Mat("Floor_Bureau", new Color(0.9f, 0.9f, 0.86f)), wall,
                Mat("Trim_Coral", new Color(1f, 0.55f, 0.5f)), "BUREAU HQ: BREAK ROOM", new Color(0.3f, 0.35f, 0.6f));

            // The machine, centre-north, with three vent pipes beside it.
            var machine = Coffee("CoffeeMachine", room, new Vector3(0f, 0f, 5.5f));
            machine.startPressure = 20f;
            machine.risePerSecond = 1.4f;
            machine.ventPerSecond = 1.0f;
            machine.stabiliseSeconds = 8f;

            var ventA = Vent("Vent_Blue", room, machine, 0, new Vector3(-2.6f, 2.6f, 6.6f), Blue);
            var ventB = Vent("Vent_Green", room, machine, 1, new Vector3(2.6f, 2.6f, 6.6f), Green);
            var ventC = Vent("Vent_Yellow", room, machine, 2, new Vector3(4.2f, 3.4f, 6.6f), Yellow);

            // Blue: socket, wired to the blue pipe.
            var wireA = Wire("Wire_Blue", room, Blue, new Vector3(-5f, 0, 1.5f), new Vector3(-5f, 0, 6.6f), new Vector3(-2.6f, 0, 6.6f));
            var stepA = StepSignal("Step_VentA", room, "vent_a");
            Socket("Socket_Blue", room, new Vector3(-5f, 0, 1.5f), 180f, ventA.gameObject, wireA.gameObject, stepA.gameObject);

            // Green: one-drone plate (1.5 kg; PIP is 1 kg).
            var wireB = Wire("Wire_Green", room, Green, new Vector3(5f, 0, -1f), new Vector3(5f, 0, 4f), new Vector3(2.6f, 0, 4f), new Vector3(2.6f, 0, 6.6f));
            var stepB = StepSignal("Step_VentB", room, "vent_b");
            Plate("Plate_Green", room, new Vector3(5f, 0, -1f), 1.5f, 1.2f, ventB.gameObject, wireB.gameObject, stepB.gameObject);

            // Yellow: two-drone plate (3 kg). The trigger is tall enough for a stack.
            var wireC = Wire("Wire_Yellow", room, Yellow, new Vector3(-4f, 0, -4f), new Vector3(8.5f, 0, -4f), new Vector3(8.5f, 0, 6.6f), new Vector3(4.2f, 0, 6.6f));
            var stepC = StepSignal("Step_VentC", room, "vent_c");
            Plate("Plate_Yellow", room, new Vector3(-4f, 0, -4f), 3f, 2.4f, ventC.gameObject, wireC.gameObject, stepC.gameObject);
            Label("HEAVY: 2 DRONES", room, new Vector3(-4f, 0.6f, -4.95f), 180f, 0.05f, new Color(0.5f, 0.4f, 0.1f));

            // Drones: one on the floor, two on the high shelf by the east wall (Tether-pull them down).
            var wood = Mat("Wood", new Color(0.8f, 0.6f, 0.42f));
            Drone("Drone_Floor", room, new Vector3(1.5f, 0.5f, -2.5f), new Color(0.55f, 0.8f, 1f));
            Box("HighShelf", room, new Vector3(9.25f, 3f, -1f), new Vector3(1.5f, 0.15f, 4f), wood);
            Box("ShelfBracket1", room, new Vector3(9.6f, 2.6f, -2.5f), new Vector3(0.15f, 0.7f, 0.15f), wood, false);
            Box("ShelfBracket2", room, new Vector3(9.6f, 2.6f, 0.5f), new Vector3(0.15f, 0.7f, 0.15f), wood, false);
            Drone("Drone_Shelf1", room, new Vector3(9.2f, 3.6f, -2f), new Color(1f, 0.7f, 0.4f));
            Drone("Drone_Shelf2", room, new Vector3(9.2f, 3.6f, 0f), new Color(0.75f, 0.55f, 1f));

            // Break-room clutter: water cooler, couch, desk with DISPATCH's mug.
            Table("Couch", room, new Vector3(-7.5f, 0, -2f), new Vector3(1.4f, 0, 3.5f), 0.8f, Mat("Couch_Teal", new Color(0.3f, 0.65f, 0.7f)));
            Table("Desk", room, new Vector3(-7f, 0, 5f), new Vector3(3f, 0, 1.4f), 0.9f, wood);
            Cylinder("WaterCooler", room, new Vector3(7.5f, 0.9f, 6f), 0.35f, 1.8f, Mat("Water", new Color(0.5f, 0.8f, 1f), 0.9f));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(0f, 0.05f, -5.5f));
            BuildSystems(task, dispatch, bot);
            FinishScene(scene, ScenePath, bot);
        }

        static (TaskDefinition, DispatchScript) BuildData()
        {
            var task = LoadOrCreate<TaskDefinition>(DataDir + "/Task_Ch1_Boss_CoffeeCrisis.asset");
            task.taskId = "ch1_boss";
            task.title = "BIG RESCUE: Coffee Crisis";
            task.rescuee = "DISPATCH (and the break room)";
            task.description = "DISPATCH's coffee machine is over-pressurised. Open all three vents before it blows.";
            task.boltsReward = 60;
            task.steps = new[]
            {
                new TaskStep { id = "vent_a", label = "Blue vent: plug into its socket" },
                new TaskStep { id = "vent_b", label = "Green vent: weigh down its plate" },
                new TaskStep { id = "vent_c", label = "Yellow vent: two drones on the heavy plate" },
                new TaskStep { id = "coffee_saved", label = "Hold all three open until it settles" },
            };

            var d = LoadOrCreate<DispatchScript>(DataDir + "/Dispatch_Ch1_Boss.asset");
            d.lines = new[]
            {
                L("intro", "PIP! Code Brown! My coffee machine is making a noise I can only describe as 'pre-explosion'. Open all three vents. Please. I haven't had my coffee.", 7f),
                L("plate_too_light", "Too light! That plate wants a Helper Drone. Or a very large sandwich.", 4f, true),
                L("drone_grabbed", "Drones stack, by the way. Very stable. Unlike me, right now.", 4f),
                L("tether_pull", "Yoink! Bring those drones down off the shelf.", 4f),
                L("tether_power", "Plugged in! You're a pressure valve now. Congratulations on your promotion.", 4f),
                L("cable_taut", "That's the end of your cable. Stay close to the socket.", 4f, true),
                L("vent_open", "Vent open! I can hear it sighing. I'm sighing too.", 3f, true),
                L("vents_all_open", "All three vents open! Hold them! Hold them steady!", 4f, true),
                L("pressure_high", "The gauge is in the red-ish. Orange-red. Red-orange. Hurry.", 4f),
                L("pressure_danger", "PIP it's SHAKING. Why is it shaking? Coffee machines shouldn't shake!", 4f),
                L("coffee_boom", "...well. The break room is now a latte. Let's try that again.", 5f, true),
                L("coffee_saved", "It's stable! You saved my coffee. You saved ME. The exit's open, hero.", 6f),
                L("fell", "Respawning. The coffee waits for no bot.", 3f, true),
                L("exit", "Chapter one complete! Next stop: Nonna Lucia's Trattoria. Bring an appetite. You can't eat. Bring one anyway.", 6f),
            };
            EditorUtility.SetDirty(task);
            EditorUtility.SetDirty(d);
            return (task, d);
        }

        static NarrationLine L(string cue, string text, float duration = 4f, bool repeatable = false) =>
            new NarrationLine { cue = cue, text = text, duration = duration, repeatable = repeatable };
    }
}
