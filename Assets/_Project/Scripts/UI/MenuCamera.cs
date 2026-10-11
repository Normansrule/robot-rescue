using UnityEngine;

namespace RobotRescue.UI
{
    /// <summary>Slow orbit around a point, for the main menu backdrop (and the media capture turntables).</summary>
    public class MenuCamera : MonoBehaviour
    {
        public Vector3 pivot;
        public float radius = 9f;
        public float height = 4f;
        public float degreesPerSecond = 8f;
        public float lookHeight = 1f;

        float angle;

        void Start() => angle = transform.eulerAngles.y;

        void LateUpdate()
        {
            angle += degreesPerSecond * Time.unscaledDeltaTime;
            Place(angle);
        }

        public void Place(float yaw)
        {
            Quaternion rot = Quaternion.Euler(0f, yaw, 0f);
            transform.position = pivot + rot * new Vector3(0f, height, -radius);
            transform.LookAt(pivot + Vector3.up * lookHeight);
        }
    }
}
