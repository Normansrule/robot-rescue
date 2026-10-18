using System.Collections.Generic;
using RobotRescue.Bots;
using RobotRescue.Core;
using RobotRescue.Narrative;
using RobotRescue.Tasks;
using RobotRescue.Tasks.Cooking;
using RobotRescue.Tools;
using RobotRescue.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// Shared kit for chamber builders: rounded toon primitives, materials, signs, PIP + camera, game systems,
    /// sky, fog and post-processing. Builders call these with world-space positions and sizes.
    /// </summary>
    public static partial class ChamberKit
    {
        public const string Root = "Assets/_Project";
        public const string MatDir = Root + "/Art/Materials";
        public const string RenderDir = Root + "/Art/Rendering";
        public const int IgnoreRaycastLayer = 2;

        public const string ToonShader = "RobotRescue/Toon";
        public const string FaceShader = "RobotRescue/BotFace";
        public const string SignShader = "RobotRescue/SignText";
        public const string SkyShader = "RobotRescue/Sky";

        public static readonly Color SkyTop = new Color(0.36f, 0.62f, 0.98f);
        public static readonly Color SkyHorizon = new Color(0.8f, 0.9f, 1f);

        static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        /// <summary>Call at the start of every build so edited colours are picked up.</summary>
        public static void ResetCache()
        {
            mats.Clear();
            MeshKit.ResetCache();
            droneCount = 0;
        }

        public static void BuildSystems(TaskDefinition task, DispatchScript dispatch, BotController bot)
        {
            var go = new GameObject("Game");
            go.AddComponent<GameManager>();
            var runner = go.AddComponent<TaskRunner>();
            runner.task = task;
            var narrator = go.AddComponent<DispatchNarrator>();
            narrator.script = dispatch;
            narrator.common = CommonDispatch();
            var hud = go.AddComponent<PrototypeHUD>();
            hud.tether = bot.GetComponent<TetherCable>();
            hud.grabber = bot.GetComponent<Grabber>();
            hud.taskRunner = runner;
            hud.narrator = narrator;
            hud.toolArm = bot.GetComponent<ToolArm>();
            hud.dispatchVoice = DispatchVoice(go);
        }

        /// <summary>DISPATCH's crunchy walkie-talkie babble, played by the HUD with each subtitle.</summary>
        public static Babble DispatchVoice(GameObject on)
        {
            var src = on.AddComponent<AudioSource>();
            src.spatialBlend = 0f;
            var voice = on.AddComponent<Babble>();
            voice.voiceHz = 260f;
            voice.radio = true;
            voice.volume = 0.35f;
            voice.syllableGap = 0.08f;
            return voice;
        }

        // ------------------------------------------------------------------ lighting, sky, post

        public static void BuildLighting()
        {
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            sun.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.82f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.86f, 0.86f, 0.9f);
            RenderSettings.ambientGroundColor = new Color(0.56f, 0.56f, 0.64f);
            RenderSettings.skybox = SkyMaterial();
            RenderSettings.sun = sun;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = SkyHorizon;
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 380f;

            var post = new GameObject("PostFX").AddComponent<Volume>();
            post.isGlobal = true;
            post.priority = 1f;
            post.sharedProfile = PostProfile();
        }

        public static Material SkyMaterial()
        {
            var shader = Shader.Find(SkyShader);
            if (!Usable(shader)) return null;
            var m = ShaderMat("Sky", shader);
            m.SetColor("_TopColor", SkyTop);
            m.SetColor("_HorizonColor", SkyHorizon);
            m.SetColor("_BottomColor", new Color(0.55f, 0.72f, 0.9f));
            return m;
        }

        /// <summary>Bloom (glowing eyes and LEDs), a soft neutral tonemap, a little extra colour and a vignette.</summary>
        public static VolumeProfile PostProfile()
        {
            EnsureFolder(RenderDir);
            string path = RenderDir + "/RobotRescue_PostFX.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (!profile)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            var bloom = Effect<Bloom>(profile);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.6f);
            bloom.scatter.Override(0.65f);
            Effect<Tonemapping>(profile).mode.Override(TonemappingMode.Neutral);
            var color = Effect<ColorAdjustments>(profile);
            color.postExposure.Override(0.1f);
            color.contrast.Override(10f);
            color.saturation.Override(16f);
            var vignette = Effect<Vignette>(profile);
            vignette.intensity.Override(0.22f);
            vignette.smoothness.Override(0.45f);
            Effect<WhiteBalance>(profile).temperature.Override(6f);
            EditorUtility.SetDirty(profile);
            return profile;
        }

        static T Effect<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet(out T existing)) return existing;
            var c = profile.Add<T>(true);
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }

        public static void EnablePost(Camera cam)
        {
            var data = cam.GetUniversalAdditionalCameraData();
            if (!data) return;
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
        }

        public static BotCamera BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.backgroundColor = SkyHorizon;
            cam.fieldOfView = 60f;
            cam.farClipPlane = 1200f;
            go.AddComponent<AudioListener>();
            EnablePost(cam);
            return go.AddComponent<BotCamera>();
        }

        // ------------------------------------------------------------------ PIP

        /// <summary>PIP with camera hookup. <paramref name="tools"/> are the Tool Arm heads it starts with (default: Grabber).
        /// Also drops a service bay (charger + repairs) under PIP's feet.</summary>
        public static BotController BuildBot(BotCamera cam, Vector3 position, float yaw = 0f, params ToolDefinition[] tools)
        {
            var bot = new GameObject("PIP");
            bot.transform.position = position;

            var rb = bot.AddComponent<Rigidbody>();
            rb.mass = 1f;
            var col = bot.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0, 0.6f, 0);
            col.height = 1.2f;
            col.radius = 0.36f;
            col.sharedMaterial = PhysMat("Bot", 0f, 0f, PhysicsMaterialCombine.Minimum);

            var white = Mat("Bot_White", new Color(0.97f, 0.97f, 1f), 0.6f);
            var bezel = Mat("Bot_Screen", new Color(0.08f, 0.1f, 0.2f), 0.8f);
            var accent = Mat("Bot_Orange", new Color(1f, 0.55f, 0.15f));
            var grey = Mat("Bot_Grey", new Color(0.55f, 0.58f, 0.66f));
            var cream = Mat("Bot_Cream", new Color(1f, 0.98f, 0.92f), 0.5f);
            var bulbGlow = Glow("Bot_AntennaGlow", new Color(1f, 0.55f, 0.15f), 1.5f);
            var looks = new LookParts();
            Vector3 o = bot.transform.position;

            // Visual root at the feet so squash-and-stretch stays grounded.
            var visual = Pivot("Visual", bot.transform, o);

            // Every body option is built here; BotCustomizer switches on the player's saved choices and paints them.
            var bodyPivot = Pivot("Body", visual, o + new Vector3(0, 0.42f, 0));
            var chassis = BotChassis(bodyPivot, o + new Vector3(0, 0.42f, 0), white, accent, looks);
            var badges = BotBadges(bodyPivot, o + new Vector3(0, 0.44f, 0.335f), accent, cream, looks);
            var spool = Role(Cylinder("CableSpool", bodyPivot, o + new Vector3(0, 0.52f, -0.32f), 0.16f, 0.1f, accent), looks.trim);
            spool.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var armL = Pivot("ArmL", visual, o + new Vector3(-0.37f, 0.6f, 0.02f));
            Role(Sphere("Upper", armL, o + new Vector3(-0.39f, 0.46f, 0.02f), 0.1f, white, new Vector3(1f, 1.9f, 1f)), looks.shell);
            Role(Sphere("Hand", armL, o + new Vector3(-0.4f, 0.3f, 0.02f), 0.085f, grey), looks.joints);
            var armR = Pivot("ArmR", visual, o + new Vector3(0.37f, 0.6f, 0.02f));
            Role(Sphere("Upper", armR, o + new Vector3(0.39f, 0.46f, 0.02f), 0.1f, white, new Vector3(1f, 1.9f, 1f)), looks.shell);
            // The tool head keeps its tool's colour on every bot so tools stay readable.
            var head = Box("ToolHead", armR, o + new Vector3(0.4f, 0.3f, 0.06f), new Vector3(0.18f, 0.16f, 0.12f), accent, false);

            var footL = Pivot("FootL", visual, o + new Vector3(-0.17f, 0.08f, 0.03f));
            var feetL = BotFeet(footL, o + new Vector3(-0.17f, 0.08f, 0.05f), grey, accent, looks);
            var footR = Pivot("FootR", visual, o + new Vector3(0.17f, 0.08f, 0.03f));
            var feetR = BotFeet(footR, o + new Vector3(0.17f, 0.08f, 0.05f), grey, accent, looks);

            var headPivot = Pivot("Head", visual, o + new Vector3(0, 0.98f, 0));
            var headWobble = Pivot("Wobble", headPivot, o + new Vector3(0, 0.98f, 0));
            var hw = headWobble.gameObject.AddComponent<Wobble>();
            hw.response = 0.5f;
            var heads = BotHeads(headWobble, o + new Vector3(0, 1.0f, 0), white, bezel, accent, grey, looks);
            var screen = FaceQuad("Face", headWobble, o + new Vector3(0, 1.0f, 0.326f), new Vector2(0.6f, 0.38f));
            var antenna = Pivot("Antenna", headWobble, o + new Vector3(0, 1.27f, 0));
            var aw = antenna.gameObject.AddComponent<Wobble>();
            aw.response = 1.4f;
            aw.stiffness = 80f;
            aw.damping = 4f;
            aw.idleSway = 4f;
            aw.maxAngle = 35f;
            Role(Cylinder("Stalk", antenna, o + new Vector3(0, 1.42f, 0), 0.025f, 0.3f, grey), looks.joints);
            var toppers = Toppers(BotCatalog.Toppers, antenna, o + new Vector3(0, 1.6f, 0), 1f, accent, grey, bulbGlow, looks);

            var port = new GameObject("TetherPort").transform;
            port.SetParent(bot.transform, false);
            port.localPosition = new Vector3(0, 0.52f, -0.42f);

            bot.AddComponent<BotInput>();
            var controller = bot.AddComponent<BotController>();
            controller.cameraRig = cam;
            controller.visual = visual;

            var tether = bot.AddComponent<TetherCable>();
            tether.cameraRig = cam;
            tether.port = port;
            tether.cableMaterial = Mat("Tether_Cable", new Color(1f, 0.55f, 0.15f), 0f, unlit: true);

            bot.AddComponent<Grabber>().cameraRig = cam;

            var arm = bot.AddComponent<ToolArm>();
            arm.toolHead = head.GetComponent<Renderer>();
            var kit = ToolKit();
            arm.startingTools = tools != null && tools.Length > 0 ? tools : new[] { kit.grabber };
            arm.allTools = new[] { kit.grabber, kit.whisk, kit.screwdriver, kit.magnet };

            bot.AddComponent<BotBattery>();
            var parts = bot.AddComponent<BotParts>();
            parts.partVisuals = new[] { armL, armR, antenna, footL, footR };

            var anim = bot.AddComponent<BotAnimator>();
            anim.body = bodyPivot;
            anim.head = headPivot;
            anim.armL = armL;
            anim.armR = armR;
            anim.footL = footL;
            anim.footR = footR;

            var face = bot.AddComponent<BotFace>();
            face.screen = screen;
            face.orientation = bot.transform;
            face.aspect = 0.6f / 0.38f;
            face.restingSmile = 0.45f;

            var src = bot.AddComponent<AudioSource>();
            src.spatialBlend = 0.3f;
            var voice = bot.AddComponent<Babble>();
            voice.voiceHz = 720f;

            var personality = bot.AddComponent<BotPersonality>();
            personality.face = face;
            personality.voice = voice;
            personality.animator = anim;

            var custom = bot.AddComponent<BotCustomizer>();
            custom.chassis = chassis;
            custom.heads = heads;
            custom.feetL = feetL;
            custom.feetR = feetR;
            custom.toppers = toppers;
            custom.badges = badges;
            custom.shell = looks.shell.ToArray();
            custom.trim = looks.trim.ToArray();
            custom.joints = looks.joints.ToArray();
            custom.glow = looks.glow.ToArray();
            custom.propeller = looks.propeller;
            custom.face = face;
            custom.voice = voice;
            custom.tether = tether;

            SetLayerRecursively(bot, IgnoreRaycastLayer);
            bot.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            ServiceBay("HomeCharger", null, new Vector3(o.x, o.y - 0.05f, o.z));

            cam.target = bot.transform;
            cam.input = bot.GetComponent<BotInput>();
            cam.transform.position = o + bot.transform.rotation * new Vector3(0, 3f, -6f);
            cam.transform.LookAt(o + Vector3.up);
            return controller;
        }

        /// <summary>An LED face screen (a quad facing the bot's forward) with the BotFace material.</summary>
        public static Renderer FaceQuad(string name, Transform parent, Vector3 worldPos, Vector2 size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            Place(go, name, parent, worldPos, new Vector3(size.x, size.y, 1f));
            // Unity's quad faces -Z; turn it to face the bot's forward (+Z).
            go.transform.rotation = (parent ? parent.rotation : Quaternion.identity) * Quaternion.Euler(0f, 180f, 0f);
            var r = go.GetComponent<Renderer>();
            var shader = Shader.Find(FaceShader);
            r.sharedMaterial = Usable(shader) ? ShaderMat("Bot_Face", shader) : Mat("Bot_Eye", new Color(0.3f, 0.95f, 1f));
            r.shadowCastingMode = ShadowCastingMode.Off;
            return r;
        }

        static Transform Pivot(string name, Transform parent, Vector3 worldPos)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.position = worldPos;
            return t;
        }

        // ------------------------------------------------------------------ primitives

        public static GameObject Box(string name, Transform parent, Vector3 worldPos, Vector3 size, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Place(go, name, parent, worldPos, size);
            var r = go.GetComponent<Renderer>();
            if (mat)
            {
                r.sharedMaterial = mat;
                // Soft, toy-like edges: swap the cube for a rounded box of the same size (the collider stays a box).
                float min = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
                if (min >= 0.05f && rounded)
                    go.GetComponent<MeshFilter>().sharedMesh = MeshKit.RoundedBoxFor(size, Mathf.Clamp(min * 0.2f, 0.015f, 0.14f));
            }
            else
            {
                Object.DestroyImmediate(r); // invisible trigger volume
                Object.DestroyImmediate(go.GetComponent<MeshFilter>());
            }
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Set false to build sharp-cornered boxes (debugging, or a deliberately boxy prop).</summary>
        public static bool rounded = true;

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

        /// <summary>
        /// World-space sign. <paramref name="facing"/> is the direction the text faces, toward its readers
        /// (0 = readable from +Z, 180 = from -Z). TextMesh is read from behind its forward, hence the +180.
        /// </summary>
        public static void Label(string text, Transform parent, Vector3 pos, float facing, float charSize, Color color)
        {
            var go = new GameObject("Sign_" + text);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, facing + 180f, 0f));
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
            // Unity's font shader draws through walls; signs use a depth-tested one so buildings hide what's inside.
            var sign = Shader.Find(SignShader);
            if (Usable(sign))
            {
                var mat = ShaderMat("SignText", sign);
                mat.mainTexture = font.material.mainTexture;
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
                go.AddComponent<RobotRescue.World.SignText>().shader = sign;
            }
        }

        public static void SetLayerRecursively(GameObject go, int layer)
        {
            foreach (Transform t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
        }

        // ------------------------------------------------------------------ materials

        /// <summary>True when a custom shader compiled; otherwise builders fall back to URP Lit so nothing turns pink.</summary>
        public static bool Usable(Shader shader) => shader && shader.isSupported && !ShaderUtil.ShaderHasError(shader);

        /// <summary>
        /// Toon material (rounded shading, outline, rim light). Falls back to URP Lit if the toon shader failed to compile.
        /// <paramref name="outline"/> off for huge surfaces (terrain, water) where an outline would only cost draw time.
        /// </summary>
        public static Material Mat(string name, Color color, float smoothness = 0.25f, bool unlit = false, bool outline = true)
        {
            if (mats.TryGetValue(name, out var cached)) return cached;
            string path = $"{MatDir}/{name}.mat";
            var toon = Shader.Find(ToonShader);
            bool useToon = !unlit && Usable(toon);
            var shader = unlit ? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color")
                : useToon ? toon : Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
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
            if (useToon)
            {
                Color.RGBToHSV(color, out float h, out float s, out float v);
                mat.SetColor("_ShadeColor", new Color(0.6f, 0.63f, 0.84f));
                mat.SetColor("_OutlineColor", Color.HSVToRGB(h, Mathf.Clamp01(s * 1.1f + 0.25f), Mathf.Clamp01(v * 0.32f)));
                mat.SetFloat("_OutlineWidth", 0.017f);
                mat.SetFloat("_SpecStrength", Mathf.Lerp(0.08f, 0.5f, smoothness));
                mat.SetFloat("_SpecSize", Mathf.Lerp(0.08f, 0.2f, smoothness));
                mat.SetFloat("_RimStrength", 0.22f);
                mat.SetColor("_EmissionColor", Color.black);
                mat.SetShaderPassEnabled("SRPDefaultUnlit", outline);
            }
            EditorUtility.SetDirty(mat);
            mats[name] = mat;
            return mat;
        }

        /// <summary>Self-lit toon material (LEDs, antenna bulb, charge rings) that blooms.</summary>
        public static Material Glow(string name, Color color, float intensity = 2f)
        {
            var m = Mat(name, color, 0.5f);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", color * intensity);
            m.SetShaderPassEnabled("SRPDefaultUnlit", false);
            return m;
        }

        /// <summary>Material asset for a specific shader (face, sky), created once and reused.</summary>
        public static Material ShaderMat(string name, Shader shader)
        {
            if (mats.TryGetValue(name, out var cached)) return cached;
            string path = $"{MatDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!mat)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else mat.shader = shader;
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
