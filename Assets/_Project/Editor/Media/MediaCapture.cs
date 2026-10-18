using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RobotRescue.Bots;

namespace RobotRescue.EditorTools.Media
{
    /// <summary>
    /// Menu: Robot Rescue > Capture README Media. Renders a screenshot (PNG) and an orbiting turntable (GIF) of every
    /// level, plus a PIP turntable and a wide banner, into docs/media/ at the project root.
    /// Headless (needs a GPU, so no -nographics):
    /// <c>Unity -batchmode -quit -projectPath . -executeMethod RobotRescue.EditorTools.Media.MediaCapture.CaptureAll</c>
    /// </summary>
    public static class MediaCapture
    {
        public static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "media"));

        const int ShotW = 1280, ShotH = 720;
        const int GifW = 480, GifH = 270, GifFrames = 60, GifDelay = 7;

        [MenuItem("Robot Rescue/Capture README Media", priority = 20)]
        public static void CaptureFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            CaptureAll();
            EditorUtility.RevealInFinder(OutDir);
        }

        public static void CaptureAll()
        {
            // Async shader compilation renders not-yet-compiled materials with a grey placeholder; wait for real shaders.
            bool asyncShaders = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            try
            {
                Directory.CreateDirectory(OutDir);
                CaptureWorld();
                CaptureMenu();
                foreach (var chapter in ProjectBuilder.Chapters)
                    foreach (var level in chapter.levels)
                    {
                        if (!File.Exists(level.path)) { Debug.LogWarning($"[Media] Missing {level.path}; run Build Everything first."); continue; }
                        EditorSceneManager.OpenScene(level.path, OpenSceneMode.Single);
                        CaptureLevel(level.Scene);
                    }
                Debug.Log($"[Media] Wrote screenshots and GIFs to {OutDir}");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                if (Application.isBatchMode) EditorApplication.Exit(1);
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = asyncShaders;
                var home = File.Exists(WorldBuilder.ScenePath) ? WorldBuilder.ScenePath : ProjectBuilder.MenuScene;
                if (File.Exists(home)) EditorSceneManager.OpenScene(home, OpenSceneMode.Single);
            }
        }

        // ------------------------------------------------------------------ shots

        static void CaptureMenu()
        {
            if (!File.Exists(ProjectBuilder.MenuScene)) return;
            EditorSceneManager.OpenScene(ProjectBuilder.MenuScene, OpenSceneMode.Single);
            Faces(Mood.Happy);
            using (var rig = new Rig(Color.clear))
            {
                // PIP's moods, one frame each (the LED face is a shader, so this works without pressing Play).
                var pipFace = FindPipFace();
                if (pipFace)
                {
                    var moods = new[] { Mood.Neutral, Mood.Happy, Mood.Love, Mood.Surprised, Mood.Determined, Mood.Sad, Mood.Dizzy, Mood.Sleepy, Mood.Angry };
                    var strip = new GifEncoder(360, 360, 70);
                    foreach (var m in moods)
                    {
                        pipFace.Preview(m);
                        rig.Aim(new Vector3(0f, 0.95f, 0f), 20f, 2.2f, 0.25f, 34f);
                        strip.AddFrame(rig.Render(360, 360).GetPixels32());
                    }
                    strip.Save(Path.Combine(OutDir, "pip_moods.gif"));
                    pipFace.Preview(Mood.Happy);
                }
                // PIP with the drone crew.
                rig.Aim(new Vector3(0f, 1.2f, 0f), 20f, 9.5f, 2.6f, 38f);
                SavePng(rig.Render(1600, 600), "crew.png");
            }
        }

        static void Faces(Mood mood)
        {
            foreach (var f in UnityEngine.Object.FindObjectsByType<BotFace>()) f.Preview(mood);
        }

        static BotFace FindPipFace()
        {
            var pip = GameObject.Find("PIP");
            return pip ? pip.GetComponent<BotFace>() : null;
        }

        /// <summary>Each destination: a wide hero shot, an orbit, each call from outside its gate, and PIP up close.</summary>
        static void CaptureWorld()
        {
            foreach (var path in WorldBuilder.ScenePaths)
            {
                if (!File.Exists(path)) continue;
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                string tag = Path.GetFileNameWithoutExtension(path).Replace("World_", "");
                Faces(Mood.Happy);
                var orbit = UnityEngine.Object.FindAnyObjectByType<RobotRescue.UI.MenuCamera>();
                Vector3 pivot = orbit ? orbit.pivot : Vector3.zero;
                float radius = orbit ? orbit.radius : 180f;
                using (var rig = new Rig(Color.clear, skybox: true))
                {
                    rig.Aim(pivot + Vector3.up * 4f, 200f, radius, radius * 0.42f, 42f);
                    SavePng(rig.Render(1600, 900), $"world_{tag}.png");
                    var gif = new GifEncoder(GifW, GifH, GifDelay);
                    for (int i = 0; i < GifFrames; i++)
                    {
                        rig.Aim(pivot + Vector3.up * 4f, 200f + 360f * i / GifFrames, radius, radius * 0.45f, 42f);
                        gif.AddFrame(rig.Render(GifW, GifH).GetPixels32());
                    }
                    gif.Save(Path.Combine(OutDir, $"orbit_{tag}.gif"));

                    foreach (var area in UnityEngine.Object.FindObjectsByType<RobotRescue.World.Area>())
                    {
                        if (!area.spawnPoint) continue;
                        var s = area.spawnPoint;
                        rig.Aim(s.position + s.forward * 4f + Vector3.up * 2f, s.eulerAngles.y + 25f, 14f, 7f, 50f);
                        SavePng(rig.Render(ShotW, ShotH), $"world_{area.sceneId}.png");
                    }
                    var pip = GameObject.Find("PIP");
                    if (pip && tag == "Station")
                    {
                        rig.Aim(pivot + Vector3.up * 4f, 200f, radius * 1.25f, radius * 0.75f, 42f);
                        SavePng(rig.Render(1600, 600), "banner.png");
                        var gif2 = new GifEncoder(360, 360, 6);
                        for (int i = 0; i < 48; i++)
                        {
                            rig.Aim(pip.transform.position + Vector3.up * 0.8f, i * 7.5f, 3.6f, 1.2f, 40f);
                            gif2.AddFrame(rig.Render(360, 360).GetPixels32());
                        }
                        gif2.Save(Path.Combine(OutDir, "pip_turntable.gif"));
                    }
                }
                Debug.Log($"[Media] {tag}");
            }
        }

        static void CaptureLevel(string name)
        {
            var b = SceneBounds();
            float size = Mathf.Max(b.size.x, b.size.z);
            Vector3 c = b.center;
            c.y = 0f;
            Faces(Mood.Happy);
            using (var rig = new Rig(new Color(0.62f, 0.83f, 1f), skybox: true))
            {
                rig.Aim(c, 340f, size * 0.62f, size * 0.72f, 50f);
                SavePng(rig.Render(ShotW, ShotH), $"{name}.png");

                var gif = new GifEncoder(GifW, GifH, GifDelay);
                for (int i = 0; i < GifFrames; i++)
                {
                    rig.Aim(c, 340f + 360f * i / GifFrames, size * 0.62f, size * 0.72f, 50f);
                    gif.AddFrame(rig.Render(GifW, GifH).GetPixels32());
                }
                gif.Save(Path.Combine(OutDir, $"{name}.gif"));
            }
            Debug.Log($"[Media] {name}");
        }

        /// <summary>Bounds of everything visible, ignoring huge backdrop planes (sea, sky boxes).</summary>
        static Bounds SceneBounds()
        {
            bool any = false;
            var b = new Bounds();
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>())
            {
                if (!r.enabled) continue;
                var rb = r.bounds;
                if (rb.size.x > 35f || rb.size.z > 35f) continue;
                if (!any) { b = rb; any = true; }
                else b.Encapsulate(rb);
            }
            return any ? b : new Bounds(Vector3.zero, Vector3.one * 10f);
        }

        static void SavePng(Texture2D tex, string file)
        {
            File.WriteAllBytes(Path.Combine(OutDir, file), tex.EncodeToPNG());
        }

        /// <summary>
        /// A temporary camera that renders at 2x and box-filters down (cheap, reliable anti-aliasing). The scene's own
        /// cameras are switched off while it exists so they can't fight over the render.
        /// </summary>
        sealed class Rig : IDisposable
        {
            readonly Camera cam;
            readonly List<Camera> disabled = new List<Camera>();
            readonly Dictionary<long, Texture2D> outputs = new Dictionary<long, Texture2D>();

            public Rig(Color background, bool skybox = true)
            {
                foreach (var other in UnityEngine.Object.FindObjectsByType<Camera>())
                    if (other.enabled) { other.enabled = false; disabled.Add(other); }
                var go = new GameObject("MediaCaptureCamera") { hideFlags = HideFlags.HideAndDontSave };
                cam = go.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = background.a > 0f ? background : new Color(0.62f, 0.83f, 1f);
                if (skybox && RenderSettings.skybox) cam.clearFlags = CameraClearFlags.Skybox;
                cam.nearClipPlane = 0.2f;
                cam.farClipPlane = 2000f;
                ChamberKit.EnablePost(cam);
                cam.enabled = false;
            }

            public void Aim(Vector3 target, float yaw, float distance, float height, float fov)
            {
                cam.fieldOfView = fov;
                cam.transform.position = target + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, height, -distance);
                cam.transform.LookAt(target);
            }

            public Texture2D Render(int w, int h)
            {
                var rt = RenderTexture.GetTemporary(w * 2, h * 2, 24, RenderTextureFormat.ARGB32);
                cam.targetTexture = rt;
                cam.aspect = (float)w / h;
                cam.Render();
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var big = new Texture2D(w * 2, h * 2, TextureFormat.RGB24, false);
                big.ReadPixels(new Rect(0, 0, w * 2, h * 2), 0, 0);
                big.Apply();
                RenderTexture.active = prev;
                cam.targetTexture = null;
                RenderTexture.ReleaseTemporary(rt);

                var src = big.GetPixels32();
                UnityEngine.Object.DestroyImmediate(big);
                long key = ((long)w << 32) | (uint)h;
                if (!outputs.TryGetValue(key, out var tex)) outputs[key] = tex = new Texture2D(w, h, TextureFormat.RGB24, false);
                var dst = new Color32[w * h];
                int sw = w * 2;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        int i = y * 2 * sw + x * 2;
                        Color32 a = src[i], b = src[i + 1], c = src[i + sw], d = src[i + sw + 1];
                        dst[y * w + x] = new Color32((byte)((a.r + b.r + c.r + d.r) >> 2), (byte)((a.g + b.g + c.g + d.g) >> 2), (byte)((a.b + b.b + c.b + d.b) >> 2), 255);
                    }
                tex.SetPixels32(dst);
                tex.Apply();
                return tex;
            }

            public void Dispose()
            {
                foreach (var t in outputs.Values) UnityEngine.Object.DestroyImmediate(t);
                UnityEngine.Object.DestroyImmediate(cam.gameObject);
                foreach (var c in disabled) if (c) c.enabled = true;
            }
        }
    }
}
