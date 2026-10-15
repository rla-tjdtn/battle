using UnityEngine;
using BlockyBattleRoyale.CameraSystem;
using BlockyBattleRoyale.Core;
using BlockyBattleRoyale.Player;
using BlockyBattleRoyale.UI;
using BlockyBattleRoyale.Weapons;
using BlockyBattleRoyale.AI;

namespace BlockyBattleRoyale.World
{
    /// <summary>
    /// Runtime fallback: if the scene was saved empty, rebuild a playable prototype on Play.
    /// Prefer using Tools > Blocky Battle Royale > Generate Prototype in the Editor.
    /// </summary>
    public class PrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private bool buildIfMissing = true;
        [SerializeField] private WeaponData defaultWeapon;

        private void Awake()
        {
            if (!buildIfMissing)
                return;

            if (GameObject.FindGameObjectWithTag("Player") != null)
                return;

            BuildMinimalPlayable();
        }

        private void BuildMinimalPlayable()
        {
            if (FindFirstObjectByType<GameManager>() == null)
            {
                var gm = new GameObject("GameManager");
                gm.AddComponent<GameManager>();
            }

            EnsureGround();
            EnsureLight();

            if (defaultWeapon == null)
                defaultWeapon = ScriptableObject.CreateInstance<WeaponData>();

            var player = BlockyCharacterBuilder.Build("Player", new Vector3(0f, 0.1f, 0f), new Color(0.2f, 0.45f, 0.85f), true, defaultWeapon);
            player.name = "Player";

            var camGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera");
            if (camGo.GetComponent<Camera>() == null)
                camGo.AddComponent<Camera>();
            camGo.tag = "MainCamera";
            var qv = camGo.GetComponent<QuarterViewCamera>();
            if (qv == null)
                qv = camGo.AddComponent<QuarterViewCamera>();
            qv.SetTarget(player.transform);

            if (camGo.GetComponent<AudioListener>() == null)
                camGo.AddComponent<AudioListener>();

            // Spawn a few dummies
            Color[] colors =
            {
                new Color(0.85f, 0.25f, 0.25f),
                new Color(0.25f, 0.7f, 0.35f),
                new Color(0.9f, 0.75f, 0.2f),
                new Color(0.4f, 0.4f, 0.45f)
            };

            for (int i = 0; i < 4; i++)
            {
                float angle = i * 90f * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(Mathf.Cos(angle) * 12f, 0.1f, Mathf.Sin(angle) * 12f);
                var enemy = BlockyCharacterBuilder.Build($"Enemy_{i + 1}", pos, colors[i % colors.Length], false, defaultWeapon);
                enemy.AddComponent<DummyEnemyAI>();
            }

            if (FindFirstObjectByType<GameHUD>() == null)
            {
                var hudGo = new GameObject("GameHUD");
                hudGo.AddComponent<GameHUD>();
            }
        }

        private static void EnsureGround()
        {
            if (GameObject.Find("Ground") != null)
                return;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(8f, 1f, 8f);
            ground.tag = "Ground";
            var rend = ground.GetComponent<Renderer>();
            if (rend != null)
                rend.sharedMaterial = BlockyCharacterBuilder.CreateLitMaterial(new Color(0.35f, 0.5f, 0.32f));
        }

        private static void EnsureLight()
        {
            if (Object.FindFirstObjectByType<Light>() != null)
                return;

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.9f);
            light.intensity = 1.1f;
            light.shadows = LightShadows.Soft;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.65f, 0.75f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.45f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f);
        }
    }
}
