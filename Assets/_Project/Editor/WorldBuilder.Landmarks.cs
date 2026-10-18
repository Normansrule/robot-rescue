using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static RobotRescue.EditorTools.ChamberKit;

namespace RobotRescue.EditorTools
{
    /// <summary>
    /// The chapters still to come, built as big readable landmarks you can see (and walk up to) from the start:
    /// the giant oak with its cat, the school with its volcano, the lighthouse, the Ferris wheel, the junkyard,
    /// and the festival on the summit. Each has a "coming soon" sign.
    /// </summary>
    public static partial class WorldBuilder
    {
        static void BuildLandmarks(List<Landmark> spots, HeightField t, Transform parent)
        {
            var root = new GameObject("Landmarks").transform;
            root.SetParent(parent, false);
            foreach (var l in spots)
            {
                var frame = new GameObject("Landmark_" + l.kind).transform;
                frame.SetParent(root, false);
                var look = l.facing;
                look.y = 0f;
                frame.SetPositionAndRotation(l.pos, Quaternion.LookRotation(look.sqrMagnitude > 0.01f ? look.normalized : Vector3.forward));
                Foundation("Deck_" + l.kind, root, new Bounds(l.pos + Vector3.down * 0.2f, new Vector3(l.radius * 1.5f, 0.1f, l.radius * 1.5f)), t);
                switch (l.kind)
                {
                    case "park": Park(frame); break;
                    case "school": School(frame); break;
                    case "junkyard": Junkyard(frame); break;
                    case "festival": Festival(frame); break;
                    case "lighthouse": Lighthouse(frame, t); break;
                    case "pier": Pier(frame, t); break;
                }
            }
        }

        // ------------------------------------------------------------------ helpers in a landmark's own frame

        static Vector3 At(Transform f, float x, float y, float z) => f.TransformPoint(new Vector3(x, y, z));

        static GameObject B(Transform f, string name, float x, float y, float z, Vector3 size, Material m, bool collider = true, Vector3? euler = null)
        {
            var go = Box(name, f, At(f, x, y, z), size, m, collider);
            go.transform.rotation = f.rotation * Quaternion.Euler(euler ?? Vector3.zero);
            return go;
        }

        static GameObject C(Transform f, string name, float x, float y, float z, float radius, float height, Material m, Vector3? euler = null)
        {
            var go = Cylinder(name, f, At(f, x, y, z), radius, height, m);
            go.transform.rotation = f.rotation * Quaternion.Euler(euler ?? Vector3.zero);
            return go;
        }

        static GameObject S(Transform f, string name, float x, float y, float z, float radius, Material m, Vector3? scale = null)
        {
            var go = Sphere(name, f, At(f, x, y, z), radius, m, scale);
            go.transform.rotation = f.rotation;
            return go;
        }

        /// <summary>Wooden sign on two legs, facing the frame's forward (toward the visitor).</summary>
        static void ComingSoon(Transform f, float z, string chapter, string title)
        {
            var wood = Mat("Sign_Wood", new Color(0.8f, 0.6f, 0.42f));
            B(f, "SignBoard", 0f, 2.1f, z, new Vector3(6.4f, 2.4f, 0.2f), wood);
            foreach (float s in new[] { -1f, 1f }) B(f, "SignLeg", s * 2.7f, 0.5f, z, new Vector3(0.2f, 1f, 0.2f), wood);
            float facing = Mathf.Atan2(f.forward.x, f.forward.z) * Mathf.Rad2Deg;
            var ink = new Color(0.35f, 0.2f, 0.1f);
            Label(chapter, f, At(f, 0f, 2.85f, z + 0.15f), facing, 0.06f, ink);
            Label(title, f, At(f, 0f, 2.15f, z + 0.15f), facing, 0.1f, ink);
            Label("COMING SOON", f, At(f, 0f, 1.4f, z + 0.15f), facing, 0.07f, new Color(0.85f, 0.3f, 0.25f));
            // A rope barrier across the way in.
            var post = Mat("Lamp_Post", new Color(0.3f, 0.35f, 0.5f));
            foreach (float s in new[] { -4.5f, 4.5f }) B(f, "RopePost", s, 0.5f, z + 1.2f, new Vector3(0.18f, 1f, 0.18f), post);
            B(f, "Rope", 0f, 0.85f, z + 1.2f, new Vector3(9f, 0.06f, 0.06f), Mat("Rope_Red", new Color(0.9f, 0.3f, 0.3f)), false);
        }

