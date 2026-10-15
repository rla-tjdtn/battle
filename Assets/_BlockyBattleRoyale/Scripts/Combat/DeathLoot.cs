using UnityEngine;
using BlockyBattleRoyale.Items;
using BlockyBattleRoyale.InventorySystem;

namespace BlockyBattleRoyale.Combat
{
    public class DeathLoot : MonoBehaviour
    {
        [SerializeField] private ItemData[] fallbackLoot;

        private Health health;
        private bool dropped;

        private void Awake()
        {
            health = GetComponent<Health>();
            if (health != null)
                health.OnDeath += Drop;
        }

        private void OnDestroy()
        {
            if (health != null)
                health.OnDeath -= Drop;
        }

        public void SetFallback(ItemData[] loot)
        {
            fallbackLoot = loot;
        }

        private void Drop()
        {
            if (dropped)
                return;
            dropped = true;

            Vector3 pos = transform.position + Vector3.up * 0.4f;
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "DeathCrate";
            crate.transform.position = pos;
            crate.transform.localScale = new Vector3(1.1f, 0.7f, 1.1f);
            var rend = crate.GetComponent<Renderer>();
            if (rend != null)
                rend.material = BlockyBattleRoyale.Player.BlockyCharacterBuilder.CreateLitMaterial(new Color(0.55f, 0.45f, 0.25f));

            var inv = GetComponent<PlayerInventory>();
            int spawned = 0;
            if (inv != null)
            {
                foreach (var slot in inv.Slots)
                {
                    if (slot.IsEmpty) continue;
                    LootSpawner.SpawnPickup(slot.item, pos + Random.insideUnitSphere * 0.8f + Vector3.up * 0.2f);
                    spawned++;
                    if (spawned >= 4) break;
                }
            }

            if (spawned == 0 && fallbackLoot != null && fallbackLoot.Length > 0)
            {
                var item = fallbackLoot[Random.Range(0, fallbackLoot.Length)];
                LootSpawner.SpawnPickup(item, pos + Vector3.forward * 0.8f);
            }
        }
    }
}
