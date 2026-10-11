using UnityEngine;

namespace RobotRescue.UI
{
    /// <summary>Menu-screen PIP: a happy idle bob and a slow look around. No physics, no input.</summary>
    public class MenuIdle : MonoBehaviour
    {
        public float bobHeight = 0.08f;
        public float bobSpeed = 3f;
        public float lookAngle = 25f;

        Transform visual;
        Vector3 basePos;
        Quaternion baseRot;

        void Start()
        {
            visual = transform.Find("Visual");
            if (visual) basePos = visual.localPosition;
            baseRot = transform.rotation;
        }

        void Update()
        {
            float t = Time.unscaledTime;
            if (visual)
            {
                float b = Mathf.Abs(Mathf.Sin(t * bobSpeed));
                visual.localPosition = basePos + Vector3.up * b * bobHeight;
                visual.localScale = new Vector3(1f + (1f - b) * 0.04f, 1f - (1f - b) * 0.04f, 1f + (1f - b) * 0.04f);
            }
            transform.rotation = baseRot * Quaternion.Euler(0f, Mathf.Sin(t * 0.6f) * lookAngle, 0f);
        }
    }
}
