using System.Collections.Generic;
using UnityEngine;

namespace BlockyBattleRoyale.Weapons
{
    public class WeaponLoadout : MonoBehaviour
    {
        [SerializeField] private List<WeaponAttachmentData> attachments = new List<WeaponAttachmentData>();
        private WeaponController weapon;

        public IReadOnlyList<WeaponAttachmentData> Attachments => attachments;

        private void Awake()
        {
            weapon = GetComponent<WeaponController>();
        }

        public bool HasSlot(AttachmentSlot slot)
        {
            foreach (var a in attachments)
                if (a != null && a.slot == slot) return true;
            return false;
        }

        public bool HasSuppressor()
        {
            foreach (var a in attachments)
                if (a != null && a.isSuppressor) return true;
            return false;
        }

        public float HearRangeMultiplier()
        {
            float m = 1f;
            foreach (var a in attachments)
                if (a != null && a.isSuppressor)
                    m *= Mathf.Clamp(a.hearRangeMultiplier, 0.15f, 1f);
            return m;
        }

        public void Equip(WeaponAttachmentData data)
        {
            if (data == null) return;
            if (HasSlot(data.slot)) return;
            attachments.Add(data);
            Apply();
        }

        public void Clear()
        {
            attachments.Clear();
            Apply();
        }

        public void Apply()
        {
            if (weapon == null || weapon.Data == null)
                return;

            // Runtime modifiers applied via WeaponController helper
            float spread = 1f, recoil = 1f, range = 0f;
            int mag = 0;
            foreach (var a in attachments)
            {
                if (a == null) continue;
                spread *= a.spreadMul;
                recoil *= a.recoilMul;
                range += a.rangeBonus;
                mag += a.magazineBonus;
            }

            weapon.SetAttachmentModifiers(spread, recoil, range, mag);
        }
    }
}
