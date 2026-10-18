using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// Procedural meshes for the soft, toy-like look: rounded boxes (cached per size in one asset), plus helpers for
    /// terrain grids and path ribbons. Rounded boxes have smooth normals all round, so the toon outline wraps cleanly.
    /// </summary>
    public static class MeshKit
    {
        const string LibraryPath = ChamberKit.Root + "/Art/Meshes/RoundedBoxes.asset";
        static Mesh library;
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        public static void ResetCache()
        {
            library = null;
            cache.Clear();
        }

        static Mesh Library()
        {
            if (library) return library;
            ChamberKit.EnsureFolder(ChamberKit.Root + "/Art/Meshes");
            library = AssetDatabase.LoadAssetAtPath<Mesh>(LibraryPath);
            if (!library)
            {
                library = BuildRoundedBox(Vector3.one, 0.1f, 3);
                library.name = "RoundedBox_Unit";
                AssetDatabase.CreateAsset(library, LibraryPath);
            }
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(LibraryPath))
                if (o is Mesh m && !cache.ContainsKey(m.name)) cache[m.name] = m;
            return library;
        }

        /// <summary>
        /// Rounded box sized for an object whose localScale will be <paramref name="size"/>: the mesh is a unit cube
        /// in object space with bevels pre-squashed so they come out round (radius <paramref name="radius"/> m) once scaled.
        /// </summary>
        public static Mesh RoundedBoxFor(Vector3 size, float radius)
        {
            Library();
            size = new Vector3(Q(size.x), Q(size.y), Q(size.z));
            radius = Mathf.Min(radius, Mathf.Min(size.x, Mathf.Min(size.y, size.z)) * 0.45f);
            string key = $"RB_{size.x:0.00}_{size.y:0.00}_{size.z:0.00}_{radius:0.000}";
            if (cache.TryGetValue(key, out var m) && m) return m;
            m = BuildRoundedBox(size, radius, 3);
            m.name = key;
            // Store in object space as a unit cube: divide positions by size, fix normals for the scale to come.
            var v = m.vertices;
            var n = m.normals;
            for (int i = 0; i < v.Length; i++)
            {
                v[i] = new Vector3(v[i].x / size.x, v[i].y / size.y, v[i].z / size.z);
                n[i] = new Vector3(n[i].x * size.x, n[i].y * size.y, n[i].z * size.z).normalized;
            }
            m.vertices = v;
            m.normals = n;
            m.RecalculateBounds();
            m.RecalculateTangents();
            AssetDatabase.AddObjectToAsset(m, LibraryPath);
            cache[key] = m;
            return m;
        }

        static float Q(float x) => Mathf.Max(0.01f, Mathf.Round(x * 100f) / 100f);

        /// <summary>Rounded box of the given world size, centred on the origin. Each face is a grid; points are pushed
        /// onto the rounded surface by clamping to the inner box and re-extending along the offset.</summary>
        public static Mesh BuildRoundedBox(Vector3 size, float radius, int bevelSegments)
        {
            Vector3 h = size * 0.5f;
            radius = Mathf.Clamp(radius, 0.001f, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.999f);
            Vector3 inner = h - Vector3.one * radius;

            float[] Coords(float half)
            {
                var list = new List<float> { -half };
                for (int i = 1; i <= bevelSegments; i++)
                {
                    float a = (1f - Mathf.Cos(Mathf.PI * 0.5f * i / bevelSegments)) * radius;
                    list.Add(-half + a);
                }
                for (int i = bevelSegments; i >= 1; i--)
                {
                    float a = (1f - Mathf.Cos(Mathf.PI * 0.5f * i / bevelSegments)) * radius;
                    float c = half - a;
                    if (c > list[list.Count - 1] + 1e-4f) list.Add(c);
                }
                if (half > list[list.Count - 1] + 1e-4f) list.Add(half);
                return list.ToArray();
            }

            float[] cx = Coords(h.x), cy = Coords(h.y), cz = Coords(h.z);
            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            void Face(Vector3 normal, Vector3 uAxis, Vector3 vAxis, float[] us, float[] vs, float depth)
            {
                int start = verts.Count;
                for (int j = 0; j < vs.Length; j++)
                    for (int i = 0; i < us.Length; i++)
                    {
                        Vector3 p = normal * depth + uAxis * us[i] + vAxis * vs[j];
                        Vector3 q = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                        Vector3 d = p - q;
                        Vector3 nrm = d.sqrMagnitude > 1e-10f ? d.normalized : normal;
                        verts.Add(q + nrm * radius);
                        norms.Add(nrm);
                        uvs.Add(new Vector2((us[i] / (us[us.Length - 1] * 2f)) + 0.5f, (vs[j] / (vs[vs.Length - 1] * 2f)) + 0.5f));
                    }
                int w = us.Length;
                for (int j = 0; j < vs.Length - 1; j++)
                    for (int i = 0; i < w - 1; i++)
                    {
                        int a = start + j * w + i, b = a + 1, c = a + w, e = c + 1;
                        tris.Add(a); tris.Add(c); tris.Add(b);
                        tris.Add(b); tris.Add(c); tris.Add(e);
                    }
            }

            // u x v must point along the normal for clockwise (Unity front-face) winding with the order above.
            Face(Vector3.right, Vector3.forward, Vector3.up, cz, cy, h.x);
            Face(Vector3.left, Vector3.back, Vector3.up, Neg(cz), cy, h.x);
            Face(Vector3.up, Vector3.right, Vector3.forward, cx, cz, h.y);
            Face(Vector3.down, Vector3.right, Vector3.back, cx, Neg(cz), h.y);
            Face(Vector3.forward, Vector3.left, Vector3.up, Neg(cx), cy, h.z);
            Face(Vector3.back, Vector3.right, Vector3.up, cx, cy, h.z);

            var mesh = new Mesh();
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static float[] Neg(float[] a)
        {
            var r = new float[a.Length];
            for (int i = 0; i < a.Length; i++) r[i] = -a[a.Length - 1 - i];
            return r;
        }
    }
}
