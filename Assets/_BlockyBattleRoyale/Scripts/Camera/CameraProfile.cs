using UnityEngine;

namespace BlockyBattleRoyale.CameraSystem
{
    [CreateAssetMenu(fileName = "CameraProfile", menuName = "Blocky BR/Camera Profile")]
    public class CameraProfile : ScriptableObject
    {
        [Header("Duckov Framing")]
        public float pitchAngle = 60f;
        public float yawAngle = 45f;
        public float distance = 22f;
        public float heightBias = 2.5f;
        public float followSmoothTime = 0.14f;
        public float lookAhead = 3.2f;
        public float maxLookAhead = 5.5f;
        public float aimLookAheadMul = 1.4f;

        [Header("Projection")]
        public bool useOrthographic;
        public float fieldOfView = 40f;
        public float aimFieldOfView = 36f;
        public float orthographicSize = 11f;
        public float aimOrthoSize = 9.5f;

        [Header("Targets (from reference)")]
        [Range(0.04f, 0.2f)] public float targetCharScreenHeight = 0.09f;
        public float charWorldHeight = 1.8f;

        public static CameraProfile CreateRuntimeDefaults()
        {
            var p = CreateInstance<CameraProfile>();
            p.name = "DuckovDefault";
            return p;
        }
    }
}
