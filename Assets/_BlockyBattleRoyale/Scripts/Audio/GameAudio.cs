using UnityEngine;

namespace BlockyBattleRoyale.Audio
{
    public class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }

        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.spatialBlend = 0f;
            }
        }

        public void PlayOneShot(AudioClip clip, float volume = 1f)
        {
            if (clip == null || sfxSource == null)
                return;
            sfxSource.PlayOneShot(clip, volume);
        }

        public void PlayAtPoint(AudioClip clip, Vector3 pos, float volume = 1f)
        {
            if (clip == null)
                return;
            AudioSource.PlayClipAtPoint(clip, pos, volume);
        }

        /// <summary>Procedural beep fallback when no clip assets exist.</summary>
        public void PlayBeep(float freq = 440f, float duration = 0.05f, float volume = 0.2f)
        {
            int sampleRate = 44100;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create("Beep", samples, 1, sampleRate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * i / sampleRate) * volume;
            clip.SetData(data, 0);
            sfxSource.PlayOneShot(clip);
        }
    }
}
