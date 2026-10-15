using UnityEngine;
using BlockyBattleRoyale.Items;

namespace BlockyBattleRoyale.Combat
{
    public class ArmorSystem : MonoBehaviour
    {
        [SerializeField] private ItemData helmet;
        [SerializeField] private ItemData vest;
        [SerializeField] private float helmetDurability;
        [SerializeField] private float vestDurability;

        public ItemData Helmet => helmet;
        public ItemData Vest => vest;
        public float HelmetDurability => helmetDurability;
        public float VestDurability => vestDurability;

        public System.Action OnArmorChanged;

        public void EquipHelmet(ItemData data)
        {
            helmet = data;
            helmetDurability = data != null ? data.durability : 0f;
            OnArmorChanged?.Invoke();
        }

        public void EquipVest(ItemData data)
        {
            vest = data;
            vestDurability = data != null ? data.durability : 0f;
            OnArmorChanged?.Invoke();
        }

        public float Mitigate(float rawDamage, HitboxType hitbox)
        {
            float damage = rawDamage;

            if (hitbox == HitboxType.Head && helmet != null && helmetDurability > 0f)
            {
                float reduced = damage * (1f - helmet.damageReduction);
                float absorbed = damage - reduced;
                helmetDurability = Mathf.Max(0f, helmetDurability - absorbed);
                damage = reduced;
                if (helmetDurability <= 0f)
                {
                    helmet = null;
                    OnArmorChanged?.Invoke();
                }
            }
            else if (hitbox != HitboxType.Head && vest != null && vestDurability > 0f)
            {
                float reduced = damage * (1f - vest.damageReduction);
                float absorbed = damage - reduced;
                vestDurability = Mathf.Max(0f, vestDurability - absorbed);
                damage = reduced;
                if (vestDurability <= 0f)
                {
                    vest = null;
                    OnArmorChanged?.Invoke();
                }
            }

            return damage;
        }
    }
}
