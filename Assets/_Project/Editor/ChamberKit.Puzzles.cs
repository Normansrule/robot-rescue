using System.Collections.Generic;
using RobotRescue.Bots;
using RobotRescue.Campaign;
using RobotRescue.Puzzles;
using RobotRescue.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RobotRescue.EditorTools
{
    /// <summary>Puzzle-toolkit pieces for chamber builders. Every position is world space; floors are at y = 0 unless noted.</summary>
    public static partial class ChamberKit
    {
        // ------------------------------------------------------------------ scene lifecycle

        /// <summary>Asks to save the open scene, then starts a fresh lit scene. Returns false if the user cancelled.</summary>
        public static bool BeginScene(out Scene scene, params string[] folders)
        {
            scene = default;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
            EnsureFolder(Root + "/Scenes");
            EnsureFolder(MatDir);
            foreach (var f in folders) EnsureFolder(f);
            ResetCache();
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildLighting();
            return true;
        }

        public static void FinishScene(Scene scene, string path, BotController bot)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
            AddSceneToBuildSettings(path);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = bot.gameObject;
            Debug.Log($"[Robot Rescue] Built {path}. Press Play!");
        }

        // ------------------------------------------------------------------ pieces

        /// <summary>Wind-up Helper Drone: a 1 m cube bot you can carry, stack, throw and Tether-pull.</summary>
        public static HelperDrone Drone(string name, Transform parent, Vector3 pos, Color color)
        {
            var go = Box(name, parent, pos, Vector3.one, Mat("Drone_" + ColorUtility.ToHtmlStringRGB(color), color, 0.5f));
            go.GetComponent<Collider>().sharedMaterial = PhysMat("Drone", 0.8f, 0.9f, PhysicsMaterialCombine.Maximum);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = 1.5f;
            var carry = go.AddComponent<Carryable>();
            carry.displayName = "Helper Drone";
            carry.firstPickupCue = "drone_grabbed";
            go.AddComponent<Respawnable>();
            var drone = go.AddComponent<HelperDrone>();

            var screen = Mat("Bot_Screen", new Color(0.08f, 0.1f, 0.2f), 0.8f);
            var eye = Mat("Drone_Eye", new Color(1f, 0.9f, 0.4f));
            var metal = Mat("Bot_Grey", new Color(0.5f, 0.52f, 0.58f));
            Box("Face", go.transform, pos + new Vector3(0, 0.1f, 0.5f), new Vector3(0.7f, 0.45f, 0.04f), screen, false);
            var eyeL = Box("EyeL", go.transform, pos + new Vector3(-0.15f, 0.12f, 0.53f), new Vector3(0.12f, 0.16f, 0.02f), eye, false);
            var eyeR = Box("EyeR", go.transform, pos + new Vector3(0.15f, 0.12f, 0.53f), new Vector3(0.12f, 0.16f, 0.02f), eye, false);
            Box("TopPlate", go.transform, pos + new Vector3(0, 0.5f, 0), new Vector3(0.8f, 0.03f, 0.8f), metal, false);

            // Wind-up key on the back: a pivot that spins around the drone's local Z.
            var key = new GameObject("WindUpKey").transform;
            key.SetParent(go.transform, false);
            key.position = pos + new Vector3(0, 0.1f, -0.62f);
            Box("Shaft", key, pos + new Vector3(0, 0.1f, -0.56f), new Vector3(0.06f, 0.06f, 0.14f), metal, false);
            Box("Bow", key, pos + new Vector3(0, 0.1f, -0.64f), new Vector3(0.45f, 0.14f, 0.04f), Mat("Bot_Orange", new Color(1f, 0.55f, 0.15f)), false);

            drone.windUpKey = key;
            drone.eyes = new[] { eyeL.transform, eyeR.transform };
            return drone;
        }

        /// <summary>Freestanding socket post. The socket faces <paramref name="yaw"/> (0 = +Z).</summary>
        public static PowerSocket Socket(string name, Transform parent, Vector3 floorPos, float yaw, params GameObject[] powered)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(floorPos, Quaternion.Euler(0, yaw, 0));
            Box("Post", root, floorPos + Vector3.up * 0.55f, new Vector3(0.4f, 1.1f, 0.4f), Mat("Socket_Post", new Color(0.85f, 0.85f, 0.92f)));
            Vector3 face = root.rotation * new Vector3(0, 0, 0.21f);
            var socket = Box("Socket", root, floorPos + Vector3.up * 0.8f + face, new Vector3(0.3f, 0.3f, 0.04f), Mat("Socket", new Color(0.2f, 0.2f, 0.25f)));
            socket.transform.rotation = root.rotation;
            var led = Box("LED", root, floorPos + Vector3.up * 1.05f + face, new Vector3(0.1f, 0.06f, 0.03f), Mat("LED", Color.gray), false);
            led.transform.rotation = root.rotation;
            var ps = socket.AddComponent<PowerSocket>();
            ps.poweredObjects = powered;
            ps.indicator = led.GetComponent<Renderer>();
            return ps;
        }

        public static PressurePlate Plate(string name, Transform parent, Vector3 floorPos, float requiredMass, float triggerHeight, params GameObject[] powered)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            var visual = Box("Plate", root, floorPos + Vector3.up * 0.05f, new Vector3(1.6f, 0.1f, 1.6f), Mat("Plate_Yellow", new Color(1f, 0.85f, 0.3f)));
            var zone = Box("Trigger", root, floorPos + Vector3.up * (0.1f + triggerHeight * 0.5f), new Vector3(1.4f, triggerHeight, 1.4f), null);
            zone.GetComponent<Collider>().isTrigger = true;
            var plate = zone.AddComponent<PressurePlate>();
            plate.requiredMass = requiredMass;
            plate.plateVisual = visual.transform;
            plate.poweredObjects = powered;
            plate.tooLightCue = "plate_too_light";
            return plate;
        }

        /// <summary>Kinematic sliding door. Add it to a source's powered list, or set openOnTaskComplete.</summary>
        public static Door SlidingDoor(string name, Transform parent, Vector3 center, Vector3 size, Vector3 openOffset)
        {
            var go = Box(name, parent, center, size, Mat("Trim_Coral", new Color(1f, 0.55f, 0.5f)));
            go.AddComponent<Rigidbody>().isKinematic = true;
            var door = go.AddComponent<Door>();
            door.openOffset = openOffset;
            return door;
        }

        /// <summary>Floor wire through the given points (y is taken from the points). Lights up when powered.</summary>
        public static PowerWire Wire(string name, Transform parent, Color color, params Vector3[] points)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var segments = new List<Renderer>();
            var mat = Mat("Wire_" + ColorUtility.ToHtmlStringRGB(color), color);
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector3 a = points[i], b = points[i + 1];
                Vector3 d = b - a;
                var seg = Box("Seg" + i, root.transform, (a + b) * 0.5f + Vector3.up * 0.015f, new Vector3(0.18f, 0.03f, d.magnitude + 0.18f), mat, false);
                seg.transform.rotation = Quaternion.LookRotation(new Vector3(d.x, 0, d.z));
                segments.Add(seg.GetComponent<Renderer>());
            }
            var wire = root.AddComponent<PowerWire>();
            wire.segments = segments.ToArray();
            wire.color = color;
            return wire;
        }

        /// <summary>Belt whose top surface is at <paramref name="center"/>.y, running toward <paramref name="yaw"/> (0 = +Z).</summary>
        public static Conveyor Belt(string name, Transform parent, Vector3 center, float length, float width, float yaw, float speed, bool startsOn)
        {
            var rot = Quaternion.Euler(0, yaw, 0);
            var belt = Box(name, parent, center + Vector3.down * 0.15f, new Vector3(width, 0.3f, length), Mat("Belt_Dark", new Color(0.25f, 0.26f, 0.3f)));
            belt.transform.rotation = rot;
            belt.GetComponent<Collider>().sharedMaterial = PhysMat("Belt", 0.6f, 0.6f, PhysicsMaterialCombine.Average);

            var stripesRoot = new GameObject(name + "_Stripes").transform;
            stripesRoot.SetParent(parent, false);
            var stripeMat = Mat("Belt_Stripe", new Color(1f, 0.8f, 0.3f));
            var stripes = new List<Transform>();
            for (float z = -length * 0.5f + 0.25f; z < length * 0.5f; z += 0.6f)
            {
                var s = Box("Stripe", stripesRoot, center + rot * new Vector3(0, 0.01f, z), new Vector3(width * 0.85f, 0.02f, 0.12f), stripeMat, false);
                s.transform.rotation = rot;
                stripes.Add(s.transform);
            }
            var rail = Mat("Steel", new Color(0.78f, 0.8f, 0.85f), 0.7f);
            foreach (float side in new[] { -1f, 1f })
            {
                var r = Box("Rail", stripesRoot, center + rot * new Vector3(side * (width * 0.5f + 0.05f), 0.02f, 0), new Vector3(0.1f, 0.12f, length), rail, false);
                r.transform.rotation = rot;
            }

            var c = belt.AddComponent<Conveyor>();
            c.speed = speed;
            c.startsOn = startsOn;
            c.length = length;
            c.stripes = stripes.ToArray();
            return c;
        }

        public static PowerCombiner Gate(string name, Transform parent, PowerCombiner.Mode mode, int inputs, params GameObject[] powered)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var g = go.AddComponent<PowerCombiner>();
            g.mode = mode;
            g.inputCount = inputs;
            g.poweredObjects = powered;
            return g;
        }

        public static TaskStepSignal StepSignal(string name, Transform parent, string stepId, string cue = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<TaskStepSignal>();
            s.stepId = stepId;
            s.cue = cue;
            return s;
        }

        public static TaskStepTrigger StepZone(string name, Transform parent, Vector3 center, Vector3 size, string stepId, string cue = null)
        {
            var go = Box(name, parent, center, size, null);
            go.GetComponent<Collider>().isTrigger = true;
            var t = go.AddComponent<TaskStepTrigger>();
            t.stepId = stepId;
            t.cue = cue;
            return t;
        }

        public static Checkpoint CheckpointZone(string name, Transform parent, Vector3 center, Vector3 size, string cue = null)
        {
            var go = Box(name, parent, center, size, null);
            go.GetComponent<Collider>().isTrigger = true;
            var cp = go.AddComponent<Checkpoint>();
            cp.cue = cue;
            return cp;
        }

        public static void KillPlane(Transform parent, float y)
        {
            var go = Box("KillZone", parent, new Vector3(0, y, 0), new Vector3(200f, 1f, 200f), null);
            go.GetComponent<Collider>().isTrigger = true;
            go.AddComponent<KillZone>();
        }

        public static void ExitFlag(Transform parent, Vector3 floorPos, Vector3 triggerSize)
        {
            var exit = Box("LevelExit", parent, floorPos + Vector3.up * (triggerSize.y * 0.5f), triggerSize, null);
            exit.GetComponent<Collider>().isTrigger = true;
            exit.AddComponent<LevelExit>();
            Box("FlagPole", parent, floorPos + new Vector3(1.2f, 1.5f, 0), new Vector3(0.1f, 3f, 0.1f), Mat("Steel", new Color(0.78f, 0.8f, 0.85f), 0.7f), false);
            Box("Flag", parent, floorPos + new Vector3(1.65f, 2.7f, 0), new Vector3(0.8f, 0.5f, 0.04f), Mat("Flag_Green", new Color(0.35f, 0.85f, 0.45f)), false);
        }

        /// <summary>Swing hook hanging from a short rope.</summary>
        public static TetherAnchor Hook(string name, Transform parent, Vector3 pos)
        {
            Box(name + "_Rope", parent, pos + Vector3.up * 0.6f, new Vector3(0.05f, 0.9f, 0.05f), Mat("Post_Blue", new Color(0.3f, 0.45f, 0.75f)), false);
            var hook = Sphere(name, parent, pos, 0.35f, Mat("Hook_Yellow", new Color(1f, 0.85f, 0.2f)), collider: true);
            return hook.AddComponent<TetherAnchor>();
        }
    }
}
