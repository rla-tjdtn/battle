using UnityEngine;
using UnityEngine.Rendering;

namespace BlockyBattleRoyale.Rendering
{
    public static class SceneLighting
    {
        public static void ApplyDaylight()
        {
            Light light = null;
            foreach (var candidate in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (candidate.type == LightType.Directional)
                {
                    light = candidate;
                    break;
                }
            }
            if (light == null)
                light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.93f, 0.82f);
            light.intensity = 1.15f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.72f;
            light.transform.rotation = Quaternion.Euler(52f, -35f, 0f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.7f, 0.78f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.46f, 0.38f);
            RenderSettings.ambientGroundColor = new Color(0.28f, 0.26f, 0.2f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0045f;
            RenderSettings.fogColor = new Color(0.72f, 0.76f, 0.7f);

            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowDistance = 80f;
            QualitySettings.shadowCascades = 2;
        }
    }
}
