using UnityEngine;
using BlockyBattleRoyale.Combat;

namespace BlockyBattleRoyale.BattleRoyale
{
    public enum ZoneState
    {
        Waiting,
        Shrinking,
        Finished
    }

    public class ZoneManager : MonoBehaviour
    {
        public static ZoneManager Instance { get; private set; }

        [SerializeField] private ZoneSettings settings;
        [SerializeField] private Vector3 center;
        [SerializeField] private Vector3 nextCenter;
        [SerializeField] private float currentRadius = 90f;
        [SerializeField] private float targetRadius = 70f;
        [SerializeField] private int phaseIndex;
        [SerializeField] private ZoneState state = ZoneState.Waiting;
        [SerializeField] private float stateTimer;
        [SerializeField] private float damagePerSecond = 1.5f;

        private GameObject wallVisual;
        private LineRenderer ring;
        private LineRenderer nextRing;

        public Vector3 Center => center;
        public Vector3 NextCenter => nextCenter;
        public float CurrentRadius => currentRadius;
        public float TargetRadius => targetRadius;
        public int PhaseIndex => phaseIndex;
        public ZoneState State => state;
        public float StateTimer => stateTimer;
        public float DamagePerSecond => damagePerSecond;

        public System.Action OnPhaseChanged;

        private void Awake()
        {
            Instance = this;
        }

        public void Configure(ZoneSettings zoneSettings, Vector3 startCenter)
        {
            settings = zoneSettings;
            center = startCenter;
            nextCenter = startCenter;
            if (settings != null)
            {
                currentRadius = settings.startRadius;
                if (settings.phases != null && settings.phases.Length > 0)
                {
                    targetRadius = settings.phases[0].endRadius;
                    damagePerSecond = settings.phases[0].damagePerSecond;
                    stateTimer = settings.phases[0].waitTime;
                }
            }
            BuildVisuals();
            PickNextCenter();
        }

        private void Update()
        {
            if (settings == null || settings.phases == null || settings.phases.Length == 0)
                return;

            if (state == ZoneState.Finished)
            {
                ApplyOutsideDamage();
                UpdateVisuals();
                return;
            }

            stateTimer -= Time.deltaTime;

            if (state == ZoneState.Waiting)
            {
                if (stateTimer <= 0f)
                {
                    state = ZoneState.Shrinking;
                    stateTimer = settings.phases[Mathf.Min(phaseIndex, settings.phases.Length - 1)].shrinkTime;
                }
            }
            else if (state == ZoneState.Shrinking)
            {
                var phase = settings.phases[Mathf.Min(phaseIndex, settings.phases.Length - 1)];
                float t = 1f - Mathf.Clamp01(stateTimer / Mathf.Max(0.01f, phase.shrinkTime));
                currentRadius = Mathf.Lerp(currentRadius, targetRadius, Time.deltaTime / Mathf.Max(0.01f, phase.shrinkTime));
                center = Vector3.Lerp(center, nextCenter, Time.deltaTime / Mathf.Max(0.01f, phase.shrinkTime));

                if (stateTimer <= 0f)
                {
                    currentRadius = targetRadius;
                    center = nextCenter;
                    phaseIndex++;
                    if (phaseIndex >= settings.phases.Length)
                    {
                        state = ZoneState.Finished;
                    }
                    else
                    {
                        var next = settings.phases[phaseIndex];
                        targetRadius = next.endRadius;
                        damagePerSecond = next.damagePerSecond;
                        state = ZoneState.Waiting;
                        stateTimer = next.waitTime;
                        PickNextCenter();
                        OnPhaseChanged?.Invoke();
                    }
                }
            }

            ApplyOutsideDamage();
            UpdateVisuals();
        }

        private void PickNextCenter()
        {
            float maxOffset = Mathf.Max(0f, currentRadius - targetRadius) * 0.45f;
            Vector2 offset = Random.insideUnitCircle * maxOffset;
            nextCenter = center + new Vector3(offset.x, 0f, offset.y);
        }

        private void ApplyOutsideDamage()
        {
            var healths = FindObjectsByType<Health>(FindObjectsSortMode.None);
            foreach (var h in healths)
            {
                if (h == null || !h.IsAlive)
                    continue;

                Vector3 p = h.transform.position;
                p.y = center.y;
                float dist = Vector3.Distance(new Vector3(h.transform.position.x, center.y, h.transform.position.z), center);
                if (dist > currentRadius)
                {
                    h.ApplyDamage(new DamageInfo
                    {
                        Amount = damagePerSecond * Time.deltaTime,
                        BodyMultiplier = 1f,
                        HitPoint = h.transform.position,
                        HitNormal = Vector3.up,
                        Attacker = null,
                        Hitbox = HitboxType.Body
                    });
                }
            }
        }

        public bool IsInside(Vector3 worldPos)
        {
            Vector3 flat = new Vector3(worldPos.x, center.y, worldPos.z);
            return Vector3.Distance(flat, center) <= currentRadius;
        }

        public Vector3 GetSafePoint(float margin = 5f)
        {
            Vector2 r = Random.insideUnitCircle * Mathf.Max(1f, currentRadius - margin);
            return center + new Vector3(r.x, 0f, r.y);
        }

        private void BuildVisuals()
        {
            wallVisual = new GameObject("ZoneWall");
            wallVisual.transform.SetParent(transform, false);
            ring = wallVisual.AddComponent<LineRenderer>();
            SetupRing(ring, new Color(0.2f, 0.6f, 1f, 0.85f), 0.35f);

            var nextGo = new GameObject("NextZoneRing");
            nextGo.transform.SetParent(transform, false);
            nextRing = nextGo.AddComponent<LineRenderer>();
            SetupRing(nextRing, new Color(1f, 1f, 1f, 0.45f), 0.2f);
        }

        private void SetupRing(LineRenderer lr, Color color, float width)
        {
            lr.loop = true;
            lr.positionCount = 64;
            lr.widthMultiplier = width;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
            lr.useWorldSpace = true;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private void UpdateVisuals()
        {
            DrawCircle(ring, center, currentRadius);
            DrawCircle(nextRing, nextCenter, targetRadius);
        }

        private void DrawCircle(LineRenderer lr, Vector3 c, float radius)
        {
            if (lr == null)
                return;
            int count = lr.positionCount;
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 2f;
                lr.SetPosition(i, c + new Vector3(Mathf.Cos(a) * radius, 0.5f, Mathf.Sin(a) * radius));
            }
        }
    }
}