        static void Pad(Transform f, float radius, Color color)
        {
            var pad = C(f, "Pad", 0f, -0.05f, 0f, radius, 0.3f, Mat("Pad_" + ColorUtility.ToHtmlStringRGB(color), color));
            var col = pad.AddComponent<BoxCollider>();
            col.size = new Vector3(0.9f, 2f, 0.9f);
        }

        // ------------------------------------------------------------------ chapter 3: Brightwater Park

        static void Park(Transform f)
        {
            Pad(f, 13f, new Color(0.62f, 0.86f, 0.45f));
            var bark = Mat("Oak_Bark", new Color(0.55f, 0.38f, 0.28f));
            var leaf = Mat("Oak_Leaves", new Color(0.36f, 0.72f, 0.33f));
            var leaf2 = Mat("Oak_Leaves_Light", new Color(0.5f, 0.82f, 0.38f));
            var trunk = C(f, "OakTrunk", 0f, 6f, -3f, 1.7f, 12f, bark);
            var tc = trunk.AddComponent<CapsuleCollider>();
            tc.radius = 0.5f;
            tc.height = 2f;
            B(f, "Branch", 3.2f, 9f, -3f, new Vector3(6f, 0.9f, 0.9f), bark, true, new Vector3(0f, 0f, 22f));
            B(f, "Branch", -3f, 10f, -2.5f, new Vector3(5f, 0.8f, 0.8f), bark, true, new Vector3(0f, 20f, -25f));
            foreach (var (x, y, z, r, m) in new[] { (0f, 15f, -3f, 6.5f, leaf), (5f, 13f, -1f, 4.5f, leaf2), (-5f, 13.5f, -4f, 4.8f, leaf), (1f, 18f, -5f, 4.2f, leaf2), (-2f, 12f, 1.5f, 3.8f, leaf2) })
                S(f, "OakCanopy", x, y, z, r, m);
            // The cat, stuck on the branch (as cats are).
            var orange = Mat("Cat_Orange", new Color(1f, 0.62f, 0.28f));
            S(f, "CatBody", 5.2f, 10.4f, -3f, 0.55f, orange, new Vector3(1.4f, 0.9f, 0.9f));
            S(f, "CatHead", 5.9f, 10.95f, -2.75f, 0.42f, orange);
            B(f, "CatEarL", 5.75f, 11.35f, -2.95f, new Vector3(0.18f, 0.25f, 0.12f), orange, false, new Vector3(0f, 0f, 20f));
            B(f, "CatEarR", 6.05f, 11.35f, -2.55f, new Vector3(0.18f, 0.25f, 0.12f), orange, false, new Vector3(0f, 0f, -20f));
            C(f, "CatTail", 4.4f, 10.0f, -3f, 0.08f, 1.1f, orange, new Vector3(0f, 0f, -25f));
            var eye = Glow("Cat_Eye", new Color(0.2f, 0.9f, 0.4f), 1.4f);
            S(f, "CatEye", 6.2f, 11f, -2.45f, 0.07f, eye);
            S(f, "CatEye", 6.25f, 11f, -2.8f, 0.07f, eye);
            // Benches and a little pond.
            var wood = Mat("Bench_Wood", new Color(0.85f, 0.6f, 0.4f));
            foreach (float x in new[] { -7f, 7f })
            {
                B(f, "BenchSeat", x, 0.5f, 5f, new Vector3(2.6f, 0.15f, 0.7f), wood);
                B(f, "BenchBack", x, 0.95f, 5.3f, new Vector3(2.6f, 0.6f, 0.12f), wood, false);
            }
            var water = C(f, "Pond", -7f, 0.12f, -6f, 3.2f, 0.06f, Mat("Pond", new Color(0.4f, 0.75f, 0.95f), 0.9f, outline: false));
            water.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            S(f, "Duck", -6.5f, 0.35f, -5.5f, 0.25f, Mat("Duck", new Color(1f, 0.92f, 0.4f)));
            ComingSoon(f, 10.5f, "CHAPTER 3", "BRIGHTWATER PARK");
            Bolt("park_oak", f, At(f, 0f, 1.2f, -7f));
        }

        // ------------------------------------------------------------------ chapter 4: School Science Fair

