using UnityEngine;

namespace BlockyBattleRoyale.Weapons
{
    public enum WeaponFireMode
    {
        SemiAuto,
        FullAuto,
        Shotgun
    }

    public enum AmmoType
    {
        Pistol,
        SMG,
        Rifle,
        Shotgun
    }

    [CreateAssetMenu(fileName = "WeaponData", menuName = "Blocky BR/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        public string weaponName = "Pistol";
        public WeaponFireMode fireMode = WeaponFireMode.SemiAuto;
        public AmmoType ammoType = AmmoType.Pistol;

        [Header("Combat")]
        public float damage = 25f;
        public float fireRate = 4f;
        public int magazineSize = 12;
        public float reloadTime = 1.6f;
        public float range = 60f;
        public float recoilPitch = 1.2f;
        public float recoilYaw = 0.4f;
        public float hipSpread = 2.5f;
        public float aimSpread = 0.6f;
        public float moveSpreadMultiplier = 1.6f;
        public int pelletCount = 1;

        [Header("Melee")]
        public bool isMelee;
        public float meleeRange = 1.8f;
        public float meleeRadius = 0.55f;

        [Header("Visual")]
        public Color weaponColor = new Color(0.18f, 0.18f, 0.2f);
        public Vector3 muzzleLocalOffset = new Vector3(0f, 0.05f, 0.45f);
    }
}
