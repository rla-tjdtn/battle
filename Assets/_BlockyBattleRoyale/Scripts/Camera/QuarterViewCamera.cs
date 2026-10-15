using UnityEngine;
using BlockyBattleRoyale.Player;

namespace BlockyBattleRoyale.CameraSystem
{
    /// <summary>
    /// Duckov-style fixed-yaw quarter view.
    /// V2 default: Perspective FOV ~40, pitch 60, yaw 45.
    /// Character should occupy roughly 6–10% of screen height at 1080p.
    /// </summary>
    public class QuarterViewCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Duckov Framing")]
        [SerializeField] private float pitchAngle = 50f;
        [SerializeField] private float yawAngle = 45f;
        [Tooltip("Distance along view back-vector from follow point.")]
        [SerializeField] private float distance = 9.5f;
        [SerializeField] private float heightBias = 0.35f;
        [SerializeField] private float followSmoothTime = 0.14f;
        [SerializeField] private float lookAhead = 0.55f;
        [SerializeField] private float maxLookAhead = 1.3f;
        [SerializeField] private float aimLookAheadMul = 1.4f;

        [Header("Projection")]
        [SerializeField] private bool useOrthographic;
        [SerializeField] private float fieldOfView = 36f;
        [SerializeField] private float aimFieldOfView = 32f;
        [SerializeField] private float orthographicSize = 11f;
        [SerializeField] private float aimOrthoSize = 9.5f;

        [Header("Feel")]
        [SerializeField] private float traumaDecay = 1.5f;
        [SerializeField] private float traumaMaxOffset = 0.35f;

        private Camera cam;
        private PlayerAim aim;
        private Vector3 currentVelocity;
        private float currentFov;
        private float currentOrtho;
        private float trauma;

        public Transform Target => target;
        public float Pitch => pitchAngle;
        public float Yaw => yawAngle;
        public float Distance => distance;
        public float FOV => fieldOfView;

        /// <summary>Estimated character screen height ratio (approx) for debug.</summary>
        public float EstimatedCharacterScreenHeightRatio
        {
            get
            {
                if (cam == null || target == null) return 0f;
                float charHeight = 1.8f;
                if (cam.orthographic)
                    return charHeight / (2f * cam.orthographicSize);
                float dist = Vector3.Distance(transform.position, target.position + Vector3.up);
                float worldH = 2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
                return worldH > 0.01f ? charHeight / worldH : 0f;
            }
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
                cam = gameObject.AddComponent<Camera>();

            ApplyPlayFraming();
            ApplyProjectionSettings();
            currentFov = fieldOfView;
            currentOrtho = orthographicSize;
        }

        private void Start()
        {
            ApplyPlayFraming();
            ApplyProjectionSettings();
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    SetTarget(player.transform);
            }
            SnapToTarget();
        }

        /// <summary>Close quarter view so the blocky body stays readable inside houses.</summary>
        public void ApplyPlayFraming()
        {
            useOrthographic = false;
            pitchAngle = 50f;
            yawAngle = 45f;
            distance = 9.5f;
            heightBias = 0.35f;
            lookAhead = 0.55f;
            maxLookAhead = 1.3f;
            fieldOfView = 36f;
            aimFieldOfView = 32f;
            followSmoothTime = 0.14f;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    SetTarget(player.transform);
                else
                    return;
            }

            Vector3 lookOffset = Vector3.zero;
            if (aim != null)
            {
                Vector3 flat = aim.AimPoint - target.position;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.01f)
                {
                    float ahead = lookAhead;
                    if (aim.IsAiming)
                        ahead *= aimLookAheadMul;
                    lookOffset = flat.normalized * Mathf.Min(ahead, maxLookAhead);
                }
            }

            Quaternion rot = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            Vector3 back = rot * Vector3.back;
            Vector3 offset = back * distance + Vector3.up * heightBias;

            Vector3 followPoint = target.position + Vector3.up * 0.9f + lookOffset;
            Vector3 desired = followPoint + offset;

            if (trauma > 0f)
            {
                trauma = Mathf.MoveTowards(trauma, 0f, Time.deltaTime * traumaDecay);
                desired += Random.insideUnitSphere * (trauma * traumaMaxOffset);
            }

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref currentVelocity, followSmoothTime);
            transform.rotation = rot;

            if (cam == null)
                return;

            if (useOrthographic)
            {
                float targetSize = (aim != null && aim.IsAiming) ? aimOrthoSize : orthographicSize;
                currentOrtho = Mathf.Lerp(currentOrtho, targetSize, Time.deltaTime * 6f);
                cam.orthographicSize = currentOrtho;
            }
            else
            {
                float targetFov = (aim != null && aim.IsAiming) ? aimFieldOfView : fieldOfView;
                currentFov = Mathf.Lerp(currentFov, targetFov, Time.deltaTime * 6f);
                cam.fieldOfView = currentFov;
            }
        }

        public void SetTarget(Transform t)
        {
            target = t;
            aim = t != null ? t.GetComponent<PlayerAim>() : null;
            SnapToTarget();
        }

        public void SnapToTarget()
        {
            if (target == null)
                return;

            Quaternion rot = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            Vector3 back = rot * Vector3.back;
            Vector3 offset = back * distance + Vector3.up * heightBias;
            transform.position = target.position + Vector3.up * 0.9f + offset;
            transform.rotation = rot;
            currentVelocity = Vector3.zero;
        }

        public void AddTrauma(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }

        public void ApplyDuckovDefaults()
        {
            ApplyPlayFraming();
            ApplyProjectionSettings();
            SnapToTarget();
        }

        public void ApplyProfile(CameraProfile profile)
        {
            if (profile == null) return;
            pitchAngle = profile.pitchAngle;
            yawAngle = profile.yawAngle;
            distance = profile.distance;
            heightBias = profile.heightBias;
            followSmoothTime = profile.followSmoothTime;
            lookAhead = profile.lookAhead;
            maxLookAhead = profile.maxLookAhead;
            aimLookAheadMul = profile.aimLookAheadMul;
            useOrthographic = profile.useOrthographic;
            fieldOfView = profile.fieldOfView;
            aimFieldOfView = profile.aimFieldOfView;
            orthographicSize = profile.orthographicSize;
            aimOrthoSize = profile.aimOrthoSize;
            ApplyProjectionSettings();
            SnapToTarget();
        }

        public void WriteToProfile(CameraProfile profile)
        {
            if (profile == null) return;
            profile.pitchAngle = pitchAngle;
            profile.yawAngle = yawAngle;
            profile.distance = distance;
            profile.heightBias = heightBias;
            profile.followSmoothTime = followSmoothTime;
            profile.lookAhead = lookAhead;
            profile.maxLookAhead = maxLookAhead;
            profile.aimLookAheadMul = aimLookAheadMul;
            profile.useOrthographic = useOrthographic;
            profile.fieldOfView = fieldOfView;
            profile.aimFieldOfView = aimFieldOfView;
            profile.orthographicSize = orthographicSize;
            profile.aimOrthoSize = aimOrthoSize;
        }

        public void SetPitch(float v) => pitchAngle = v;
        public void SetYaw(float v) => yawAngle = v;
        public void SetDistance(float v) => distance = v;
        public void SetFov(float v) { fieldOfView = v; ApplyProjectionSettings(); }

        private void ApplyProjectionSettings()
        {
            if (cam == null)
                return;

            cam.orthographic = useOrthographic;
            cam.orthographicSize = orthographicSize;
            cam.fieldOfView = fieldOfView;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 400f;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (cam == null)
                cam = GetComponent<Camera>();
            ApplyProjectionSettings();
        }
#endif
    }
}
