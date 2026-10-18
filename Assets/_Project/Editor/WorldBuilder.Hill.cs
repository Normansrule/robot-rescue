using System.Collections.Generic;
using RobotRescue.Bots;
using RobotRescue.Core;
using RobotRescue.Narrative;
using RobotRescue.Puzzles;
using RobotRescue.UI;
using RobotRescue.World;
using UnityEngine;
using UnityEngine.Rendering;
using static RobotRescue.EditorTools.ChamberKit;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// What the calls stand on and what surrounds them: rig legs over open sea at Brightwater Reach, hull plating
    /// over a planet at the Bureau Station. Plus decks, bridges, scenery, and the world's systems.
    /// </summary>
    public static partial class WorldBuilder
    {
        /// <summary>The level everything stands down to (the sea bed, or the void under the station).</summary>
        class HeightField
        {
            public float level;
            public float Height(float x, float z) => level;
        }

        class Landmark
        {
            public string kind;
            public Vector3 pos;
            public float radius;
            public float ringAngle = float.NaN;
            public Vector3 facing;
        }

        static Style style;

        static List<Landmark> PlanLandmarks(Plaza lookout)
        {
            var list = new List<Landmark>();
            if (style != Style.Ocean) return list;
            Vector3 f = lookout.forward, r = Vector3.Cross(Vector3.up, f), b = lookout.center;
            // Chapters 3 to 8 (the park, the school, the docks and lighthouse, the pier, the Old Rig, the festival) are playable now:
            // real platforms on the bridge chain, not landmarks. Nothing is left "coming soon".
            foreach (var l in list)
            {
                if (!float.IsNaN(l.ringAngle))
                {
                    var dir = new Vector3(Mathf.Cos(l.ringAngle * Mathf.Deg2Rad), 0f, Mathf.Sin(l.ringAngle * Mathf.Deg2Rad));
                    l.pos = new Vector3(IslandCenter.x, SeaY + 3f, IslandCenter.y) + dir * (IslandRadius * 0.82f);
                    l.facing = dir;
                }
                for (int tries = 0; tries < 14; tries++)
                {
                    var rect = Rect2.Around(l.pos, l.radius + 4f);
                    if (blocked.Exists(x => x.Overlaps(rect))) { l.pos += (float.IsNaN(l.ringAngle) ? f : -l.facing) * 9f + r * (tries % 2 == 0 ? 6f : -6f); continue; }
                    break;
                }
                if (float.IsNaN(l.ringAngle)) l.facing = (lookout.center - l.pos).normalized;
                blocked.Add(Rect2.Around(l.pos, l.radius));
            }
            return list;
        }

        // ================================================================== decks and legs

        static void Foundations(Placed p, HeightField t, Transform parent)
        {
            int i = 0;
            foreach (var b in p.floors) Foundation($"{p.root.name}_Deck{i++}", parent, p.WorldBounds(b), t);
        }

        /// <summary>
        /// Under a floor: at sea, a deck slab on chunky yellow rig legs down into the water; in orbit, a white hull
        /// section with a glowing underside strip and a little thruster.
        /// </summary>
        static void Foundation(string name, Transform parent, Bounds b, HeightField t)
        {
            float top = b.min.y;
            var size = new Vector3(b.size.x + 1f, 0f, b.size.z + 1f);
            if (style == Style.Ocean)
            {
                Box(name, parent, new Vector3(b.center.x, top - 0.45f, b.center.z), new Vector3(size.x, 0.9f, size.z), Mat("Deck_Steel", new Color(0.72f, 0.76f, 0.84f)));
                var leg = Mat("Rig_Leg", new Color(1f, 0.78f, 0.32f));
                var band = Mat("Rig_Band", new Color(0.97f, 0.97f, 1f));
                int nx = Mathf.Max(2, Mathf.CeilToInt(size.x / 8f) + 1), nz = Mathf.Max(2, Mathf.CeilToInt(size.z / 8f) + 1);
                float bottom = t.level, legTop = top - 0.9f;
                for (int ix = 0; ix < nx; ix++)
                    for (int iz = 0; iz < nz; iz++)
                    {
                        if (ix != 0 && ix != nx - 1 && iz != 0 && iz != nz - 1) continue;   // only round the edge
                        float x = b.center.x - size.x * 0.5f + 0.7f + (size.x - 1.4f) * ix / (nx - 1);
                        float z = b.center.z - size.z * 0.5f + 0.7f + (size.z - 1.4f) * iz / (nz - 1);
                        Cylinder("Leg", parent, new Vector3(x, (legTop + bottom) * 0.5f, z), 0.42f, legTop - bottom, leg);
                        Cylinder("LegBand", parent, new Vector3(x, SeaY + 0.6f, z), 0.48f, 0.5f, band);
                    }
            }
            else
            {
                Box(name, parent, new Vector3(b.center.x, top - 0.8f, b.center.z), new Vector3(size.x, 1.6f, size.z), Mat("Hull_White", new Color(0.93f, 0.94f, 0.98f)));
                Box(name + "_Glow", parent, new Vector3(b.center.x, top - 1.62f, b.center.z), new Vector3(size.x - 1.2f, 0.06f, size.z - 1.2f), Glow("Hull_Glow", new Color(0.45f, 0.85f, 1f), 1.4f), false);
                Cylinder("Thruster", parent, new Vector3(b.center.x, top - 2.1f, b.center.z), Mathf.Min(1.2f, size.x * 0.15f), 1f, Mat("Hull_Grey", new Color(0.6f, 0.64f, 0.74f)));
                Cylinder("ThrusterGlow", parent, new Vector3(b.center.x, top - 2.62f, b.center.z), Mathf.Min(0.9f, size.x * 0.11f), 0.05f, Glow("Thruster_Glow", new Color(1f, 0.6f, 0.3f), 2.2f));
            }
        }

        // ================================================================== bridges

        static void BuildWalkway(string name, Walkway w, HeightField t, Transform parent)
        {
            var pts = w.points;
            int n = pts.Count;
            if (n < 2) return;
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);

            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var tris = new List<int>();
            bool ocean = style == Style.Ocean;
            var top = ocean ? new Color(0.97f, 0.88f, 0.7f) : new Color(0.9f, 0.92f, 1f);
            var edge = ocean ? new Color(0.62f, 0.66f, 0.78f) : new Color(0.55f, 0.6f, 0.78f);
            var rights = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                var d = pts[Mathf.Min(i + 1, n - 1)] - pts[Mathf.Max(i - 1, 0)];
                d.y = 0f;
                rights[i] = Vector3.Cross(Vector3.up, d.normalized).normalized;
            }

            int baseTop = verts.Count;
            for (int i = 0; i < n; i++)
            {
                verts.Add(pts[i] - rights[i] * PathHalfWidth);
                verts.Add(pts[i] + rights[i] * PathHalfWidth);
                var c = top * ((i / 2) % 2 == 0 ? 1f : 0.95f);
                c.a = 1f;
                cols.Add(c); cols.Add(c);
            }
            for (int i = 0; i < n - 1; i++)
            {
                int l0 = baseTop + i * 2, r0 = l0 + 1, l1 = l0 + 2, r1 = l0 + 3;
                tris.AddRange(new[] { l0, l1, r1, l0, r1, r0 });
            }
            // Sides and underside: a chunky bridge deck.
            foreach (float s in new[] { -1f, 1f })
            {
                int b0 = verts.Count;
                for (int i = 0; i < n; i++)
                {
                    var tp = pts[i] + rights[i] * PathHalfWidth * s;
                    verts.Add(tp); verts.Add(tp + Vector3.down * 0.55f);
                    cols.Add(edge); cols.Add(edge * 0.85f);
                }
                for (int i = 0; i < n - 1; i++)
                {
                    int t0 = b0 + i * 2, bo0 = t0 + 1, t1 = t0 + 2, bo1 = t0 + 3;
                    if (s > 0f) tris.AddRange(new[] { bo0, t0, t1, bo0, t1, bo1 });
                    else tris.AddRange(new[] { bo0, t1, t0, bo0, bo1, t1 });
                }
            }
            int u0 = verts.Count;
            for (int i = 0; i < n; i++)
            {
                verts.Add(pts[i] - rights[i] * PathHalfWidth + Vector3.down * 0.55f);
                verts.Add(pts[i] + rights[i] * PathHalfWidth + Vector3.down * 0.55f);
                cols.Add(edge * 0.75f); cols.Add(edge * 0.75f);
            }
            for (int i = 0; i < n - 1; i++)
            {
                int l0 = u0 + i * 2, r0 = l0 + 1, l1 = l0 + 2, r1 = l0 + 3;
                tris.AddRange(new[] { l0, r1, l1, l0, r0, r1 });
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            Keep(mesh);
            var go = new GameObject("Surface");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Vertex("Bridge_" + style);
            go.AddComponent<MeshCollider>().sharedMesh = mesh;

            // Legs into the sea every few metres (none in orbit: the station's gravity plating holds it up).
            if (ocean)
            {
                var leg = Mat("Rig_Leg", new Color(1f, 0.78f, 0.32f));
                for (int i = 4; i < n - 4; i += 8)
                    foreach (float s in new[] { -1f, 1f })
                    {
                        var at = pts[i] + rights[i] * (PathHalfWidth - 0.4f) * s;
                        float legTop = at.y - 0.55f;
                        Cylinder("BridgeLeg", root, new Vector3(at.x, (legTop + t.level) * 0.5f, at.z), 0.22f, legTop - t.level, leg);
                    }
            }

            if (!w.fenced) return;
            var rail = ocean ? Mat("Fence_White", new Color(0.97f, 0.95f, 0.9f)) : Mat("Rail_Lilac", new Color(0.78f, 0.72f, 1f));
            bool was = rounded;
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3? last = null;
                for (int i = 3; i < n - 3; i += 2)
                {
                    var at = pts[i] + rights[i] * (PathHalfWidth - 0.12f) * side;
                    rounded = true;
                    Box("Post", root, at + Vector3.up * 0.38f, new Vector3(0.14f, 0.76f, 0.14f), rail, false);
                    if (last.HasValue)
                    {
                        rounded = false;
                        var a = last.Value + Vector3.up * 0.62f;
                        var bpos = at + Vector3.up * 0.62f;
                        var bar = Box("Rail", root, (a + bpos) * 0.5f, new Vector3(0.08f, 0.08f, Vector3.Distance(a, bpos) + 0.05f), rail);
                        bar.transform.rotation = Quaternion.LookRotation(bpos - a);
                        var col = bar.GetComponent<BoxCollider>();
                        col.size = new Vector3(2.5f, 9f, 1f);   // a knee-high invisible wall (in rail-local units)
                        col.center = new Vector3(0f, -3.5f, 0f);
                    }
                    last = at;
                    if ((i / 2) % 6 == 2 && side == ((i / 12) % 2 == 0 ? 1 : -1))
                        Lamp("Lamp", root, pts[i] + rights[i] * (PathHalfWidth + 0.4f) * side);
                }
            }
            rounded = was;
        }

        // ================================================================== surroundings

        static void BuildSea(Transform parent)
        {
            var sea = Cylinder("Sea", parent, new Vector3(IslandCenter.x, SeaY, IslandCenter.y), 1600f, 0.2f, Mat("Sea", new Color(0.3f, 0.66f, 0.95f), 0.9f, outline: false));
            sea.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var kill = Box("SeaKillZone", parent, new Vector3(IslandCenter.x, SeaY - 1.5f, IslandCenter.y), new Vector3(4000f, 1f, 4000f), null);
            kill.GetComponent<Collider>().isTrigger = true;
            kill.AddComponent<KillZone>();
        }

        /// <summary>Stars, a big blue planet below (Brightwater Reach is down there somewhere), and a fall-out zone.</summary>
        static void BuildSpace(Transform parent, float floorY)
        {
            var root = new GameObject("Space").transform;
            root.SetParent(parent, false);
            var star = Glow("Star", new Color(1f, 0.97f, 0.9f), 3f);
            for (int i = 0; i < 260; i++)
            {
                var dir = new Vector3(Rand(-1f, 1f), Rand(-0.15f, 1f), Rand(-1f, 1f)).normalized;
                var s = Sphere("Star", root, dir * 1500f, Rand(1.2f, 3.2f), star);
                s.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            var planetC = new Vector3(IslandCenter.x + 200f, floorY - 900f, IslandCenter.y + 500f);
            var ocean = Sphere("Planet", root, planetC, 700f, Mat("Planet_Ocean", new Color(0.25f, 0.55f, 0.95f), 0.6f, outline: false));
            ocean.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var land = Mat("Planet_Land", new Color(0.45f, 0.78f, 0.45f), 0.2f, outline: false);
            var cloud = Mat("Planet_Cloud", Color.white, 0f, outline: false);
            for (int i = 0; i < 9; i++)
            {
                var dir = new Vector3(Rand(-1f, 1f), Rand(0.3f, 1f), Rand(-1f, 0.4f)).normalized;
                Sphere("Continent", root, planetC + dir * 640f, Rand(90f, 170f), land, new Vector3(1f, 0.35f, 1f)).transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
                var c = Sphere("CloudBand", root, planetC + dir * 705f + Random.onUnitSphere * 40f, Rand(50f, 110f), cloud, new Vector3(1.6f, 0.12f, 0.8f));
                c.transform.rotation = Quaternion.FromToRotation(Vector3.up, dir);
            }
            var kill = Box("VoidKillZone", root, new Vector3(IslandCenter.x, floorY - 28f, IslandCenter.y), new Vector3(4000f, 1f, 4000f), null);
            kill.GetComponent<Collider>().isTrigger = true;
            kill.AddComponent<KillZone>();
        }

        // ================================================================== scenery

        static float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        static bool Free(Vector3 p, float clearance)
        {
            if ((new Vector2(p.x, p.z) - IslandCenter).magnitude > IslandRadius + 60f) return false;
            foreach (var r in blocked) if (r.Distance(p.x, p.z) < clearance) return false;
            foreach (var w in walkways)
                foreach (var q in w.points)
                    if (new Vector2(q.x - p.x, q.z - p.z).sqrMagnitude < (PathHalfWidth + clearance) * (PathHalfWidth + clearance)) return false;
            return true;
        }

        /// <summary>A small deck bolted to the side of a bridge, so things beside the way are reachable (hop the rail).</summary>
        static Vector3 SideDeck(string name, Transform parent, HeightField t, Vector3 pathPoint, Vector3 right, float s, float width, float depth)
        {
            var center = pathPoint + right * s * (PathHalfWidth + depth * 0.5f - 0.1f);
            var rot = Quaternion.LookRotation(right * s);
            var deck = Box(name, parent, center + Vector3.down * 0.25f, new Vector3(width, 0.5f, depth), Mat("Porch_Boards", new Color(0.88f, 0.72f, 0.54f)));
            deck.transform.rotation = rot;
            var b = new Bounds(center + Vector3.down * 0.5f, new Vector3(Mathf.Max(width, depth) * 0.8f, 0.1f, Mathf.Max(width, depth) * 0.8f));
            Foundation(name + "_Under", parent, b, t);
            blocked.Add(Rect2.Around(center, Mathf.Max(width, depth) * 0.5f));
            return center;
        }

        static void Scenery(HeightField t, Transform parent)
        {
            var root = new GameObject("Scenery").transform;
            root.SetParent(parent, false);
            int bolts = 0;

            // Climbing masts on little side decks, each with a golden memory bolt on top.
            int masts = 0;
            foreach (var w in walkways)
            {
                if (masts >= 6 || w.points.Count < 12) continue;
                int i = w.points.Count / 2;
                var d = w.points[i + 1] - w.points[i - 1];
                var right = Vector3.Cross(Vector3.up, new Vector3(d.x, 0, d.z).normalized);
                foreach (float s in new[] { 1f, -1f })
                {
                    var probe = w.points[i] + right * s * (PathHalfWidth + 2f);
                    if (!Free(probe, 1.5f)) continue;
                    var at = SideDeck("MastDeck" + masts, root, t, w.points[i], right, s, 4f, 4f);
                    Spire("Mast" + masts, root, at, Rand(6.5f, 10f), "mast_" + masts);
                    masts++;
                    bolts++;
                    break;
                }
            }

            // Crew cabins on side decks, with potted trees. Every third one has a flat roof hiding a bolt.
            int cabins = 0;
            for (int tries = 0; tries < 300 && cabins < 10; tries++)
            {
                var w = walkways[rng.Next(walkways.Count)];
                if (w.points.Count < 10) continue;
                int i = rng.Next(4, w.points.Count - 4);
                var d = w.points[i + 1] - w.points[i - 1];
                var right = Vector3.Cross(Vector3.up, new Vector3(d.x, 0, d.z).normalized);
                float s = rng.Next(2) == 0 ? 1f : -1f;
                var probe = w.points[i] + right * s * (PathHalfWidth + 5f);
                if (!Free(probe, 4.5f)) continue;
                var at = SideDeck("CabinDeck" + cabins, root, t, w.points[i], right, s, 9f, 9f);
                bool flat = cabins % 3 == 1;
                Cottage("Cabin" + cabins, root, t, at + right * s * 1.2f, -right * s, cabins, flat ? "cabin_" + cabins : null);
                if (flat) bolts++;
                var side = Vector3.Cross(Vector3.up, right * s);
                foreach (float k in new[] { -1f, 1f })
                {
                    var pot = at - right * s * 3.2f + side * k * 3.4f;
                    Cylinder("Planter", root, pot + Vector3.up * 0.3f, 0.45f, 0.6f, Mat("Planter", new Color(0.85f, 0.5f, 0.38f)));
                    Tree("Tree", root, pot + Vector3.up * 0.55f, 0.55f, cabins + (int)k);
                }
                cabins++;
            }

            if (style == Style.Ocean) OceanDressing(root, t);
            else SpaceDressing(root, t);
            Debug.Log($"[World] Scenery: {masts} masts, {cabins} cabins, {bolts} bolts outside the calls.");
        }

        static void OceanDressing(Transform root, HeightField t)
        {
            // Buoys and little boats.
            var red = Mat("Buoy_Red", new Color(0.95f, 0.32f, 0.3f));
            var white = Mat("Buoy_White", new Color(0.98f, 0.97f, 0.95f));
            for (int i = 0, made = 0; i < 300 && made < 28; i++)
            {
                var at = new Vector3(IslandCenter.x + Rand(-IslandRadius, IslandRadius), SeaY, IslandCenter.y + Rand(-IslandRadius, IslandRadius));
                if (!Free(at, 3f)) continue;
                Sphere("Buoy", root, at + Vector3.up * 0.3f, 0.6f, made % 2 == 0 ? red : white);
                Cylinder("BuoyTop", root, at + Vector3.up * 1.2f, 0.12f, 1f, white);
                Sphere("BuoyLight", root, at + Vector3.up * 1.8f, 0.15f, Glow("Buoy_Light", new Color(1f, 0.85f, 0.4f), 2f));
                made++;
            }
            Color[] hulls = { new Color(0.35f, 0.6f, 0.95f), new Color(1f, 0.6f, 0.4f), new Color(0.98f, 0.97f, 0.95f) };
            for (int i = 0, made = 0; i < 200 && made < 9; i++)
            {
                var at = new Vector3(IslandCenter.x + Rand(-IslandRadius, IslandRadius), SeaY + 0.35f, IslandCenter.y + Rand(-IslandRadius, IslandRadius));
                if (!Free(at, 6f)) continue;
                var boat = new GameObject("Boat").transform;
                boat.SetParent(root, false);
                boat.SetPositionAndRotation(at, Quaternion.Euler(0f, Rand(0f, 360f), 0f));
                B(boat, "Hull", 0f, 0f, 0f, new Vector3(2.2f, 0.9f, 5f), Mat("Boat_" + made % 3, hulls[made % 3]));
                B(boat, "Cabin", 0f, 0.9f, -0.6f, new Vector3(1.6f, 1f, 1.8f), white);
                B(boat, "Mast", 0f, 2.2f, 0.8f, new Vector3(0.12f, 3f, 0.12f), white, false);
                made++;
            }
            // Rocky islets, and seagulls overhead.
            var rock = Mat("Rock", new Color(0.7f, 0.66f, 0.66f));
            for (int i = 0, made = 0; i < 200 && made < 10; i++)
            {
                var at = new Vector3(IslandCenter.x + Rand(-IslandRadius - 40f, IslandRadius + 40f), SeaY, IslandCenter.y + Rand(-IslandRadius - 40f, IslandRadius + 40f));
                if (!Free(at, 8f)) continue;
                for (int k = 0; k < 4; k++)
                {
                    float size = Rand(2f, 5f);
                    var r = Box("Islet", root, at + new Vector3(Rand(-3f, 3f), size * 0.2f, Rand(-3f, 3f)), new Vector3(size * 1.3f, size, size), rock);
                    r.transform.rotation = Quaternion.Euler(Rand(-10f, 10f), Rand(0f, 360f), Rand(-10f, 10f));
                }
                made++;
            }
            var gull = Mat("Gull", Color.white, 0f, outline: false);
            for (int i = 0; i < 16; i++)
            {
                var at = new Vector3(IslandCenter.x + Rand(-160f, 160f), Rand(22f, 45f), IslandCenter.y + Rand(-160f, 160f));
                var g = new GameObject("Gull").transform;
                g.SetParent(root, false);
                g.SetPositionAndRotation(at, Quaternion.Euler(0f, Rand(0f, 360f), 0f));
                B(g, "WingL", -0.45f, 0.1f, 0f, new Vector3(0.9f, 0.05f, 0.28f), gull, false, new Vector3(0f, 0f, 18f));
                B(g, "WingR", 0.45f, 0.1f, 0f, new Vector3(0.9f, 0.05f, 0.28f), gull, false, new Vector3(0f, 0f, -18f));
            }
            // Other platform towns on the horizon: the Reach goes on and on.
            for (int i = 0; i < 9; i++)
            {
                float a = i * 40f + Rand(-10f, 10f);
                var dir = new Vector3(Mathf.Cos(a * Mathf.Deg2Rad), 0f, Mathf.Sin(a * Mathf.Deg2Rad));
                var c = new Vector3(IslandCenter.x, SeaY, IslandCenter.y) + dir * Rand(330f, 460f);
                float w = Rand(30f, 60f), h = Rand(8f, 18f);
                var town = new GameObject("DistantPlatform").transform;
                town.SetParent(root, false);
                town.position = c;
                Box("Deck", town, c + Vector3.up * h, new Vector3(w, 2f, w * 0.7f), Mat("Deck_Steel", new Color(0.72f, 0.76f, 0.84f)), false);
                foreach (float sx in new[] { -0.4f, 0.4f })
                    foreach (float sz in new[] { -0.3f, 0.3f })
                        Cylinder("Leg", town, c + new Vector3(sx * w, h * 0.5f, sz * w), 1.5f, h, Mat("Rig_Leg", new Color(1f, 0.78f, 0.32f)));
                for (int k = 0; k < 4; k++)
                    Box("Building", town, c + new Vector3(Rand(-w * 0.35f, w * 0.35f), h + 4f, Rand(-w * 0.2f, w * 0.2f)), new Vector3(Rand(5f, 10f), Rand(5f, 14f), Rand(5f, 9f)), Mat("House_" + k % HouseColors.Length, HouseColors[k % HouseColors.Length]), false);
            }
            Clouds(root, 34);
        }

        static void SpaceDressing(Transform root, HeightField t)
        {
            // Solar arrays, satellites and slow-tumbling cargo floating round the station.
            var panel = Mat("Solar_Blue", new Color(0.25f, 0.4f, 0.9f), 0.8f);
            var frame = Mat("Hull_White", new Color(0.93f, 0.94f, 0.98f));
            for (int i = 0, made = 0; i < 200 && made < 8; i++)
            {
                var at = new Vector3(IslandCenter.x + Rand(-IslandRadius, IslandRadius), Rand(-10f, 30f), IslandCenter.y + Rand(-IslandRadius, IslandRadius));
                if (!Free(at, 14f)) continue;
                var arr = new GameObject("SolarArray").transform;
                arr.SetParent(root, false);
                arr.SetPositionAndRotation(at, Quaternion.Euler(0f, Rand(0f, 180f), Rand(-20f, 20f)));
                B(arr, "Spine", 0f, 0f, 0f, new Vector3(26f, 0.5f, 0.5f), frame, false);
                foreach (float s in new[] { -1f, 1f })
                    for (int k = 0; k < 3; k++)
                        B(arr, "Panel", -9f + k * 9f, 0f, s * 3.2f, new Vector3(8f, 0.15f, 5.4f), panel, false);
                made++;
            }
            Color[] cargo = { new Color(1f, 0.62f, 0.25f), new Color(0.55f, 0.8f, 1f), new Color(0.75f, 0.55f, 1f), new Color(1f, 0.85f, 0.35f) };
            for (int i = 0, made = 0; i < 300 && made < 24; i++)
            {
                var at = new Vector3(IslandCenter.x + Rand(-IslandRadius, IslandRadius), Rand(-15f, 40f), IslandCenter.y + Rand(-IslandRadius, IslandRadius));
                if (!Free(at, 5f)) continue;
                float s = Rand(1.2f, 3f);
                var c = Box("Cargo", root, at, Vector3.one * s, Mat("Crate_" + made % 4, cargo[made % 4]), false);
                c.transform.rotation = Quaternion.Euler(Rand(0f, 360f), Rand(0f, 360f), Rand(0f, 360f));
                made++;
            }
            var rock = Mat("Asteroid", new Color(0.62f, 0.56f, 0.58f));
            for (int i = 0; i < 26; i++)
            {
                var dir = new Vector3(Rand(-1f, 1f), Rand(-0.3f, 0.5f), Rand(-1f, 1f)).normalized;
                var at = new Vector3(IslandCenter.x, 0f, IslandCenter.y) + dir * Rand(280f, 520f);
                float s = Rand(6f, 22f);
                Box("Asteroid", root, at, new Vector3(s * 1.3f, s, s * 1.1f), rock, false).transform.rotation = Quaternion.Euler(Rand(0f, 360f), Rand(0f, 360f), 0f);
            }
            // A big ring of habitat modules round the whole station, far out: there's more of the Bureau up here.
            var ringMat = Mat("Hull_White", new Color(0.93f, 0.94f, 0.98f));
            var window = Glow("Window_Warm", new Color(1f, 0.9f, 0.6f), 0.9f);
            var ringC = new Vector3(IslandCenter.x, 60f, IslandCenter.y);
            for (int i = 0; i < 40; i++)
            {
                float a = i * Mathf.PI * 2f / 40f;
                var at = ringC + new Vector3(Mathf.Cos(a), 0.18f * Mathf.Sin(a), Mathf.Sin(a)) * 420f;
                var seg = Box("RingSegment", root, at, new Vector3(68f, 9f, 12f), ringMat, false);
                seg.transform.rotation = Quaternion.LookRotation(new Vector3(-Mathf.Sin(a), 0f, Mathf.Cos(a))) * Quaternion.Euler(0f, 90f, 0f);
                var lit = Box("RingWindows", root, at + Vector3.up * 1f - (at - ringC).normalized * 6.1f, new Vector3(1f, 2f, 1f), window, false);
                lit.transform.rotation = seg.transform.rotation;
                lit.transform.localScale = new Vector3(60f, 2f, 0.4f);
            }
        }

        static void Clouds(Transform root, int count)
        {
            var cloud = Mat("Cloud", new Color(1f, 1f, 1f), 0f, outline: false);
            for (int i = 0; i < count; i++)
            {
                var c = new Vector3(IslandCenter.x + Rand(-420f, 420f), Rand(85f, 135f), IslandCenter.y + Rand(-420f, 420f));
                var cr = new GameObject("Cloud").transform;
                cr.SetParent(root, false);
                for (int k = 0; k < 5; k++)
                {
                    var puff = Sphere("Puff", cr, c + new Vector3(Rand(-14f, 14f), Rand(-2f, 4f), Rand(-6f, 6f)), Rand(6f, 11f), cloud, new Vector3(1.4f, 0.8f, 1f));
                    puff.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }
        }

        /// <summary>A climbable mast with chunky handholds on every side and a memory bolt on top.</summary>
        static void Spire(string name, Transform parent, Vector3 baseAt, float height, string boltId)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            float topY = baseAt.y + height;
            var mat = style == Style.Ocean ? Mat("Mast_Red", new Color(0.95f, 0.45f, 0.4f)) : Mat("Mast_Lilac", new Color(0.72f, 0.66f, 0.98f));
            var pillar = Box("Pillar", root, new Vector3(baseAt.x, baseAt.y + height * 0.5f, baseAt.z), new Vector3(1.8f, height, 1.8f), mat);
            pillar.AddComponent<ClimbSurface>();
            Box("Cap", root, new Vector3(baseAt.x, topY + 0.2f, baseAt.z), new Vector3(2.6f, 0.4f, 2.6f), Mat("Hull_White", new Color(0.93f, 0.94f, 0.98f)));
            var hold = Mat("Grip_Hold", new Color(1f, 0.82f, 0.3f));
            for (float y = baseAt.y + 0.8f; y < topY - 0.3f; y += 0.9f)
                for (int face = 0; face < 4; face++)
                {
                    var dir = Quaternion.Euler(0f, face * 90f, 0f) * Vector3.forward;
                    var across = Vector3.Cross(Vector3.up, dir);
                    Sphere("Hold", root, new Vector3(baseAt.x, y, baseAt.z) + dir * 0.93f + across * Rand(-0.5f, 0.5f), 0.11f, hold, new Vector3(1.3f, 0.8f, 0.7f));
                }
            Sphere("Beacon", root, new Vector3(baseAt.x + 0.9f, topY + 0.7f, baseAt.z), 0.18f, Glow("Mast_Beacon", new Color(1f, 0.4f, 0.35f), 2.4f));
            Bolt(boltId, root, new Vector3(baseAt.x, topY + 1.2f, baseAt.z));
        }

        static readonly Color[] HouseColors =
        {
            new Color(1f, 0.8f, 0.7f), new Color(0.75f, 0.88f, 1f), new Color(1f, 0.93f, 0.6f),
            new Color(0.8f, 0.95f, 0.78f), new Color(0.9f, 0.8f, 1f), new Color(1f, 0.75f, 0.8f),
        };

        static void Cottage(string name, Transform parent, HeightField t, Vector3 at, Vector3 face, int variant, string boltId)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(at, Quaternion.LookRotation(face));
            Vector3 P(float x, float y, float z) => root.TransformPoint(new Vector3(x, y, z));
            GameObject R(GameObject g, Quaternion local) { g.transform.rotation = root.rotation * local; return g; }
            float w = 5f + variant % 3, d = 4.6f, h = boltId != null ? 5.2f : 3.4f;
            float sink = 0.45f;
            var walls = R(Box("Walls", root, P(0f, (h - sink) * 0.5f, 0f), new Vector3(w, h + sink, d), Mat("House_" + variant % HouseColors.Length, HouseColors[variant % HouseColors.Length])), Quaternion.identity);
            walls.AddComponent<ClimbSurface>().staminaMultiplier = 0.9f;
            var roofMat = Mat("Roof_" + variant % 3, variant % 3 == 0 ? new Color(0.92f, 0.38f, 0.35f) : variant % 3 == 1 ? new Color(0.35f, 0.55f, 0.85f) : new Color(0.5f, 0.42f, 0.38f));
            if (boltId == null)
            {
                R(Box("Roof", root, P(0f, h, 0f), new Vector3(w + 0.6f, d * 0.78f, d * 0.78f), roofMat), Quaternion.Euler(45f, 0f, 0f));
                R(Box("Chimney", root, P(w * 0.3f, h + 1.4f, -0.6f), new Vector3(0.6f, 1.4f, 0.6f), Mat("Brick", new Color(0.8f, 0.45f, 0.35f))), Quaternion.identity);
            }
            else
            {
                R(Box("RoofTop", root, P(0f, h + 0.1f, 0f), new Vector3(w + 0.3f, 0.2f, d + 0.3f), roofMat), Quaternion.identity);
                foreach (float s in new[] { -1f, 1f })
                    R(Box("Parapet", root, P(0f, h + 0.45f, s * (d * 0.5f + 0.05f)), new Vector3(w + 0.3f, 0.5f, 0.2f), roofMat), Quaternion.identity);
                Bolt(boltId, root, P(0f, h + 1.1f, 0f));
            }
            R(Box("Door", root, P(0f, 0.95f, d * 0.5f + 0.03f), new Vector3(1.1f, 1.9f, 0.08f), Mat("Door_Wood", new Color(0.62f, 0.42f, 0.3f)), false), Quaternion.identity);
            var glass = Glow("Window_Warm", new Color(1f, 0.9f, 0.6f), 0.9f);
            foreach (float s in new[] { -1f, 1f })
                R(Box("Window", root, P(s * w * 0.3f, 1.9f, d * 0.5f + 0.03f), new Vector3(0.9f, 0.8f, 0.06f), glass, false), Quaternion.identity);
            Box("Doormat", root, P(0f, 0.02f, d * 0.5f + 0.6f), new Vector3(1.2f, 0.04f, 0.7f), Mat("Doormat", new Color(0.95f, 0.6f, 0.3f)), false).transform.rotation = root.rotation;
        }

        static void Tree(string name, Transform parent, Vector3 at, float s, int variant)
        {
            variant = Mathf.Abs(variant);
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.position = at;
            var trunk = Cylinder("Trunk", root, at + Vector3.up * 1.2f * s, 0.22f * s, 2.4f * s, Mat("Bark", new Color(0.62f, 0.44f, 0.32f)));
            var col = trunk.AddComponent<CapsuleCollider>();
            col.radius = 0.5f;
            col.height = 2f;
            Color[] leaves = { new Color(0.4f, 0.75f, 0.35f), new Color(0.5f, 0.82f, 0.38f), new Color(0.35f, 0.68f, 0.42f), new Color(1f, 0.7f, 0.78f) };
            var leaf = Mat("Leaves_" + (variant % 11 == 0 ? 3 : variant % 3), leaves[variant % 11 == 0 ? 3 : variant % 3]);
            if (variant % 4 == 3)
            {
                // Pine: stacked blobs getting smaller.
                for (int i = 0; i < 3; i++)
                    Sphere("Leaves", root, at + Vector3.up * (2.4f + i * 1.1f) * s, (1.3f - i * 0.32f) * s, Mat("Pine", new Color(0.25f, 0.6f, 0.42f)), new Vector3(1f, 0.75f, 1f));
            }
            else
            {
                Sphere("Leaves", root, at + Vector3.up * 3f * s, 1.45f * s, leaf);
                Sphere("Leaves", root, at + new Vector3(0.8f, 2.6f, 0.3f) * s, 0.95f * s, leaf);
                Sphere("Leaves", root, at + new Vector3(-0.7f, 2.7f, -0.4f) * s, 1f * s, leaf);
            }
        }

        // ================================================================== PIP, cameras, systems

        static WorldDirector BuildSystems(Destination dest, Transform podSpawn)
        {
            var first = placed[0];
            var cam = BuildCamera();
            var spawn = podSpawn ? podSpawn : first.area.spawnPoint;
            var bot = BuildBot(cam, spawn.position, spawn.eulerAngles.y);

            // BuildBot drops a service bay under PIP; move it to the side of the first porch.
            var home = GameObject.Find("HomeCharger");
            if (home)
            {
                Vector3 n = first.Dir(first.normal), side = Vector3.Cross(Vector3.up, n);
                home.transform.SetParent(first.root, true);
                home.transform.position = first.W(first.entrance) + n * 4.6f + side * 3.6f;
            }

            var game = new GameObject("Game");
            game.AddComponent<GameManager>();
            var narrator = game.AddComponent<DispatchNarrator>();
            narrator.common = CommonDispatch();
            var hud = game.AddComponent<PrototypeHUD>();
            hud.tether = bot.GetComponent<TetherCable>();
            hud.grabber = bot.GetComponent<Grabber>();
            hud.toolArm = bot.GetComponent<ToolArm>();
            hud.narrator = narrator;
            hud.dispatchVoice = DispatchVoice(game);

            // Title flyover: a slow orbit round the whole place.
            var center = Vector3.zero;
            foreach (var p in placed) center += p.origin;
            center /= placed.Count;
            float radius = 0f;
            foreach (var p in placed) radius = Mathf.Max(radius, Vector3.Distance(new Vector3(center.x, 0, center.z), new Vector3(p.origin.x, 0, p.origin.z)));
            radius = Mathf.Clamp(radius + 70f, 110f, 240f);
            var tc = new GameObject("TitleCamera");
            var c = tc.AddComponent<Camera>();
            c.clearFlags = CameraClearFlags.Skybox;
            c.fieldOfView = 42f;
            c.nearClipPlane = 0.5f;
            c.farClipPlane = 2500f;
            tc.AddComponent<AudioListener>();
            EnablePost(c);
            var orbit = tc.AddComponent<MenuCamera>();
            orbit.pivot = center;
            orbit.radius = radius;
            orbit.height = radius * 0.42f;
            orbit.lookHeight = 4f;
            orbit.degreesPerSecond = 2.5f;
            orbit.Place(200f);

            var world = new GameObject("World");
            var director = world.AddComponent<WorldDirector>();
            var areas = new Area[placed.Count];
            for (int i = 0; i < placed.Count; i++) areas[i] = placed[i].area;
            director.areas = areas;
            director.bot = bot;
            director.botCamera = cam;
            director.titleCamera = c;
            director.displayName = dest.title;
            director.titleScene = System.IO.Path.GetFileNameWithoutExtension(Destinations[0].scene);
            director.podSpawn = podSpawn;
            var links = new List<WorldDirector.WorldLink>();
            foreach (var other in Destinations)
            {
                var range = LevelRange(other);
                if (range.first >= 0) links.Add(new WorldDirector.WorldLink { scene = System.IO.Path.GetFileNameWithoutExtension(other.scene), firstLevel = range.first, lastLevel = range.last });
            }
            director.links = links.ToArray();
            var menu = world.AddComponent<MainMenu>();
            menu.subtitle = dest.style == Style.Station ? "Robot Rescue Bureau  -  Station Control" : "Brightwater Reach  -  ocean platform city";
            director.titleMenu = menu;
            director.worldNarrator = narrator;
            return director;
        }
    }
}
