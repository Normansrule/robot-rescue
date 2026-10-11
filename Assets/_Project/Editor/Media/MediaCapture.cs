using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
                if (File.Exists(ProjectBuilder.MenuScene)) EditorSceneManager.OpenScene(ProjectBuilder.MenuScene, OpenSceneMode.Single);
            }
        }

        // ------------------------------------------------------------------ shots

        static void CaptureMenu()
        {
            if (!File.Exists(ProjectBuilder.MenuScene)) return;
            EditorSceneManager.OpenScene(ProjectBuilder.MenuScene, OpenSceneMode.Single);
            using (var rig = new Rig(Color.clear))
            {
                // Wide banner.
                rig.Aim(new Vector3(0f, 1.2f, 0f), 20f, 9.5f, 2.6f, 38f);
                SavePng(rig.Render(1600, 600), "banner.png");

                // PIP turntable, close up.
                var gif = new GifEncoder(360, 360, 6);
                for (int i = 0; i < 48; i++)
                {
                    rig.Aim(new Vector3(0f, 0.7f, 0f), 20f + i * 7.5f, 3.4f, 1.3f, 40f);
                    gif.AddFrame(rig.Render(360, 360).GetPixels32());
                }
                gif.Save(Path.Combine(OutDir, "pip_turntable.gif"));
            }
        }

        static void CaptureLevel(string name)
        {
            var b = SceneBounds();
            float size = Mathf.Max(b.size.x, b.size.z);
            Vector3 c = b.center;
            c.y = 0f;
            using (var rig = new Rig(new Color(0.62f, 0.83f, 1f)))
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
            foreach (var r in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
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

            public Rig(Color background)
            {
                foreach (var other in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    if (other.enabled) { other.enabled = false; disabled.Add(other); }
                var go = new GameObject("MediaCaptureCamera") { hideFlags = HideFlags.HideAndDontSave };
                cam = go.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = background.a > 0f ? background : new Color(0.62f, 0.83f, 1f);
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 500f;
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
