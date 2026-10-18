using System.Collections.Generic;
using UnityEngine;

namespace RobotRescue.Bots
{
    /// <summary>
    /// Procedural robot voice: little synthesized "syllables" strung together with a pitch contour per tone, like the
    /// chatter in R.E.P.O. or Animal Crossing. No audio files: clips are generated once per voice and cached.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class Babble : MonoBehaviour
    {
        public enum Tone { Chatter, Happy, Sad, Question, Surprised, Effort, Sleepy }

        [Tooltip("Base pitch of the voice in Hz. PIP ~700, drones 450..1000, DISPATCH ~260 (radio).")]
        public float voiceHz = 700f;
        [Range(0f, 1f)] public float volume = 0.45f;
        [Tooltip("Crunchy, band-limited radio voice (DISPATCH).")]
        public bool radio;
        public float syllableGap = 0.095f;

        public bool IsTalking => queue.Count > 0 || Time.time < busyUntil;

        struct Syllable { public float at; public int clip; public float pitch; public float volume; }

        static readonly Dictionary<int, AudioClip[]> cache = new Dictionary<int, AudioClip[]>();
        const int SampleRate = 22050;
        const int ClipCount = 10;

        readonly Queue<Syllable> queue = new Queue<Syllable>();
        AudioSource source;
        AudioClip[] clips;
        float busyUntil;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            LoadClips();
        }

        /// <summary>Changes the voice's pitch (the bot workshop's Voice option) and swaps to that voice's clips.</summary>
        public void SetVoice(float hz)
        {
            if (Mathf.Approximately(hz, voiceHz) && clips != null) return;
            voiceHz = hz;
            LoadClips();
        }

        void LoadClips()
        {
            int key = Mathf.RoundToInt(voiceHz) * 2 + (radio ? 1 : 0);
            if (!cache.TryGetValue(key, out clips) || clips == null || clips.Length == 0 || !clips[0])
                cache[key] = clips = Synthesize(voiceHz, radio);
        }

        /// <summary>Babble roughly as long as <paramref name="text"/> would take to say.</summary>
        public void Say(string text, Tone tone = Tone.Chatter) =>
            Say(Mathf.Clamp(string.IsNullOrEmpty(text) ? 2 : text.Length / 7, 2, 16), tone);

        public void Say(int syllables, Tone tone = Tone.Chatter)
        {
            if (!isActiveAndEnabled || clips == null) return;
            queue.Clear();
            float t = Time.time;
            for (int i = 0; i < syllables; i++)
            {
                float u = syllables > 1 ? i / (float)(syllables - 1) : 0f;
                float pitch = tone switch
                {
                    Tone.Happy => 1.05f + 0.25f * u + Random.Range(-0.05f, 0.08f),
                    Tone.Sad => 1.0f - 0.3f * u + Random.Range(-0.04f, 0.04f),
                    Tone.Question => (i == syllables - 1 ? 1.35f : 1f) + Random.Range(-0.08f, 0.08f),
                    Tone.Surprised => 1.4f + Random.Range(-0.05f, 0.1f),
                    Tone.Effort => 0.85f + Random.Range(-0.04f, 0.04f),
                    Tone.Sleepy => 0.7f - 0.1f * u,
                    _ => 1f + Random.Range(-0.15f, 0.15f),
                };
                float gap = syllableGap * (tone == Tone.Sleepy ? 2.2f : tone == Tone.Surprised ? 0.7f : Random.Range(0.85f, 1.25f));
                queue.Enqueue(new Syllable { at = t, clip = Random.Range(0, clips.Length), pitch = pitch, volume = volume * Random.Range(0.8f, 1f) });
                t += gap;
            }
            busyUntil = t;
        }

        void Update()
        {
            while (queue.Count > 0 && queue.Peek().at <= Time.time)
            {
                var s = queue.Dequeue();
                source.pitch = s.pitch;
                source.PlayOneShot(clips[s.clip], s.volume);
            }
        }

        static AudioClip[] Synthesize(float hz, bool radio)
        {
            var rng = new System.Random(Mathf.RoundToInt(hz) * 31 + (radio ? 7 : 0));
            var result = new AudioClip[ClipCount];
            for (int c = 0; c < ClipCount; c++)
            {
                float dur = 0.06f + (float)rng.NextDouble() * 0.05f;
                int n = Mathf.CeilToInt(dur * SampleRate);
                var data = new float[n];
                float f0 = hz * (0.85f + (float)rng.NextDouble() * 0.35f);
                float f1 = f0 * (0.8f + (float)rng.NextDouble() * 0.45f);
                float formant = 2.5f + (float)rng.NextDouble() * 2f;
                float vib = 28f + (float)rng.NextDouble() * 20f;
                double phase = 0;
                float crush = 0f, hold = 0f;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)SampleRate;
                    float u = i / (float)n;
                    float f = Mathf.Lerp(f0, f1, u);
                    phase += 2.0 * Mathf.PI * f / SampleRate;
                    float s = Mathf.Sin((float)phase);
                    // Soft square plus a "vowel" overtone gives a chirpy, toy-like timbre.
                    float wave = 0.55f * (float)System.Math.Tanh(3.0 * s) + 0.3f * Mathf.Sin((float)phase * formant) * (0.6f + 0.4f * Mathf.Sin(t * vib * 6.283f));
                    float env = Mathf.Clamp01(t / 0.006f) * Mathf.Exp(-u * 2.2f) * Mathf.Clamp01((1f - u) / 0.15f);
                    float v = wave * env;
                    if (radio)
                    {
                        // Sample-and-hold + 4-bit crush + hiss: walkie-talkie DISPATCH.
                        if (i % 3 == 0) hold = Mathf.Round(v * 8f) / 8f;
                        v = hold + ((float)rng.NextDouble() - 0.5f) * 0.06f * env;
                        crush = Mathf.Lerp(crush, v, 0.55f);
                        v = crush;
                    }
                    data[i] = v * 0.8f;
                }
                var clip = AudioClip.Create($"babble_{hz:0}_{c}", n, 1, SampleRate, false);
                clip.SetData(data, 0);
                result[c] = clip;
            }
            return result;
        }
    }
}
