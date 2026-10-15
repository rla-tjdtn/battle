using UnityEngine;
using BlockyBattleRoyale.AI;
using BlockyBattleRoyale.Player;
using BlockyBattleRoyale.Weapons;

namespace BlockyBattleRoyale.Networking
{
    /// <summary>
    /// Ensures solo matches always have AI fillers when under target count.
    /// Used until NGO multiplayer is re-enabled on a compatible Unity package set.
    /// </summary>
    public class OfflineMatchFiller : MonoBehaviour
    {
        [SerializeField] private int targetCombatants = 8;
        [SerializeField] private WeaponData botWeapon;

        public void Configure(WeaponData weapon, int target)
        {
            botWeapon = weapon;
            targetCombatants = target;
        }

        private void Start()
        {
            int alive = FindObjectsByType<Combat.Health>(FindObjectsSortMode.None).Length;
            int need = Mathf.Max(0, targetCombatants - alive);
            Color[] colors =
            {
                new Color(0.8f, 0.3f, 0.3f), new Color(0.3f, 0.7f, 0.4f), new Color(0.9f, 0.7f, 0.2f),
                new Color(0.4f, 0.4f, 0.5f), new Color(0.7f, 0.4f, 0.2f), new Color(0.5f, 0.3f, 0.6f),
                new Color(0.2f, 0.6f, 0.6f)
            };

            for (int i = 0; i < need; i++)
            {
                Vector2 c = Random.insideUnitCircle * 35f;
                var bot = BlockyCharacterBuilder.Build($"FillBot_{i}", new Vector3(c.x, 0.05f, c.y), colors[i % colors.Length], false, botWeapon);
                bot.AddComponent<BotBrain>();
                bot.GetComponent<InventorySystem.PlayerInventory>()?.SetStartingWeapon(botWeapon);
            }
        }
    }
}