        static void School(Transform f)
        {
            Pad(f, 15f, new Color(0.85f, 0.85f, 0.82f));
            var brick = Mat("School_Brick", new Color(0.95f, 0.65f, 0.5f));
            var trim = Mat("School_Trim", new Color(0.98f, 0.96f, 0.9f));
            var roof = Mat("School_Roof", new Color(0.35f, 0.45f, 0.75f));
            B(f, "SchoolHall", 0f, 3.5f, -5f, new Vector3(18f, 7f, 9f), brick);
            B(f, "SchoolRoof", 0f, 7.2f, -5f, new Vector3(18.8f, 0.5f, 9.8f), roof);
            B(f, "BellTower", 0f, 9.5f, -5f, new Vector3(3f, 4.5f, 3f), trim);
            B(f, "BellRoof", 0f, 12.2f, -5f, new Vector3(2.4f, 2.4f, 2.4f), roof, false, new Vector3(45f, 45f, 0f));
            S(f, "Bell", 0f, 10.3f, -3.4f, 0.5f, Mat("Bell_Gold", new Color(1f, 0.8f, 0.3f)));
            var glass = Glow("School_Window", new Color(0.6f, 0.85f, 1f), 0.8f);
            for (int i = -3; i <= 3; i++)
            {
                if (i == 0) continue;
                B(f, "Window", i * 2.4f, 2f, -0.47f, new Vector3(1.4f, 1.3f, 0.08f), glass, false);
                B(f, "Window", i * 2.4f, 5f, -0.47f, new Vector3(1.4f, 1.3f, 0.08f), glass, false);
            }
            B(f, "Doors", 0f, 1.4f, -0.45f, new Vector3(2.6f, 2.8f, 0.1f), Mat("Door_Blue", new Color(0.3f, 0.5f, 0.85f)), false);
            Label("BRIGHTWATER SCHOOL", f, At(f, 0f, 6.2f, -0.35f), Mathf.Atan2(f.forward.x, f.forward.z) * Mathf.Rad2Deg, 0.12f, new Color(0.3f, 0.3f, 0.55f));
            // The volcano project, slightly too big for the table.
            var rock = Mat("Volcano_Rock", new Color(0.6f, 0.42f, 0.32f));
            float[] radii = { 3.4f, 2.8f, 2.2f, 1.6f, 1.1f };
            for (int i = 0; i < radii.Length; i++) C(f, "Volcano", 8f, 0.6f + i * 1.1f, 5f, radii[i], 1.2f, rock);
            C(f, "Lava", 8f, 6.2f, 5f, 0.9f, 0.2f, Glow("Lava", new Color(1f, 0.4f, 0.1f), 2.5f));
            foreach (var (x, y, z) in new[] { (7.6f, 6.8f, 5f), (8.4f, 7.3f, 5.3f), (8f, 7.9f, 4.8f) })
                S(f, "LavaBlob", x, y, z, 0.25f, Glow("Lava", new Color(1f, 0.4f, 0.1f), 2.5f));
            B(f, "FairTable", -7f, 0.45f, 5f, new Vector3(3f, 0.9f, 1.4f), trim);
            S(f, "PotatoClock", -7.6f, 1.1f, 5f, 0.25f, Mat("Potato", new Color(0.8f, 0.65f, 0.4f)), new Vector3(1.3f, 1f, 1f));
            B(f, "Trophy", -6.3f, 1.25f, 5f, new Vector3(0.3f, 0.7f, 0.3f), Mat("Bell_Gold", new Color(1f, 0.8f, 0.3f)));
            ComingSoon(f, 11.5f, "CHAPTER 4", "SCIENCE FAIR");
            Bolt("school_roof", f, At(f, -7f, 8.3f, -5f));
        }

        // ------------------------------------------------------------------ chapter 5: Docks & Lighthouse

