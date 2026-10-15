using UnityEngine;
using UnityEngine.UI;
using BlockyBattleRoyale.BattleRoyale;
using BlockyBattleRoyale.Combat;
using BlockyBattleRoyale.Core;
using BlockyBattleRoyale.Items;
using BlockyBattleRoyale.InventorySystem;
using BlockyBattleRoyale.Weapons;

namespace BlockyBattleRoyale.UI
{
    public class GameHUD : MonoBehaviour
    {
        [SerializeField] private Text healthText;
        [SerializeField] private Text ammoText;
        [SerializeField] private Text aliveText;
        [SerializeField] private Text killText;
        [SerializeField] private Text killFeedText;
        [SerializeField] private Text resultText;
        [SerializeField] private Text zoneText;
        [SerializeField] private Text promptText;
        [SerializeField] private Text inventoryText;
        [SerializeField] private Image healthBar;
        [SerializeField] private Image armorBar;
        [SerializeField] private Image crosshair;
        [SerializeField] private GameObject resultPanel;

        private Health playerHealth;
        private WeaponController playerWeapon;
        private ArmorSystem armor;
        private PlayerInventory inventory;
        private float killFeedTimer;

        private void Start()
        {
            BindPlayer();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAliveCountChanged += UpdateAlive;
                GameManager.Instance.OnKillCountChanged += UpdateKills;
                UpdateAlive(GameManager.Instance.AliveCount);
                UpdateKills(GameManager.Instance.KillCount);
            }

            if (resultPanel != null)
                resultPanel.SetActive(false);
            if (crosshair != null)
                crosshair.rectTransform.sizeDelta = new Vector2(6f, 6f);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnAliveCountChanged -= UpdateAlive;
                GameManager.Instance.OnKillCountChanged -= UpdateKills;
            }

