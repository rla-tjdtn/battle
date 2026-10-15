using UnityEngine;

namespace BlockyBattleRoyale.Visibility
{
    public class GrassPatch : MonoBehaviour
    {
        private void Reset()
        {
            var col = GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            var stealth = other.GetComponentInParent<GrassStealth>();
            stealth?.SetHidden(true);
        }

        private void OnTriggerExit(Collider other)
        {
            var stealth = other.GetComponentInParent<GrassStealth>();
            stealth?.SetHidden(false);
        }
    }
}
