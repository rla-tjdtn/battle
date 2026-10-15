using UnityEngine;
using BlockyBattleRoyale.Items;

namespace BlockyBattleRoyale.Visibility
{
    public static class VisibilityUtility
    {
        public static bool CanSee(Transform observer, Transform target, float maxRange)
        {
            if (observer == null || target == null)
                return false;

            Vector3 origin = observer.position + Vector3.up * 1.3f;
            Vector3[] points =
            {
                target.position + Vector3.up * 1.4f, // head
                target.position + Vector3.up * 1.0f, // torso
                target.position + Vector3.up * 0.4f  // legs
            };

            float dist = Vector3.Distance(observer.position, target.position);
            if (dist > maxRange)
                return false;

            // Smoke hides beyond close range
            if (SmokeCloud.IsInsideAny(target.position) && dist > 4f)
                return false;

            // Grass stealth beyond close range
            var stealth = target.GetComponent<GrassStealth>();
            if (stealth != null && stealth.IsHidden && dist > stealth.RevealDistance)
                return false;

            int hits = 0;
            foreach (var p in points)
            {
                Vector3 dir = p - origin;
                if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.transform == target || hit.transform.IsChildOf(target) || hit.transform.root == target.root)
                        hits++;
                }
                else
                {
                    hits++;
                }
            }

            return hits >= 1;
        }
    }
}
