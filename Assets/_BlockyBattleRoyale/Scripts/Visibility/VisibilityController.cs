using UnityEngine;

namespace BlockyBattleRoyale.Visibility
{
    /// <summary>
    /// GAMEPLAY visibility (separate from CameraOcclusion / RoofFade).
    /// Hides enemy renderers the local player cannot detect via LOS rules.
    /// Does not disable colliders or hitboxes — bullets still interact.
    /// </summary>
    public class VisibilityController : MonoBehaviour
    {
        [Header("Gameplay Vision (not camera)")]
        [SerializeField] private float viewRange = 45f;
        [SerializeField] private float viewAngleDegrees = 220f;
        [SerializeField] private float updateInterval = 0.12f;

        private Transform player;
        private float nextUpdate;

        private void Update()
        {
            if (Time.time < nextUpdate)
                return;
            nextUpdate = Time.time + updateInterval;

            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
                else return;
            }

            var combatants = FindObjectsByType<Combat.Health>(FindObjectsSortMode.None);
            foreach (var h in combatants)
            {
                if (h == null || h.transform == player)
                    continue;

                bool visible = !h.IsAlive || CanDetect(player, h.transform);
                SetRenderers(h.gameObject, visible);
            }
        }

        private bool CanDetect(Transform from, Transform to)
        {
            Vector3 flat = to.position - from.position;
            flat.y = 0f;
            float dist = flat.magnitude;
            if (dist > viewRange)
                return false;

            if (dist > 2.5f)
            {
                Vector3 fwd = from.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude > 0.001f)
                {
                    float ang = Vector3.Angle(fwd.normalized, flat.normalized);
                    if (ang > viewAngleDegrees * 0.5f)
                        return false;
                }
            }

            return VisibilityUtility.CanSee(from, to, viewRange);
        }

        private static void SetRenderers(GameObject go, bool visible)
        {
            var rends = go.GetComponentsInChildren<Renderer>();
            foreach (var r in rends)
            {
                if (r != null)
                    r.enabled = visible;
            }
        }
    }
}