        static void Lighthouse(Transform f, HeightField t)
        {
            Pad(f, 11f, new Color(0.92f, 0.85f, 0.7f));
            var red = Mat("Lighthouse_Red", new Color(0.92f, 0.3f, 0.3f));
            var white = Mat("Lighthouse_White", new Color(0.98f, 0.97f, 0.95f));
            for (int i = 0; i < 8; i++)
            {
                float r = Mathf.Lerp(2.6f, 1.7f, i / 7f);
                var seg = C(f, "Tower", 0f, 0.9f + i * 1.8f, -2f, r, 1.8f, i % 2 == 0 ? white : red);
                if (i == 0) { var c = seg.AddComponent<CapsuleCollider>(); c.radius = 0.5f; c.height = 2f; }
            }
            C(f, "Gallery", 0f, 15.4f, -2f, 2.3f, 0.3f, Mat("Lamp_Post", new Color(0.3f, 0.35f, 0.5f)));
            C(f, "LampRoom", 0f, 16.5f, -2f, 1.3f, 1.8f, Glow("Lighthouse_Lamp", new Color(1f, 0.92f, 0.55f), 2.2f));
            S(f, "Cap", 0f, 17.6f, -2f, 1.4f, red, new Vector3(1f, 0.6f, 1f));
            // A dock running out into the water.
            var plank = Mat("Dock_Planks", new Color(0.72f, 0.55f, 0.4f));
            float deckY = -0.2f;
            B(f, "Dock", 4f, deckY, 14f, new Vector3(3.6f, 0.3f, 22f), plank);
            for (int i = 0; i < 6; i++)
                foreach (float s in new[] { -1f, 1f })
                    C(f, "Piling", 4f + s * 1.9f, deckY - 3.5f, 5f + i * 4f, 0.2f, 7f, plank);
            B(f, "Boat", 7.5f, SeaY + 0.6f - f.position.y, 20f, new Vector3(2f, 1f, 4.5f), Mat("Boat_Blue", new Color(0.35f, 0.6f, 0.95f)));
            B(f, "Crate", 3f, deckY + 0.55f, 7f, new Vector3(0.8f, 0.8f, 0.8f), Mat("Crate_Orange", new Color(1f, 0.62f, 0.25f)));
            var sign = new GameObject("SignFrame").transform;
            sign.SetParent(f, false);
            sign.SetPositionAndRotation(At(f, -5f, 0f, 2f), Quaternion.LookRotation(-f.forward));
            ComingSoon(sign, 0f, "CHAPTER 5", "DOCKS & LIGHTHOUSE");
            Bolt("dock_end", f, At(f, 4f, deckY + 1.2f, 24f));
        }

        // ------------------------------------------------------------------ chapter 6: Funland Pier

        static void Pier(Transform f, HeightField t)
        {
            Pad(f, 12f, new Color(0.95f, 0.85f, 0.65f));
            var plank = Mat("Pier_Planks", new Color(0.82f, 0.62f, 0.46f));
            float deckY = -0.2f;
            B(f, "PierDeck", 0f, deckY, 16f, new Vector3(16f, 0.4f, 22f), plank);
            for (int i = 0; i < 5; i++)
                foreach (float s in new[] { -7f, 0f, 7f })
                    C(f, "Piling", s, deckY - 3.5f, 7f + i * 4.5f, 0.3f, 7f, plank);
            // Ferris wheel: rim, spokes and colourful cabins in the frame's X/Y plane.
            float cx = 0f, cy = deckY + 12.5f, cz = 18f, radius = 10f;
            var steel = Mat("Wheel_White", new Color(0.97f, 0.97f, 1f));
            var lights = Glow("Wheel_Lights", new Color(1f, 0.75f, 0.85f), 1.6f);
            int segs = 28;
            for (int i = 0; i < segs; i++)
            {
                float a = i * Mathf.PI * 2f / segs;
                float len = 2f * Mathf.PI * radius / segs + 0.1f;
                B(f, "Rim", cx + Mathf.Cos(a) * radius, cy + Mathf.Sin(a) * radius, cz, new Vector3(len, 0.3f, 0.3f), steel, false, new Vector3(0f, 0f, a * Mathf.Rad2Deg + 90f));
                if (i % 2 == 0) S(f, "Bulb", cx + Mathf.Cos(a) * radius, cy + Mathf.Sin(a) * radius, cz - 0.3f, 0.16f, lights);
            }
            Color[] cabin = { new Color(1f, 0.5f, 0.5f), new Color(0.5f, 0.75f, 1f), new Color(1f, 0.85f, 0.35f), new Color(0.6f, 0.9f, 0.55f) };
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI * 2f / 8f;
                B(f, "Spoke", cx + Mathf.Cos(a) * radius * 0.5f, cy + Mathf.Sin(a) * radius * 0.5f, cz, new Vector3(radius, 0.18f, 0.18f), steel, false, new Vector3(0f, 0f, a * Mathf.Rad2Deg));
                B(f, "Cabin", cx + Mathf.Cos(a) * radius, cy + Mathf.Sin(a) * radius - 1f, cz + 0.6f, new Vector3(1.6f, 1.4f, 1.4f), Mat("Cabin_" + i % 4, cabin[i % 4]), false);
            }
            C(f, "Hub", cx, cy, cz, 0.9f, 0.8f, Mat("Wheel_Hub", new Color(1f, 0.55f, 0.2f)), new Vector3(90f, 0f, 0f));
            foreach (float s in new[] { -1f, 1f })
                foreach (float lean in new[] { -1f, 1f })
                    B(f, "Leg", cx + s * 3f, deckY + 6.2f, cz + lean * 1.4f, new Vector3(0.4f, 13.4f, 0.4f), steel, true, new Vector3(lean * 6f, 0f, s * 14f));
            // Striped game stall.
            B(f, "Stall", -6f, deckY + 1.5f, 9f, new Vector3(3f, 2.6f, 2.4f), Mat("Stall_White", new Color(0.98f, 0.96f, 0.92f)));
            for (int i = 0; i < 4; i++)
                B(f, "Awning", -7.1f + i * 0.75f, deckY + 3.05f, 9.2f, new Vector3(0.75f, 0.12f, 2.9f), Mat(i % 2 == 0 ? "Awning_Red" : "Stall_White", i % 2 == 0 ? new Color(0.95f, 0.35f, 0.35f) : new Color(0.98f, 0.96f, 0.92f)), false);
            var sign = new GameObject("SignFrame").transform;
            sign.SetParent(f, false);
            sign.SetPositionAndRotation(At(f, 0f, 0f, 1f), Quaternion.LookRotation(-f.forward));
            ComingSoon(sign, 0f, "CHAPTER 6", "FUNLAND PIER");
            Bolt("pier_wheel", f, At(f, 6f, deckY + 1.2f, 24f));
        }

