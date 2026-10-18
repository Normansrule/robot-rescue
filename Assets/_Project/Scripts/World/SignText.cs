using UnityEngine;

namespace RobotRescue.World
{
    /// <summary>
    /// Draws a TextMesh sign with a depth-tested shader, so walls in front of it hide it.
    /// The builders already give the sign a depth-tested material; the font's atlas can be rebuilt at runtime, so in play
    /// the sign switches to a shared copy whose texture is re-linked whenever that happens.
    /// </summary>
    [RequireComponent(typeof(TextMesh))]
    public class SignText : MonoBehaviour
    {
        public Shader shader;

        static readonly System.Collections.Generic.Dictionary<Font, Material> shared = new System.Collections.Generic.Dictionary<Font, Material>();
        TextMesh text;

        void Awake()
        {
            text = GetComponent<TextMesh>();
            if (!shader || !shader.isSupported || !text.font) return;
            if (!shared.TryGetValue(text.font, out var mat) || !mat)
            {
                mat = new Material(shader) { name = "SignText_" + text.font.name, mainTexture = text.font.material.mainTexture };
                shared[text.font] = mat;
            }
            GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        void OnEnable() => Font.textureRebuilt += Relink;
        void OnDisable() => Font.textureRebuilt -= Relink;

        void Relink(Font font)
        {
            if (text && font == text.font && shared.TryGetValue(font, out var mat) && mat) mat.mainTexture = font.material.mainTexture;
        }
    }
}
