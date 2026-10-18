using System.Collections.Generic;
using RobotRescue.Bots;
using RobotRescue.Campaign;
using RobotRescue.Core;
using RobotRescue.Narrative;
using RobotRescue.Puzzles;
using RobotRescue.Tasks;
using RobotRescue.UI;
using RobotRescue.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using static RobotRescue.EditorTools.ChamberKit;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// Menu: Robot Rescue > Build Open World. Stitches every level scene into one continuous Brightwater hillside
    /// (World.unity): each call becomes a building with a front gate and a porch, joined to the next by a walkway
    /// that climbs the hill. Later chapters are visible up the slope and along the shore, locked for now.
    /// Run Build Everything first (it calls this at the end), so the level scenes are fresh.
    /// </summary>
    public static partial class WorldBuilder
    {
        enum Style { Station, Ocean }

        /// <summary>A place the Bureau sends PIP. Each is its own seamless scene; mission pods fly between them.</summary>
        class Destination
        {
            public string name, title, scene;
            public Style style;
            public string[] levels;
        }

        static readonly Destination[] Destinations =
        {
            new Destination { name = "Station", title = "Bureau Station", scene = Root + "/Scenes/World_Station.unity", style = Style.Station,
                levels = new[] { "Test01_StackEmUp", "Test02_ConveyorCaper", "Test03_SwingShift", "Ch1_Boss_CoffeeCrisis" } },
            new Destination { name = "Brightwater", title = "Brightwater Reach", scene = Root + "/Scenes/World_Brightwater.unity", style = Style.Ocean,
                levels = new[] { "Ch2_Call01_PastaEmergency", "Ch2_Call02_WhiskBusiness", "Ch2_Boss_PastaPanic", "Ch3_Call01_LooseScrews", "Ch3_Boss_UpATree",
                    "Ch4_Call01_CatchTheHum", "Ch4_Boss_SlightlyTooBig",
                    "Ch5_Call01_HeavyLifting", "Ch5_Boss_LightsOut",
                    "Ch6_Call01_CarouselChaos", "Ch6_Boss_StuckAtTheTop",
                    "Ch7_Call01_GoingDown", "Ch7_Boss_UnitZero",
                    "Ch8_Call01_StageFright", "Ch8_Boss_TheFoundersSong" } },
        };

        /// <summary>The Bureau Station: the game boots here (title screen, training, first missions).</summary>
        public static string ScenePath => Destinations[0].scene;
        public static IEnumerable<string> ScenePaths { get { foreach (var d in Destinations) yield return d.scene; } }
        static string MeshPath => Root + "/Art/Meshes/World_" + current.name + ".asset";
        static Destination current;

        /// <summary>Campaign indices of the calls at a destination (first, last), or (-1, -1).</summary>
        static (int first, int last) LevelRange(Destination d)
        {
            int index = 0, first = -1, last = -1;
            foreach (var chapter in ProjectBuilder.Chapters)
                foreach (var l in chapter.levels)
                {
                    if (System.Array.IndexOf(d.levels, l.Scene) >= 0) { if (first < 0) first = index; last = index; }
                    index++;
                }
            return (first, last);
        }

        const float SeaY = -6f;
        const float Rise = 2.5f;
        const float IslandRadius = 200f;
        const float PathHalfWidth = 1.7f;
        static readonly Vector2 IslandCenter = new Vector2(0f, 20f);

        /// <summary>How a saved level is opened up: where its front gate is cut, and where its exit leads out.</summary>
        class Spec
        {
            public string scene;
            public string wall;          // wall that gets the front gate
            public float along;          // gate centre along that wall (level coordinates)
            public Vector3 normal;       // outward direction of that wall
            public string exitWall;      // optional: wall to open at the exit
            public float exitAlong, exitBottom;
            public Vector3 outlet;       // where PIP steps out after the exit flag (used when exitWall is null)
            public Vector3 outletDir;
            public string[] remove = new string[0];
            public float? killTop;       // pits: top of the area's kill zone
        }

        static readonly Spec[] Specs =
        {
            new Spec { scene = "Test01_StackEmUp", wall = "Wall_S", along = 0f, normal = Vector3.back,
                exitWall = "Wall_N", exitAlong = 0f, exitBottom = 3f, outletDir = Vector3.forward },
            new Spec { scene = "Test02_ConveyorCaper", wall = "Wall_W", along = -3f, normal = Vector3.left,
                outlet = new Vector3(-4f, 0f, -13.5f), outletDir = Vector3.back },
            new Spec { scene = "Test03_SwingShift", wall = "Wall_S", along = 0f, normal = Vector3.back,
                exitWall = "Wall_N", exitAlong = 0f, exitBottom = 0f, outletDir = Vector3.forward, killTop = -3f },
            new Spec { scene = "Ch1_Boss_CoffeeCrisis", wall = "Wall_W", along = -5.5f, normal = Vector3.left,
                outlet = new Vector3(0f, 0f, -14.75f), outletDir = Vector3.back, remove = new[] { "Corridor_End" } },
            new Spec { scene = "Ch2_Call01_PastaEmergency", wall = "Wall_E", along = -3.8f, normal = Vector3.right,
                outlet = new Vector3(0f, 0f, -18f), outletDir = Vector3.back, killTop = -2f },
            new Spec { scene = "Ch2_Call02_WhiskBusiness", wall = "Wall_W", along = -4.5f, normal = Vector3.left,
                outlet = new Vector3(0f, 0f, -13.75f), outletDir = Vector3.back, remove = new[] { "Corridor_End" } },
            new Spec { scene = "Ch2_Boss_PastaPanic", wall = "Wall_W", along = -3.5f, normal = Vector3.left,
                outlet = new Vector3(0f, 0f, -15.75f), outletDir = Vector3.back, remove = new[] { "Corridor_End" } },
            new Spec { scene = "Ch3_Call01_LooseScrews", wall = "Wall_E", along = 4f, normal = Vector3.right,
                outlet = new Vector3(0f, 0f, -15.75f), outletDir = Vector3.back, remove = new[] { "Corridor_End" }, killTop = -2f },
            new Spec { scene = "Ch3_Boss_UpATree", wall = "Wall_E", along = -5.5f, normal = Vector3.right,
                outlet = new Vector3(0f, 0f, -17.75f), outletDir = Vector3.back, remove = new[] { "Corridor_End" } },
            new Spec { scene = "Ch4_Call01_CatchTheHum", wall = "Wall_W", along = -4f, normal = Vector3.left,
                outlet = new Vector3(0f, 0f, -15.75f), outletDir = Vector3.back, remove = new[] { "Corridor_End" } },
            new Spec { scene = "Ch4_Boss_SlightlyTooBig", wall = "Wall_W", along = -6f, normal = Vector3.left,
                outlet = new Vector3(0f, 0f, -16.75f), outletDir = Vector3.back, remove = new[] { "Corridor_End" } },
            new Spec { scene = "Ch5_Call01_HeavyLifting", wall = "Wall_W", along = -4f, normal = Vector3.left,
                outlet = new Vector3(0f, 0f, -15.75f), outletDir = Vector3.back, remove = new[] { "Corridor_End" }, killTop = -2f },
            new Spec { scene = "Ch5_Boss_LightsOut", wall = "Wall_W", along = -7f, normal = Vector3.left,
                outlet = new Vector3(0f, 0f, -17.75f), outletDir = Vector3.back, remove = new[] { "Corridor_End" }, killTop = -2f },
            new Spec { scene = "Ch6_Call01_CarouselChaos", wall = "Wall_E", along = -3f, normal = Vector3.right,
                outlet = new Vector3(0f, 0f, -16.75f), outletDir = Vector3.back, remove = new[] { "Corridor_End" }, killTop = -2f },
            new Spec { scene = "Ch6_Boss_StuckAtTheTop", wall = "Wall_E", along = -6f, normal = Vector3.right,
                outlet = new Vector3(0f, 0f, -18.75f), outletDir = Vector3.back, remove = new[] { "Corridor_End" }, killTop = -2f },
            new Spec { scene = "Ch7_Call01_GoingDown", wall = "Wall_W", along = -7f, normal = Vector3.left,
                outlet = new Vector3(0f, 0f, -17.75f), outletDir = Vector3.back, remove = new[] { "Corridor_End" }, killTop = -2f },
            new Spec { scene = "Ch7_Boss_UnitZero", wall = "Wall_W", along = -6f, normal = Vector3.left,
                outlet = new Vector3(0f, 0f, -16.75f), outletDir = Vector3.back, remove = new[] { "Corridor_End" }, killTop = -2f },
            new Spec { scene = "Ch8_Call01_StageFright", wall = "Wall_E", along = -3f, normal = Vector3.right,
                outlet = new Vector3(0f, 0f, -16.75f), outletDir = Vector3.back, remove = new[] { "Corridor_End" } },
            new Spec { scene = "Ch8_Boss_TheFoundersSong", wall = "Wall_E", along = -6f, normal = Vector3.right,
                outlet = new Vector3(0f, 0f, -18.75f), outletDir = Vector3.back, remove = new[] { "Corridor_End" } },
        };

        static readonly HashSet<string> FloorNames = new HashSet<string> { "Floor", "StartFloor", "FarFloor", "Island", "Corridor", "Landing" };
        static readonly HashSet<string> Excluded = new HashSet<string> { "Sun", "PostFX", "Main Camera", "PIP", "HomeCharger" };

        /// <summary>A level placed on the hill. Everything is built in level coordinates, then the root is moved.</summary>
        class Placed
        {
            public Spec spec;
            public ProjectBuilder.Level level;
            public string chapter;
            public int index;
            public Transform root;
            public Area area;
            public Vector3 entrance, normal, outlet, outletDir;   // level coordinates
            public float porchDepth, porchWidth;
            public Bounds footprint;                             // level coordinates, walls + porch
            public readonly List<Bounds> floors = new List<Bounds>();
            public float yaw;
            public Vector3 origin;

            public Quaternion Rot => Quaternion.Euler(0f, yaw, 0f);
            public Vector3 W(Vector3 local) => origin + Rot * local;
            public Vector3 Dir(Vector3 local) => Rot * local;
            public Rect2 WorldRect(float grow = 0f) => Rect2.Of(this, footprint, grow);
            public Bounds WorldBounds(Bounds b)
            {
                var r = Rect2.Of(this, b, 0f);
                float y0 = origin.y + b.min.y, y1 = origin.y + b.max.y;
                return new Bounds(new Vector3((r.x0 + r.x1) * 0.5f, (y0 + y1) * 0.5f, (r.z0 + r.z1) * 0.5f), new Vector3(r.x1 - r.x0, y1 - y0, r.z1 - r.z0));
            }
        }

        struct Rect2
        {
            public float x0, z0, x1, z1;
            public Vector2 Center => new Vector2((x0 + x1) * 0.5f, (z0 + z1) * 0.5f);

            public static Rect2 Of(Placed p, Bounds b, float grow)
            {
                var r = new Rect2 { x0 = float.MaxValue, z0 = float.MaxValue, x1 = float.MinValue, z1 = float.MinValue };
                foreach (var c in new[] { new Vector3(b.min.x, 0, b.min.z), new Vector3(b.max.x, 0, b.min.z), new Vector3(b.min.x, 0, b.max.z), new Vector3(b.max.x, 0, b.max.z) })
                {
                    var w = p.W(c);
                    r.x0 = Mathf.Min(r.x0, w.x); r.x1 = Mathf.Max(r.x1, w.x);
                    r.z0 = Mathf.Min(r.z0, w.z); r.z1 = Mathf.Max(r.z1, w.z);
                }
                return r.Grow(grow);
            }

            public static Rect2 Around(Vector3 c, float half) => new Rect2 { x0 = c.x - half, x1 = c.x + half, z0 = c.z - half, z1 = c.z + half };
            public Rect2 Grow(float g) => new Rect2 { x0 = x0 - g, z0 = z0 - g, x1 = x1 + g, z1 = z1 + g };
            public bool Overlaps(Rect2 o) => x0 < o.x1 && o.x0 < x1 && z0 < o.z1 && o.z0 < z1;
            public bool Contains(Vector3 p) => p.x > x0 && p.x < x1 && p.z > z0 && p.z < z1;

            /// <summary>Horizontal distance from a point to the rectangle (0 inside).</summary>
            public float Distance(float x, float z)
            {
                float dx = Mathf.Max(x0 - x, 0f, x - x1), dz = Mathf.Max(z0 - z, 0f, z - z1);
                return Mathf.Sqrt(dx * dx + dz * dz);
            }
        }

        /// <summary>A walkway: centreline samples (world space) from one doorway to the next porch.</summary>
        class Walkway
        {
            public readonly List<Vector3> points = new List<Vector3>();
            public bool fenced = true;
        }

        static readonly List<Mesh> meshes = new List<Mesh>();
        static readonly List<Placed> placed = new List<Placed>();
        static readonly List<Walkway> walkways = new List<Walkway>();
        static readonly List<Rect2> blocked = new List<Rect2>();      // footprints of everything solid (areas, plazas, landmarks)
        static readonly List<(Rect2 rect, float limit)> carve = new List<(Rect2, float)>(); // terrain must stay below these
        static readonly List<Vector4> anchors = new List<Vector4>(); // (x, z, height, weight) the hill bends toward
        static System.Random rng;

        // ================================================================== entry point

        [MenuItem("Robot Rescue/Build Open World", priority = 2)]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build();
        }

        public static void Build()
        {
            foreach (var chapter in ProjectBuilder.Chapters)
                foreach (var l in chapter.levels)
                    if (!System.IO.File.Exists(l.path)) { Debug.LogError($"[World] Missing {l.path}. Run Build Everything first."); return; }
            foreach (var d in Destinations) BuildDestination(d);
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        static void BuildDestination(Destination dest)
        {
            current = dest;
            style = dest.style;
            meshes.Clear(); placed.Clear(); walkways.Clear(); blocked.Clear(); carve.Clear(); anchors.Clear();
            rng = new System.Random(20261011 + dest.name.Length);
            ResetCache();
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(Root + "/Art/Meshes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLighting();
            if (style == Style.Ocean)
            {
                RenderSettings.fogStartDistance = 90f;
                RenderSettings.fogEndDistance = 560f;
            }
            else SpaceLighting();

            // 1. Bring this destination's levels in and open them up (gate, exit, porch).
            int index = 0;
            foreach (var chapter in ProjectBuilder.Chapters)
                foreach (var level in chapter.levels)
                {
                    int i = index++;
                    if (System.Array.IndexOf(dest.levels, level.Scene) < 0) continue;
                    var spec = System.Array.Find(Specs, s => s.scene == level.Scene);
                    if (spec == null) { Debug.LogError($"[World] No world spec for {level.Scene}"); continue; }
                    placed.Add(Import(scene, spec, level, chapter.title, i, placed.Count == 0));
                }
            if (placed.Count == 0) return;

            // 2. Lay them out, each a little higher than the last, joined by bridges.
            Layout();
            foreach (var p in placed)
            {
                p.root.SetPositionAndRotation(p.origin, p.Rot);
                blocked.Add(p.WorldRect());
            }
            var lookout = BuildLookout(dest);

            // 3. Landmarks for what's still to come.
            var landmarkSpots = PlanLandmarks(lookout);
            float floorY = float.MaxValue;
            foreach (var p in placed) floorY = Mathf.Min(floorY, p.origin.y);
            var ground = new HeightField { level = style == Style.Ocean ? SeaY - 4f : floorY - 30f };

            // 4. Decks, bridges, surroundings.
            var worldRoot = new GameObject(dest.title).transform;
            foreach (var p in placed) Foundations(p, ground, worldRoot);
            Foundation("Lookout_Deck", worldRoot, lookout.bounds, ground);
            for (int i = 0; i < walkways.Count; i++) BuildWalkway("Bridge_" + i, walkways[i], ground, worldRoot);
            if (style == Style.Ocean) BuildSea(worldRoot); else BuildSpace(worldRoot, floorY);
            BuildLandmarks(landmarkSpots, ground, worldRoot);
            Scenery(ground, worldRoot);

            // 5. Arrival by pod, PIP, cameras, systems.
            Transform podSpawn = lookout.podSpawn;
            if (style == Style.Ocean)
            {
                var first = placed[0];
                Vector3 n = first.Dir(first.normal), side = Vector3.Cross(Vector3.up, n);
                Pod("ReturnPod_Arrival", first.root, first.W(first.entrance) + n * 4.8f - side * 3.8f, -n, "BACK TO STATION",
                    System.IO.Path.GetFileNameWithoutExtension(Destinations[0].scene), -1, -1, "Bureau Station");
                podSpawn = first.area.spawnPoint;
            }
            var director = BuildSystems(dest, podSpawn);

            SaveMeshes();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, dest.scene);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = director.gameObject;
            Debug.Log($"[Robot Rescue] Built {dest.title} with {placed.Count} calls: {dest.scene}");
        }

        /// <summary>Deep-blue starry sky, no fog, cool light: up in orbit.</summary>
        static void SpaceLighting()
        {
            var shader = Shader.Find(SkyShader);
            if (Usable(shader))
            {
                var m = ShaderMat("Sky_Space", shader);
                m.SetColor("_TopColor", new Color(0.02f, 0.02f, 0.07f));
                m.SetColor("_HorizonColor", new Color(0.12f, 0.12f, 0.3f));
                m.SetColor("_BottomColor", new Color(0.05f, 0.08f, 0.2f));
                m.SetFloat("_CloudCover", 0f);
                m.SetFloat("_SunSize", 0.03f);
                RenderSettings.skybox = m;
            }
            RenderSettings.fog = false;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.6f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.6f, 0.6f, 0.78f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.4f, 0.6f);
        }

        // ================================================================== import a level

        static Placed Import(Scene world, Spec spec, ProjectBuilder.Level level, string chapter, int index, bool home)
        {
            var levelScene = EditorSceneManager.OpenScene(level.path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(world);
            var root = new GameObject($"Area_{index + 1:00}_{level.Scene}").transform;
            foreach (var go in levelScene.GetRootGameObjects())
            {
                if (Excluded.Contains(go.name) || go.GetComponentInChildren<BotController>(true) || go.GetComponentInChildren<BotCamera>(true)) continue;
                SceneManager.MoveGameObjectToScene(go, world);
                go.transform.SetParent(root, true);
            }
            EditorSceneManager.CloseScene(levelScene, true);

            var p = new Placed { spec = spec, level = level, chapter = chapter, index = index, root = root };
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (t && (t.name == "KillZone" || System.Array.IndexOf(spec.remove, t.name) >= 0)) Object.DestroyImmediate(t.gameObject);

            // Footprint and floors, before anything is added.
            bool any = false;
            var fp = new Bounds();
            foreach (var c in root.GetComponentsInChildren<Collider>(true))
            {
                if (c.isTrigger || c.attachedRigidbody && !c.attachedRigidbody.isKinematic) continue;
                var b = c.bounds;
                if (!any) { fp = b; any = true; } else fp.Encapsulate(b);
                if (FloorNames.Contains(c.name) && c is BoxCollider) p.floors.Add(b);
            }

            // Front gate.
            var gate = CutOpening(root, spec.wall, spec.along, 0f, 3.6f, 3f, true);
            p.normal = spec.normal;
            p.entrance = new Vector3(gate.center.x, 0f, gate.center.z) + spec.normal * gate.thickness * 0.5f;
            var door = SlidingDoor("FrontGate", root, new Vector3(gate.center.x, 1.8f, gate.center.z),
                gate.alongX ? new Vector3(3f, 3.6f, 0.3f) : new Vector3(0.3f, 3.6f, 3f),
                (gate.alongX ? Vector3.right : Vector3.forward) * 3.2f * gate.slide);

            // Exit.
            if (!string.IsNullOrEmpty(spec.exitWall))
            {
                var exit = CutOpening(root, spec.exitWall, spec.exitAlong, spec.exitBottom, 3.6f, 3f, true);
                p.outlet = new Vector3(exit.center.x, spec.exitBottom, exit.center.z) + spec.outletDir * exit.thickness * 0.5f;
            }
            else p.outlet = spec.outlet;
            p.outletDir = spec.outletDir;

            // Porch with a service bay, a sign over the gate and two lamps.
            p.porchDepth = home ? 9f : 5f;
            p.porchWidth = home ? 12f : 6f;
            Vector3 n = p.normal, side = Vector3.Cross(Vector3.up, n);
            Vector3 porchCenter = p.entrance + n * p.porchDepth * 0.5f;
            Vector3 porchSize = Abs(n * p.porchDepth + side * p.porchWidth) + Vector3.up * 0.5f;
            var porch = Box("Porch", root, porchCenter + Vector3.down * 0.25f, porchSize, Mat("Porch_Boards", new Color(0.88f, 0.72f, 0.54f)));
            p.floors.Add(porch.GetComponent<Collider>().bounds);
            fp.Encapsulate(porch.GetComponent<Collider>().bounds);
            p.footprint = fp;

            float facing = Mathf.Atan2(n.x, n.z) * Mathf.Rad2Deg;
            var title = (level.big ? "BIG RESCUE: " : "") + level.title;
            Label(title.ToUpperInvariant(), root, p.entrance + n * 0.32f + Vector3.up * 4.1f, facing, 0.085f, new Color(0.25f, 0.2f, 0.45f));
            if (home) Label(current.style == Style.Station ? "ROBOT RESCUE BUREAU" : "BRIGHTWATER REACH", root, p.entrance + n * 0.32f + Vector3.up * 5.4f, facing, 0.17f, new Color(1f, 0.5f, 0.15f));
            foreach (float s in new[] { -1f, 1f })
                Lamp("GateLamp", root, p.entrance + n * 0.7f + side * s * 2.3f);
            if (!home) ServiceBay("ServiceBay", root, p.entrance + n * (p.porchDepth * 0.55f) + side * 1.75f);

            var spawn = new GameObject("Spawn").transform;
            spawn.SetParent(root, false);
            spawn.SetPositionAndRotation(p.entrance + n * (p.porchDepth * 0.62f) - side * 1.1f + Vector3.up * 0.05f, Quaternion.LookRotation(-n));

            // Pits get their own kill zone, just above the hillside below them.
            if (spec.killTop.HasValue)
            {
                var k = Box("PitKillZone", root, new Vector3(fp.center.x, spec.killTop.Value - 0.8f, fp.center.z), new Vector3(fp.size.x, 1.6f, fp.size.z), null);
                k.GetComponent<Collider>().isTrigger = true;
                k.AddComponent<KillZone>();
            }

            // Area component: trigger around the building (not the porch), plus its systems.
            var areaBox = new Bounds(new Vector3(fp.center.x, 0, fp.center.z), Vector3.zero);
            foreach (var c in root.GetComponentsInChildren<Collider>(true))
                if (!c.isTrigger && c.name != "Porch" && !(c.attachedRigidbody && !c.attachedRigidbody.isKinematic)) areaBox.Encapsulate(c.bounds);
            root.gameObject.layer = IgnoreRaycastLayer;
            var trigger = root.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = areaBox.center + Vector3.up * 1f;
            trigger.size = areaBox.size + new Vector3(-0.6f, 2f, -0.6f);
            var area = root.gameObject.AddComponent<Area>();
            p.area = area;
            area.sceneId = level.Scene;
            area.title = level.title;
            area.chapterTitle = chapter;
            area.levelIndex = index;
            area.bigRescue = level.big;
            area.spawnPoint = spawn;
            area.entranceDoor = door;

            var game = root.Find("Game");
            if (game)
            {
                game.name = "Systems";
                Object.DestroyImmediate(game.GetComponent<PrototypeHUD>());
                Object.DestroyImmediate(game.GetComponent<GameManager>());
                Object.DestroyImmediate(game.GetComponent<Babble>());
                Object.DestroyImmediate(game.GetComponent<AudioSource>());
                area.runner = game.GetComponent<TaskRunner>();
                area.narrator = game.GetComponent<DispatchNarrator>();
                if (area.narrator) area.narrator.common = null;
            }
            var exits = new List<Door>();
            foreach (var d in root.GetComponentsInChildren<Door>(true))
            {
                if (d.name != "ExitDoor") continue;
                exits.Add(d);
                if (d.openOnTaskComplete && area.runner && area.runner.task) d.taskId = area.runner.task.taskId;
            }
            area.exitDoors = exits.ToArray();
            return p;
        }

        struct Cut { public Vector3 center; public bool alongX; public float thickness; public float slide; }

        /// <summary>
        /// Opens a doorway in an axis-aligned wall box by replacing it with up to four pieces (left, right, below,
        /// above). Adds a sill so there's no gap underfoot.
        /// </summary>
        static Cut CutOpening(Transform root, string wallName, float along, float bottom, float height, float width, bool sill)
        {
            Transform wall = null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == wallName) { wall = t; break; }
            if (!wall) { Debug.LogError($"[World] {root.name}: no wall called {wallName}"); return new Cut { center = Vector3.zero, alongX = true, thickness = 0.5f, slide = 1f }; }

            Vector3 c = wall.position, s = wall.lossyScale;
            var mat = wall.GetComponent<Renderer>() ? wall.GetComponent<Renderer>().sharedMaterial : Mat("Wall", Color.white);
            var parent = wall.parent;
            bool alongX = s.x >= s.z;
            float a0 = alongX ? c.x - s.x * 0.5f : c.z - s.z * 0.5f, a1 = alongX ? c.x + s.x * 0.5f : c.z + s.z * 0.5f;
            float y0 = c.y - s.y * 0.5f, y1 = c.y + s.y * 0.5f;
            float o0 = along - width * 0.5f, o1 = along + width * 0.5f;
            float oy0 = Mathf.Max(bottom, y0), oy1 = Mathf.Min(bottom + height, y1);
            float thick = alongX ? s.z : s.x, across = alongX ? c.z : c.x;

            void Piece(string suffix, float b0, float b1, float h0, float h1)
            {
                if (b1 - b0 < 0.02f || h1 - h0 < 0.02f) return;
                float mid = (b0 + b1) * 0.5f, len = b1 - b0;
                var pos = alongX ? new Vector3(mid, (h0 + h1) * 0.5f, across) : new Vector3(across, (h0 + h1) * 0.5f, mid);
                var size = alongX ? new Vector3(len, h1 - h0, thick) : new Vector3(thick, h1 - h0, len);
                Box(wallName + "_" + suffix, parent, pos, size, mat);
            }
            Piece("L", a0, o0, y0, y1);
            Piece("R", o1, a1, y0, y1);
            Piece("Below", o0, o1, y0, oy0);
            Piece("Above", o0, o1, oy1, y1);
            Object.DestroyImmediate(wall.gameObject);

            var center = alongX ? new Vector3(along, bottom + height * 0.5f, across) : new Vector3(across, bottom + height * 0.5f, along);
            if (sill)
            {
                var sillPos = new Vector3(center.x, bottom - 0.25f, center.z);
                Box(wallName + "_Sill", parent, sillPos, alongX ? new Vector3(width + 0.2f, 0.5f, thick + 0.1f) : new Vector3(thick + 0.1f, 0.5f, width + 0.2f), Mat("Porch_Boards", new Color(0.88f, 0.72f, 0.54f)));
            }
            return new Cut { center = center, alongX = alongX, thickness = thick, slide = (a1 - o1) >= (o0 - a0) ? 1f : -1f };
        }

        static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));

        // ================================================================== layout

        /// <summary>
        /// The first call sits by the south shore. Each next one is placed a short walk beyond the previous exit,
        /// a little higher up, turned so its gate faces the walkway, never overlapping anything already placed.
        /// </summary>
        static void Layout()
        {
            var first = placed[0];
            first.yaw = 0f;
            Vector3 porchFront0 = new Vector3(0f, 0f, -150f);
            Vector3 e0 = porchFront0 - first.Dir(first.normal) * first.porchDepth;
            first.origin = e0 - first.Dir(first.entrance);
            var rects = new List<Rect2> { first.WorldRect(3f) };

            for (int i = 1; i < placed.Count; i++)
            {
                var prev = placed[i - 1];
                var p = placed[i];
                Vector3 P = prev.W(prev.outlet), D = prev.Dir(prev.outletDir);
                Vector3 side = Vector3.Cross(Vector3.up, D);
                float bestScore = float.MinValue;
                (float yaw, Vector3 origin, List<Vector3> path) best = default;

                for (int relax = 0; relax < 3 && bestScore == float.MinValue; relax++)
                    foreach (float yaw in new[] { 0f, 90f, 180f, 270f })
                        foreach (float len in new[] { 12f, 16f, 20f, 26f, 34f })
                            foreach (float s in new[] { 0f, -8f, 8f, -16f, 16f, -24f, 24f })
                            {
                                p.yaw = yaw;
                                Vector3 N = p.Dir(p.normal);
                                Vector3 F = P + D * len + side * s;
                                F.y = P.y + Rise;
                                if (Vector3.Dot(F - P, D) < 3f || Vector3.Dot(P - F, N) < 2f) continue;
                                Vector3 E = F - N * p.porchDepth;
                                p.origin = E - p.Dir(p.entrance);
                                var rect = p.WorldRect(relax == 2 ? 1.5f : 3f);
                                if (rects.Exists(r => r.Overlaps(rect))) continue;
                                if (!InsideIsland(rect, 40f)) continue;
                                var path = Curve(P, D, F, -N);
                                if (!PathClear(path, rects, rect, relax)) continue;

                                Vector2 c = rect.Center;
                                float score = (c.y - prev.WorldRect().Center.y) * 1.0f - Mathf.Abs(c.x) * 0.35f - PathLength(path) * 0.25f - Mathf.Abs(s) * 0.05f;
                                if (score > bestScore) { bestScore = score; best = (yaw, p.origin, path); }
                            }

                if (bestScore == float.MinValue)
                {
                    Debug.LogWarning($"[World] Couldn't fit {p.level.Scene} neatly; placing it further out.");
                    p.yaw = 0f;
                    Vector3 F = P + D * 40f + Vector3.up * Rise;
                    Vector3 E = F - p.Dir(p.normal) * p.porchDepth;
                    p.origin = E - p.Dir(p.entrance);
                    best = (0f, p.origin, Curve(P, D, F, -p.Dir(p.normal)));
                }
                p.yaw = best.yaw;
                p.origin = best.origin;
                var w = new Walkway();
                w.points.AddRange(best.path);
                walkways.Add(w);
                rects.Add(p.WorldRect(3f));
            }
        }

        static bool InsideIsland(Rect2 r, float margin)
        {
            foreach (var c in new[] { new Vector2(r.x0, r.z0), new Vector2(r.x1, r.z0), new Vector2(r.x0, r.z1), new Vector2(r.x1, r.z1) })
                if ((c - IslandCenter).magnitude > IslandRadius - margin) return false;
            return true;
        }

        static bool PathClear(List<Vector3> path, List<Rect2> rects, Rect2 own, int relax)
        {
            for (int i = 0; i < path.Count; i++)
            {
                float t = (float)i / (path.Count - 1);
                if (t < 0.12f || t > 0.88f) continue;
                var pt = path[i];
                foreach (var r in rects) if (r.Grow(-2.5f).Contains(pt)) return false;
                if (own.Grow(-2.5f).Contains(pt)) return false;
                if (relax < 2)
                    foreach (var w in walkways)
                        foreach (var q in w.points)
                            if ((new Vector2(q.x - pt.x, q.z - pt.z)).sqrMagnitude < 36f) return false;
            }
            return true;
        }

        /// <summary>Cubic curve leaving <paramref name="a"/> along <paramref name="da"/>, arriving at <paramref name="b"/> moving along <paramref name="db"/>.
        /// Height eases in and out. Resampled about every 1.2 m.</summary>
        static List<Vector3> Curve(Vector3 a, Vector3 da, Vector3 b, Vector3 db)
        {
            float dist = Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
            float k = Mathf.Max(3f, dist * 0.42f);
            Vector3 p0 = a - da * 0.4f, p1 = a + da * k, p2 = b - db * k, p3 = b + db * 0.4f;
            var raw = new List<Vector3>();
            for (int i = 0; i <= 80; i++)
            {
                float t = i / 80f, u = 1f - t;
                var q = u * u * u * p0 + 3f * u * u * t * p1 + 3f * u * t * t * p2 + t * t * t * p3;
                q.y = Mathf.Lerp(a.y, b.y, t * t * (3f - 2f * t));
                raw.Add(q);
            }
            var outPts = new List<Vector3> { raw[0] };
            float acc = 0f;
            for (int i = 1; i < raw.Count; i++)
            {
                acc += Vector3.Distance(raw[i - 1], raw[i]);
                if (acc >= 1.2f || i == raw.Count - 1) { outPts.Add(raw[i]); acc = 0f; }
            }
            return outPts;
        }

        static float PathLength(List<Vector3> pts)
        {
            float l = 0f;
            for (int i = 1; i < pts.Count; i++) l += Vector3.Distance(pts[i - 1], pts[i]);
            return l;
        }

        // ================================================================== lookout (end of the road, for now)

        struct Plaza { public Vector3 center; public Bounds bounds; public Vector3 forward; public Transform podSpawn; }

        /// <summary>
        /// The end of the road at each destination. On the station it's the Launch Bay (mission pods to other places);
        /// at Brightwater Reach it's a lookout over the platforms still to be connected, with a pod home.
        /// </summary>
        static Plaza BuildLookout(Destination dest)
        {
            var last = placed[placed.Count - 1];
            Vector3 P = last.W(last.outlet), D = last.Dir(last.outletDir), side = Vector3.Cross(Vector3.up, D);
            var rects = new List<Rect2>();
            foreach (var p in placed) rects.Add(p.WorldRect(3f));
            Vector3 best = P + D * 18f;
            float bestScore = float.MinValue;
            foreach (float len in new[] { 14f, 18f, 24f, 30f })
                foreach (float s in new[] { 0f, -10f, 10f, -20f, 20f })
                {
                    Vector3 c = P + D * (len + 8f) + side * s;
                    var r = Rect2.Around(c, 11f);
                    if (rects.Exists(x => x.Overlaps(r)) || !InsideIsland(r, 30f)) continue;
                    float score = c.z * 0.5f - Mathf.Abs(c.x) * 0.2f - len * 0.3f;
                    if (score > bestScore) { bestScore = score; best = c; }
                }
            best.y = P.y + Rise;
            Vector3 toward = (best - P);
            toward.y = 0f;
            toward.Normalize();
            var w = new Walkway();
            w.points.AddRange(Curve(P, D, best - toward * 8f, toward));
            walkways.Add(w);

            bool station = dest.style == Style.Station;
            var root = new GameObject(station ? "LaunchBay" : "Lookout").transform;
            var plaza = Box("Plaza", root, best + Vector3.down * 0.25f, new Vector3(16f, 0.5f, 16f), Mat(station ? "Bay_Floor" : "Porch_Boards", station ? new Color(0.86f, 0.88f, 0.96f) : new Color(0.88f, 0.72f, 0.54f)));
            var b = plaza.GetComponent<Collider>().bounds;
            blocked.Add(Rect2.Around(best, 8f));

            float facing = Mathf.Atan2(-toward.x, -toward.z) * Mathf.Rad2Deg;
            var frame = new GameObject("Signage").transform;
            frame.SetParent(root, false);
            frame.SetPositionAndRotation(best, Quaternion.LookRotation(toward));
            Vector3 F(float x, float y, float z) => frame.TransformPoint(new Vector3(x, y, z));
            GameObject Local(GameObject go) { go.transform.rotation = frame.rotation; return go; }
            var ink = station ? Color.white : new Color(0.35f, 0.2f, 0.1f);
            var board = station ? Mat("Screen_Navy", new Color(0.12f, 0.16f, 0.32f)) : Mat("Sign_Wood", new Color(0.8f, 0.6f, 0.42f));
            var white = station ? Mat("Rail_Lilac", new Color(0.78f, 0.72f, 1f)) : Mat("Fence_White", new Color(0.97f, 0.95f, 0.9f));
            ServiceBay("ServiceBay", root, F(-5f, 0f, -3f));

            var spawn = new GameObject("PodSpawn").transform;
            spawn.SetParent(root, false);
            spawn.SetPositionAndRotation(F(0f, 0.05f, -4f), Quaternion.LookRotation(toward));

            if (station)
            {
                Local(Box("MissionBoard", frame, F(0f, 2.6f, -6.9f), new Vector3(7f, 3f, 0.2f), board));
                Label("MISSION CONTROL", root, F(0f, 3.7f, -6.75f), facing + 180f, 0.11f, new Color(0.5f, 0.9f, 1f));
                Label("Pods launch to wherever\nthe Bureau is needed.", root, F(0f, 2.6f, -6.75f), facing + 180f, 0.06f, ink);
                var brightwater = Destinations[1];
                var range = LevelRange(brightwater);
                Pod("Pod_Brightwater", root, F(0f, 0f, 4.5f), -toward, "BRIGHTWATER REACH",
                    System.IO.Path.GetFileNameWithoutExtension(brightwater.scene), range.first, range.first, brightwater.title);
                Pod("Pod_Moon", root, F(-5.5f, 0f, 3.5f), -toward, "MOON MINE\nCOMING SOON", null, -1, int.MaxValue, "the Moon Mine");
                Pod("Pod_Jungle", root, F(5.5f, 0f, 3.5f), -toward, "JUNGLE LAB\nCOMING SOON", null, -1, int.MaxValue, "the Jungle Lab");
                Lamp("BayLamp", root, F(-7.2f, 0f, -7.2f));
                Lamp("BayLamp", root, F(7.2f, 0f, -7.2f));
                Bolt("launch_bay", root, F(6f, 1f, -5f));
            }
            else
            {
                Local(Box("SignBoard", frame, F(0f, 1.9f, 6.2f), new Vector3(6f, 2.2f, 0.2f), board));
                foreach (float s in new[] { -1f, 1f }) Local(Box("SignLeg", frame, F(s * 2.6f, 0.5f, 6.2f), new Vector3(0.2f, 1f, 0.2f), board));
                Label("REACH LOOKOUT", root, F(0f, 2.45f, 6.05f), facing, 0.11f, ink);
                Label("Bridges to the other platforms\nare still being built!", root, F(0f, 1.6f, 6.05f), facing, 0.05f, ink);
                for (int i = -3; i <= 3; i++) Local(Box("RailPost", frame, F(i * 2.2f, 0.45f, 7.6f), new Vector3(0.15f, 0.9f, 0.15f), white));
                Local(Box("Rail", frame, F(0f, 0.85f, 7.6f), new Vector3(13.4f, 0.1f, 0.1f), white));
                Pod("ReturnPod_Lookout", root, F(5f, 0f, -1f), -toward, "BACK TO STATION",
                    System.IO.Path.GetFileNameWithoutExtension(Destinations[0].scene), -1, -1, "Bureau Station");
                Lamp("PlazaLamp", root, F(-7.2f, 0f, 7.2f));
                Lamp("PlazaLamp", root, F(7.2f, 0f, 7.2f));
                Bolt("lookout", root, F(-5f, 1f, 4f));
            }
            return new Plaza { center = best, bounds = b, forward = toward, podSpawn = station ? spawn : null };
        }

        /// <summary>
        /// A rounded drop pod on a launch ring. Walk in to fly to <paramref name="targetScene"/> (null: a locked
        /// "coming soon" pod). It launches once <paramref name="requiresLevel"/> is unlocked.
        /// </summary>
        static void Pod(string name, Transform parent, Vector3 floorPos, Vector3 facing, string label, string targetScene, int targetLevel, int requiresLevel, string destination)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(floorPos, Quaternion.LookRotation(facing));
            Cylinder("LaunchRing", root, floorPos + Vector3.up * 0.06f, 1.7f, 0.12f, Mat("Hull_Grey", new Color(0.6f, 0.64f, 0.74f)));
            Cylinder("RingGlow", root, floorPos + Vector3.up * 0.13f, 1.45f, 0.02f, Glow("Bay_Glow", new Color(0.3f, 0.7f, 1f), 1.6f));
            var capsule = new GameObject("Capsule").transform;
            capsule.SetParent(root, false);
            capsule.position = floorPos;
            bool open = !string.IsNullOrEmpty(targetScene);
            var shell = open ? Mat("Pod_Orange", new Color(1f, 0.6f, 0.25f)) : Mat("Pod_Grey", new Color(0.72f, 0.74f, 0.82f));
            Sphere("Shell", capsule, floorPos + Vector3.up * 1.5f, 1.05f, shell, new Vector3(1f, 1.35f, 1f));
            Sphere("Nose", capsule, floorPos + Vector3.up * 2.85f, 0.32f, Mat("Hull_White", new Color(0.93f, 0.94f, 0.98f)));
            var window = Sphere("Window", capsule, floorPos + Vector3.up * 1.75f + root.forward * 0.86f, 0.38f, Glow("Pod_Window", new Color(0.5f, 0.85f, 1f), 0.9f), new Vector3(1f, 1f, 0.4f));
            window.transform.rotation = root.rotation;
            for (int i = 0; i < 3; i++)
            {
                var dir = Quaternion.Euler(0f, i * 120f + 60f, 0f) * root.forward;
                var fin = Box("Fin", capsule, floorPos + Vector3.up * 0.55f + dir * 0.95f, new Vector3(0.12f, 0.9f, 0.6f), Mat("Hull_White", new Color(0.93f, 0.94f, 0.98f)), false);
                fin.transform.rotation = Quaternion.LookRotation(dir);
            }
            var light = Sphere("ReadyLight", capsule, floorPos + Vector3.up * 3.3f, 0.16f, Glow("Pod_Light", new Color(0.3f, 1f, 0.45f), 2f));
            float facingDeg = Mathf.Atan2(facing.x, facing.z) * Mathf.Rad2Deg;
            Label(label, root, floorPos + Vector3.up * 4f, facingDeg, 0.07f, open ? new Color(1f, 0.6f, 0.25f) : new Color(0.7f, 0.72f, 0.82f));

            var zone = Box("BoardZone", root, floorPos + Vector3.up * 1.2f, new Vector3(2.2f, 2.4f, 2.2f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var pod = zone.AddComponent<MissionPod>();
            pod.targetScene = targetScene;
            pod.targetLevel = targetLevel;
            pod.requiresLevel = open ? requiresLevel : int.MaxValue;
            pod.destinationName = destination;
            pod.capsule = capsule;
            pod.readyLight = light.GetComponent<Renderer>();
            // Solid body so PIP bumps the shell rather than walking through it (the trigger sits just in front).
            var body = capsule.Find("Shell").gameObject.AddComponent<CapsuleCollider>();
            body.radius = 0.4f;
            body.height = 1.6f;
            zone.transform.position = floorPos + Vector3.up * 1.2f + root.forward * 1.2f;
        }

        // ================================================================== small shared props

        static void Lamp(string name, Transform parent, Vector3 floorPos)
        {
            Box(name + "_Post", parent, floorPos + Vector3.up * 1.3f, new Vector3(0.14f, 2.6f, 0.14f), Mat("Lamp_Post", new Color(0.3f, 0.35f, 0.5f)), false);
            Sphere(name + "_Bulb", parent, floorPos + Vector3.up * 2.75f, 0.22f, Glow("Lamp_Warm", new Color(1f, 0.85f, 0.55f), 1.6f));
        }

        static Material Vertex(string name, float smoothness = 0.1f)
        {
            var m = Mat(name, Color.white, smoothness, outline: false);
            if (m.HasProperty("_VertexColor")) m.SetFloat("_VertexColor", 1f);
            if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.08f);
            if (m.HasProperty("_SpecStrength")) m.SetFloat("_SpecStrength", 0f);
            return m;
        }

        static Mesh Keep(Mesh m)
        {
            meshes.Add(m);
            return m;
        }

        static void SaveMeshes()
        {
            if (meshes.Count == 0) return;
            AssetDatabase.DeleteAsset(MeshPath);
            AssetDatabase.CreateAsset(meshes[0], MeshPath);
            for (int i = 1; i < meshes.Count; i++) AssetDatabase.AddObjectToAsset(meshes[i], meshes[0]);
            AssetDatabase.ImportAsset(MeshPath);
        }
    }
}
