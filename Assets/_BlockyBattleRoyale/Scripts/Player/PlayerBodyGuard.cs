using UnityEngine;

namespace BlockyBattleRoyale.Player
{
    /// <summary>Keeps the local body rendered and standing on the ground.</summary>
    public class PlayerBodyGuard : MonoBehaviour
    {
        private CharacterController controller;
        private Renderer[] renderers;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Start()
        {
            ShowBody();
            LiftOntoGround();
        }

        private void LateUpdate()
        {
            ShowBody();
            if (transform.position.y < -1.5f)
                LiftOntoGround();
        }

        private void ShowBody()
        {
            if (renderers == null) return;
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r != null && !r.enabled)
                    r.enabled = true;
            }
        }

        private void LiftOntoGround()
        {
            Vector3 origin = transform.position.y < -1.5f
                ? new Vector3(transform.position.x, 12f, transform.position.z)
                : transform.position + Vector3.up * 0.35f;
            var hits = Physics.RaycastAll(origin, Vector3.down, 40f, ~0, QueryTriggerInteraction.Ignore);
            RaycastHit chosen = default;
            bool found = false;
            float best = float.MaxValue;
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                    continue;
                if (hit.normal.y < 0.45f)
                    continue;
                if (hit.distance < best)
                {
                    best = hit.distance;
                    chosen = hit;
                    found = true;
                }
            }
            if (!found)
                return;

            float gap = chosen.point.y - transform.position.y;
            bool buried = gap > 0.08f && gap < 1.4f;
            bool fell = transform.position.y < -1.5f;
            if (!buried && !fell)
                return;

            Vector3 placed = chosen.point + Vector3.up * 0.08f;
            if (controller != null)
            {
                controller.enabled = false;
                transform.position = placed;
                controller.enabled = true;
            }
            else
            {
                transform.position = placed;
            }
        }
    }
}
