using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RobotRescue.Bots;
using RobotRescue.Puzzles;

namespace RobotRescue.EditorTools.Media
{
    /// <summary>
    /// Menu: Robot Rescue > Lint Levels. Static checks on every level scene for things screenshots miss:
    /// climbs taller than a full stamina bar can finish, carryables starting inside walls or hanging in the air, and
    /// doors that nothing can ever open. Writes docs/media/levellint.txt.
    /// Headless: <c>Unity -batchmode -quit -projectPath . -executeMethod RobotRescue.EditorTools.Media.LevelLint.Run</c>
    /// </summary>
    public static class LevelLint
    {
        [MenuItem("Robot Rescue/Lint Levels", priority = 24)]
        public static void Run()
        {
            var report = new List<string>();
            var paths = new List<string>();
            foreach (var chapter in ProjectBuilder.Chapters)
                foreach (var level in chapter.levels)
                    if (File.Exists(level.path)) paths.Add(level.path);
            if (File.Exists(PumpHouseBuilder.ScenePath)) paths.Add(PumpHouseBuilder.ScenePath);

            int total = 0;
            foreach (var path in paths)
            {
                EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                Physics.SyncTransforms();
                var issues = new List<string>();
                Climbs(issues);
                Carryables(issues);
                Doors(issues);
                Signs(issues);
                total += issues.Count;
                string counts = $"{Object.FindObjectsByType<ClimbSurface>().Length} climbs, {Object.FindObjectsByType<Carryable>().Length} carryables, {Object.FindObjectsByType<Door>().Length} doors";
                report.Add($"== {Path.GetFileNameWithoutExtension(path)} ({counts}): {(issues.Count == 0 ? "ok" : issues.Count + " issue(s)")}");
                foreach (var i in issues) report.Add("   " + i);
            }
            report.Insert(0, $"Level lint: {paths.Count} scenes, {total} issue(s)");
            var file = Path.Combine(MediaCapture.OutDir, "levellint.txt");
            File.WriteAllLines(file, report);
            foreach (var line in report) Debug.Log("[Lint] " + line);
        }

        // ------------------------------------------------------------------ climbs

        /// <summary>
        /// Climb budget: a full bar while moving lasts 1/staminaMove seconds at climbSpeed. Climbs between two
        /// standing spots longer than that (scaled by the surface's stamina multiplier) can't be finished.
        /// </summary>
        static void Climbs(List<string> issues)
        {
            var bot = Object.FindAnyObjectByType<BotController>();
            float budget = bot ? bot.climbSpeed / bot.staminaMove : 2.2f / 0.14f;
            foreach (var surface in Object.FindObjectsByType<ClimbSurface>())
            {
                var cols = surface.GetComponentsInChildren<Collider>();
                if (cols.Length == 0) { issues.Add($"climb '{Name(surface)}' has no collider, so it can't be grabbed"); continue; }
                var b = cols[0].bounds;
                foreach (var c in cols) b.Encapsulate(c.bounds);

                // Highest standing spot below the climb: the ground the bot starts from.
                float start = b.min.y;
                if (Physics.Raycast(new Vector3(b.center.x, b.min.y + 0.2f, b.center.z) + Outward(b) * (Mathf.Max(b.extents.x, b.extents.z) + 0.6f), Vector3.down, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore))
                    start = Mathf.Min(start, hit.point.y);
                float height = b.max.y - start;
                float cost = height * Mathf.Max(0.01f, surface.staminaMultiplier);
                if (cost > budget * 0.9f)
                    issues.Add($"climb '{Name(surface)}' is {height:0.0} m (x{surface.staminaMultiplier:0.##} stamina) vs ~{budget:0.0} m per full bar; check it has rest ledges");

                // Somewhere to stand at the top: probe just past the top edge on all four sides.
                bool ledge = false;
                foreach (var d in new[] { Vector3.forward, Vector3.back, Vector3.left, Vector3.right })
                {
                    var probe = new Vector3(b.center.x, b.max.y + 1.5f, b.center.z) + d * 0.3f;
                    if (Physics.Raycast(probe, Vector3.down, out var top, 2.2f, ~0, QueryTriggerInteraction.Ignore) && top.normal.y > 0.7f) { ledge = true; break; }
                }
                if (!ledge) issues.Add($"climb '{Name(surface)}' has no flat top to stand on (tops out at {b.max.y:0.0} m); may be a leap-off climb");
            }
        }

