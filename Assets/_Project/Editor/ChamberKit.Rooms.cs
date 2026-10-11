using RobotRescue.Puzzles;
using UnityEngine;

namespace RobotRescue.EditorTools
{
    public static partial class ChamberKit
    {
        /// <summary>
        /// Rectangular room centred on the origin with a doorway in the south wall (x = 0), a sliding door that opens
        /// when the task completes, and a short corridor south to the exit flag.
        /// </summary>
        public static Door RoomWithExit(Transform p, float sizeX, float sizeZ, float height, Material floor, Material wall, Material trim, string title, Color titleColor)
        {
            float hx = sizeX * 0.5f, hz = sizeZ * 0.5f, door = 3f;
            Box("Floor", p, new Vector3(0, -0.25f, 0), new Vector3(sizeX, 0.5f, sizeZ), floor);
            Box("Wall_N", p, new Vector3(0, height * 0.5f, hz + 0.25f), new Vector3(sizeX + 1f, height, 0.5f), wall);
            Box("Wall_E", p, new Vector3(hx + 0.25f, height * 0.5f, 0), new Vector3(0.5f, height, sizeZ), wall);
            Box("Wall_W", p, new Vector3(-hx - 0.25f, height * 0.5f, 0), new Vector3(0.5f, height, sizeZ), wall);
            float side = (sizeX + 1f - door) * 0.5f;
            Box("Wall_S_Left", p, new Vector3(-(door * 0.5f + side * 0.5f), height * 0.5f, -hz - 0.25f), new Vector3(side, height, 0.5f), wall);
            Box("Wall_S_Right", p, new Vector3(door * 0.5f + side * 0.5f, height * 0.5f, -hz - 0.25f), new Vector3(side, height, 0.5f), wall);
            Box("Wall_S_Lintel", p, new Vector3(0, (height + 3.5f) * 0.5f, -hz - 0.25f), new Vector3(door, height - 3.5f, 0.5f), wall);
            Box("Skirting_N", p, new Vector3(0, 0.15f, hz - 0.02f), new Vector3(sizeX, 0.3f, 0.05f), trim, false);
            Label(title, p, new Vector3(0, height - 0.6f, hz - 0.05f), 180f, 0.13f, titleColor);

            var exit = SlidingDoor("ExitDoor", p, new Vector3(0, 1.75f, -hz - 0.25f), new Vector3(door, 3.5f, 0.3f), new Vector3(door + 0.2f, 0, 0));
            exit.openOnTaskComplete = true;
            Label("EXIT", p, new Vector3(0, 3.8f, -hz + 0.05f), 0f, 0.1f, Color.white);

            // Corridor to the flag.
            Box("Corridor", p, new Vector3(0, -0.25f, -hz - 3.5f), new Vector3(door + 2f, 0.5f, 6.5f), floor);
            Box("Corridor_WallL", p, new Vector3(-door * 0.5f - 1.25f, 1.5f, -hz - 3.5f), new Vector3(0.5f, 3f, 6.5f), wall);
            Box("Corridor_WallR", p, new Vector3(door * 0.5f + 1.25f, 1.5f, -hz - 3.5f), new Vector3(0.5f, 3f, 6.5f), wall);
            Box("Corridor_End", p, new Vector3(0, 1.5f, -hz - 7f), new Vector3(door + 3f, 3f, 0.5f), wall);
            ExitFlag(p, new Vector3(-0.6f, 0f, -hz - 5f), new Vector3(door, 2f, 2.5f));
            KillPlane(p, -12f);
            return exit;
        }

        /// <summary>Simple solid table/counter of the given footprint, top at <paramref name="height"/>.</summary>
        public static GameObject Table(string name, Transform p, Vector3 floorCenter, Vector3 footprint, float height, Material mat)
        {
            return Box(name, p, floorCenter + Vector3.up * (height * 0.5f), new Vector3(footprint.x, height, footprint.z), mat);
        }
    }
}
