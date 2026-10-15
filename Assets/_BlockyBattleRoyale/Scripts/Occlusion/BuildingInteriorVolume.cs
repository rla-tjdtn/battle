using UnityEngine;

namespace BlockyBattleRoyale.Occlusion
{
    [RequireComponent(typeof(Collider))]
    public class BuildingInteriorVolume : MonoBehaviour
    {
        [SerializeField] private RoofFadeController roofFade;

        public void Configure(RoofFadeController fade)
        {
            roofFade = fade;
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            roofFade?.SetInside(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            roofFade?.SetInside(false);
        }
    }
}