        static Vector3 Outward(Bounds b) => b.extents.x < b.extents.z ? Vector3.right : Vector3.forward;

        // ------------------------------------------------------------------ carryables

        static void Carryables(List<string> issues)
        {
            foreach (var c in Object.FindObjectsByType<Carryable>())
            {
                if (!c.gameObject.activeInHierarchy) continue;
                var col = c.GetComponentInChildren<Collider>();
                if (!col) continue;
                var b = col.bounds;

                // Clipping: overlapping a solid that isn't part of itself.
                foreach (var other in Physics.OverlapBox(b.center, b.extents * 0.8f, c.transform.rotation, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (other.transform.IsChildOf(c.transform) || other.attachedRigidbody == col.attachedRigidbody && other.attachedRigidbody) continue;
                    if (other.GetComponentInParent<Carryable>()) continue; // stacked props settle on play
                    issues.Add($"carryable '{Name(c)}' starts inside '{Name(other)}'");
                    break;
                }

                // Floating: more than 0.3 m of air under it (it will drop, but probably wasn't meant to).
                var rb = col.attachedRigidbody;
                if (rb && !rb.isKinematic)
                {
                    var hits = Physics.RaycastAll(new Vector3(b.center.x, b.min.y + 0.05f, b.center.z), Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore);
                    float gap = 99f;
                    foreach (var h in hits) if (!h.transform.IsChildOf(c.transform)) gap = Mathf.Min(gap, h.distance - 0.05f);
                    if (gap > 0.3f) issues.Add(gap >= 20f ? $"carryable '{Name(c)}' has nothing under it (falls out of the level)" : $"carryable '{Name(c)}' floats {gap:0.0} m above the floor");
                }
            }
        }

        // ------------------------------------------------------------------ doors

        /// <summary>A door opens on task completion, or when something that powers things lists it.</summary>
        static void Doors(List<string> issues)
        {
            var doors = Object.FindObjectsByType<Door>();
            if (doors.Length == 0) return;
            var referenced = new HashSet<Object>();
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>())
            {
                if (!mb || mb is Door) continue;
                var so = new SerializedObject(mb);
                var it = so.GetIterator();
                while (it.Next(true))
                    if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue)
                    {
                        var o = it.objectReferenceValue;
                        referenced.Add(o);
                        if (o is Component comp) referenced.Add(comp.gameObject);
                    }
            }
            foreach (var d in doors)
            {
                if (d.openOnTaskComplete) continue;
                if (referenced.Contains(d) || referenced.Contains(d.gameObject)) continue;
                bool parentRef = false;
                for (var t = d.transform.parent; t && !parentRef; t = t.parent) parentRef = referenced.Contains(t.gameObject);
                if (!parentRef) issues.Add($"door '{Name(d)}' isn't opened by a task and nothing powers it");
            }
        }

        // ------------------------------------------------------------------ signs

        /// <summary>Signs mounted on a board (their parent has a renderer) shouldn't run off the board's edges.</summary>
        static void Signs(List<string> issues)
        {
            foreach (var s in Object.FindObjectsByType<RobotRescue.World.SignText>())
            {
                var parent = s.transform.parent;
                if (!parent || !parent.TryGetComponent<Renderer>(out var board) || !s.TryGetComponent<Renderer>(out var text)) continue;
                Bounds b = board.bounds, t = text.bounds;
                float over = Mathf.Max(b.min.x - t.min.x, t.max.x - b.max.x, b.min.z - t.min.z, t.max.z - b.max.z, b.min.y - t.min.y, t.max.y - b.max.y);
                if (over > 0.15f)
                    issues.Add($"sign '{s.GetComponent<TextMesh>().text}' is {Mathf.Max(t.size.x, t.size.z):0.0} m wide and runs {over:0.0} m past its board '{parent.name}' ({Mathf.Max(b.size.x, b.size.z):0.0} m)");
            }
        }

        static string Name(Component c)
        {
            var t = c.transform;
            return t.parent ? t.parent.name + "/" + t.name : t.name;
        }
    }
}