            UnbindPlayer();
        }

        private void Update()
        {
            if (playerHealth == null || playerWeapon == null)
                BindPlayer();

            if (killFeedTimer > 0f)
            {
                killFeedTimer -= Time.deltaTime;
                if (killFeedTimer <= 0f && killFeedText != null)
                    killFeedText.text = string.Empty;
            }

            if (crosshair != null)
            {
                bool aiming = Input.GetMouseButton(1);
                crosshair.rectTransform.localScale = Vector3.one * (aiming ? 0.75f : 1f);
                crosshair.color = aiming ? new Color(1f, 0.85f, 0.3f, 0.95f) : new Color(1f, 1f, 1f, 0.85f);
            }

            UpdateZone();
            UpdatePrompt();
            UpdateInventoryPanel();
            UpdateArmorBar();
        }

        private void BindPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            var health = player.GetComponent<Health>();
            var weapon = player.GetComponent<WeaponController>();
            armor = player.GetComponent<ArmorSystem>();
            var inv = player.GetComponent<PlayerInventory>();
            if (inv != inventory)
            {
                if (inventory != null)
                    inventory.OnInventoryChanged -= HandleAmmo;
                inventory = inv;
                if (inventory != null)
                    inventory.OnInventoryChanged += HandleAmmo;
            }

            if (health != null && health != playerHealth)
            {
                if (playerHealth != null)
                    playerHealth.OnHealthChanged -= HandleHealth;
                playerHealth = health;
                playerHealth.OnHealthChanged += HandleHealth;
                HandleHealth(playerHealth.CurrentHealth, playerHealth.MaxHealth);
            }

            if (weapon != null && weapon != playerWeapon)
            {
                if (playerWeapon != null)
                {
                    playerWeapon.OnAmmoChanged -= HandleAmmo;
                    playerWeapon.OnReloadStarted -= HandleReloadStart;
                    playerWeapon.OnReloadFinished -= HandleAmmo;
                }

                playerWeapon = weapon;
                playerWeapon.OnAmmoChanged += HandleAmmo;
                playerWeapon.OnReloadStarted += HandleReloadStart;
                playerWeapon.OnReloadFinished += HandleAmmo;
                HandleAmmo();
            }
        }

        private void UnbindPlayer()
        {
            if (playerHealth != null)
                playerHealth.OnHealthChanged -= HandleHealth;
            if (playerWeapon != null)
            {
                playerWeapon.OnAmmoChanged -= HandleAmmo;
                playerWeapon.OnReloadStarted -= HandleReloadStart;
                playerWeapon.OnReloadFinished -= HandleAmmo;
            }
        }

        private void HandleHealth(float current, float max)
        {
            if (healthText != null)
                healthText.text = $"HP  {Mathf.CeilToInt(current)}/{Mathf.CeilToInt(max)}";
            if (healthBar != null)
                healthBar.fillAmount = max > 0f ? current / max : 0f;
        }

        private void HandleAmmo()
        {
            if (ammoText == null || playerWeapon == null)
                return;

            var data = playerWeapon.Data;
            string name = data != null ? data.weaponName : "Fists";
            if (data != null && data.isMelee)
            {
                ammoText.text = $"{name}  [LMB Punch]   " + (inventory != null ? inventory.HotbarLine() : "");
                return;
            }

            if (playerWeapon.IsReloading)
                ammoText.text = $"{name}  RELOADING";
            else
                ammoText.text = $"{name}  {playerWeapon.CurrentAmmo}/{playerWeapon.MagazineSize}   " +
                                (inventory != null ? inventory.HotbarLine() : "");
        }

        private void HandleReloadStart() => HandleAmmo();

        private void UpdateAlive(int count)
        {
            if (aliveText != null)
                aliveText.text = $"ALIVE  {count}";
        }

        private void UpdateKills(int count)
        {
            if (killText != null)
                killText.text = $"KILLS  {count}";
        }

        private void UpdateZone()
        {
            if (zoneText == null || ZoneManager.Instance == null)
                return;

            var z = ZoneManager.Instance;
            string state = z.State == ZoneState.Waiting ? "NEXT" : z.State == ZoneState.Shrinking ? "SHRINKING" : "FINAL";
            zoneText.text = $"ZONE {z.PhaseIndex + 1}/5  {state}  {Mathf.CeilToInt(z.StateTimer)}s  R:{z.CurrentRadius:0}";
        }

        private void UpdatePrompt()
        {
            if (promptText == null)
                return;

            if (WorldPickup.Nearest != null && WorldPickup.Nearest.PlayerNear && WorldPickup.Nearest.Data != null)
                promptText.text = $"[E]  {WorldPickup.Nearest.Data.itemName}";
            else if (inventory != null && !inventory.IsOpen)
                promptText.text = "Shift달리기 Space회피 Ctrl앉기 Z엎드 | C무기넣기 V근접 F상호작용 | 1/2총 Tab가방";
            else
                promptText.text = string.Empty;
        }

        private void UpdateInventoryPanel()
        {
            if (inventoryText == null || inventory == null)
                return;

            if (!inventory.IsOpen)
            {
                inventoryText.text = string.Empty;
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== BACKPACK (Tab) ===");
            sb.AppendLine(inventory.DebugSummary());
            sb.AppendLine("1/2 Guns  3 Melee/Fists  Shift Run  C Crouch  Z Prone");
            sb.AppendLine("--- Items ---");
            foreach (var s in inventory.Slots)
            {
                if (!s.IsEmpty)
                    sb.AppendLine($"- {s.item.itemName} x{s.count}");
            }
            inventoryText.text = sb.ToString();
        }

        private void UpdateArmorBar()
        {
            if (armorBar == null || armor == null)
                return;

            float v = 0f;
            if (armor.Vest != null)
                v += armor.VestDurability / Mathf.Max(1f, armor.Vest.durability) * 0.5f;
            if (armor.Helmet != null)
                v += armor.HelmetDurability / Mathf.Max(1f, armor.Helmet.durability) * 0.5f;
            armorBar.fillAmount = v;
        }

        public void ShowKillFeed(string killer, string victim)
        {
            if (killFeedText == null)
                return;
            killFeedText.text = $"{killer}  eliminated  {victim}";
            killFeedTimer = 3.5f;
        }

        public void ShowResult(bool victory, int kills)
        {
            if (resultPanel != null)
                resultPanel.SetActive(true);

            if (resultText != null)
            {
                float time = MatchManager.Instance != null ? MatchManager.Instance.MatchTime : 0f;
                resultText.text = victory
                    ? $"VICTORY\nKills: {kills}\nTime: {time:0}s\nEsc / Menu to leave"
                    : $"ELIMINATED\nKills: {kills}\nTime: {time:0}s\nEsc / Menu to leave";
            }
        }

        public void BindUIReferences(
            Text health, Text ammo, Text alive, Text kills, Text feed, Text result,
            Image bar, Image cross, GameObject panel,
            Text zone = null, Text prompt = null, Text inventory = null, Image armor = null)
        {
            healthText = health;
            ammoText = ammo;
            aliveText = alive;
            killText = kills;
            killFeedText = feed;
            resultText = result;
            healthBar = bar;
            crosshair = cross;
            resultPanel = panel;
            zoneText = zone;
            promptText = prompt;
            inventoryText = inventory;
            armorBar = armor;
        }
    }
}
