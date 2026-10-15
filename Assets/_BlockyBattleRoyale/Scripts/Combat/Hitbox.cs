using UnityEngine;

namespace BlockyBattleRoyale.Combat
{
    public class Hitbox : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private HitboxType hitboxType = HitboxType.Body;
        [SerializeField] private float damageMultiplier = 1f;

        public Health Health => health != null ? health : health = GetComponentInParent<Health>();
        public HitboxType Type => hitboxType;

        public float GetMultiplier()
        {
            if (damageMultiplier > 0f && !Mathf.Approximately(damageMultiplier, 1f))
                return damageMultiplier;

            return hitboxType switch
            {
                HitboxType.Head => 1.8f,
                HitboxType.Limb => 0.75f,
                _ => 1f
            };
        }

        public void Configure(Health owner, HitboxType type)
        {
            health = owner;
            hitboxType = type;
        }
    }
}
