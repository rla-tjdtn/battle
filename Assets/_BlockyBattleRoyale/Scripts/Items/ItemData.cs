using UnityEngine;
using BlockyBattleRoyale.Weapons;

namespace BlockyBattleRoyale.Items
{
    public enum ItemCategory
    {
        Weapon,
        Ammo,
        Helmet,
        Vest,
        Backpack,
        Heal,
        Throwable,
        Attachment,
        Fuel
    }

    public enum HealType
    {
        Bandage,
        FirstAid,
        MedKit,
        EnergyDrink,
        Painkiller
    }

    public enum ThrowableType
    {
        Grenade,
        Smoke
    }

    [CreateAssetMenu(fileName = "ItemData", menuName = "Blocky BR/Item Data")]
    public class ItemData : ScriptableObject
    {
        public string itemName = "Item";
        public ItemCategory category = ItemCategory.Ammo;
        public Sprite icon;
        public Color tint = Color.white;
        public int maxStack = 1;
        public float weight = 1f;
        public Vector3 worldScale = new Vector3(0.6f, 0.35f, 0.6f);

        [Header("Weapon")]
        public WeaponData weaponData;
        public WeaponAttachmentData attachmentData;

        [Header("Ammo")]
        public AmmoType ammoType = AmmoType.Pistol;
        public int ammoAmount = 30;

        [Header("Armor")]
        [Range(1, 3)] public int armorLevel = 1;
        public float damageReduction = 0.2f;
        public float durability = 100f;

        [Header("Heal")]
        public HealType healType = HealType.Bandage;
        public float healAmount = 15f;
        public float useDuration = 2.5f;
        public bool blockActionsWhileUsing = true;

        [Header("Throwable")]
        public ThrowableType throwableType = ThrowableType.Grenade;
        public float throwForce = 14f;
        public float explodeDelay = 2.2f;
        public float explodeRadius = 5f;
        public float explodeDamage = 80f;
        public float smokeDuration = 10f;
        public float smokeRadius = 6f;
    }
}
