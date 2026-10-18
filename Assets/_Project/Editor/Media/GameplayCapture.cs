using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using RobotRescue.Bots;

namespace RobotRescue.EditorTools.Media
{
    /// <summary>
    /// Menu: Robot Rescue > Capture Gameplay. Presses Play on every level and both worlds, drives PIP with a virtual
    /// keyboard and mouse (walk to the nearest pickup, grab, throw, jump, wave), and saves a few in-game stills plus a
    /// short GIF per scene into docs/media/gameplay/. Frames come from the game camera, so IMGUI HUD text isn't in them.
    /// Headless (needs a GPU, and no -quit; it exits when done):
    /// <c>Unity -batchmode -projectPath . -executeMethod RobotRescue.EditorTools.Media.GameplayCapture.CaptureAll</c>
    /// </summary>
    [InitializeOnLoad]
    public static class GameplayCapture
    {
        const string QueueKey = "RR.GameplayCapture.Queue", IndexKey = "RR.GameplayCapture.Index";
        const float Duration = 11f, FrameStep = 0.1f;
        const int GifW = 480, GifH = 270, ShotW = 1280, ShotH = 720;
        static readonly float[] ShotTimes = { 2.5f, 5.5f, 8.5f };

        public static string OutDir => Path.Combine(MediaCapture.OutDir, "gameplay");

        static GameplayCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        [MenuItem("Robot Rescue/Capture Gameplay", priority = 21)]
        public static void CaptureFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CaptureAll();
        }

        public static void CaptureAll()
        {
            var scenes = new List<string>();
            if (File.Exists(ProjectBuilder.MenuScene)) scenes.Add(ProjectBuilder.MenuScene);
            foreach (var chapter in ProjectBuilder.Chapters)
                foreach (var level in chapter.levels)
                    if (File.Exists(level.path)) scenes.Add(level.path);
            foreach (var w in WorldBuilder.ScenePaths)
                if (File.Exists(w)) scenes.Add(w);
            // Optional "-rrScenes A,B" on the command line re-records just those scenes.
            var args = System.Environment.GetCommandLineArgs();
            int at = System.Array.IndexOf(args, "-rrScenes");
            if (at >= 0 && at + 1 < args.Length)
            {
                var only = new HashSet<string>(args[at + 1].Split(','));
                scenes.RemoveAll(s => !only.Contains(Path.GetFileNameWithoutExtension(s)));
            }
            Directory.CreateDirectory(OutDir);
            SessionState.SetString(QueueKey, string.Join("|", scenes));
            SessionState.SetInt(IndexKey, 0);
            Next();
        }

        static string[] Queue => SessionState.GetString(QueueKey, "").Split(new[] { '|' }, System.StringSplitOptions.RemoveEmptyEntries);

        static void Next()
        {
            var q = Queue;
            int i = SessionState.GetInt(IndexKey, 0);
            if (i >= q.Length)
            {
                SessionState.EraseString(QueueKey);
                Debug.Log($"[Gameplay] Wrote gameplay captures to {OutDir}");
                if (Application.isBatchMode) EditorApplication.Exit(0);
                return;
            }
            ShaderUtil.allowAsyncCompilation = false;
            // Fresh campaign state each time so worlds open on the title and nothing is locked.
            PlayerPrefs.DeleteKey("rr.worldSpawn");
            EditorSceneManager.OpenScene(q[i], OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (string.IsNullOrEmpty(SessionState.GetString(QueueKey, ""))) return;
            if (change == PlayModeStateChange.EnteredPlayMode)
            {
                var q = Queue;
                int i = SessionState.GetInt(IndexKey, 0);
                if (i < q.Length) Driver.Start(Path.GetFileNameWithoutExtension(q[i]));
            }
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetInt(IndexKey, SessionState.GetInt(IndexKey, 0) + 1);
                EditorApplication.delayCall += Next;
            }
        }

        /// <summary>One scene's scripted play-through.</summary>
        sealed class Driver
        {
            static Driver current;

            readonly string scene;
            readonly GifEncoder gif = new GifEncoder(GifW, GifH, 10);
            readonly HashSet<Transform> visited = new HashSet<Transform>();
            Keyboard keyboard;
            Mouse mouse;
            InputSettings.BackgroundBehavior oldBackground;
            InputSettings.EditorInputBehaviorInPlayMode oldEditorBehavior;
            float start, nextFrame;
            int shot;
            bool leftTitle, holding;
            float grabTime = -10f;
            Transform target;

            Driver(string scene) => this.scene = scene;

            public static void Start(string scene)
            {
                current = new Driver(scene);
                current.Begin();
            }

            void Begin()
            {
                var s = InputSystem.settings;
                oldBackground = s.backgroundBehavior;
                oldEditorBehavior = s.editorInputBehaviorInPlayMode;
                s.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                s.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard = InputSystem.AddDevice<Keyboard>("CaptureKeyboard");
                mouse = InputSystem.AddDevice<Mouse>("CaptureMouse");
                start = Time.realtimeSinceStartup;
                nextFrame = start + 0.8f;
                EditorApplication.update += Tick;
                Debug.Log($"[Gameplay] Playing {scene}");
            }