        // ------------------------------------------------------------------ chapter 7: Old Bureau Junkyard

        static void Junkyard(Transform f)
        {
            Pad(f, 13f, new Color(0.7f, 0.64f, 0.58f));
            Color[] junk = { new Color(0.6f, 0.65f, 0.75f), new Color(0.9f, 0.55f, 0.3f), new Color(0.55f, 0.75f, 0.6f), new Color(0.85f, 0.8f, 0.5f), new Color(0.7f, 0.5f, 0.6f) };
            for (int pile = 0; pile < 4; pile++)
            {
                float px = (pile % 2 == 0 ? -1 : 1) * Rand(3f, 8f), pz = Rand(-8f, 2f);
                for (int k = 0; k < 9; k++)
                {
                    float sz = Rand(0.8f, 2f);
                    B(f, "Junk", px + Rand(-1.6f, 1.6f), sz * 0.5f + (k / 3) * 1.1f, pz + Rand(-1.6f, 1.6f), new Vector3(sz, sz * Rand(0.6f, 1f), sz * Rand(0.7f, 1.2f)), Mat("Junk_" + k % junk.Length, junk[k % junk.Length]), true, new Vector3(Rand(-15f, 15f), Rand(0f, 90f), Rand(-15f, 15f)));
                }
            }
            for (int i = 0; i < 3; i++) C(f, "Tyre", 9f, 0.25f + i * 0.5f, 5f, 0.8f, 0.45f, Mat("Tyre", new Color(0.25f, 0.25f, 0.28f)));
            // Old crane with a sad, rusty robot head hanging from it.
            var rust = Mat("Crane_Rust", new Color(0.85f, 0.55f, 0.3f));
            B(f, "CraneMast", -9f, 6f, -6f, new Vector3(1f, 12f, 1f), rust);
            B(f, "CraneBoom", -4.5f, 11.8f, -6f, new Vector3(10f, 0.7f, 0.7f), rust, false);
            B(f, "CraneRope", -0.5f, 9.5f, -6f, new Vector3(0.06f, 4.2f, 0.06f), Mat("Lamp_Post", new Color(0.3f, 0.35f, 0.5f)), false);
            B(f, "OldBotHead", -0.5f, 7f, -6f, new Vector3(1.6f, 1.1f, 1.2f), Mat("OldBot", new Color(0.65f, 0.68f, 0.72f)), false);
            B(f, "OldBotScreen", -0.5f, 7f, -5.38f, new Vector3(1.2f, 0.7f, 0.05f), Glow("OldBot_Eyes", new Color(0.4f, 0.9f, 1f), 0.8f), false);
            var fence = Mat("Junk_Fence", new Color(0.6f, 0.6f, 0.65f));
            for (int i = -5; i <= 5; i++) if (Mathf.Abs(i) > 1) B(f, "FencePost", i * 2.2f, 1.2f, 9f, new Vector3(0.15f, 2.4f, 0.15f), fence);
            Label("THE OLD RIG", f, At(f, 0f, 3.6f, 9.1f), Mathf.Atan2(f.forward.x, f.forward.z) * Mathf.Rad2Deg, 0.1f, new Color(0.45f, 0.3f, 0.2f));
            ComingSoon(f, 11.5f, "CHAPTER 7", "THE OLD RIG");
            Bolt("junk_crane", f, At(f, -9f, 13f, -6f));
        }

