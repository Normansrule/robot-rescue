using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RobotRescue.World;

namespace RobotRescue.EditorTools.Media
{
    /// <summary>
    /// Menu: Robot Rescue > Check Signs Through Walls. For every call building in both worlds, renders eye-level views
    /// from four sides and checks each sign a wall stands in front of: it renders the view with and without those
    /// signs and counts pixels that change. Any change means a sign is drawing through a wall. Stills go to
    /// docs/media/signcheck/. Headless (needs a GPU):
    /// <c>Unity -batchmode -quit -projectPath . -executeMethod RobotRescue.EditorTools.Media.SignCheck.Run</c>
    /// </summary>
    public static class SignCheck
    {
        public static string OutDir => Path.Combine(MediaCapture.OutDir, "signcheck");
        const int W = 960, H = 540;

        [MenuItem("Robot Rescue/Check Signs Through Walls", priority = 23)]
        public static void Run()
        {
            Directory.CreateDirectory(OutDir);
            ShaderUtil.allowAsyncCompilation = false;
            int leaks = 0, checkedViews = 0;
            foreach (var path in WorldBuilder.ScenePaths)
            {
                if (!File.Exists(path)) continue;
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                string world = Path.GetFileNameWithoutExtension(path).Replace("World_", "");
                Physics.SyncTransforms();
                var signs = new List<MeshRenderer>();
                foreach (var s in Object.FindObjectsByType<SignText>())
                    if (s.TryGetComponent<MeshRenderer>(out var r) && r.enabled) signs.Add(r);

                var cams = new List<Camera>();
                foreach (var c in Object.FindObjectsByType<Camera>()) if (c.enabled) { c.enabled = false; cams.Add(c); }
                var go = new GameObject("SignCheckCamera") { hideFlags = HideFlags.HideAndDontSave };
                var cam = go.AddComponent<Camera>();
                cam.enabled = false;
                cam.fieldOfView = 55f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 600f;
                cam.clearFlags = RenderSettings.skybox ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                ChamberKit.EnablePost(cam);
                // The first renders of a freshly opened scene can still be warming up; don't count that as a leak.
                for (int i = 0; i < 3; i++) Render(cam);

                foreach (var area in Object.FindObjectsByType<Area>())
                {
                    var b = AreaBounds(area);
                    float reach = Mathf.Max(b.extents.x, b.extents.z) + 5f;
                    // Stand on the call's own floor, not the bottom of its bounds (Brightwater calls sit on stilts).
                    float floor = area.spawnPoint ? area.spawnPoint.position.y : b.min.y;
                    for (int side = 0; side < 4; side++)
                    {
                        float yaw = side * 90f + 20f;
                        Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                        Vector3 eye = new Vector3(b.center.x, floor + 1.6f, b.center.z) - dir * reach;
                        cam.transform.position = eye;
                        cam.transform.rotation = Quaternion.LookRotation(new Vector3(b.center.x, floor + 2.2f, b.center.z) - eye);

                        var hidden = Occluded(cam, signs);
                        checkedViews++;
                        var with = Render(cam);
                        string file = $"{world}_{area.sceneId}_{side}.png";
                        int diff = 0;
                        if (hidden.Count > 0)
                        {
                            foreach (var r in hidden) r.enabled = false;
                            var without = Render(cam);
                            foreach (var r in hidden) r.enabled = true;
                            for (int i = 0; i < with.Length; i++)
                                if (Mathf.Abs(with[i].r - without[i].r) + Mathf.Abs(with[i].g - without[i].g) + Mathf.Abs(with[i].b - without[i].b) > 24) diff++;
                        }
                        Save(with, file);
                        if (diff > 40)
                        {
                            leaks++;
                            var names = new List<string>();
                            foreach (var r in hidden) names.Add($"'{r.GetComponent<TextMesh>().text.Replace("\n", " ")}'");
                            Debug.LogWarning($"[SignCheck] LEAK {file}: {diff} px from walled-off signs {string.Join(", ", names)}");
                        }
                        else Debug.Log($"[SignCheck] ok {file} ({hidden.Count} signs behind walls, {diff} px changed)");
                    }
                }
                Object.DestroyImmediate(go);
                foreach (var c in cams) if (c) c.enabled = true;
            }
            Debug.Log($"[SignCheck] {checkedViews} views, {leaks} with signs showing through walls. Stills in {OutDir}");
        }

        static Bounds AreaBounds(Area area)
        {
            var root = area.transform;
            bool any = false;
            var b = new Bounds(root.position, Vector3.one);
            foreach (var r in root.GetComponentsInChildren<Renderer>())
            {
                if (r.bounds.size.x > 40f || r.bounds.size.z > 40f) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            return b;
        }

        /// <summary>Signs in view that a collider sits in front of (sampled at the centre and corners of each sign).</summary>
        static List<MeshRenderer> Occluded(Camera cam, List<MeshRenderer> signs)
        {
            var list = new List<MeshRenderer>();
            var planes = GeometryUtility.CalculateFrustumPlanes(cam);
            Vector3 eye = cam.transform.position;
            foreach (var r in signs)
            {
                var b = r.bounds;
                if (!GeometryUtility.TestPlanesAABB(planes, b)) continue;
                int blocked = 0, total = 0;
                foreach (var p in new[] { b.center, b.center + new Vector3(b.extents.x, 0, 0) * 0.7f, b.center - new Vector3(b.extents.x, 0, 0) * 0.7f })
                {
                    total++;
                    Vector3 to = p - eye;
                    float d = to.magnitude;
                    if (Physics.Raycast(eye, to / d, out var hit, d - 0.25f, ~0, QueryTriggerInteraction.Ignore) && !hit.transform.IsChildOf(r.transform.parent ? r.transform.parent : r.transform))
                        blocked++;
                }
                if (blocked == total) list.Add(r);
            }
            return list;
        }

        static Color32[] Render(Camera cam)
        {
            var rt = RenderTexture.GetTemporary(W, H, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.aspect = (float)W / H;
            cam.Render();
            cam.targetTexture = null;
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            var px = tex.GetPixels32();
            Object.DestroyImmediate(tex);
            return px;
        }

        static void Save(Color32[] px, string file)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.SetPixels32(px);
            File.WriteAllBytes(Path.Combine(OutDir, file), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
    }
}
