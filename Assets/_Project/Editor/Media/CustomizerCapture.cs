using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RobotRescue.Bots;
using RobotRescue.UI;

namespace RobotRescue.EditorTools.Media
{
    /// <summary>
    /// Menu: Robot Rescue > Capture Customizer. Presses Play on the main menu, opens Customize bots, flips through
    /// workshop options the way the arrow buttons do (live preview, nothing saved), and writes a still per look plus a
    /// GIF into docs/media/customizer/. Logs which parts are showing after each flip so a broken preview is obvious.
    /// Headless (needs a GPU, and no -quit; it exits when done):
    /// <c>Unity -batchmode -projectPath . -executeMethod RobotRescue.EditorTools.Media.CustomizerCapture.CaptureAll</c>
    /// </summary>
    [InitializeOnLoad]
    public static class CustomizerCapture
    {
        const string ActiveKey = "RR.CustomizerCapture.Active";
        const float Step = 1.1f;

        public static string OutDir => Path.Combine(MediaCapture.OutDir, "customizer");

        static CustomizerCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayMode;
        }

        [MenuItem("Robot Rescue/Capture Customizer", priority = 22)]
        public static void CaptureFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CaptureAll();
        }

        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutDir);
            ShaderUtil.allowAsyncCompilation = false;
            SessionState.SetBool(ActiveKey, true);
            EditorSceneManager.OpenScene(ProjectBuilder.MenuScene, OpenSceneMode.Single);
            EditorApplication.EnterPlaymode();
        }

        static void OnPlayMode(PlayModeStateChange change)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (change == PlayModeStateChange.EnteredPlayMode) new Driver().Begin();
            else if (change == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.EraseBool(ActiveKey);
                Debug.Log($"[Customizer] Wrote captures to {OutDir}");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
        }

        sealed class Driver
        {
            readonly GifEncoder gif = new GifEncoder(480, 360, 12);
            readonly List<System.Action<BotLook>> flips = new List<System.Action<BotLook>>
            {
                l => l.shell++, l => l.chassis++, l => l.head++, l => l.feet++, l => l.topper++,
                l => l.badge++, l => l.trim++, l => l.eyes++, l => l.expression++, l => l.joints++,
                l => { l.chassis += 2; l.head += 2; l.shell += 3; l.topper += 2; },
            };
            BotWorkshop workshop;
            FieldInfo lookField;
            MethodInfo preview;
            BotCustomizer bot;
            Camera cam;
            float start, next;
            int step = -1, changed;

            public void Begin()
            {
                start = Time.realtimeSinceStartup;
                next = start + 1.5f;
                EditorApplication.update += Tick;
            }

            void Tick()
            {
                if (!EditorApplication.isPlaying) { EditorApplication.update -= Tick; return; }
                if (Time.realtimeSinceStartup < next) return;
                next += Step;
                try
                {
                    if (step < 0) Open();
                    else if (step < flips.Count)
                    {
                        var look = (BotLook)lookField.GetValue(workshop);
                        string before = Signature();
                        flips[step](look);
                        lookField.SetValue(workshop, look.Sanitized());
                        preview.Invoke(workshop, null);
                        string after = Signature();
                        if (after != before) changed++;
                        Debug.Log($"[Customizer] flip {step + 1}: {(after != before ? "updated" : "NO CHANGE")} -> {after}");
                    }
                    else { Finish(); return; }
                    Shot();
                    step++;
                }
                catch (System.Exception e) { Debug.LogException(e); Finish(); }
            }

            void Open()
            {
                var menu = Object.FindAnyObjectByType<MainMenu>();
                if (!menu) throw new System.Exception("No MainMenu in the menu scene");
                var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                workshop = (BotWorkshop)typeof(MainMenu).GetField("workshop", flags).GetValue(menu);
                var pageField = typeof(MainMenu).GetField("page", flags);
                workshop.Open();
                pageField.SetValue(menu, System.Enum.Parse(pageField.FieldType, "Workshop"));
                lookField = typeof(BotWorkshop).GetField("look", flags);
                preview = typeof(BotWorkshop).GetMethod("Preview", flags);
                bot = (BotCustomizer)typeof(BotWorkshop).GetField("preview", flags).GetValue(workshop);
                if (!bot) throw new System.Exception("Workshop found no BotCustomizer on the rug");
                Debug.Log($"[Customizer] Workshop open, preview bot '{bot.name}' -> {Signature()}");

                var go = new GameObject("CustomizerCaptureCamera") { hideFlags = HideFlags.HideAndDontSave };
                cam = go.AddComponent<Camera>();
                cam.enabled = false;
                cam.clearFlags = CameraClearFlags.Skybox;
                cam.fieldOfView = 32f;
                ChamberKit.EnablePost(cam);
            }

            string Signature()
            {
                var parts = new List<string>();
                void Add(string role, GameObject[] set)
                {
                    if (set == null) return;
                    for (int i = 0; i < set.Length; i++) if (set[i] && set[i].activeSelf) parts.Add($"{role}{i}");
                }
                Add("body", bot.chassis); Add("head", bot.heads); Add("feet", bot.feetL); Add("top", bot.toppers); Add("badge", bot.badges);
                if (bot.shell != null && bot.shell.Length > 0 && bot.shell[0] && bot.shell[0].sharedMaterial)
                    parts.Add("shell#" + ColorUtility.ToHtmlStringRGB(bot.shell[0].sharedMaterial.color));
                if (bot.trim != null && bot.trim.Length > 0 && bot.trim[0] && bot.trim[0].sharedMaterial)
                    parts.Add("trim#" + ColorUtility.ToHtmlStringRGB(bot.trim[0].sharedMaterial.color));
                if (bot.face) parts.Add("eyes#" + ColorUtility.ToHtmlStringRGB(bot.face.eyeColor) + $" smile{bot.face.restingSmile:0.##}");
                return string.Join(" ", parts);
            }

            void Shot()
            {
                Vector3 c = bot.transform.position + Vector3.up * 0.9f;
                var fwd = bot.transform.forward;
                cam.transform.position = c + fwd * 4.2f + Vector3.up * 0.9f + bot.transform.right * 1.2f;
                cam.transform.LookAt(c);
                var px = Render(cam, 480, 360);
                gif.AddFrame(px);
                var big = new Texture2D(960, 720, TextureFormat.RGB24, false);
                big.SetPixels32(Render(cam, 960, 720));
                File.WriteAllBytes(Path.Combine(OutDir, $"customizer_{step + 1:00}.png"), big.EncodeToPNG());
                Object.DestroyImmediate(big);
            }

            void Finish()
            {
                EditorApplication.update -= Tick;
                Debug.Log($"[Customizer] {changed}/{flips.Count} flips changed the bot on the rug");
                if (cam && bot) Probe(new Vector2(0.5f, 0.78f));
                gif.Save(Path.Combine(OutDir, "customizer.gif"));
                if (cam) Object.DestroyImmediate(cam.gameObject);
                EditorApplication.ExitPlaymode();
            }

            /// <summary>Logs every visible renderer under a viewport point, nearest first.</summary>
            void Probe(Vector2 viewport)
            {
                cam.aspect = 960f / 720f;
                var ray = cam.ViewportPointToRay(viewport);
                var hits = new List<(float d, string what)>();
                foreach (var r in Object.FindObjectsByType<Renderer>())
                    if (r.enabled && r.gameObject.activeInHierarchy && r.bounds.IntersectRay(ray, out float d))
                        hits.Add((d, $"{HierarchyPath(r.transform)} [{r.sharedMaterial?.name}] size {r.bounds.size}"));
                hits.Sort((a, b) => a.d.CompareTo(b.d));
                foreach (var h in hits) Debug.Log($"[Customizer] probe {h.d:0.0}m: {h.what}");
            }

            static string HierarchyPath(Transform t) => t.parent ? HierarchyPath(t.parent) + "/" + t.name : t.name;

            static Color32[] Render(Camera cam, int w, int h)
            {
                var rt = RenderTexture.GetTemporary(w, h, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.aspect = (float)w / h;
                cam.Render();
                cam.targetTexture = null;
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
