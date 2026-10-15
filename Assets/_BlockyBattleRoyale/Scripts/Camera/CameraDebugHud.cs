using UnityEngine;

namespace BlockyBattleRoyale.CameraSystem
{
    /// <summary>
    /// Dev overlay: F3 toggle. Shows FOV / pitch / estimated character screen ratio.
    /// </summary>
    public class CameraDebugHud : MonoBehaviour
    {
        [SerializeField] private bool visible;
        private QuarterViewCamera qv;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F3))
                visible = !visible;

            if (qv == null)
                qv = FindFirstObjectByType<QuarterViewCamera>();
        }

        private void OnGUI()
        {
            if (!visible || qv == null)
                return;

            const int w = 360;
            GUI.Box(new Rect(12, 12, w, 120), "Camera Debug (F3)");
            GUI.Label(new Rect(24, 40, w - 24, 20), $"Pitch {qv.Pitch:0}  Yaw {qv.Yaw:0}  Dist {qv.Distance:0.0}");
            GUI.Label(new Rect(24, 60, w - 24, 20), $"FOV {qv.FOV:0.0}  CharScreenH ~{qv.EstimatedCharacterScreenHeightRatio * 100f:0.0}%");
            GUI.Label(new Rect(24, 80, w - 24, 20), "Target Duckov-like: ~6–10% screen height");
            if (GUI.Button(new Rect(24, 100, 160, 24), "Apply Duckov Defaults"))
                qv.ApplyDuckovDefaults();
        }
    }
}
