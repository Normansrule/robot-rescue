using System.Collections.Generic;
using RobotRescue.Bots;
using RobotRescue.Campaign;
using RobotRescue.Core;
using RobotRescue.Narrative;
using RobotRescue.Puzzles;
using RobotRescue.Tasks;
using RobotRescue.Tasks.Cooking;
using RobotRescue.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using static RobotRescue.EditorTools.ChamberKit;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// Menu: Robot Rescue > Build Cooking Chamber Scene.
    /// Generates the greybox chamber "Call 2-1: Pasta Emergency" from primitives, plus its data assets
    /// (ingredient, recipe, task, DISPATCH lines) and materials. Safe to re-run: assets are updated in place
    /// and the scene is rebuilt from scratch.
    ///
    /// The puzzle, in order a player usually finds it:
    ///  1. The tap has no water. Its pressure plate needs more weight than PIP: push the crate onto it.
    ///  2. Carry the pot under the running tap to fill it.
    ///  3. The pasta is on a shelf out of reach: Tether-pull it down (or climb the crate, if you're clever).
    ///  4. Put the pot on the stove, plug your Tether into the stove socket to power it, drop/throw the pasta in.
    ///     You're the battery, so you have to stay within cable range while it boils.
    ///  5. Unplug, carry the cooked pot to Nonna's serving hatch. The exit door opens.
    ///  6. Swing across the harbour gap on the Tether hook to the exit flag.
    /// </summary>
    public static class CookingChamberBuilder
    {
        const string ScenePath = Root + "/Scenes/Ch2_Call01_PastaEmergency.unity";
        const string DataDir = Root + "/Data/Ch2";

        [MenuItem("Robot Rescue/Build Cooking Chamber Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(Root + "/Scenes");
            EnsureFolder(MatDir);
            EnsureFolder(DataDir);
            ResetCache();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var data = BuildData();

            BuildLighting();
            var kitchen = new GameObject("Kitchen").transform;
            BuildRoom(kitchen);
            BuildStove(kitchen);
            var stream = BuildSink(kitchen);
            BuildPressurePlate(kitchen, stream);
            var pot = BuildPot(kitchen, data.recipe);
            BuildPasta(kitchen, data.pasta);
            BuildServingHatch(kitchen);
            BuildExit();

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(0f, 0.05f, -3.5f));
            BuildSystems(data, bot, pot);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = bot.gameObject;
            Debug.Log($"[Robot Rescue] Built {ScenePath}. Press Play!");
        }

        // ------------------------------------------------------------------ data assets

        struct ChamberData
        {
            public IngredientDefinition pasta;
            public RecipeDefinition recipe;
            public TaskDefinition task;
            public DispatchScript dispatch;
        }

        static ChamberData BuildData()
        {
            var d = new ChamberData();

            d.pasta = LoadOrCreate<IngredientDefinition>(DataDir + "/Ingredient_Pasta.asset");
            d.pasta.id = "pasta";
            d.pasta.displayName = "Pasta";
            d.pasta.rawColor = new Color(1f, 0.82f, 0.35f);
            d.pasta.cookedColor = new Color(1f, 0.95f, 0.72f);

            d.recipe = LoadOrCreate<RecipeDefinition>(DataDir + "/Recipe_NonnasSpaghetti.asset");
            d.recipe.dishName = "Nonna's Spaghetti";
            d.recipe.needsWater = true;
            d.recipe.ingredients = new[] { d.pasta };
            d.recipe.cookTime = 8f;

            d.task = LoadOrCreate<TaskDefinition>(DataDir + "/Task_Ch2_Call01_PastaEmergency.asset");
            d.task.taskId = "ch2_call01";
            d.task.title = "Call 2-1: Pasta Emergency";
            d.task.rescuee = "Nonna Lucia";
            d.task.description = "Lunch rush in eight minutes and the pasta is still in the box.";
            d.task.boltsReward = 25;
            d.task.steps = new[]
            {
                new TaskStep { id = "fill_water", label = "Fill the pot at the sink" },
                new TaskStep { id = "add_pasta", label = "Get the pasta into the pot" },
                new TaskStep { id = "power_stove", label = "Power the stove with your Tether" },
                new TaskStep { id = "cook", label = "Boil the pasta" },
                new TaskStep { id = "serve", label = "Serve it at Nonna's hatch" },
            };

            d.dispatch = LoadOrCreate<DispatchScript>(DataDir + "/Dispatch_Ch2_Call01.asset");
            d.dispatch.lines = new[]
            {
                Line("intro", "Morning, PIP! Your first real call. Nonna Lucia's lunch rush is in eight minutes and her pasta is still in the box. No pressure. Some pressure. Pasta pressure.", 7f),
                Line("plate_too_light", "The plate isn't budging. No offence, PIP, but you weigh about as much as a sandwich. Find something heavier.", 6f),
                Line("tap_on", "Water's running! Get the pot under that tap."),
                Line("water_filled", "Pot filled. Step one of being a chef: done. Step two: don't set anything on fire."),
                Line("tether_pull", "Yoink! The Bureau approves tether-pulling for objects, snacks and, in emergencies, cats.", 5f),
                Line("pasta_grabbed", "That's the pasta! Into the pot it goes. Dropping it in is fine. Throwing it in is more fun.", 5f),
                Line("ingredient_added", "Pasta's in! Now it needs heat. The stove's dead though... if only someone had a power cable growing out of their back.", 6f),
                Line("wrong_ingredient", "Nonna says that doesn't go in there.", 3f, true),
                Line("tether_power", "Plugged in! You are now technically a kitchen appliance. I've never been prouder.", 5f),
                Line("cable_taut", "That's the end of your cable. You're the battery, PIP. Batteries don't wander off.", 5f),
                Line("boiling", "It's boiling! Stay plugged in. Watching water boil is officially part of your job now.", 5f),
                Line("cooked", "Al dente! Unplug and get it to the serving hatch before Nonna starts doing the angry hand thing.", 6f),
                Line("served_raw", "Nonna sent it back. Apparently 'crunchy spaghetti' isn't a dish. Cook it first!", 5f, true),
                Line("served", "Nonna Lucia is saved! She says you're a good boy. I told her you're a robot. She doesn't care.", 6f),
                Line("swing_hint", "Exit's over the harbour. See that hook? Tether it, swing, and jump to let go at the top.", 6f),
                Line("tether_swing", "Swinging over open water. Very heroic. Please don't tell Insurance.", 4f),
                Line("fell", "Splash! Don't worry, you're waterproof. Mostly. Try again.", 4f, true),
                Line("fell", "Bureau policy: if at first you don't succeed, respawn.", 4f, true),
                Line("exit", "Call complete! Take five, PIP. Not five minutes. Five seconds. The phones are ringing.", 6f),
            };

            EditorUtility.SetDirty(d.pasta);
            EditorUtility.SetDirty(d.recipe);
            EditorUtility.SetDirty(d.task);
            EditorUtility.SetDirty(d.dispatch);
            return d;
        }

        static NarrationLine Line(string cue, string text, float duration = 4f, bool repeatable = false) =>
            new NarrationLine { cue = cue, text = text, duration = duration, repeatable = repeatable };

        // ------------------------------------------------------------------ environment

        static void BuildRoom(Transform p)
        {
            var floor = Mat("Floor_Cream", new Color(0.98f, 0.93f, 0.82f));
            var wall = Mat("Wall_Mint", new Color(0.66f, 0.9f, 0.8f));
            var trim = Mat("Trim_Coral", new Color(1f, 0.55f, 0.5f));

            // Kitchen: x -8..8, z -6..6. Door gap in the south wall at x -1.5..1.5.
            Box("Floor", p, new Vector3(0, -0.25f, 0), new Vector3(16, 0.5f, 12), floor);
            Box("Wall_N", p, new Vector3(0, 2.5f, 6.25f), new Vector3(17, 5, 0.5f), wall);
            Box("Wall_E", p, new Vector3(8.25f, 2.5f, 0), new Vector3(0.5f, 5, 12), wall);
            Box("Wall_W", p, new Vector3(-8.25f, 2.5f, 0), new Vector3(0.5f, 5, 12), wall);
            Box("Wall_S_Left", p, new Vector3(-4.75f, 2.5f, -6.25f), new Vector3(6.5f, 5, 0.5f), wall);
            Box("Wall_S_Right", p, new Vector3(4.75f, 2.5f, -6.25f), new Vector3(6.5f, 5, 0.5f), wall);
            Box("Wall_S_Lintel", p, new Vector3(0, 4.25f, -6.25f), new Vector3(3, 1.5f, 0.5f), wall);
            Box("Skirting_N", p, new Vector3(0, 0.15f, 5.98f), new Vector3(16, 0.3f, 0.05f), trim, false);

            var door = Box("ExitDoor", p, new Vector3(0, 1.75f, -6.25f), new Vector3(3, 3.5f, 0.3f), trim);
            var doorBody = door.AddComponent<Rigidbody>();
            doorBody.isKinematic = true;
            var doorComp = door.AddComponent<Door>();
            doorComp.openOnTaskComplete = true;
            doorComp.openOffset = new Vector3(3.2f, 0, 0);
            Label("EXIT", p, new Vector3(0, 3.8f, -5.95f), 180f, 0.12f, Color.white);

            // Cheerful clutter so the room reads as a trattoria kitchen.
            var wood = Mat("Wood", new Color(0.8f, 0.6f, 0.42f));
            Box("Table", p, new Vector3(-1.5f, 0.4f, 3.5f), new Vector3(2.2f, 0.8f, 1.2f), wood);
            Label("NONNA LUCIA'S TRATTORIA", p, new Vector3(0, 4.3f, 5.95f), 180f, 0.14f, new Color(0.85f, 0.25f, 0.25f));
        }

        static Transform BuildStove(Transform p)
        {
            var root = new GameObject("Stove").transform;
            root.SetParent(p);
            var stove = root.gameObject.AddComponent<Stove>();

            Box("Body", root, new Vector3(-6.6f, 0.5f, 2.5f), new Vector3(1.6f, 1f, 1.4f), Mat("Stove_Red", new Color(0.92f, 0.3f, 0.3f)));
            var burner = Box("Burner", root, new Vector3(-6.6f, 1.02f, 2.5f), new Vector3(1.1f, 0.04f, 0.9f), Mat("Burner", new Color(0.25f, 0.25f, 0.28f)), false);
            stove.burnerVisual = burner.GetComponent<Renderer>();

            var zone = Box("BurnerZone", root, new Vector3(-6.6f, 1.35f, 2.5f), new Vector3(1.3f, 0.6f, 1.1f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            zone.AddComponent<BurnerZone>();

            // Socket on the front face, facing into the room (+x).
            var socket = Box("PowerSocket", root, new Vector3(-5.78f, 0.55f, 2.5f), new Vector3(0.06f, 0.35f, 0.35f), Mat("Socket", new Color(0.2f, 0.2f, 0.25f)));
            var led = Box("SocketLED", root, new Vector3(-5.76f, 0.85f, 2.5f), new Vector3(0.04f, 0.1f, 0.1f), Mat("LED", Color.gray), false);
            var ps = socket.AddComponent<PowerSocket>();
            ps.poweredObjects = new[] { root.gameObject };
            ps.indicator = led.GetComponent<Renderer>();

            Label("STOVE", root, new Vector3(-6.6f, 2.0f, 2.5f), 90f, 0.1f, new Color(0.6f, 0.15f, 0.15f));
            return root;
        }

        static WaterStream BuildSink(Transform p)
        {
            var root = new GameObject("Sink").transform;
            root.SetParent(p);
            var teal = Mat("Sink_Teal", new Color(0.4f, 0.75f, 0.8f));
            var steel = Mat("Steel", new Color(0.78f, 0.8f, 0.85f), 0.7f);
            Box("Counter", root, new Vector3(6.5f, 0.45f, 3f), new Vector3(2.5f, 0.9f, 1.4f), teal);
            Box("FaucetPipe", root, new Vector3(6.5f, 1.6f, 3.6f), new Vector3(0.15f, 1.4f, 0.15f), steel, false);
            Box("FaucetSpout", root, new Vector3(6.5f, 2.25f, 3.15f), new Vector3(0.15f, 0.15f, 0.9f), steel, false);

            var zone = Box("WaterStream", root, new Vector3(6.5f, 1.5f, 2.6f), new Vector3(1f, 1.6f, 1f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var stream = zone.AddComponent<WaterStream>();
            var water = Cylinder("StreamVisual", zone.transform, new Vector3(6.5f, 1.55f, 2.72f), 0.15f, 1.3f, Mat("Water", new Color(0.5f, 0.8f, 1f), 0.9f));
            stream.streamVisual = water;

            Label("SINK", root, new Vector3(6.5f, 2.9f, 3.6f), 0f, 0.1f, new Color(0.15f, 0.4f, 0.5f));
            return stream;
        }

        static void BuildPressurePlate(Transform p, WaterStream stream)
        {
            var root = new GameObject("TapPlate").transform;
            root.SetParent(p);
            var visual = Box("Plate", root, new Vector3(3f, 0.05f, -2f), new Vector3(1.6f, 0.1f, 1.6f), Mat("Plate_Yellow", new Color(1f, 0.85f, 0.3f)));
            var zone = Box("PlateTrigger", root, new Vector3(3f, 0.5f, -2f), new Vector3(1.4f, 0.8f, 1.4f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var plate = zone.AddComponent<PressurePlate>();
            plate.requiredMass = 2f;
            plate.poweredObjects = new[] { stream.gameObject };
            plate.plateVisual = visual.transform;
            plate.tooLightCue = "plate_too_light";

            // Blue "pipe" painted on the floor from the plate to the sink: a readable hint.
            var pipe = Mat("Pipe_Blue", new Color(0.35f, 0.6f, 1f));
            Box("PipeA", root, new Vector3(4.75f, 0.015f, -2f), new Vector3(2.0f, 0.03f, 0.2f), pipe, false);
            Box("PipeB", root, new Vector3(5.85f, 0.015f, 0.15f), new Vector3(0.2f, 0.03f, 4.5f), pipe, false);

            // The crate that's heavy enough. Starts north of the plate: push it south.
            var crate = Box("Crate", root, new Vector3(3f, 0.5f, 1f), new Vector3(1f, 1f, 1f), Mat("Crate_Orange", new Color(1f, 0.62f, 0.25f)));
            crate.GetComponent<Collider>().sharedMaterial = PhysMat("Crate", 0.45f, 0.45f, PhysicsMaterialCombine.Average);
            var rb = crate.AddComponent<Rigidbody>();
            rb.mass = 3f;
            crate.AddComponent<Pushable>();
            crate.AddComponent<Respawnable>();
        }

        static CookingPot BuildPot(Transform p, RecipeDefinition recipe)
        {
            var pot = new GameObject("Pot");
            pot.transform.SetParent(p);
            pot.transform.position = new Vector3(-3.5f, 0.31f, -1.5f);
            var col = pot.AddComponent<BoxCollider>();
            col.size = new Vector3(0.8f, 0.6f, 0.8f);
            var rb = pot.AddComponent<Rigidbody>();
            rb.mass = 1.2f;

            var steel = Mat("Pot_Steel", new Color(0.62f, 0.68f, 0.78f), 0.6f);
            Cylinder("Body", pot.transform, pot.transform.position, 0.4f, 0.6f, steel);
            Box("HandleL", pot.transform, pot.transform.position + new Vector3(-0.47f, 0.2f, 0), new Vector3(0.12f, 0.06f, 0.2f), steel, false);
            Box("HandleR", pot.transform, pot.transform.position + new Vector3(0.47f, 0.2f, 0), new Vector3(0.12f, 0.06f, 0.2f), steel, false);
            var water = Cylinder("Water", pot.transform, pot.transform.position + Vector3.up * 0.28f, 0.36f, 0.02f, Mat("PotWater", Color.cyan));
            var food = Cylinder("Contents", pot.transform, pot.transform.position + Vector3.up * 0.3f, 0.28f, 0.04f, Mat("PotFood", Color.yellow));

            var mouth = new GameObject("Mouth");
            mouth.transform.SetParent(pot.transform, false);
            mouth.transform.localPosition = new Vector3(0, 0.45f, 0);
            var mc = mouth.AddComponent<BoxCollider>();
            mc.isTrigger = true;
            mc.size = new Vector3(0.9f, 0.4f, 0.9f);
            mouth.AddComponent<PotMouth>();

            var carry = pot.AddComponent<Carryable>();
            carry.displayName = "Pot";
            var cp = pot.AddComponent<CookingPot>();
            cp.recipe = recipe;
            cp.waterVisual = water.GetComponent<Renderer>();
            cp.contentsVisual = food.GetComponent<Renderer>();
            pot.AddComponent<Respawnable>();
            return cp;
        }

        static void BuildPasta(Transform p, IngredientDefinition pastaDef)
        {
            var wood = Mat("Wood", new Color(0.8f, 0.6f, 0.42f));
            // High shelf over the north wall, out of jump reach.
            Box("PastaShelf", p, new Vector3(-3f, 3f, 5.6f), new Vector3(2f, 0.12f, 0.8f), wood);
            Box("ShelfBracketL", p, new Vector3(-3.8f, 2.75f, 5.9f), new Vector3(0.08f, 0.4f, 0.2f), wood, false);
            Box("ShelfBracketR", p, new Vector3(-2.2f, 2.75f, 5.9f), new Vector3(0.08f, 0.4f, 0.2f), wood, false);

            var pasta = Box("PastaBox", p, new Vector3(-3f, 3.32f, 5.35f), new Vector3(0.4f, 0.5f, 0.25f), Mat("Pasta_Box", new Color(1f, 0.8f, 0.3f)));
            Box("Label", pasta.transform, pasta.transform.position + new Vector3(0, 0, -0.13f), new Vector3(0.3f, 0.2f, 0.01f), Mat("Pasta_Label", new Color(0.2f, 0.45f, 0.9f)), false);
            var rb = pasta.AddComponent<Rigidbody>();
            rb.mass = 0.4f;
            var carry = pasta.AddComponent<Carryable>();
            carry.displayName = "Pasta";
            carry.firstPickupCue = "pasta_grabbed";
            pasta.AddComponent<Ingredient>().definition = pastaDef;
            pasta.AddComponent<Respawnable>();
        }

        static void BuildServingHatch(Transform p)
        {
            var root = new GameObject("ServingHatch").transform;
            root.SetParent(p);
            var wood = Mat("Wood", new Color(0.8f, 0.6f, 0.42f));
            Box("HatchCounter", root, new Vector3(-7.4f, 0.5f, -3f), new Vector3(1.2f, 1f, 2f), wood);
            Box("Awning", root, new Vector3(-7.5f, 2.6f, -3f), new Vector3(1.2f, 0.12f, 2.4f), Mat("Awning_Red", new Color(0.95f, 0.35f, 0.35f)), false);

            var zone = Box("HatchTrigger", root, new Vector3(-7.1f, 1.5f, -3f), new Vector3(1.6f, 1.2f, 2f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var hatch = zone.AddComponent<ServingHatch>();

            var served = new GameObject("ServedPlate");
            served.transform.SetParent(root);
            Cylinder("Plate", served.transform, new Vector3(-7.5f, 1.02f, -3f), 0.35f, 0.04f, Mat("Plate_White", Color.white));
            Cylinder("Spaghetti", served.transform, new Vector3(-7.5f, 1.1f, -3f), 0.22f, 0.12f, Mat("Spaghetti", new Color(1f, 0.92f, 0.65f)));
            Sphere("Sauce", served.transform, new Vector3(-7.5f, 1.18f, -3f), 0.18f, Mat("Sauce", new Color(0.85f, 0.15f, 0.1f)));
            hatch.servedVisual = served;

            Label("SERVE", root, new Vector3(-7.7f, 3.1f, -3f), 90f, 0.12f, new Color(0.6f, 0.2f, 0.2f));
        }

        static void BuildExit()
        {
            var root = new GameObject("HarbourGap").transform;
            var floor = Mat("Boardwalk", new Color(0.75f, 0.58f, 0.42f));
            var post = Mat("Post_Blue", new Color(0.3f, 0.45f, 0.75f));

            // Corridor z -6..-8.5, gap z -8.5..-13, landing z -13..-18.
            Box("Corridor", root, new Vector3(0, -0.25f, -7.25f), new Vector3(4f, 0.5f, 2.5f), floor);
            Box("Landing", root, new Vector3(0, -0.25f, -15.5f), new Vector3(5f, 0.5f, 5f), floor);
            Box("HarbourWater", root, new Vector3(0, -3f, -11f), new Vector3(40f, 0.1f, 30f), Mat("Harbour", new Color(0.35f, 0.65f, 0.95f), 0.9f), false);

            var cp = Box("Checkpoint_Corridor", root, new Vector3(0, 1f, -7.3f), new Vector3(4f, 2f, 2f), null);
            cp.GetComponent<Collider>().isTrigger = true;
            cp.AddComponent<Checkpoint>().cue = "swing_hint";

            // Swing hook on a gantry over the gap.
            Box("GantryPostL", root, new Vector3(-4f, 0.5f, -10.75f), new Vector3(0.3f, 13f, 0.3f), post);
            Box("GantryPostR", root, new Vector3(4f, 0.5f, -10.75f), new Vector3(0.3f, 13f, 0.3f), post);
            Box("GantryBeam", root, new Vector3(0, 7.1f, -10.75f), new Vector3(8.3f, 0.3f, 0.3f), post);
            Box("HookRope", root, new Vector3(0, 6.85f, -10.75f), new Vector3(0.05f, 0.4f, 0.05f), post, false);
            var hook = Sphere("SwingHook", root, new Vector3(0, 6.5f, -10.75f), 0.35f, Mat("Hook_Yellow", new Color(1f, 0.85f, 0.2f)), collider: true);
            hook.AddComponent<TetherAnchor>();

            var kill = Box("KillZone", root, new Vector3(0, -6f, -11f), new Vector3(60f, 1f, 60f), null);
            kill.GetComponent<Collider>().isTrigger = true;
            kill.AddComponent<KillZone>();

            var exit = Box("LevelExit", root, new Vector3(0, 1f, -16f), new Vector3(4f, 2f, 3f), null);
            exit.GetComponent<Collider>().isTrigger = true;
            exit.AddComponent<LevelExit>();
            Box("FlagPole", root, new Vector3(1.5f, 1.5f, -16.5f), new Vector3(0.1f, 3f, 0.1f), Mat("Steel", Color.gray), false);
            Box("Flag", root, new Vector3(1.95f, 2.7f, -16.5f), new Vector3(0.8f, 0.5f, 0.04f), Mat("Flag_Green", new Color(0.35f, 0.85f, 0.45f)), false);
        }

        // ------------------------------------------------------------------ bot, camera, systems

        static void BuildSystems(ChamberData data, BotController bot, CookingPot pot) =>
            ChamberKit.BuildSystems(data.task, data.dispatch, bot, pot);

    }
}
