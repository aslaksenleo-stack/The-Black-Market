using System;
using UnityEngine;

namespace BlackMarket.Audio
{
    public enum Sound
    {
        Click,
        DoorBell,
        CounterBell,
        Coins,
        DealDone,
        Refused,
        Inspect,
        FlawFound,
        Footstep,
        ItemDown,
        Upgrade
    }

    /// <summary>
    /// All game sounds, synthesised in code when the shop opens (no audio files needed):
    /// bells, coins, chimes, footsteps and a quiet room ambience. Original sounds, so no licences.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoundKit : MonoBehaviour
    {
        private const int Rate = 44100;

        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.35f;

        private AudioSource effects;
        private AudioSource ambience;
        private AudioClip[] clips;
        private float lastFootstep;

        public static SoundKit Current { get; private set; }

        /// <summary>Plays a sound if a SoundKit exists in the scene. Safe to call from anywhere.</summary>
        public static void Play(Sound sound, float volumeScale = 1f)
        {
            if (Current != null) Current.PlayInternal(sound, volumeScale);
        }

        private void Awake()
        {
            Current = this;
            effects = gameObject.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.spatialBlend = 0f;

            clips = new AudioClip[Enum.GetValues(typeof(Sound)).Length];
            clips[(int)Sound.Click] = Make("Click", 0.05f, Click);
            clips[(int)Sound.DoorBell] = Make("DoorBell", 1.4f, t => ShopBell(t, 1568f, 0.9f) + ShopBell(t - 0.11f, 1976f, 0.7f));
            clips[(int)Sound.CounterBell] = Make("CounterBell", 1.8f, t => ShopBell(t, 1318f, 1f));
            clips[(int)Sound.Coins] = Make("Coins", 0.7f, Coins);
            clips[(int)Sound.DealDone] = Make("DealDone", 0.9f, t => Chime(t, 523.25f, 0f) + Chime(t, 659.25f, 0.09f) + Chime(t, 783.99f, 0.18f));
            clips[(int)Sound.Refused] = Make("Refused", 0.6f, t => Chime(t, 311.13f, 0f) * 0.8f + Chime(t, 233.08f, 0.14f) * 0.8f);
            clips[(int)Sound.Inspect] = Make("Inspect", 0.35f, Whoosh);
            clips[(int)Sound.FlawFound] = Make("FlawFound", 0.7f, t => Chime(t, 466.16f, 0f) + Chime(t, 493.88f, 0.02f) * 0.7f);
            clips[(int)Sound.Footstep] = Make("Footstep", 0.12f, Footstep);
            clips[(int)Sound.ItemDown] = Make("ItemDown", 0.18f, Thud);
            clips[(int)Sound.Upgrade] = Make("Upgrade", 1.1f, t => Chime(t, 392f, 0f) + Chime(t, 523.25f, 0.1f) + Chime(t, 659.25f, 0.2f) + Chime(t, 1046.5f, 0.3f) * 0.6f);

            ambience = gameObject.AddComponent<AudioSource>();
            ambience.clip = MakeAmbience();
            ambience.loop = true;
            ambience.volume = ambienceVolume;
            ambience.spatialBlend = 0f;
            ambience.Play();
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        private void PlayInternal(Sound sound, float volumeScale)
        {
            if (sound == Sound.Footstep)
            {
                if (Time.time - lastFootstep < 0.15f) return;
                lastFootstep = Time.time;
            }

            AudioClip clip = clips[(int)sound];
            if (clip != null) effects.PlayOneShot(clip, volume * volumeScale);
        }

        // ------------------------------------------------------------------ Synthesis

        private static AudioClip Make(string name, float seconds, Func<float, float> wave)
        {
            int samples = Mathf.CeilToInt(seconds * Rate);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                data[i] = Mathf.Clamp(wave(i / (float)Rate), -1f, 1f) * 0.8f;
            }

            AudioClip clip = AudioClip.Create(name, samples, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sine(float t, float freq) => Mathf.Sin(2f * Mathf.PI * freq * t);

        // A small brass bell: inharmonic partials, sharp attack, long decay.
        private static float ShopBell(float t, float f, float amp)
        {
            if (t < 0f) return 0f;
            float env = Mathf.Exp(-t * 3.2f) * Mathf.Min(1f, t * 400f);
            return amp * env * (0.6f * Sine(t, f) + 0.25f * Sine(t, f * 2.76f) * Mathf.Exp(-t * 4f) + 0.12f * Sine(t, f * 5.4f) * Mathf.Exp(-t * 7f)) * 0.5f;
        }

        // Soft marimba-like note.
        private static float Chime(float t, float f, float start)
        {
            t -= start;
            if (t < 0f) return 0f;
            float env = Mathf.Exp(-t * 6f) * Mathf.Min(1f, t * 300f);
            return env * (0.7f * Sine(t, f) + 0.2f * Sine(t, f * 4f) * Mathf.Exp(-t * 12f)) * 0.35f;
        }

        private static float Click(float t)
        {
            return Mathf.Exp(-t * 180f) * Sine(t, 2200f) * 0.4f;
        }

        // Several metallic clinks at irregular moments.
        private static float Coins(float t)
        {
            float[] starts = { 0f, 0.06f, 0.11f, 0.19f, 0.24f, 0.33f };
            float[] freqs = { 3520f, 4186f, 3951f, 4699f, 3729f, 4435f };
            float sum = 0f;
            for (int i = 0; i < starts.Length; i++)
            {
                float lt = t - starts[i];
                if (lt < 0f) continue;
                float env = Mathf.Exp(-lt * 28f);
                sum += env * (Sine(lt, freqs[i]) + 0.5f * Sine(lt, freqs[i] * 1.5f)) * 0.22f;
            }

            return sum;
        }

        private static float Whoosh(float t)
        {
            float env = Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.35f));
            return env * Noise(t * 9000f) * 0.12f;
        }

        private static float Footstep(float t)
        {
            float env = Mathf.Exp(-t * 45f);
            return env * (Sine(t, 90f) * 0.6f + Noise(t * 3000f) * 0.25f) * 0.5f;
        }

        private static float Thud(float t)
        {
            float env = Mathf.Exp(-t * 35f);
            return env * (Sine(t, 140f) * 0.7f + Sine(t, 310f) * 0.3f + Noise(t * 6000f) * 0.2f) * 0.6f;
        }

        // Deterministic hash noise in -1..1.
        private static float Noise(float x)
        {
            int i = Mathf.FloorToInt(x);
            float f = x - i;
            float a = Hash(i), b = Hash(i + 1);
            return Mathf.Lerp(a, b, f * f * (3f - 2f * f));
        }

        private static float Hash(int n)
        {
            unchecked
            {
                n = (n << 13) ^ n;
                int v = (n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff;
                return 1f - v / 1073741824f;
            }
        }

        // Quiet room tone with the odd crackle from the lanterns. Loops seamlessly.
        private static AudioClip MakeAmbience()
        {
            const float seconds = 6f;
            int samples = Mathf.CeilToInt(seconds * Rate);
            var data = new float[samples];
            var rnd = new System.Random(3);
            float brown = 0f;
            for (int i = 0; i < samples; i++)
            {
                brown = (brown + ((float)rnd.NextDouble() * 2f - 1f) * 0.02f) * 0.995f;
                data[i] = brown * 0.6f;
            }

            for (int c = 0; c < 26; c++)
            {
                int at = rnd.Next(samples - 2000);
                float amp = 0.05f + 0.1f * (float)rnd.NextDouble();
                for (int k = 0; k < 600; k++)
                {
                    data[at + k] += amp * Mathf.Exp(-k / 60f) * ((float)rnd.NextDouble() * 2f - 1f);
                }
            }

            // Crossfade the ends so the loop has no click.
            int fade = Rate / 4;
            for (int i = 0; i < fade; i++)
            {
                float w = i / (float)fade;
                data[i] = data[i] * w + data[samples - fade + i] * (1f - w);
            }

            AudioClip clip = AudioClip.Create("Ambience", samples - fade, 1, Rate, false);
            var trimmed = new float[samples - fade];
            Array.Copy(data, trimmed, trimmed.Length);
            clip.SetData(trimmed, 0);
            return clip;
        }
    }
}
