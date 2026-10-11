using RobotRescue.Puzzles;
using RobotRescue.Tasks.Cooking;
using RobotRescue.Tasks.Rescue;
using RobotRescue.Tools;
using UnityEditor;
using UnityEngine;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// Kitchen and rescue pieces for chamber builders: stoves, sinks, pots, bowls, ingredient dispensers, the serving
    /// hatch, trash, tool pedestals, the critic and the coffee machine. Each piece has a front (+Z when yaw = 0).
    /// </summary>
    public static partial class ChamberKit
    {
        public const string ToolDir = Root + "/Data/Tools";

        public struct ToolSet
        {
            public ToolDefinition grabber, whisk, screwdriver, magnet;
        }

        /// <summary>The four Tool Arm heads from the brief. Screwdriver and Magnet arrive in later chapters.</summary>
        public static ToolSet ToolKit()
        {
            EnsureFolder(ToolDir);
            return new ToolSet
            {
                grabber = Tool("Grabber", ToolKind.Grabber, new Color(1f, 0.55f, 0.15f), "grab"),
                whisk = Tool("Whisk", ToolKind.Whisk, new Color(0.55f, 0.85f, 1f), "whisk"),
                screwdriver = Tool("Screwdriver", ToolKind.Screwdriver, new Color(1f, 0.85f, 0.25f), "screw in"),
                magnet = Tool("Magnet", ToolKind.Magnet, new Color(1f, 0.35f, 0.35f), "magnetise"),
            };
        }

        static ToolDefinition Tool(string name, ToolKind kind, Color color, string verb)
        {
            var t = LoadOrCreate<ToolDefinition>($"{ToolDir}/Tool_{name}.asset");
            t.id = name.ToLowerInvariant();
            t.displayName = name;
            t.kind = kind;
            t.color = color;
            t.useVerb = verb;
            EditorUtility.SetDirty(t);
            return t;
        }

        // ------------------------------------------------------------------ helpers for rotated props

        static Transform PropRoot(string name, Transform parent, Vector3 floorPos, float yaw)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(floorPos, Quaternion.Euler(0f, yaw, 0f));
            return root;
        }

        /// <summary>Box placed in the root's local frame and rotated with it.</summary>
        static GameObject BoxL(string name, Transform root, Vector3 local, Vector3 size, Material mat, bool collider = true)
        {
            var go = Box(name, root, root.TransformPoint(local), size, mat, collider);
            go.transform.rotation = root.rotation;
            return go;
        }

        // ------------------------------------------------------------------ cooking stations

        /// <summary>Stove with a burner on top. With a socket, PIP powers it by Tether; otherwise list it as a powered object.</summary>
        public static Stove KitchenStove(string name, Transform parent, Vector3 floorPos, float yaw, bool withSocket, out PowerSocket socket)
        {
            var root = PropRoot(name, parent, floorPos, yaw);
            var stove = root.gameObject.AddComponent<Stove>();
            BoxL("Body", root, new Vector3(0, 0.5f, 0), new Vector3(1.6f, 1f, 1.4f), Mat("Stove_Red", new Color(0.92f, 0.3f, 0.3f)));
            stove.burnerVisual = BoxL("Burner", root, new Vector3(0, 1.02f, 0), new Vector3(1.1f, 0.04f, 0.9f), Mat("Burner", new Color(0.25f, 0.25f, 0.28f)), false).GetComponent<Renderer>();
            BoxL("Knob1", root, new Vector3(-0.45f, 0.8f, 0.72f), new Vector3(0.14f, 0.14f, 0.06f), Mat("Bot_Grey", new Color(0.5f, 0.52f, 0.58f)), false);
            BoxL("Knob2", root, new Vector3(0.45f, 0.8f, 0.72f), new Vector3(0.14f, 0.14f, 0.06f), Mat("Bot_Grey", new Color(0.5f, 0.52f, 0.58f)), false);
            var zone = BoxL("BurnerZone", root, new Vector3(0, 1.35f, 0), new Vector3(1.3f, 0.6f, 1.1f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            zone.AddComponent<BurnerZone>();

            socket = null;
            if (withSocket)
            {
                var s = BoxL("PowerSocket", root, new Vector3(0, 0.45f, 0.72f), new Vector3(0.35f, 0.35f, 0.06f), Mat("Socket", new Color(0.2f, 0.2f, 0.25f)));
                var led = BoxL("SocketLED", root, new Vector3(0, 0.72f, 0.74f), new Vector3(0.1f, 0.06f, 0.03f), Mat("LED", Color.gray), false);
                socket = s.AddComponent<PowerSocket>();
                socket.poweredObjects = new[] { root.gameObject };
                socket.indicator = led.GetComponent<Renderer>();
            }
            Label(name.ToUpperInvariant(), root, root.TransformPoint(new Vector3(0, 2f, 0)), yaw, 0.09f, new Color(0.6f, 0.15f, 0.15f));
            return stove;
        }

        /// <summary>Sink counter with a tap. The stream sits just in front; carry a pot through it to fill.</summary>
        public static WaterStream Sink(string name, Transform parent, Vector3 floorPos, float yaw, bool startsOn)
        {
            var root = PropRoot(name, parent, floorPos, yaw);
            var steel = Mat("Steel", new Color(0.78f, 0.8f, 0.85f), 0.7f);
            BoxL("Counter", root, new Vector3(0, 0.45f, 0), new Vector3(2.2f, 0.9f, 1.4f), Mat("Sink_Teal", new Color(0.4f, 0.75f, 0.8f)));
            BoxL("FaucetPipe", root, new Vector3(0, 1.6f, -0.6f), new Vector3(0.15f, 1.4f, 0.15f), steel, false);
            BoxL("FaucetSpout", root, new Vector3(0, 2.25f, -0.15f), new Vector3(0.15f, 0.15f, 0.9f), steel, false);
            var zone = BoxL("WaterStream", root, new Vector3(0, 1.5f, 0.4f), new Vector3(1f, 1.6f, 1f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var stream = zone.AddComponent<WaterStream>();
            stream.startsOn = startsOn;
            stream.streamVisual = Cylinder("StreamVisual", zone.transform, root.TransformPoint(new Vector3(0, 1.55f, 0.28f)), 0.15f, 1.3f, Mat("Water", new Color(0.5f, 0.8f, 1f), 0.9f));
            Label("SINK", root, root.TransformPoint(new Vector3(0, 2.9f, -0.6f)), yaw, 0.1f, new Color(0.15f, 0.4f, 0.5f));
            return stream;
        }

        public static CookingPot Pot(string name, Transform parent, Vector3 pos, RecipeDefinition[] recipes, float burnAfter)
        {
            var pot = new GameObject(name);
            pot.transform.SetParent(parent, false);
            pot.transform.position = pos;
            pot.AddComponent<BoxCollider>().size = new Vector3(0.8f, 0.6f, 0.8f);
            pot.AddComponent<Rigidbody>().mass = 1.2f;

            var steel = Mat("Pot_Steel", new Color(0.62f, 0.68f, 0.78f), 0.6f);
            Cylinder("Body", pot.transform, pos, 0.4f, 0.6f, steel);
            Box("HandleL", pot.transform, pos + new Vector3(-0.47f, 0.2f, 0), new Vector3(0.12f, 0.06f, 0.2f), steel, false);
            Box("HandleR", pot.transform, pos + new Vector3(0.47f, 0.2f, 0), new Vector3(0.12f, 0.06f, 0.2f), steel, false);
            var water = Cylinder("Water", pot.transform, pos + Vector3.up * 0.28f, 0.36f, 0.02f, Mat("PotWater", Color.cyan));
            var food = Cylinder("Contents", pot.transform, pos + Vector3.up * 0.3f, 0.28f, 0.04f, Mat("PotFood", Color.yellow));
            Mouth(pot.transform, 0.45f);

            pot.AddComponent<Carryable>().displayName = "Pot";
            var cp = pot.AddComponent<CookingPot>();
            cp.recipes = recipes;
            cp.burnAfter = burnAfter;
            cp.waterVisual = water.GetComponent<Renderer>();
            cp.contentsVisual = food.GetComponent<Renderer>();
            pot.AddComponent<Respawnable>();
            return cp;
        }

        public static MixingBowl Bowl(string name, Transform parent, Vector3 pos, RecipeDefinition[] recipes)
        {
            var bowl = new GameObject(name);
            bowl.transform.SetParent(parent, false);
            bowl.transform.position = pos;
            var col = bowl.AddComponent<BoxCollider>();
            col.size = new Vector3(0.8f, 0.36f, 0.8f);
            col.center = new Vector3(0, 0.02f, 0);
            bowl.AddComponent<Rigidbody>().mass = 0.9f;

            Sphere("Body", bowl.transform, pos + Vector3.down * 0.02f, 0.42f, Mat("Bowl_Blue", new Color(0.45f, 0.65f, 1f), 0.5f), new Vector3(1f, 0.5f, 1f));
            var food = Cylinder("Contents", bowl.transform, pos + Vector3.up * 0.17f, 0.3f, 0.03f, Mat("BowlFood", Color.red));
            var swirl = Box("Swirl", bowl.transform, pos + Vector3.up * 0.2f, new Vector3(0.45f, 0.02f, 0.08f), Mat("Swirl_White", new Color(1f, 1f, 1f, 1f)), false);
            Mouth(bowl.transform, 0.35f);

            bowl.AddComponent<Carryable>().displayName = "Mixing bowl";
            var mb = bowl.AddComponent<MixingBowl>();
            mb.recipes = recipes;
            mb.contentsVisual = food.GetComponent<Renderer>();
            mb.swirl = swirl.transform;
            bowl.AddComponent<Respawnable>();
            return mb;
        }

        static void Mouth(Transform owner, float height)
        {
            var mouth = new GameObject("Mouth");
            mouth.transform.SetParent(owner, false);
            mouth.transform.localPosition = new Vector3(0, height, 0);
            var mc = mouth.AddComponent<BoxCollider>();
            mc.isTrigger = true;
            mc.size = new Vector3(0.9f, 0.4f, 0.9f);
            mouth.AddComponent<PotMouth>();
        }

        /// <summary>Hand-placed ingredient (or, inactive, a dispenser's template).</summary>
        public static Ingredient IngredientProp(IngredientDefinition def, Transform parent, Vector3 pos)
        {
            var go = Box(def.displayName, parent, pos, new Vector3(0.38f, 0.38f, 0.38f), Mat("Ingredient_" + def.id, def.rawColor));
            go.AddComponent<Rigidbody>().mass = 0.3f;
            var carry = go.AddComponent<Carryable>();
            carry.displayName = def.displayName;
            var ing = go.AddComponent<Ingredient>();
            ing.definition = def;
            return ing;
        }

        /// <summary>Endless crate of one ingredient. A fresh one appears at <paramref name="spawnPos"/> whenever it's taken.</summary>
        public static IngredientDispenser Dispenser(string name, Transform parent, Vector3 floorPos, float yaw, IngredientDefinition def, Vector3 spawnPos)
        {
            var root = PropRoot(name, parent, floorPos, yaw);
            BoxL("Crate", root, new Vector3(0, 0.4f, 0), new Vector3(1f, 0.8f, 1f), Mat("Crate_Wood", new Color(0.78f, 0.58f, 0.38f)));
            BoxL("Stripe", root, new Vector3(0, 0.55f, 0.51f), new Vector3(0.8f, 0.2f, 0.02f), Mat("Ingredient_" + def.id, def.rawColor), false);
            Label(def.displayName.ToUpperInvariant(), root, root.TransformPoint(new Vector3(0, 0.55f, 0.53f)), yaw, 0.05f, new Color(0.15f, 0.1f, 0.05f));

            var spawn = new GameObject("SpawnPoint").transform;
            spawn.SetParent(root, false);
            spawn.position = spawnPos;

            var template = IngredientProp(def, root, spawnPos);
            template.name = def.displayName;
            template.gameObject.SetActive(false);

            var d = root.gameObject.AddComponent<IngredientDispenser>();
            d.template = template.gameObject;
            d.spawnPoint = spawn;
            return d;
        }

        public static ServingHatch Hatch(string name, Transform parent, Vector3 floorPos, float yaw, OrderBoard board)
        {
            var root = PropRoot(name, parent, floorPos, yaw);
            var wood = Mat("Wood", new Color(0.8f, 0.6f, 0.42f));
            BoxL("HatchCounter", root, new Vector3(0, 0.5f, 0), new Vector3(2f, 1f, 1.2f), wood);
            BoxL("Awning", root, new Vector3(0, 2.6f, -0.1f), new Vector3(2.4f, 0.12f, 1.2f), Mat("Awning_Red", new Color(0.95f, 0.35f, 0.35f)), false);
            BoxL("AwningPostL", root, new Vector3(-1.1f, 1.8f, -0.5f), new Vector3(0.1f, 1.6f, 0.1f), wood, false);
            BoxL("AwningPostR", root, new Vector3(1.1f, 1.8f, -0.5f), new Vector3(0.1f, 1.6f, 0.1f), wood, false);
            var zone = BoxL("HatchTrigger", root, new Vector3(0, 1.5f, 0.3f), new Vector3(2f, 1.2f, 1.6f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var hatch = zone.AddComponent<ServingHatch>();
            hatch.orderBoard = board;
            Label("SERVE", root, root.TransformPoint(new Vector3(0, 3.1f, -0.1f)), yaw, 0.12f, new Color(0.6f, 0.2f, 0.2f));
            return hatch;
        }

        public static TrashBin Trash(string name, Transform parent, Vector3 floorPos)
        {
            var root = PropRoot(name, parent, floorPos, 0f);
            var green = Mat("Bin_Green", new Color(0.35f, 0.6f, 0.4f));
            Box("Base", root, floorPos + Vector3.up * 0.05f, new Vector3(1.1f, 0.1f, 1.1f), green);
            foreach (var (x, z, sx, sz) in new[] { (-0.5f, 0f, 0.1f, 1.1f), (0.5f, 0f, 0.1f, 1.1f), (0f, -0.5f, 1.1f, 0.1f), (0f, 0.5f, 1.1f, 0.1f) })
                Box("Side", root, floorPos + new Vector3(x, 0.45f, z), new Vector3(sx, 0.8f, sz), green);
            var zone = Box("BinTrigger", root, floorPos + Vector3.up * 0.5f, new Vector3(0.9f, 0.8f, 0.9f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var bin = zone.AddComponent<TrashBin>();
            Label("TRASH", root, floorPos + Vector3.up * 1.2f, 180f, 0.07f, new Color(0.2f, 0.4f, 0.25f));
            return bin;
        }

        /// <summary>Spinning tool on a pedestal; walk into it to unlock it.</summary>
        public static ToolPickup ToolPedestal(string name, Transform parent, Vector3 floorPos, ToolDefinition tool, string stepId, string cue)
        {
            var root = PropRoot(name, parent, floorPos, 0f);
            Cylinder("Pedestal", root, floorPos + Vector3.up * 0.3f, 0.45f, 0.6f, Mat("Pedestal", new Color(0.92f, 0.92f, 0.98f)));
            Box("PedestalCollider", root, floorPos + Vector3.up * 0.3f, new Vector3(0.8f, 0.6f, 0.8f), null);
            var visual = new GameObject("ToolVisual").transform;
            visual.SetParent(root, false);
            visual.position = floorPos + Vector3.up * 1.2f;
            var toolMat = Mat("Tool_" + tool.displayName, tool.color, 0.6f);
            Box("Handle", visual, visual.position + Vector3.down * 0.15f, new Vector3(0.08f, 0.4f, 0.08f), Mat("Bot_Grey", new Color(0.5f, 0.52f, 0.58f)), false);
            for (int i = 0; i < 3; i++)
            {
                var loop = Box("Loop" + i, visual, visual.position + Vector3.up * 0.15f, new Vector3(0.32f, 0.42f, 0.03f), toolMat, false);
                loop.transform.rotation = Quaternion.Euler(0f, i * 60f, 0f);
            }
            var zone = Box("PickupTrigger", root, floorPos + Vector3.up * 1f, new Vector3(1.2f, 2f, 1.2f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var pickup = zone.AddComponent<ToolPickup>();
            pickup.tool = tool;
            pickup.visual = visual;
            pickup.stepId = stepId;
            pickup.cue = cue;
            Label(tool.displayName.ToUpperInvariant(), root, floorPos + Vector3.up * 1.9f, 180f, 0.08f, tool.color * 0.7f);
            return pickup;
        }

        public static OrderBoard Orders(string name, Transform parent, Vector3 signPos, float yaw, string critic, params OrderSpec[] orders)
        {
            var root = PropRoot(name, parent, signPos, yaw);
            BoxL("Board", root, Vector3.zero, new Vector3(2.4f, 1.2f, 0.08f), Mat("Chalkboard", new Color(0.15f, 0.22f, 0.2f)), false);
            Label("ORDERS", root, root.TransformPoint(new Vector3(0, 0.3f, 0.06f)), yaw, 0.09f, Color.white);
            var board = root.gameObject.AddComponent<OrderBoard>();
            board.criticName = critic;
            board.orders = orders;
            return board;
        }

        public static OrderSpec Order(RecipeDefinition recipe, float patience, float delayBefore) =>
            new OrderSpec { recipe = recipe, patience = patience, delayBefore = delayBefore };

        /// <summary>A grumpy food critic at his table. His face reddens as orders expire.</summary>
        public static CriticNPC Critic(string name, Transform parent, Vector3 floorPos, float yaw, OrderBoard board)
        {
            var root = PropRoot(name, parent, floorPos, yaw);
            var wood = Mat("Wood", new Color(0.8f, 0.6f, 0.42f));
            BoxL("Table", root, new Vector3(0, 0.4f, 0.9f), new Vector3(1.4f, 0.8f, 0.9f), wood);
            Cylinder("Plate", root, root.TransformPoint(new Vector3(0, 0.82f, 0.8f)), 0.25f, 0.03f, Mat("Plate_White", Color.white));

            var body = new GameObject("Grumbleton").transform;
            body.SetParent(root, false);
            body.localPosition = Vector3.zero;
            var suit = Mat("Critic_Suit", new Color(0.25f, 0.22f, 0.35f));
            BoxL("Torso", body, new Vector3(0, 0.9f, 0), new Vector3(0.8f, 1f, 0.5f), suit);
            var head = Sphere("Head", body, body.TransformPoint(new Vector3(0, 1.65f, 0)), 0.3f, Mat("Critic_Face", new Color(1f, 0.85f, 0.72f)));
            BoxL("Moustache", body, new Vector3(0, 1.58f, 0.28f), new Vector3(0.32f, 0.06f, 0.05f), Mat("Critic_Hair", new Color(0.3f, 0.2f, 0.15f)), false);
            BoxL("HatBrim", body, new Vector3(0, 1.93f, 0), new Vector3(0.6f, 0.04f, 0.6f), suit, false);
            BoxL("HatTop", body, new Vector3(0, 2.1f, 0), new Vector3(0.38f, 0.32f, 0.38f), suit, false);
            BoxL("Monocle", body, new Vector3(0.11f, 1.7f, 0.29f), new Vector3(0.1f, 0.1f, 0.02f), Mat("Hook_Yellow", new Color(1f, 0.85f, 0.2f)), false);

            var critic = body.gameObject.AddComponent<CriticNPC>();
            critic.board = board;
            critic.face = head.GetComponent<Renderer>();
            Label("MR. GRUMBLETON", root, root.TransformPoint(new Vector3(0, 2.6f, 0)), yaw, 0.07f, new Color(0.35f, 0.2f, 0.45f));
            return critic;
        }

        // ------------------------------------------------------------------ Chapter 1 boss

        /// <summary>DISPATCH's coffee machine: a big shaking urn with a gauge and warning light.</summary>
        public static CoffeeMachine Coffee(string name, Transform parent, Vector3 floorPos)
        {
            var root = PropRoot(name, parent, floorPos, 0f);
            Box("Plinth", root, floorPos + Vector3.up * 0.3f, new Vector3(3f, 0.6f, 2.4f), Mat("Steel", new Color(0.78f, 0.8f, 0.85f), 0.7f));

            var body = new GameObject("Body").transform;
            body.SetParent(root, false);
            body.position = floorPos + Vector3.up * 0.6f;
            var chrome = Mat("Coffee_Chrome", new Color(0.85f, 0.35f, 0.3f), 0.85f);
            Cylinder("Urn", body, floorPos + Vector3.up * 2f, 1f, 2.8f, chrome);
            Sphere("Dome", body, floorPos + Vector3.up * 3.4f, 1f, chrome, new Vector3(1f, 0.6f, 1f));
            // Front faces -Z (toward the player's side of the room).
            Box("Spout", body, floorPos + new Vector3(0, 1.3f, -1.1f), new Vector3(0.2f, 0.2f, 0.5f), Mat("Steel", new Color(0.78f, 0.8f, 0.85f), 0.7f), false);
            Cylinder("Mug", body, floorPos + new Vector3(0, 0.82f, -1.25f), 0.2f, 0.4f, Mat("Mug_White", Color.white));
            Box("GaugeFace", body, floorPos + new Vector3(0, 2.4f, -1.02f), new Vector3(0.8f, 0.8f, 0.04f), Mat("Plate_White", Color.white), false);
            var pivot = new GameObject("GaugeNeedle").transform;
            pivot.SetParent(body, false);
            pivot.SetPositionAndRotation(floorPos + new Vector3(0, 2.4f, -1.05f), Quaternion.Euler(0f, 180f, 0f));
            Box("Needle", pivot, pivot.position + Vector3.up * 0.18f, new Vector3(0.04f, 0.36f, 0.02f), Mat("Needle", new Color(0.9f, 0.15f, 0.1f)), false);
            var light = Sphere("WarningLight", body, floorPos + Vector3.up * 4.05f, 0.22f, Mat("WarningLight", new Color(0.35f, 0.3f, 0.3f)));
            Label("DISPATCH'S COFFEE", root, floorPos + new Vector3(0, 1.6f, -1.03f), 180f, 0.06f, Color.white);

            var machine = root.gameObject.AddComponent<CoffeeMachine>();
            machine.body = body;
            machine.gaugeNeedle = pivot;
            machine.warningLight = light.GetComponent<Renderer>();
            return machine;
        }

        /// <summary>A vent pipe running from the machine; steam puffs out of its cap while powered.</summary>
        public static SteamVent Vent(string name, Transform parent, CoffeeMachine machine, int index, Vector3 capPos, Color color)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var pipe = Mat("Pipe_" + ColorUtility.ToHtmlStringRGB(color), color);
            Box("Pipe", root, new Vector3(capPos.x, capPos.y * 0.5f, capPos.z), new Vector3(0.3f, capPos.y, 0.3f), pipe);
            Box("Cap", root, capPos + Vector3.up * 0.1f, new Vector3(0.5f, 0.2f, 0.5f), pipe, false);
            var steam = Sphere("Steam", root, capPos + Vector3.up * 0.7f, 0.4f, Mat("Steam", new Color(0.95f, 0.97f, 1f), 0.1f));
            var vent = root.gameObject.AddComponent<SteamVent>();
            vent.machine = machine;
            vent.index = index;
            vent.steam = steam.transform;
            return vent;
        }
    }
}