        // ------------------------------------------------------------------ chapter 8: Brightwater Festival (summit)

        static void Festival(Transform f)
        {
            Pad(f, 17f, new Color(0.7f, 0.9f, 0.55f));
            Color[] stripes = { new Color(1f, 0.5f, 0.5f), new Color(0.5f, 0.75f, 1f), new Color(1f, 0.85f, 0.35f), new Color(0.75f, 0.6f, 1f) };
            var white = Mat("Stall_White", new Color(0.98f, 0.96f, 0.92f));
            for (int i = 0; i < 5; i++)
            {
                float a = (i - 2) * 0.55f;
                float x = Mathf.Sin(a) * 11f, z = -Mathf.Cos(a) * 11f + 4f;
                var tent = new GameObject("Tent").transform;
                tent.SetParent(f, false);
                tent.SetPositionAndRotation(At(f, x, 0f, z), f.rotation * Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f));
                B(tent, "TentBase", 0f, 1.2f, 0f, new Vector3(3.6f, 2.4f, 3.6f), white);
                B(tent, "TentRoof", 0f, 2.4f, 0f, new Vector3(2.9f, 2.9f, 2.9f), Mat("Tent_" + i % 4, stripes[i % 4]), false, new Vector3(45f, 0f, 45f));
                S(tent, "TentTop", 0f, 4.5f, 0f, 0.2f, Mat("Bell_Gold", new Color(1f, 0.8f, 0.3f)));
            }
            // Stage with bunting.
            B(f, "Stage", 0f, 0.6f, -10f, new Vector3(10f, 1.2f, 5f), Mat("Stage_Wood", new Color(0.8f, 0.55f, 0.4f)));
            B(f, "StageBack", 0f, 3.5f, -12.3f, new Vector3(10f, 4.6f, 0.3f), Mat("Stage_Curtain", new Color(0.75f, 0.25f, 0.35f)));
            Label("FOUNDERS' FESTIVAL", f, At(f, 0f, 4.8f, -12.1f), Mathf.Atan2(f.forward.x, f.forward.z) * Mathf.Rad2Deg, 0.16f, new Color(1f, 0.9f, 0.5f));
            var pole = Mat("Lamp_Post", new Color(0.3f, 0.35f, 0.5f));
            foreach (float s in new[] { -1f, 1f }) B(f, "BuntingPole", s * 9f, 3f, 0f, new Vector3(0.2f, 6f, 0.2f), pole);
            for (int i = 0; i < 18; i++)
            {
                float x = -8.5f + i;
                float sag = 5.6f - Mathf.Sin(i / 17f * Mathf.PI) * 1.2f;
                B(f, "Flag", x, sag, 0f, new Vector3(0.5f, 0.6f, 0.04f), Mat("Tent_" + i % 4, stripes[i % 4]), false, new Vector3(0f, 0f, 45f));
            }
            // Balloons drifting over the summit.
            for (int i = 0; i < 14; i++)
            {
                float x = Rand(-14f, 14f), z = Rand(-14f, 14f), y = Rand(7f, 16f);
                S(f, "Balloon", x, y, z, 0.7f, Glow("Balloon_" + i % 4, stripes[i % 4], 0.45f), new Vector3(1f, 1.2f, 1f));
                B(f, "String", x, y - 1.6f, z, new Vector3(0.03f, 2.2f, 0.03f), Mat("Fence_White", new Color(0.97f, 0.95f, 0.9f)), false);
            }
            ComingSoon(f, 15.5f, "CHAPTER 8", "FOUNDERS' FESTIVAL");
            Bolt("festival_stage", f, At(f, 0f, 2.2f, -10f));
        }
    }
}
