using UnityEngine;

namespace BlockyBattleRoyale.Combat
{
    public class BoostSystem : MonoBehaviour
    {
        [SerializeField] private float maxBoost = 100f;
        [SerializeField] private float currentBoost;
        [SerializeField] private float healPerSecondAtFull = 1.5f;

        private Health health;

        public float Normalized => maxBoost > 0f ? currentBoost / maxBoost : 0f;
        public float Current => currentBoost;

        private void Awake()
        {
            health = GetComponent<Health>();
        }

        private void Update()
        {
            if (currentBoost >= 60f && health != null && health.IsAlive)
                health.Heal(healPerSecondAtFull * Time.deltaTime * (currentBoost / maxBoost));

            currentBoost = Mathf.Max(0f, currentBoost - Time.deltaTime * 0.5f);
        }

        public void AddBoost(float amount)
        {
            currentBoost = Mathf.Clamp(currentBoost + amount, 0f, maxBoost);
        }
    }
}