            void End()
            {
                EditorApplication.update -= Tick;
                if (keyboard != null) InputSystem.RemoveDevice(keyboard);
                if (mouse != null) InputSystem.RemoveDevice(mouse);
                InputSystem.settings.backgroundBehavior = oldBackground;
                InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorBehavior;
                gif.Save(Path.Combine(OutDir, $"{scene}.gif"));
                Debug.Log($"[Gameplay] {scene}: {shot} stills + gif");
                current = null;
                EditorApplication.ExitPlaymode();
            }

            void Tick()
            {
                if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; current = null; return; }
                float t = Time.realtimeSinceStartup - start;
                try
                {
                    Steer(t);
                    if (Time.realtimeSinceStartup >= nextFrame)
                    {
                        nextFrame += FrameStep;
                        var cam = GameCamera();
                        if (cam)
                        {
                            gif.AddFrame(Render(cam, GifW, GifH));
                            if (shot < ShotTimes.Length && t >= ShotTimes[shot])
                            {
                                var tex = new Texture2D(ShotW, ShotH, TextureFormat.RGB24, false);
                                tex.SetPixels32(Render(cam, ShotW, ShotH));
                                File.WriteAllBytes(Path.Combine(OutDir, $"{scene}_{++shot}.png"), tex.EncodeToPNG());
                                Object.DestroyImmediate(tex);
                            }
                        }
                    }
                }
                catch (System.Exception e) { Debug.LogException(e); End(); return; }
                if (t >= Duration) End();
            }

            /// <summary>Feeds this frame's virtual key/mouse state: walk to the nearest unvisited pickup, grab, throw.</summary>
            void Steer(float t)
            {
                var world = RobotRescue.World.WorldDirector.Instance;
                if (world && world.InTitle)
                {
                    // Hold the title flyover for a beat so the clip shows it, then press Continue.
                    // Start at this destination's own first call (Continue would fly back to the campaign frontier).
                    if (!leftTitle && t > 2f)
                    {
                        leftTitle = true;
                        var first = world.areas != null && world.areas.Length > 0 ? world.areas[0] : null;
                        if (first) world.Begin(first.levelIndex);
                        else world.Continue();
                    }
                    return;
                }
                var bot = Object.FindAnyObjectByType<BotController>();
                var cam = Object.FindAnyObjectByType<BotCamera>();
                var keys = new List<Key>();
                Vector2 look = Vector2.zero;
                bool click = false;
                if (bot && cam && t > 1f)
                {
                    if (!target || visited.Contains(target)) target = Nearest(bot.transform.position);
                    Vector3 fwd = cam.PlanarForward;
                    if (target)
                    {
                        Vector3 to = target.position - bot.transform.position;
                        to.y = 0f;
                        float angle = Vector3.SignedAngle(fwd, to, Vector3.up);
                        look.x = Mathf.Clamp(angle * 0.25f, -12f, 12f);
                        if (Mathf.Abs(angle) < 70f) keys.Add(Key.W);
                        else keys.Add(angle > 0 ? Key.D : Key.A);
                        if (to.magnitude < 1.6f && !holding && t - grabTime > 0.6f)
                        {
                            keys.Add(Key.E);
                            holding = true;
                            grabTime = t;
                            visited.Add(target);
                            target = null;
                        }
                    }
                    else keys.Add(Key.W);
                    if (holding && t - grabTime > 2.2f) { click = true; holding = false; }
                    if (Mathf.Repeat(t, 3.1f) < 0.12f) keys.Add(Key.Space);
                    if (t > 9.6f && t < 9.75f) keys.Add(Key.G);
                }
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys.ToArray()));
                InputSystem.QueueStateEvent(mouse, new MouseState { delta = look, buttons = (ushort)(click ? 1 : 0) });
            }

            Transform Nearest(Vector3 from)
            {
                Transform best = null;
                float bestD = 22f;
                void Consider(Component c)
                {
                    if (!c || !c.gameObject.activeInHierarchy || visited.Contains(c.transform)) return;
                    float d = Vector3.Distance(from, c.transform.position);
                    if (d > 1.2f && d < bestD && Mathf.Abs(c.transform.position.y - from.y) < 2.5f) { bestD = d; best = c.transform; }
                }
                foreach (var c in Object.FindObjectsByType<RobotRescue.Puzzles.Carryable>()) Consider(c);
                foreach (var c in Object.FindObjectsByType<RobotRescue.Tools.ToolPickup>()) Consider(c);
                return best;
            }

            static Camera GameCamera()
            {
                Camera best = null;
                foreach (var c in Object.FindObjectsByType<Camera>())
                    if (c.isActiveAndEnabled && c.targetTexture == null && (!best || c.depth > best.depth)) best = c;
                return best;
            }

            static Color32[] Render(Camera cam, int w, int h)
            {
                var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
                var prevTarget = cam.targetTexture;
                cam.targetTexture = rt;
                cam.aspect = (float)w / h;
                cam.Render();
                cam.targetTexture = prevTarget;
                cam.ResetAspect();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                tex.Apply();
                RenderTexture.active = prev;
                RenderTexture.ReleaseTemporary(rt);
                var px = tex.GetPixels32();
                Object.DestroyImmediate(tex);
                return px;
            }
        }
    }
}
