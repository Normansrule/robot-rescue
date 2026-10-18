using UnityEngine;

namespace RobotRescue.Bots
{
    public enum Mood { Neutral, Happy, Surprised, Determined, Angry, Sad, Dizzy, Love, Sleepy }

    /// <summary>
    /// Drives the RobotRescue/BotFace shader on a bot's LED screen: blinking, looking at things, moods, talking.
    /// Moods are short-lived reactions on top of a resting personality (a grumpy drone rests a bit angry).
    /// </summary>
    public class BotFace : MonoBehaviour
    {
        public Renderer screen;
        [Tooltip("The bot's body frame for look directions (defaults to this transform).")]
        public Transform orientation;
        [ColorUsage(false, true)] public Color eyeColor = new Color(0.6f, 2.4f, 2.8f);
        public float aspect = 1.5f;

        [Header("Resting personality")]
        [Range(-1f, 1f)] public float restingSmile = 0.35f;
        [Range(0f, 1f)] public float restingAngry, restingSad, restingSleepy;

        public Mood CurrentMood => Time.time < moodUntil ? mood : Mood.Neutral;
        public bool Powered { get; set; } = true;
        public float Glitch { get; set; }
        /// <summary>0..1 extra sleepiness (low battery, a drone running flat).</summary>
        public float Drowsy { get; set; }

        Mood mood;
        float moodUntil, talkUntil, nextBlink, blinkUntil;
        Vector2 look, lookTarget;
        float lookUntil;
        float happy, surprise, angry, sad, dizzy, love, sleepy, mouth, mouthOpen, blush, power = 1f;
        MaterialPropertyBlock mpb;

        static readonly int IdEye = Shader.PropertyToID("_EyeColor"), IdAspect = Shader.PropertyToID("_Aspect"),
            IdLook = Shader.PropertyToID("_Look"), IdBlink = Shader.PropertyToID("_Blink"), IdHappy = Shader.PropertyToID("_Happy"),
            IdSurprise = Shader.PropertyToID("_Surprise"), IdAngry = Shader.PropertyToID("_Angry"), IdSad = Shader.PropertyToID("_Sad"),
            IdDizzy = Shader.PropertyToID("_Dizzy"), IdLove = Shader.PropertyToID("_Love"), IdSleepy = Shader.PropertyToID("_Sleepy"),
            IdMouth = Shader.PropertyToID("_Mouth"), IdMouthOpen = Shader.PropertyToID("_MouthOpen"), IdBlush = Shader.PropertyToID("_Blush"),
            IdPower = Shader.PropertyToID("_Power"), IdGlitch = Shader.PropertyToID("_Glitch");

        void Awake()
        {
            if (!orientation) orientation = transform;
            nextBlink = Time.time + Random.Range(1f, 4f);
        }

        public void SetMood(Mood m, float seconds)
        {
            mood = m;
            moodUntil = Time.time + seconds;
            if (m == Mood.Surprised) blinkUntil = 0f;
        }

        public void Talk(float seconds) => talkUntil = Mathf.Max(talkUntil, Time.time + seconds);

        /// <summary>Look toward a world point for a moment.</summary>
        public void LookAt(Vector3 worldPoint, float seconds = 0.2f)
        {
            Vector3 d = orientation.InverseTransformPoint(worldPoint);
            if (d.z < -0.2f) d.z = -0.2f;
            d.Normalize();
            // The bot's own left shows up on the viewer's right of the screen.
            lookTarget = new Vector2(-d.x * 1.6f, d.y * 1.6f);
            lookUntil = Time.time + seconds;
        }

        void Update() => Step(Time.deltaTime);

        /// <summary>Snaps straight to a mood and writes the screen now. Works in the editor (for screenshots).</summary>
        public void Preview(Mood m, Vector2 lookAt = default)
        {
            mood = m;
            moodUntil = float.MaxValue;
            lookTarget = lookAt;
            lookUntil = float.MaxValue;
            blinkUntil = 0f;
            nextBlink = float.MaxValue;
            Step(10f);
            look = lookAt;
            Step(0f);
        }

