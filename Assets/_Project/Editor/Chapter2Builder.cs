using RobotRescue.Narrative;
using RobotRescue.Tasks;
using RobotRescue.Tasks.Cooking;
using UnityEditor;
using UnityEngine;
using static RobotRescue.EditorTools.ChamberKit;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// Menu: Robot Rescue > Chapter 2 > ...
    /// Nonna Lucia's Trattoria after Call 2-1 (see <see cref="CookingChamberBuilder"/>):
    ///  - Call 2-2 "Whisk Business": pick up the Whisk, then clear three gentle order tickets (sauce, spaghetti, sauce).
    ///  - Big Rescue "Pasta Panic": five orders for food critic Mr. Grumbleton while the kitchen falls apart
    ///    (fuses blow, pots rattle off the stove, the pantry conveyor reverses). Three expired tickets and he storms out.
    /// </summary>
    public static class Chapter2Builder
    {
        public const string WhiskScene = Root + "/Scenes/Ch2_Call02_WhiskBusiness.unity";
        public const string BossScene = Root + "/Scenes/Ch2_Boss_PastaPanic.unity";
        const string DataDir = Root + "/Data/Ch2";

        struct Menu
        {
            public IngredientDefinition pasta, tomato, carrot, basil;
            public RecipeDefinition spaghetti, sauce, minestrone, pesto;
            public RecipeDefinition[] boiled, whisked;
        }

        [MenuItem("Robot Rescue/Chapter 2/Call 2-2: Whisk Business")]
        public static void BuildWhiskBusiness()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var m = BuildMenu();
            var tools = ToolKit();

            var task = LoadOrCreate<TaskDefinition>(DataDir + "/Task_Ch2_Call02_WhiskBusiness.asset");
            task.taskId = "ch2_call02";
            task.title = "Call 2-2: Whisk Business";
            task.rescuee = "Nonna Lucia";
            task.description = "Nonna's arm is tired from whisking. Lend her yours. Your robot one.";
            task.boltsReward = 30;
            task.steps = new[]
            {
                new TaskStep { id = "get_whisk", label = "Pick up the Whisk tool" },
                new TaskStep { id = "whisk", label = "Whisk a tomato sauce" },
                new TaskStep { id = "cook", label = "Boil some spaghetti" },
                new TaskStep { id = "orders_done", label = "Clear every order ticket" },
            };
            EditorUtility.SetDirty(task);

            var room = new GameObject("Kitchen").transform;
            Kitchen(room, "NONNA LUCIA'S TRATTORIA");

            var board = Orders("OrderBoard", room, new Vector3(-1f, 3.2f, 6.9f), 180f, "Nonna Lucia",
                Order(m.sauce, 120f, 6f), Order(m.spaghetti, 150f, 12f), Order(m.sauce, 120f, 25f));
            board.maxActive = 2;
            board.maxStrikes = 3;

            ToolPedestal("WhiskStand", room, new Vector3(-2.5f, 0f, -3.5f), tools.whisk, "get_whisk", "got_whisk");
            KitchenStove("Stove", room, new Vector3(-5.5f, 0f, 6.25f), 180f, true, out _);
            Sink("Sink", room, new Vector3(5.5f, 0f, 6.25f), 180f, true);
            Pot("Pot", room, new Vector3(-3.5f, 0.31f, 3.5f), m.boiled, 0f);
            var wood = Mat("Wood", new Color(0.8f, 0.6f, 0.42f));
            Table("PrepTable", room, new Vector3(1.5f, 0f, 1.5f), new Vector3(2.4f, 0f, 1.2f), 0.8f, wood);
            Bowl("Mixing bowl", room, new Vector3(1.5f, 1.02f, 1.5f), m.whisked);
            Dispenser("Crate_Pasta", room, new Vector3(-8.2f, 0f, 1f), 90f, m.pasta, new Vector3(-8.2f, 1.05f, 1f));
            Dispenser("Crate_Tomato", room, new Vector3(-8.2f, 0f, -1f), 90f, m.tomato, new Vector3(-8.2f, 1.05f, -1f));
            Hatch("ServingHatch", room, new Vector3(8.4f, 0f, -2f), -90f, board);
            Trash("Trash", room, new Vector3(7.8f, 0f, 3f));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(0f, 0.05f, -4.5f), 0f, tools.grabber);
            BuildSystems(task, Dispatch("Dispatch_Ch2_Call02", false), bot);
            FinishScene(scene, WhiskScene, bot);
        }

        [MenuItem("Robot Rescue/Chapter 2/Big Rescue: Pasta Panic")]
        public static void BuildPastaPanic()
        {
            if (!BeginScene(out var scene, DataDir)) return;
            var m = BuildMenu();
            var tools = ToolKit();

            var task = LoadOrCreate<TaskDefinition>(DataDir + "/Task_Ch2_Boss_PastaPanic.asset");
            task.taskId = "ch2_boss";
            task.title = "BIG RESCUE: Pasta Panic";
            task.rescuee = "Nonna Lucia (and her review)";
            task.description = "The food critic Mr. Grumbleton is here. Five dishes. The kitchen is falling apart.";
            task.boltsReward = 80;
            task.steps = new[]
            {
                new TaskStep { id = "cook", label = "Get a pot boiling" },
                new TaskStep { id = "whisk", label = "Whisk something" },
                new TaskStep { id = "orders_done", label = "Cook 5 dishes before he storms out" },
            };
            EditorUtility.SetDirty(task);

            var room = new GameObject("Kitchen").transform;
            Kitchen(room, "PASTA PANIC!", 24f, 18f);

            var board = Orders("OrderBoard", room, new Vector3(4f, 3.4f, 8.9f), 180f, "Mr. Grumbleton",
                Order(m.spaghetti, 75f, 4f),
                Order(m.sauce, 65f, 12f),
                Order(m.minestrone, 90f, 18f),
                Order(m.pesto, 65f, 20f),
                Order(m.spaghetti, 70f, 16f));
            board.maxActive = 3;
            board.maxStrikes = 3;

            var chaos = new GameObject("KitchenChaos").AddComponent<KitchenChaos>();
            chaos.transform.SetParent(room, false);
            chaos.board = board;

            // Two stoves: A is Tether-powered, B runs off a drone plate (wired), so PIP can run both.
            KitchenStove("Stove A", room, new Vector3(-7f, 0f, 8.25f), 180f, true, out _);
            var stoveB = KitchenStove("Stove B", room, new Vector3(-3f, 0f, 8.25f), 180f, false, out _);
            var wireB = Wire("Wire_StoveB", room, new Color(1f, 0.55f, 0.15f), new Vector3(-9.5f, 0, 3f), new Vector3(-9.5f, 0, 6.5f), new Vector3(-3f, 0, 6.5f), new Vector3(-3f, 0, 7.5f));
            Plate("Plate_StoveB", room, new Vector3(-9.5f, 0f, 3f), 1.5f, 1.2f, stoveB.gameObject, wireB.gameObject);
            Drone("Drone", room, new Vector3(-9.5f, 0.5f, -0.5f), new Color(0.55f, 0.8f, 1f));

            Sink("Sink", room, new Vector3(2f, 0f, 8.25f), 180f, true);
            Pot("Pot A", room, new Vector3(-7f, 0.31f, 5f), m.boiled, 15f);
            Pot("Pot B", room, new Vector3(-3f, 0.31f, 5f), m.boiled, 15f);

            var wood = Mat("Wood", new Color(0.8f, 0.6f, 0.42f));
            Table("PrepTable", room, new Vector3(2f, 0f, 1.5f), new Vector3(3.6f, 0f, 1.2f), 0.8f, wood);
            Bowl("Mixing bowl", room, new Vector3(1f, 1.02f, 1.5f), m.whisked);
            Bowl("Mixing bowl 2", room, new Vector3(3f, 1.02f, 1.5f), m.whisked);

            // Pantry belt along the south wall: crates drop ingredients on the west end, the belt carries them east.
            Belt("PantryBelt", room, new Vector3(-7f, 0.3f, -7.2f), 9f, 1.4f, 90f, 1.6f, true);
            var steel = Mat("Steel", new Color(0.78f, 0.8f, 0.85f), 0.7f);
            Box("BeltStop_W", room, new Vector3(-11.65f, 0.55f, -7.2f), new Vector3(0.3f, 0.5f, 1.6f), steel);
            Box("BeltStop_E", room, new Vector3(-2.35f, 0.55f, -7.2f), new Vector3(0.3f, 0.5f, 1.6f), steel);
            var ingredients = new[] { m.pasta, m.tomato, m.carrot, m.basil };
            for (int i = 0; i < ingredients.Length; i++)
            {
                float x = -11f + i * 1.5f;
                var d = Dispenser("Crate_" + ingredients[i].displayName, room, new Vector3(x, 0f, -8.45f), 0f, ingredients[i], new Vector3(x, 0.6f, -7.2f));
                d.maxAlive = 2;
                d.cooldown = 2.5f;
            }
            Label("PANTRY", room, new Vector3(-7f, 2.2f, -8.9f), 0f, 0.1f, new Color(0.45f, 0.3f, 0.15f));

            Hatch("ServingHatch", room, new Vector3(11.4f, 0f, -1f), -90f, board);
            Critic("Critic", room, new Vector3(10.3f, 0f, -6.5f), -90f, board);
            Trash("Trash", room, new Vector3(8f, 0f, 5f));

            var cam = BuildCamera();
            var bot = BuildBot(cam, new Vector3(0f, 0.05f, -4f), 0f, tools.grabber, tools.whisk);
            BuildSystems(task, Dispatch("Dispatch_Ch2_Boss", true), bot);
            FinishScene(scene, BossScene, bot);
        }

        // ------------------------------------------------------------------ shared

        static void Kitchen(Transform room, string title, float sizeX = 18f, float sizeZ = 14f)
        {
            RoomWithExit(room, sizeX, sizeZ, 5.5f,
                Mat("Floor_Cream", new Color(0.98f, 0.93f, 0.82f)),
                Mat("Wall_Mint", new Color(0.66f, 0.9f, 0.8f)),
                Mat("Trim_Coral", new Color(1f, 0.55f, 0.5f)),
                title, new Color(0.85f, 0.25f, 0.25f));
        }

        static Menu BuildMenu()
        {
            var m = new Menu
            {
                pasta = Ingredient("Pasta", "pasta", new Color(1f, 0.82f, 0.35f), new Color(1f, 0.95f, 0.72f)),
                tomato = Ingredient("Tomato", "tomato", new Color(0.95f, 0.22f, 0.18f), new Color(0.8f, 0.15f, 0.1f)),
                carrot = Ingredient("Carrot", "carrot", new Color(1f, 0.55f, 0.15f), new Color(0.95f, 0.5f, 0.2f)),
                basil = Ingredient("Basil", "basil", new Color(0.3f, 0.75f, 0.3f), new Color(0.35f, 0.65f, 0.3f)),
            };
            m.spaghetti = Recipe("Spaghetti", CookMethod.Boil, true, 8f, new Color(1f, 0.92f, 0.65f), m.pasta);
            m.minestrone = Recipe("Minestrone", CookMethod.Boil, true, 10f, new Color(0.95f, 0.5f, 0.3f), m.tomato, m.carrot);
            m.sauce = Recipe("Tomato Sauce", CookMethod.Whisk, false, 4f, new Color(0.85f, 0.15f, 0.1f), m.tomato);
            m.pesto = Recipe("Pesto", CookMethod.Whisk, false, 4f, new Color(0.35f, 0.7f, 0.3f), m.basil);
            m.boiled = new[] { m.spaghetti, m.minestrone };
            m.whisked = new[] { m.sauce, m.pesto };
            return m;
        }

        static IngredientDefinition Ingredient(string name, string id, Color raw, Color cooked)
        {
            var d = LoadOrCreate<IngredientDefinition>($"{DataDir}/Ingredient_{name}.asset");
            d.id = id;
            d.displayName = name;
            d.rawColor = raw;
            d.cookedColor = cooked;
            EditorUtility.SetDirty(d);
            return d;
        }

        static RecipeDefinition Recipe(string name, CookMethod method, bool water, float time, Color color, params IngredientDefinition[] ingredients)
        {
            var r = LoadOrCreate<RecipeDefinition>($"{DataDir}/Recipe_{name.Replace(" ", "")}.asset");
            r.dishName = name;
            r.method = method;
            r.needsWater = water;
            r.cookTime = time;
            r.dishColor = color;
            r.ingredients = ingredients;
            EditorUtility.SetDirty(r);
            return r;
        }

        static DispatchScript Dispatch(string asset, bool boss)
        {
            var d = LoadOrCreate<DispatchScript>($"{DataDir}/{asset}.asset");
            var lines = new System.Collections.Generic.List<NarrationLine>
            {
                L("tap_on", "Water's running. Pots go under the tap, PIP, not your head.", 3f),
                L("water_filled", "Pot filled!", 2f, true),
                L("ingredient_added", "In it goes!", 2f, true),
                L("wrong_ingredient", "That doesn't go in there. Check the ticket!", 3f, true),
                L("bowl_ready_to_whisk", "Set the bowl down and hold F (or B) to whisk it.", 4f, true),
                L("whisking", "Whisk whisk whisk!", 2f),
                L("whisked", "Smooth as silk. Off to the hatch!", 3f, true),
                L("tether_power", "Plugged in. Stove's on. You are the kitchen now.", 3f),
                L("boiling", "Bubbling! Keep that stove powered.", 3f, true),
                L("cooked", "Done! Get it out before it burns.", 3f, true),
                L("burnt", "It burnt! Bin it in the trash and start again.", 4f, true),
                L("trash", "Binned. Fresh start.", 2f, true),
                L("served_raw", "That's not cooked yet!", 3f, true),
                L("served_burnt", "Charcoal is not on the menu.", 3f, true),
                L("order_wrong", "Nobody ordered that. Check the tickets, top right.", 3f, true),
                L("order_new", "New ticket!", 2f, true),
                L("order_served", "Order up! Nicely done.", 2f, true),
                L("order_hero", "With seconds to spare! HERO MOMENT!", 3f, true),
                L("order_expired", "A ticket ran out. That's a strike.", 3f, true),
                L("fell", "Respawned. Back to the stove!", 3f, true),
            };
            if (boss)
            {
                lines.AddRange(new[]
                {
                    L("intro", "Code Red Sauce! Food critic Mr. Grumbleton just sat down. Five dishes, PIP. Three missed tickets and he writes the review. In ink.", 7f),
                    L("plate_too_light", "Stove B runs off that plate. It wants a Helper Drone on it.", 4f, true),
                    L("chaos_fuse", "The fuse blew! Every cable just popped out. Plug back in!", 4f, true),
                    L("chaos_rattle", "A pot's rattling off the stove! Grab it!", 3f, true),
                    L("chaos_belt", "The pantry belt's running backwards! Who designed this kitchen?!", 4f, true),
                    L("panic_failed", "Mr. Grumbleton has left the building. Backspace to try again, I'll stall the critics' guild.", 6f),
                    L("panic_won", "He... he SMILED. Nonna is crying. I'm crying. The exit's open!", 6f),
                    L("exit", "Chapter two complete! Nonna's getting five stars. You're getting a shiny new bolt.", 6f),
                });
            }
            else
            {
                lines.AddRange(new[]
                {
                    L("intro", "Nonna's whisking arm has retired. Grab the Whisk from its stand and help her through lunch.", 6f),
                    L("got_whisk", "Whisk equipped! Tab or Y swaps tools. Ingredients go in the bowl, then hold F or B.", 6f),
                    L("panic_won", "Every ticket cleared! Nonna says you can come back any time. She means it as a threat.", 6f),
                    L("panic_failed", "Too many tickets ran out. Backspace to try again!", 5f),
                    L("exit", "Call complete! But I'm hearing rumours of a food critic...", 5f),
                });
            }
            d.lines = lines.ToArray();
            EditorUtility.SetDirty(d);
            return d;
        }

        static NarrationLine L(string cue, string text, float duration = 4f, bool repeatable = false) =>
            new NarrationLine { cue = cue, text = text, duration = duration, repeatable = repeatable };
    }
}
