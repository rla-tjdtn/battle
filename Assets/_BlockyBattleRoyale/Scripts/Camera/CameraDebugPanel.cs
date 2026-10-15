using UnityEngine;

namespace BlockyBattleRoyale.CameraSystem
{
    /// <summary>F8 live camera tweaks (V3). Works alongside F3 status HUD.</summary>
    public class CameraDebugPanel : MonoBehaviour
    {
        [SerializeField] private QuarterViewCamera quarterView;
        [SerializeField] private bool visible;

        private void Awake()
        {
            if (quarterView == null)
                quarterView = FindFirstObjectByType<QuarterViewCamera>();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F8))
                visible = !visible;
        }

        private void OnGUI()
        {
            if (!visible || quarterView == null)
                return;

            const float w = 320f;
            GUILayout.BeginArea(new Rect(12, 12, w, 320), GUI.skin.box);
            GUILayout.Label("CAMERA DEBUG (F8)");
            float pitch = GUILayout.HorizontalSlider(quarterView.Pitch, 40f, 80f);
            GUILayout.Label($"Pitch {pitch:0.0}");
            float yaw = GUILayout.HorizontalSlider(quarterView.Yaw, 0f, 90f);
            GUILayout.Label($"Yaw {yaw:0.0}");
            float dist = GUILayout.HorizontalSlider(quarterView.Distance, 8f, 45f);
            GUILayout.Label($"Distance {dist:0.0}");
            float fov = GUILayout.HorizontalSlider(quarterView.FOV, 25f, 60f);
            GUILayout.Label($"FOV {fov:0.0}");
            GUILayout.Label($"Char screen H ≈ {quarterView.EstimatedCharacterScreenHeightRatio * 100f:0.0}% (target 8–10%)");

            if (Mathf.Abs(pitch - quarterView.Pitch) > 0.01f) quarterView.SetPitch(pitch);
            if (Mathf.Abs(yaw - quarterView.Yaw) > 0.01f) quarterView.SetYaw(yaw);
            if (Mathf.Abs(dist - quarterView.Distance) > 0.01f) quarterView.SetDistance(dist);
            if (Mathf.Abs(fov - quarterView.FOV) > 0.01f) quarterView.SetFov(fov);

            if (GUILayout.Button("Apply Duckov Defaults"))
                quarterView.ApplyDuckovDefaults();
            GUILayout.EndArea();
        }
    }
}
