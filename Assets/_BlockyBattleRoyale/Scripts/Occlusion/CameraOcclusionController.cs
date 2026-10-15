using System.Collections.Generic;
using UnityEngine;

namespace BlockyBattleRoyale.Occlusion
{
    /// <summary>
    /// Fades CameraOccluder objects between camera and local player.
    /// Separate from gameplay VisibilityController.
    /// </summary>
    public class CameraOcclusionController : MonoBehaviour
    {
        [SerializeField] private Transform followTarget;
        [SerializeField] private float sphereRadius = 0.45f;
        [SerializeField] private LayerMask occluderMask = ~0;
        [SerializeField] private float fadeSpeed = 8f;
        [SerializeField] private float hiddenAlpha = 0.15f;

        private readonly HashSet<Renderer> current = new();
        private readonly Dictionary<Renderer, float> alphas = new();
        private readonly Dictionary<Renderer, MaterialPropertyBlock> blocks = new();
        private readonly Dictionary<Renderer, Color> baseColors = new();
        private readonly RaycastHit[] hits = new RaycastHit[24];
        private static readonly int ColorId = Shader.PropertyToID("_BaseColor");

        public void SetTarget(Transform t) => followTarget = t;

        private void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || followTarget == null) return;

            Vector3 from = cam.transform.position;
            Vector3 to = followTarget.position + Vector3.up * 1.0f;
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist < 0.1f) return;

            current.Clear();
            int n = Physics.SphereCastNonAlloc(from, sphereRadius, dir.normalized, hits, dist, occluderMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                var h = hits[i];
                if (h.collider == null) continue;
                // Marker only — never CompareTag custom tags (undefined tags spam/crash)
                var marker = h.collider.GetComponentInParent<CameraOccluderMarker>();
                if (marker == null) continue;
                foreach (var r in marker.Renderers)
                {
                    if (r != null) current.Add(r);
                }
            }

            // Approach target alphas
            var keys = new List<Renderer>(alphas.Keys);
            foreach (var r in keys)
            {
                float target = current.Contains(r) ? hiddenAlpha : 1f;
                alphas[r] = Mathf.MoveTowards(alphas[r], target, Time.deltaTime * fadeSpeed);
                Apply(r, alphas[r]);
                if (!current.Contains(r) && alphas[r] >= 0.999f)
                    alphas.Remove(r);
            }

            foreach (var r in current)
            {
                if (!alphas.ContainsKey(r))
                {
                    alphas[r] = 1f;
                    Ensure(r);
                }
            }
        }

        private void Ensure(Renderer r)
        {
            if (r == null || blocks.ContainsKey(r)) return;
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            blocks[r] = mpb;
            Color c = Color.white;
            if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(ColorId))
                c = r.sharedMaterial.GetColor(ColorId);
            baseColors[r] = c;
        }

        private void Apply(Renderer r, float a)
        {
            if (r == null) return;
            Ensure(r);
            if (!blocks.ContainsKey(r)) return;
            Color c = baseColors[r];
            c.a = a;
            blocks[r].SetColor(ColorId, c);
            r.SetPropertyBlock(blocks[r]);
        }
    }
}
