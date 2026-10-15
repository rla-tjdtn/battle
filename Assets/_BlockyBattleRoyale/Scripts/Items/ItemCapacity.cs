using BlockyBattleRoyale.Weapons;

namespace BlockyBattleRoyale.Items
{
    /// <summary>Balance numbers for backpack capacity. Not copied from another game.</summary>
    public static class ItemCapacity
    {
        public static float MaxForLevel(int level)
        {
            switch (level)
            {
                case 1: return 150f;
                case 2: return 200f;
                case 3: return 250f;
                default: return 50f;
            }
        }

        public static float PerRound(AmmoType type)
        {
            switch (type)
            {
                case AmmoType.Pistol: return 0.25f;
                case AmmoType.SMG: return 0.35f;
                case AmmoType.Rifle: return 0.5f;
                case AmmoType.Shotgun: return 0.7f;
                default: return 0.5f;
            }
        }

        public static float OfStack(ItemData item)
        {
            if (item == null) return 0f;
            switch (item.category)
            {
                case ItemCategory.Ammo:
                    return PerRound(item.ammoType) * UnityEngine.Mathf.Max(1, item.ammoAmount);
                case ItemCategory.Heal:
                    if (item.healType == HealType.Bandage) return 2f;
                    if (item.healType == HealType.FirstAid || item.healType == HealType.MedKit) return 10f;
                    return 4f;
                case ItemCategory.Throwable:
                    return item.throwableType == ThrowableType.Smoke ? 14f : 12f;
                case ItemCategory.Backpack:
                    return 0f;
                default:
                    return UnityEngine.Mathf.Max(1f, item.weight);
            }
        }
    }
}
