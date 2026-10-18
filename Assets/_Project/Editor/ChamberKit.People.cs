using UnityEngine;

namespace RobotRescue.EditorTools
{
    public static partial class ChamberKit
    {
        /// <summary>
        /// A friendly round person (Rosa, the science-fair kids, teachers): body, head, rosy cheeks, dot eyes and an
        /// optional hat. <paramref name="height"/> 1 is a grown-up, about 0.75 a kid. Has a solid collider and a name tag.
        /// </summary>
        public static Transform Person(string name, Transform parent, Vector3 floorPos, float yaw, Color outfit, Color top,
            Color skin, Color hair, float height = 1f, bool glasses = false)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(floorPos, Quaternion.Euler(0f, yaw, 0f));
            float k = height;
            Vector3 P(float x, float y, float z) => root.TransformPoint(new Vector3(x, y, z) * k);
            string id = name.Replace(" ", "").Replace(".", "");
            var body = Mat("Person_" + id + "_Outfit", outfit);
            var shirt = Mat("Person_" + id + "_Top", top);
            var face = Mat("Person_" + id + "_Skin", skin);
            var hairMat = Mat("Person_" + id + "_Hair", hair);
            Sphere("Body", root, P(0f, 0.78f, 0f), 0.42f * k, body, new Vector3(1f, 1.55f, 0.85f));
            Sphere("Top", root, P(0f, 1.15f, 0.12f), 0.24f * k, shirt, new Vector3(1.3f, 0.8f, 0.8f));
            Sphere("Head", root, P(0f, 1.62f, 0f), 0.3f * k, face);
            Sphere("Hair", root, P(0f, 1.72f, -0.06f), 0.3f * k, hairMat, new Vector3(1.05f, 0.8f, 1f));
            var eye = Mat("Eye_Black", new Color(0.12f, 0.1f, 0.15f));
            foreach (float s in new[] { -1f, 1f })
            {
                Sphere("Eye", root, P(s * 0.1f, 1.66f, 0.26f), 0.04f * k, eye);
                Sphere("Cheek", root, P(s * 0.17f, 1.56f, 0.22f), 0.055f * k, Mat("Cheek", new Color(1f, 0.55f, 0.55f)), new Vector3(1f, 0.7f, 0.5f));
                Sphere("Arm", root, P(s * 0.45f, 0.95f, 0.05f), 0.11f * k, shirt, new Vector3(1f, 2.6f, 1f));
                Sphere("Hand", root, P(s * 0.47f, 0.66f, 0.06f), 0.08f * k, face);
                Sphere("Shoe", root, P(s * 0.16f, 0.08f, 0.05f), 0.13f * k, Mat("Boot_Brown", new Color(0.45f, 0.32f, 0.25f)), new Vector3(1f, 0.7f, 1.4f));
                if (glasses)
                {
                    var lens = Cylinder("Glasses", root, P(s * 0.1f, 1.66f, 0.29f), 0.07f * k, 0.01f * k, Mat("Glasses", new Color(0.25f, 0.3f, 0.45f)));
                    lens.transform.rotation = root.rotation * Quaternion.Euler(90f, 0f, 0f);
                }
            }
            Box("Collider", root, P(0f, 0.95f, 0f), new Vector3(0.8f, 1.9f, 0.8f) * k, null);
            Label(name.ToUpperInvariant(), root, P(0f, 2.3f, 0f) + Vector3.up * 0.15f, yaw, 0.04f, new Color(0.25f, 0.3f, 0.45f));
            return root;
        }
    }
}
