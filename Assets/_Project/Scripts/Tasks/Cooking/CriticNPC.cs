using UnityEngine;

namespace RobotRescue.Tasks.Cooking
{
    /// <summary>Mr. Grumbleton: sways while he waits, fidgets as his mood sours, and bounces if you win him over.</summary>
    public class CriticNPC : MonoBehaviour
    {
        public OrderBoard board;
        public Renderer face;
        public Color happy = new Color(1f, 0.85f, 0.72f);
        public Color angry = new Color(1f, 0.45f, 0.4f);

        Vector3 basePos;
        Quaternion baseRot;

        void Awake()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
        }

        void Update()
        {
            float mood = board ? board.Meter01 : 1f;
            float t = Time.time;
            if (board && board.Won)
            {
                transform.localPosition = basePos + Vector3.up * Mathf.Abs(Mathf.Sin(t * 6f)) * 0.25f;
                transform.localRotation = baseRot;
            }
            else
            {
                float fidget = Mathf.Lerp(8f, 1.5f, mood);
                transform.localRotation = baseRot * Quaternion.Euler(0f, 0f, Mathf.Sin(t * fidget) * Mathf.Lerp(6f, 2f, mood));
                transform.localPosition = basePos;
            }
            if (face) face.material.color = Color.Lerp(angry, happy, mood);
        }
    }
}
