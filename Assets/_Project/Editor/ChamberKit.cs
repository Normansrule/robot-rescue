using System.Collections.Generic;
using RobotRescue.Bots;
using RobotRescue.Core;
using RobotRescue.Narrative;
using RobotRescue.Tasks;
using RobotRescue.Tasks.Cooking;
using RobotRescue.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// Shared greybox kit for chamber builders: primitives, materials, signs, PIP + camera, game systems.
    /// Builders call these with world-space positions and sizes.
    /// </summary>
    public static partial class ChamberKit
    {
        public const string Root = "Assets/_Project";
        public const string MatDir = Root + "/Art/Materials";
        public const int IgnoreRaycastLayer = 2;

        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        /// <summary>Call at the start of every build so edited colours are picked up.</summary>
        public static void ResetCache() => mats.Clear();

        public static void BuildSystems(TaskDefinition task, DispatchScript dispatch, BotController bot, CookingPot pot = null)
        {
            var go = new GameObject("Game");
            go.AddComponent<GameManager>();
            var runner = go.AddComponent<TaskRunner>();
            runner.task = task;
            var narrator = go.AddComponent<DispatchNarrator>();
            narrator.script = dispatch;
            var hud = go.AddComponent<PrototypeHUD>();
            hud.tether = bot.GetComponent<TetherCable>();
            hud.grabber = bot.GetComponent<Grabber>();
            hud.taskRunner = runner;
            hud.narrator = narrator;
            hud.pot = pot;
        }

        public static void BuildLighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.78f, 0.86f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.92f, 0.86f, 0.8f);
            RenderSettings.ambientGroundColor = new Color(0.55f, 0.5f, 0.5f);
            RenderSettings.skybox = null;
        }

        public static BotCamera BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.62f, 0.83f, 1f);
            cam.fieldOfView = 60f;
            go.AddComponent<AudioListener>();
            return go.AddComponent<BotCamera>();
        }

        public static BotController BuildBot(BotCamera cam, Vector3 position, float yaw = 0f)
        {
            var bot = new GameObject("PIP");
            bot.transform.position = position;

            var rb = bot.AddComponent<Rigidbody>();
            rb.mass = 1f;
            var col = bot.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.55f, 0);
            col.height = 1.1f;
            col.radius = 0.35f;
            col.sharedMaterial = PhysMat("Bot", 0f, 0f, PhysicsMaterialCombine.Minimum);

            // Visual: pivot at the feet so squash-and-stretch stays grounded.
            var visual = new GameObject("Visual").transform;
            visual.SetParent(bot.transform, false);
            var white = Mat("Bot_White", new Color(0.97f, 0.97f, 1f), 0.6f);
            var screen = Mat("Bot_Screen", new Color(0.08f, 0.1f, 0.2f), 0.8f);
            var eye = Mat("Bot_Eye", new Color(0.3f, 0.95f, 1f));
            var accent = Mat("Bot_Orange", new Color(1f, 0.55f, 0.15f));
            var grey = Mat("Bot_Grey", new Color(0.5f, 0.52f, 0.58f));
            Vector3 o = bot.transform.position;

            Sphere("Body", visual, o + new Vector3(0, 0.42f, 0), 0.35f, white, new Vector3(1f, 0.92f, 1f));
            Box("Head", visual, o + new Vector3(0, 0.92f, 0), new Vector3(0.62f, 0.42f, 0.5f), white, false);
            Box("Face", visual, o + new Vector3(0, 0.92f, 0.23f), new Vector3(0.5f, 0.3f, 0.05f), screen, false);
            Box("EyeL", visual, o + new Vector3(-0.11f, 0.94f, 0.26f), new Vector3(0.08f, 0.13f, 0.02f), eye, false);
            Box("EyeR", visual, o + new Vector3(0.11f, 0.94f, 0.26f), new Vector3(0.08f, 0.13f, 0.02f), eye, false);
            Box("Antenna", visual, o + new Vector3(0, 1.25f, 0), new Vector3(0.03f, 0.25f, 0.03f), grey, false);
            Sphere("AntennaBall", visual, o + new Vector3(0, 1.4f, 0), 0.06f, accent);
            Sphere("FootL", visual, o + new Vector3(-0.18f, 0.1f, 0.05f), 0.11f, grey);
            Sphere("FootR", visual, o + new Vector3(0.18f, 0.1f, 0.05f), 0.11f, grey);
            var spool = Cylinder("CableSpool", visual, o + new Vector3(0, 0.55f, -0.34f), 0.17f, 0.08f, accent);
            spool.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var port = new GameObject("TetherPort").transform;
            port.SetParent(bot.transform, false);
            port.localPosition = new Vector3(0, 0.55f, -0.4f);

            bot.AddComponent<BotInput>();
            var controller = bot.AddComponent<BotController>();
            controller.cameraRig = cam;
            controller.visual = visual;

            var tether = bot.AddComponent<TetherCable>();
            tether.cameraRig = cam;
            tether.port = port;
            tether.cableMaterial = Mat("Tether_Cable", new Color(1f, 0.55f, 0.15f), 0f, unlit: true);

            bot.AddComponent<Grabber>().cameraRig = cam;

            SetLayerRecursively(bot, IgnoreRaycastLayer);
            bot.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            cam.target = bot.transform;
            cam.input = bot.GetComponent<BotInput>();
            cam.transform.position = o + bot.transform.rotation * new Vector3(0, 3f, -6f);
            cam.transform.LookAt(o + Vector3.up);
            return controller;
        }


        public static GameObject Box(string name, Transform parent, Vector3 worldPos, Vector3 size, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Place(go, name, parent, worldPos, size);
            var r = go.GetComponent<Renderer>();
            if (mat) r.sharedMaterial = mat;
            else Object.DestroyImmediate(r); // invisible trigger volume
            if (!mat) Object.DestroyImmediate(go.GetComponent<MeshFilter>());
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Cylinder visual (no collider: Unity's cylinder collider is a capsule, which rolls).</summary>
        public static GameObject Cylinder(string name, Transform parent, Vector3 worldPos, float radius, float height, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Place(go, name, parent, worldPos, new Vector3(radius * 2f, height * 0.5f, radius * 2f));
            go.GetComponent<Renderer>().sharedMaterial = mat;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static GameObject Sphere(string name, Transform parent, Vector3 worldPos, float radius, Material mat, Vector3? scale = null, bool collider = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Place(go, name, parent, worldPos, Vector3.Scale(Vector3.one * radius * 2f, scale ?? Vector3.one));
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Positions in world space; the scale is applied relative to the parent so nested props stay correct.</summary>
        public static void Place(GameObject go, string name, Transform parent, Vector3 worldPos, Vector3 worldSize)
        {
            go.name = name;
            if (parent)
            {
                go.transform.SetParent(parent, false);
                Vector3 ps = parent.lossyScale;
                go.transform.localScale = new Vector3(worldSize.x / ps.x, worldSize.y / ps.y, worldSize.z / ps.z);
            }
            else go.transform.localScale = worldSize;
            go.transform.position = worldPos;
        }

        public static void Label(string text, Transform parent, Vector3 pos, float yaw, float charSize, Color color)
        {
            var go = new GameObject("Sign_" + text);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = charSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        public static Material Mat(string name, Color color, float smoothness = 0.25f, bool unlit = false)
        {
            if (mats.TryGetValue(name, out var cached)) return cached;
            string path = $"{MatDir}/{name}.mat";
            var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit")
                         ?? Shader.Find(unlit ? "Unlit/Color" : "Standard");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            mats[name] = mat;
            return mat;
        }

        public static PhysicsMaterial PhysMat(string name, float dynamicFriction, float staticFriction, PhysicsMaterialCombine combine)
        {
            string path = $"{MatDir}/Phys_{name}.asset";
            var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (!pm)
            {
                pm = new PhysicsMaterial(name);
                AssetDatabase.CreateAsset(pm, path);
            }
            pm.dynamicFriction = dynamicFriction;
            pm.staticFriction = staticFriction;
            pm.frictionCombine = combine;
            pm.bounciness = 0f;
            EditorUtility.SetDirty(pm);
            return pm;
        }

        public static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        public static void AddSceneToBuildSettings(string path)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (scenes.Exists(s => s.path == path)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
