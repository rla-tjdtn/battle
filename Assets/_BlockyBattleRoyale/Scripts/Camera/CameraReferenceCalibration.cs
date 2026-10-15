using UnityEngine;

namespace BlockyBattleRoyale.CameraSystem
{
    /// <summary>
    /// Compares estimated character screen height to reference target band.
    /// </summary>
    public class CameraReferenceCalibration : MonoBehaviour
    {
        [SerializeField] private QuarterViewCamera cameraRig;
        [SerializeField] private CameraProfile profile;
        [SerializeField] private float outdoorTarget = 0.09f;
        [SerializeField] private float tolerance = 0.05f;

        public float CurrentRatio => cameraRig != null ? cameraRig.EstimatedCharacterScreenHeightRatio : 0f;
        public bool WithinTolerance
        {
            get
            {
                float r = CurrentRatio;
                float t = profile != null ? profile.targetCharScreenHeight : outdoorTarget;
                return Mathf.Abs(r - t) <= t * tolerance;
            }
        }

        public string StatusText
        {
            get
            {
                float r = CurrentRatio;
                float t = profile != null ? profile.targetCharScreenHeight : outdoorTarget;
                float err = t > 0.001f ? Mathf.Abs(r - t) / t : 0f;
                return $"CharH {r * 100f:0.0}% vs target {t * 100f:0.0}%  err {err * 100f:0.0}%  {(WithinTolerance ? "OK" : "ADJUST")}";
            }
        }

        private void Awake()
        {
            if (cameraRig == null)
                cameraRig = FindFirstObjectByType<QuarterViewCamera>();
        }

        public void ApplyProfile()
        {
            if (cameraRig != null && profile != null)
                cameraRig.ApplyProfile(profile);
        }

        /// <summary>Nudges distance so estimated char screen height approaches target.</summary>
        public void AutoFitDistance()
        {
            if (cameraRig == null) return;
            float t = profile != null ? profile.targetCharScreenHeight : outdoorTarget;
            for (int i = 0; i < 24; i++)
            {
                float r = cameraRig.EstimatedCharacterScreenHeightRatio;
                if (Mathf.Abs(r - t) < 0.002f) break;
                float dist = cameraRig.Distance;
                if (r > t) cameraRig.SetDistance(dist + 0.4f);
                else cameraRig.SetDistance(Mathf.Max(8f, dist - 0.4f));
                cameraRig.SnapToTarget();
            }
        }
    }
}
