using UnityEngine;

namespace BlockyBattleRoyale.Weapons
{
    public enum AttachmentSlot
    {
        Sight,
        Muzzle,
        Grip,
        Magazine,
        Stock
    }

    [CreateAssetMenu(fileName = "Attachment", menuName = "Blocky BR/Weapon Attachment")]
    public class WeaponAttachmentData : ScriptableObject
    {
        public string attachmentName = "Red Dot";
        public AttachmentSlot slot = AttachmentSlot.Sight;
        public float spreadMul = 0.85f;
        public float recoilMul = 0.9f;
        public float rangeBonus;
        public int magazineBonus;
        public float aimOffsetBonus = 1.1f;
        public Color tint = new Color(0.3f, 0.3f, 0.32f);
        public bool isSuppressor;
        public float hearRangeMultiplier = 1f;
    }
}
