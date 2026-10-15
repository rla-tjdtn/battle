using System.Collections.Generic;
using UnityEngine;

namespace BlockyBattleRoyale.Occlusion
{
    /// <summary>
    /// Per-building roof hide for local player. Uses enable toggle (stable)
    /// with optional soft fade via PropertyBlock when materials support alpha.
    /// </summary>
    public class RoofFadeController : MonoBehaviour
    {
        [SerializeField] private Renderer[] roofRenderers;
        [SerializeField] private float fadeSpeed = 8f;

        private float current = 1f;
        private float target = 1f;
        private bool prepared;

        public void Configure(Renderer[] roofs)
        {
            roofRenderers = roofs;
            prepared = false;
        }

        public void SetInside(bool inside)
        {
            target = inside ? 0f : 1f;
        }

        private void Update()
        {
            if (roofRenderers == null || roofRenderers.Length == 0)
                return;

            if (Mathf.Abs(current - target) < 0.01f)
            {
                current = target;
                ApplyVisible(current > 0.5f);
                return;
            }

            current = Mathf.MoveTowards(current, target, Time.deltaTime * fadeSpeed);
            ApplyVisible(current > 0.5f);
        }

        private void ApplyVisible(bool visible)
        {
            foreach (var r in roofRenderers)
            {
                if (r != null)
                    r.enabled = visible;
            }
        }
    }
}
