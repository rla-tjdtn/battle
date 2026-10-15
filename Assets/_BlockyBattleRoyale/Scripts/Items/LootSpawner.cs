using UnityEngine;

namespace BlockyBattleRoyale.Items
{
    public class LootSpawner : MonoBehaviour
    {
        [SerializeField] private ItemData[] lootTable;
        [SerializeField] private int spawnCount = 40;
        [SerializeField] private float mapRadius = 70f;

        public void Configure(ItemData[] table, int count, float radius)
        {
            lootTable = table;
            spawnCount = count;
            mapRadius = radius;
        }

        public void SpawnAll()
        {
            if (lootTable == null || lootTable.Length == 0)
                return;

            for (int i = 0; i < spawnCount; i++)
            {
                Vector2 circle = Random.insideUnitCircle * mapRadius;
                Vector3 pos = new Vector3(circle.x, 0.4f, circle.y);
                if (Physics.Raycast(pos + Vector3.up * 20f, Vector3.down, out RaycastHit hit, 40f))
                    pos.y = hit.point.y + 0.4f;

                var item = lootTable[Random.Range(0, lootTable.Length)];
                SpawnPickup(item, pos);
            }
        }

        public static GameObject SpawnPickup(ItemData item, Vector3 pos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Loot_" + (item != null ? item.itemName : "Item");
            go.transform.position = pos;
            go.transform.localScale = item != null ? item.worldScale : new Vector3(0.5f, 0.3f, 0.5f);
            Object.Destroy(go.GetComponent<BoxCollider>());
            var pickup = go.AddComponent<WorldPickup>();
            pickup.Configure(item, 1);
            return go;
        }
    }
}
