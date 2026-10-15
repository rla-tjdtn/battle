using System.Collections.Generic;
using UnityEngine;
using BlockyBattleRoyale.Combat;
using BlockyBattleRoyale.Items;
using BlockyBattleRoyale.Weapons;

namespace BlockyBattleRoyale.InventorySystem
{
    /// <summary>PUBG-style: 2 guns + melee/fists + backpack capacity + Tab inventory.</summary>
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField] private int slotCount = 20;
        [SerializeField] private float maxWeight = 80f;
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

        [Header("Equipment")]
        [SerializeField] private WeaponData primary1;
        [SerializeField] private WeaponData primary2;
        [SerializeField] private WeaponData melee;
        [SerializeField] private WeaponData fistsFallback;
        [SerializeField] private int backpackLevel; // 0 none, 1-3 equipped

        private WeaponController weapon;
        private ArmorSystem armor;
        private int activeWeaponIndex = 3; // start on fists
        private readonly Dictionary<AmmoType, int> ammoReserve = new Dictionary<AmmoType, int>();
        private GameObject backpackVisual;

        public IReadOnlyList<InventorySlot> Slots => slots;
        public WeaponData Primary1 => primary1;
        public WeaponData Primary2 => primary2;
        public WeaponData Melee => melee != null ? melee : fistsFallback;
        public int ActiveSlot => activeWeaponIndex;
        public int BackpackLevel => backpackLevel;
        public float CurrentWeight { get; private set; }
        public float MaxWeight => maxWeight;
        public float UsedCapacity => CurrentWeight;
        public float MaxCapacity => maxWeight;
        public bool IsOpen { get; private set; }
        public string StatusMessage { get; private set; } = "";

        public System.Action OnInventoryChanged;

        private void Awake()
        {
            weapon = GetComponent<WeaponController>();
            armor = GetComponent<ArmorSystem>();
            EnsureSlots();
            ApplyBackpackStats();
            EnsureBackpackVisual();
        }

        private void Start()
        {
            var hp = GetComponent<Health>();
            if (hp != null)
                hp.OnDeath += () => { IsOpen = false; Core.InputContext.InventoryOpen = false; };
            if (fistsFallback == null)
                fistsFallback = Resources.Load<WeaponData>("Weapons/Fists");
            if (melee == null)
                melee = fistsFallback;
            // Prefer fists until a gun is picked up
            if (primary1 == null && primary2 == null)
                EquipWeaponSlot(3);
        }

        private void Update()
        {
            if (!CompareTag("Player"))
                return;

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                IsOpen = !IsOpen;
                Core.InputContext.InventoryOpen = IsOpen;
            }

            if (IsOpen && Input.GetKeyDown(KeyCode.LeftBracket))
                SortSlots();
            if (IsOpen && Input.GetKeyDown(KeyCode.Backspace))
                DropHalfOfLastStack();

            bool seatModifier = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (!seatModifier && Input.GetKeyDown(KeyCode.Alpha1)) { EquipWeaponSlot(1); weapon?.SetHolstered(false); }
            if (!seatModifier && Input.GetKeyDown(KeyCode.Alpha2)) { EquipWeaponSlot(2); weapon?.SetHolstered(false); }
            if (Input.GetKeyDown(KeyCode.V) || Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Alpha4))
            {
                EquipWeaponSlot(3);
                weapon?.SetHolstered(false);
            }
            if (Input.GetKeyDown(KeyCode.Alpha5))
                GetComponent<Combat.HealUser>()?.TryUseBestHeal();
            if (Input.GetKeyDown(KeyCode.C))
                weapon?.ToggleHolster();
            if (Input.GetKeyDown(KeyCode.G)) TryThrowGrenade();
        }

        private void EnsureSlots()
        {
            while (slots.Count < slotCount)
                slots.Add(new InventorySlot());
        }

        private void ApplyBackpackStats()
        {
            maxWeight = ItemCapacity.MaxForLevel(backpackLevel);
            slotCount = backpackLevel == 0 ? 8 : backpackLevel == 1 ? 16 : backpackLevel == 2 ? 22 : 28;
            EnsureSlots();
            // Trim excess empty slots if downgraded
            while (slots.Count > slotCount)
            {
                int last = slots.Count - 1;
                if (!slots[last].IsEmpty) break;
                slots.RemoveAt(last);
            }
        }

        private void EnsureBackpackVisual()
        {
            var pack = transform.Find("Visual/Pack");
            if (pack != null)
                backpackVisual = pack.gameObject;
            RefreshBackpackVisual();
        }

        private void RefreshBackpackVisual()
        {
            if (backpackVisual == null) return;
            backpackVisual.SetActive(backpackLevel > 0);
            float s = backpackLevel <= 1 ? 0.8f : backpackLevel == 2 ? 1.05f : 1.35f;
            backpackVisual.transform.localScale = new Vector3(0.42f, 0.36f, 0.16f) * s;
        }

        public bool TryAdd(ItemData item, int amount = 1)
        {
            int left = amount;
            AcceptStacks(item, ref left);
            return left <= 0;
        }

        public int AcceptStacks(ItemData item, ref int amount)
        {
            if (item == null || amount <= 0)
                return 0;
            int before = amount;

            if (item.category == ItemCategory.Attachment && item.attachmentData != null)
            {
                var loadout = GetComponent<WeaponLoadout>();
                if (Core.GameSettings.AutoAttachment && loadout != null && !loadout.HasSlot(item.attachmentData.slot))
                {
                    loadout.Equip(item.attachmentData);
                    amount -= 1;
                    StatusMessage = item.attachmentData.attachmentName + " 장착";
                    OnInventoryChanged?.Invoke();
                    return 1;
                }
            }

            if (item.category == ItemCategory.Backpack)
            {
                int lvl = Mathf.Clamp(item.armorLevel > 0 ? item.armorLevel : 1, 1, 3);
                bool wantEquip = (backpackLevel == 0 && Core.GameSettings.AutoBag)
                    || (lvl > backpackLevel && Core.GameSettings.AutoUpgrade);
                if (!wantEquip)
                {
                    // stored in a normal slot below
                }
                else
                {
                float nextMax = ItemCapacity.MaxForLevel(lvl);
                if (UsedCapacity > nextMax)
                {
                    StatusMessage = "아이템이 더 많아서 작은 가방으로 바꿀 수 없습니다";
                    OnInventoryChanged?.Invoke();
                    return 0;
                }
                backpackLevel = lvl;
                ApplyBackpackStats();
                RefreshBackpackVisual();
                amount -= 1;
                StatusMessage = $"가방 Lv{lvl}  용량 {MaxCapacity:0}";
                RecalcWeight();
                OnInventoryChanged?.Invoke();
                return 1;
                }
            }

            if (item.category == ItemCategory.Weapon && item.weaponData != null && Core.GameSettings.AutoWeapon)
            {
                var wd = item.weaponData;
                if (wd.isMelee)
                {
                    melee = wd;
                    EquipWeaponSlot(3);
                    amount -= 1;
                    RecalcWeight();
                    OnInventoryChanged?.Invoke();
                    return 1;
                }

                if (primary1 == null)
                {
                    primary1 = wd;
                    EquipWeaponSlot(1);
                    amount -= 1;
                    RecalcWeight();
                    OnInventoryChanged?.Invoke();
                    return 1;
                }

                if (primary2 == null)
                {
                    primary2 = wd;
                    EquipWeaponSlot(2);
                    amount -= 1;
                    RecalcWeight();
                    OnInventoryChanged?.Invoke();
                    return 1;
                }

                // Both slots full — swap into active gun slot
                if (activeWeaponIndex == 2)
                    primary2 = wd;
                else
                    primary1 = wd;
                EquipWeaponSlot(activeWeaponIndex == 2 ? 2 : 1);
                amount -= 1;
                RecalcWeight();
                OnInventoryChanged?.Invoke();
                return 1;
            }

            if (item.category == ItemCategory.Helmet && ShouldAutoArmor(armor != null ? armor.Helmet : null, item))
            {
                if (armor != null && armor.Helmet != null)
                    WorldPickup.Spawn(armor.Helmet, 1, transform.position + transform.forward);
                armor?.EquipHelmet(item);
                amount -= 1;
                RecalcWeight();
                OnInventoryChanged?.Invoke();
                return 1;
            }

            if (item.category == ItemCategory.Vest && ShouldAutoArmor(armor != null ? armor.Vest : null, item))
            {
                if (armor != null && armor.Vest != null)
                    WorldPickup.Spawn(armor.Vest, 1, transform.position + transform.forward);
                armor?.EquipVest(item);
                amount -= 1;
                RecalcWeight();
                OnInventoryChanged?.Invoke();
                return 1;
            }

            if (item.category == ItemCategory.Ammo)
            {
                int rounds = item.ammoAmount * amount;
                int taken = AcceptRounds(item, rounds);
                int full = Mathf.Max(1, item.ammoAmount);
                int stacks = taken / full;
                amount -= stacks;
                return stacks;
            }

            float unit = Mathf.Max(0.01f, ItemCapacity.OfStack(item));
            int room = Mathf.FloorToInt(Mathf.Max(0f, maxWeight - UsedCapacity) / unit);
            int budget = Mathf.Min(amount, room);
            if (budget <= 0)
            {
                StatusMessage = "가방 공간 부족";
                OnInventoryChanged?.Invoke();
                return 0;
            }

            int placed = 0;
            for (int i = 0; i < slots.Count && placed < budget; i++)
            {
                if (!slots[i].IsEmpty && slots[i].item == item && slots[i].count < item.maxStack)
                {
                    int add = Mathf.Min(item.maxStack - slots[i].count, budget - placed);
                    slots[i].count += add;
                    placed += add;
                }
            }

            for (int i = 0; i < slots.Count && placed < budget; i++)
            {
                if (slots[i].IsEmpty)
                {
                    int add = Mathf.Min(item.maxStack, budget - placed);
                    slots[i].item = item;
                    slots[i].count = add;
                    placed += add;
                }
            }

            amount -= placed;
            if (placed < before && amount > 0)
                StatusMessage = "일부만 획득했습니다";
            RecalcWeight();
            OnInventoryChanged?.Invoke();
            return placed;
        }

        public int AcceptRounds(ItemData item, int rounds)
        {
            if (item == null || rounds <= 0) return 0;
            float per = ItemCapacity.PerRound(item.ammoType);
            int fit = Mathf.FloorToInt(Mathf.Max(0f, maxWeight - UsedCapacity) / Mathf.Max(0.01f, per));
            int take = Mathf.Clamp(fit, 0, rounds);
            if (take <= 0)
            {
                StatusMessage = "가방 공간 부족";
                OnInventoryChanged?.Invoke();
                return 0;
            }
            if (!ammoReserve.ContainsKey(item.ammoType))
                ammoReserve[item.ammoType] = 0;
            ammoReserve[item.ammoType] += take;
            if (take < rounds)
                StatusMessage = $"{take}발만 획득, {rounds - take}발 남음";
            RecalcWeight();
            OnInventoryChanged?.Invoke();
            return take;
        }

        public bool ConsumeItem(ItemData item, int amount)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (!slots[i].IsEmpty && slots[i].item == item)
                {
                    if (slots[i].count < amount)
                        return false;
                    slots[i].count -= amount;
                    if (slots[i].count <= 0)
                        slots[i].Clear();
                    RecalcWeight();
                    OnInventoryChanged?.Invoke();
                    return true;
                }
            }
            return false;
        }

        public ItemData FindBestHeal()
        {
            ItemData best = null;
            float bestHeal = -1f;
            foreach (var s in slots)
            {
                if (s.IsEmpty || s.item.category != ItemCategory.Heal)
                    continue;
                if (s.item.healAmount > bestHeal)
                {
                    bestHeal = s.item.healAmount;
                    best = s.item;
                }
            }
            return best;
        }

        private static bool ShouldAutoArmor(ItemData current, ItemData incoming)
        {
            if (!Core.GameSettings.AutoArmor) return false;
            if (current == null) return true;
            if (!Core.GameSettings.AutoUpgrade) return false;
            float now = current.armorLevel * 100f + current.durability;
            float next = incoming.armorLevel * 100f + incoming.durability;
            return next > now;
        }

        public int CountItem(ItemCategory category)
        {
            int n = 0;
            foreach (var s in slots)
                if (!s.IsEmpty && s.item.category == category)
                    n += s.count;
            return n;
        }

        public bool ConsumeCategory(ItemCategory category, int amount)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].IsEmpty || slots[i].item.category != category) continue;
                if (slots[i].count < amount) return false;
                slots[i].count -= amount;
                if (slots[i].count <= 0) slots[i].Clear();
                RecalcWeight();
                OnInventoryChanged?.Invoke();
                return true;
            }
            return false;
        }

        public int GetAmmo(AmmoType type) => ammoReserve.TryGetValue(type, out int v) ? v : 0;

        public bool ConsumeAmmo(AmmoType type, int amount)
        {
            if (GetAmmo(type) < amount) return false;
            ammoReserve[type] -= amount;
            RecalcWeight();
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void EquipWeaponSlot(int index)
        {
            activeWeaponIndex = index;
            WeaponData data = null;
            if (index == 1) data = primary1;
            else if (index == 2) data = primary2;
            else data = Melee;

            if (data == null && index != 3)
            {
                // empty gun slot → fists
                activeWeaponIndex = 3;
                data = Melee;
            }

            if (weapon != null)
                weapon.SetWeaponData(data);
            OnInventoryChanged?.Invoke();
        }

        public void SetStartingWeapon(WeaponData data)
        {
            if (data != null && data.isMelee)
            {
                melee = data;
                EquipWeaponSlot(3);
            }
            else
            {
                primary1 = data;
                EquipWeaponSlot(1);
            }
        }

        public void SetFists(WeaponData fists)
        {
            fistsFallback = fists;
            if (melee == null) melee = fists;
        }

        private void TryThrowGrenade()
        {
            foreach (var s in slots)
            {
                if (!s.IsEmpty && s.item.category == ItemCategory.Throwable)
                {
                    var item = s.item;
                    ConsumeItem(item, 1);
                    ThrowableProjectile.Spawn(transform, item);
                    return;
                }
            }
        }

        private void RecalcWeight()
        {
            float w = 0f;
            foreach (var s in slots)
            {
                if (!s.IsEmpty)
                    w += ItemCapacity.OfStack(s.item) * s.count;
            }
            foreach (var pair in ammoReserve)
                w += ItemCapacity.PerRound(pair.Key) * pair.Value;
            CurrentWeight = w;
        }

        public void SortSlots()
        {
            slots.Sort((a, b) =>
            {
                if (a.IsEmpty && b.IsEmpty) return 0;
                if (a.IsEmpty) return 1;
                if (b.IsEmpty) return -1;
                int c = a.item.category.CompareTo(b.item.category);
                if (c != 0) return c;
                return string.Compare(a.item.itemName, b.item.itemName, System.StringComparison.Ordinal);
            });
            OnInventoryChanged?.Invoke();
        }

        public void DropHalfOfLastStack()
        {
            for (int i = slots.Count - 1; i >= 0; i--)
            {
                if (slots[i].IsEmpty || slots[i].count <= 0) continue;
                int drop = Mathf.Max(1, slots[i].count / 2);
                var data = slots[i].item;
                slots[i].count -= drop;
                if (slots[i].count <= 0) slots[i].Clear();
                RecalcWeight();
                WorldPickup.Spawn(data, drop, transform.position + transform.forward * 1.2f);
                StatusMessage = $"{data.itemName} {drop} 버림";
                OnInventoryChanged?.Invoke();
                return;
            }
        }

        public string HotbarLine()
        {
            string s1 = primary1 != null ? primary1.weaponName : "-";
            string s2 = primary2 != null ? primary2.weaponName : "-";
            string s3 = Melee != null ? Melee.weaponName : "Fists";
            string mark = activeWeaponIndex == 1 ? ">" : " ";
            string mark2 = activeWeaponIndex == 2 ? ">" : " ";
            string mark3 = activeWeaponIndex == 3 ? ">" : " ";
            return $"[{mark}1 {s1}]  [{mark2}2 {s2}]  [{mark3}3 {s3}]";
        }

        public string DebugSummary()
        {
            float ratio = maxWeight <= 0f ? 0f : CurrentWeight / maxWeight;
            string warn = ratio >= 1f ? "  FULL" : ratio >= 0.8f ? "  거의 가득" : "";
            string bag = backpackLevel <= 0 ? "가방 없음" : $"가방 Lv{backpackLevel}";
            return $"{bag}  {CurrentWeight:0}/{maxWeight:0}{warn}\n{StatusMessage}\n{HotbarLine()}";
        }
    }
}
