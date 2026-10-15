using UnityEngine;
using BlockyBattleRoyale.Occlusion;

namespace BlockyBattleRoyale.World
{
    /// <summary>
    /// Legacy wrapper — prefers RoofFadeController alpha fade when present.
    /// </summary>
    public class RoofFader : MonoBehaviour
    {
        [SerializeField] private Renderer[] roofRenderers;
        [SerializeField] private Collider roomTrigger;
        [SerializeField] private RoofFadeController fadeController;

        private void Reset()
        {
            roomTrigger = GetComponent<Collider>();
        }

        public void Configure(Renderer[] roofs, Collider trigger)
        {
            roofRenderers = roofs;
            roomTrigger = trigger;
            if (roomTrigger != null)
                roomTrigger.isTrigger = true;

            fadeController = GetComponentInParent<RoofFadeController>();
            if (fadeController == null)
            {
                fadeController = gameObject.AddComponent<RoofFadeController>();
                fadeController.Configure(roofs);
            }
            else
            {
                fadeController.Configure(roofs);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;
            if (fadeController != null)
                fadeController.SetInside(true);
            else
                SetRoofsVisible(false);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;
            if (fadeController != null)
                fadeController.SetInside(false);
            else
                SetRoofsVisible(true);
        }

        private void SetRoofsVisible(bool visible)
        {
            if (roofRenderers == null)
                return;
            foreach (var r in roofRenderers)
            {
                if (r != null)
                    r.enabled = visible;
            }
        }
    }
}
