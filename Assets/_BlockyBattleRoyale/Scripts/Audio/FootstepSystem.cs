using UnityEngine;

namespace BlockyBattleRoyale.Audio
{
    public enum SurfaceKind
    {
        Grass,
        Dirt,
        Asphalt,
        Wood,
        Concrete,
        Metal
    }

    /// <summary>Plays procedural footstep blips by surface + stance (volume scales).</summary>
    public class FootstepSystem : MonoBehaviour
    {
        [SerializeField] private float walkInterval = 0.42f;
        [SerializeField] private float runInterval = 0.28f;
        [SerializeField] private float crouchInterval = 0.55f;
        [SerializeField] private float proneInterval = 0.75f;

        private Player.PlayerMotor motor;
        private AudioSource source;
        private float timer;
        private SurfaceKind surface = SurfaceKind.Grass;

        public SurfaceKind CurrentSurface => surface;
        public float LastStepVolume { get; private set; }

        private void Awake()
        {
            motor = GetComponent<Player.PlayerMotor>();
            source = gameObject.AddComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.playOnAwake = false;
            source.maxDistance = 35f;
            source.rolloffMode = AudioRolloffMode.Linear;
        }

        private void Update()
        {
            if (motor == null || !motor.IsMoving)
            {
                timer = 0f;
                return;
            }

            DetectSurface();
            float interval = walkInterval;
            float vol = 0.35f;
            if (motor.IsProne) { interval = proneInterval; vol = 0.08f; }
            else if (motor.IsCrouching) { interval = crouchInterval; vol = 0.15f; }
            else if (motor.IsRunning) { interval = runInterval; vol = 0.7f; }

            timer += Time.deltaTime;
            if (timer >= interval)
            {
                timer = 0f;
                PlayStep(vol);
            }
        }

        private void DetectSurface()
        {
            if (!Physics.Raycast(transform.position + Vector3.up, Vector3.down, out RaycastHit hit, 2.5f))
            {
                surface = SurfaceKind.Grass;
                return;
            }

            string n = hit.collider.name.ToLowerInvariant();
            if (n.Contains("road") || n.Contains("asphalt")) surface = SurfaceKind.Asphalt;
            else if (n.Contains("wood") || n.Contains("floor") || n.Contains("floor")) surface = SurfaceKind.Wood;
            else if (n.Contains("concrete") || n.Contains("metal")) surface = SurfaceKind.Concrete;
            else if (n.Contains("dirt") || n.Contains("mud")) surface = SurfaceKind.Dirt;
            else if (n.Contains("meshterrain") || n.Contains("ground") || n.Contains("grass")) surface = SurfaceKind.Grass;
            else surface = SurfaceKind.Dirt;
        }

        private void PlayStep(float volume)
        {
            LastStepVolume = volume;
            // Procedural short noise blip (no external audio assets)
            float freq = surface switch
            {
                SurfaceKind.Asphalt => 180f,
                SurfaceKind.Wood => 220f,
                SurfaceKind.Concrete => 160f,
                SurfaceKind.Metal => 320f,
                SurfaceKind.Dirt => 140f,
                _ => 200f
            };
            int sampleRate = 44100;
            float duration = 0.04f;
            int samples = Mathf.CeilToInt(sampleRate * duration);
            var clip = AudioClip.Create("Step", samples, 1, sampleRate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float env = 1f - (i / (float)samples);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * 0.35f + (Random.value - 0.5f) * 0.08f * env;
            }
            clip.SetData(data, 0);
            source.PlayOneShot(clip, volume);
        }
    }
}