        void Step(float dt)
        {
            float t = Time.time;
            Mood m = CurrentMood;

            if (t > lookUntil) lookTarget = Vector2.zero;
            if (m == Mood.Dizzy) lookTarget = new Vector2(Mathf.Cos(t * 9f), Mathf.Sin(t * 9f)) * 0.6f;
            look = Vector2.Lerp(look, lookTarget, 1f - Mathf.Exp(-14f * dt));

            if (t > nextBlink)
            {
                blinkUntil = t + 0.11f;
                nextBlink = t + (Random.value < 0.2f ? 0.25f : Random.Range(2f, 5f));
            }
            float blink = t < blinkUntil ? 1f : 0f;

            float tHappy = 0f, tSurprise = 0f, tAngry = restingAngry, tSad = restingSad, tDizzy = 0f, tLove = 0f;
            float tSleepy = Mathf.Max(restingSleepy, Drowsy), tMouth = restingSmile, tBlush = 0f;
            switch (m)
            {
                case Mood.Happy: tHappy = 1f; tMouth = 0.95f; tBlush = 1f; tAngry = 0f; tSad = 0f; tSleepy = 0f; break;
                case Mood.Surprised: tSurprise = 1f; tAngry = 0f; tSad = 0f; tSleepy = 0f; break;
                case Mood.Determined: tAngry = 0.75f; tMouth = -0.1f; tSleepy = 0f; break;
                case Mood.Angry: tAngry = 1f; tMouth = -0.7f; tSleepy = 0f; break;
                case Mood.Sad: tSad = 1f; tAngry = 0f; tMouth = -0.75f; break;
                case Mood.Dizzy: tDizzy = 1f; tAngry = 0f; tSad = 0f; tMouth = -0.25f; break;
                case Mood.Love: tLove = 1f; tBlush = 1f; tMouth = 1f; tAngry = 0f; tSad = 0f; tSleepy = 0f; break;
                case Mood.Sleepy: tSleepy = 1f; tMouth = 0.1f; break;
            }

            float k = 1f - Mathf.Exp(-12f * dt);
            happy = Mathf.Lerp(happy, tHappy, k);
            surprise = Mathf.Lerp(surprise, tSurprise, k);
            angry = Mathf.Lerp(angry, tAngry, k);
            sad = Mathf.Lerp(sad, tSad, k);
            dizzy = Mathf.Lerp(dizzy, tDizzy, k);
            love = Mathf.Lerp(love, tLove, k);
            sleepy = Mathf.Lerp(sleepy, tSleepy, k);
            mouth = Mathf.Lerp(mouth, tMouth, k);
            blush = Mathf.Lerp(blush, tBlush, k);
            power = Mathf.Lerp(power, Powered ? 1f : 0f, 1f - Mathf.Exp(-6f * dt));

            float talk = t < talkUntil ? Mathf.Abs(Mathf.Sin(t * 19f) * Mathf.Sin(t * 7.3f + 1f)) : 0f;
            mouthOpen = Mathf.Lerp(mouthOpen, talk, 1f - Mathf.Exp(-25f * dt));

            if (!screen) return;
            mpb ??= new MaterialPropertyBlock();
            screen.GetPropertyBlock(mpb);
            mpb.SetColor(IdEye, eyeColor);
            mpb.SetFloat(IdAspect, aspect);
            mpb.SetVector(IdLook, new Vector4(look.x, look.y, 0f, 0f));
            mpb.SetFloat(IdBlink, Mathf.Max(blink, sleepy > 0.95f ? 1f : 0f));
            mpb.SetFloat(IdHappy, happy);
            mpb.SetFloat(IdSurprise, surprise);
            mpb.SetFloat(IdAngry, angry);
            mpb.SetFloat(IdSad, sad);
            mpb.SetFloat(IdDizzy, dizzy);
            mpb.SetFloat(IdLove, love);
            mpb.SetFloat(IdSleepy, Mathf.Clamp01(sleepy));
            mpb.SetFloat(IdMouth, mouth);
            mpb.SetFloat(IdMouthOpen, mouthOpen);
            mpb.SetFloat(IdBlush, blush);
            mpb.SetFloat(IdPower, power);
            mpb.SetFloat(IdGlitch, Glitch);
            screen.SetPropertyBlock(mpb);
        }
    }
}
