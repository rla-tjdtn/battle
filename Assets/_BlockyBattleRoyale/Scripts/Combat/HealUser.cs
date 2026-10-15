using UnityEngine;
using BlockyBattleRoyale.Items;
using BlockyBattleRoyale.InventorySystem;
using BlockyBattleRoyale.Player;
using BlockyBattleRoyale.Weapons;

namespace BlockyBattleRoyale.Combat
{
    public class HealUser : MonoBehaviour
    {
        private Health health;
        private PlayerInventory inventory;
        private PlayerMotor motor;
        private WeaponController weapon;
        private bool usingItem;
        private float endTime;
        private ItemData activeItem;

        public bool IsUsing => usingItem;
        public float Progress => usingItem && activeItem != null
            ? 1f - Mathf.Clamp01((endTime - Time.time) / Mathf.Max(0.01f, activeItem.useDuration))
            : 0f;

        private void Awake()
        {
            health = GetComponent<Health>();
            inventory = GetComponent<PlayerInventory>();
            motor = GetComponent<PlayerMotor>();
            weapon = GetComponent<WeaponController>();
        }

        private void Update()
        {
            if (usingItem)
            {
                if (Input.GetMouseButton(0) || Input.GetKey(KeyCode.LeftShift))
                    Cancel();

                if (Time.time >= endTime)
                    Complete();
                return;
            }

            if (!CompareTag("Player"))
                return;

            if (Input.GetKeyDown(KeyCode.Alpha4))
                TryUseBestHeal();
        }

        public bool TryUse(ItemData item)
        {
            if (usingItem || item == null || item.category != ItemCategory.Heal)
                return false;
            if (health == null || !health.IsAlive)
                return false;
            if (health.CurrentHealth >= health.MaxHealth &&
                item.healType != HealType.EnergyDrink && item.healType != HealType.Painkiller)
                return false;

            activeItem = item;
            usingItem = true;
            endTime = Time.time + item.useDuration;

            if (item.blockActionsWhileUsing)
            {
                if (weapon != null) weapon.enabled = false;
            }

            return true;
        }

        public void TryUseBestHeal()
        {
            if (inventory == null)
                return;

            var item = inventory.FindBestHeal();
            if (item != null && TryUse(item))
                inventory.ConsumeItem(item, 1);
        }

        private void Complete()
        {
            if (activeItem != null && health != null)
            {
                health.Heal(activeItem.healAmount);
                if (activeItem.healType == HealType.EnergyDrink || activeItem.healType == HealType.Painkiller)
                    GetComponent<BoostSystem>()?.AddBoost(40f);
            }

            Cancel();
        }

        public void Cancel()
        {
            usingItem = false;
            activeItem = null;
            if (weapon != null) weapon.enabled = true;
        }
    }
}
