using UnityEngine;
using BlockyBattleRoyale.Items;
using BlockyBattleRoyale.Player;

namespace BlockyBattleRoyale.World
{
    public class SupplyDropSystem : MonoBehaviour
    {
        [SerializeField] private ItemData[] rareLoot;
        [SerializeField] private float firstDropTime = 90f;
        [SerializeField] private float interval = 120f;
        [SerializeField] private float mapRadius = 55f;

        private float nextDrop;

        public void Configure(ItemData[] loot, float radius)
        {
            rareLoot = loot;
            mapRadius = radius;
            nextDrop = Time.time + firstDropTime;
        }

        private void Update()
        {
            if (rareLoot == null || rareLoot.Length == 0)
                return;

            if (Time.time >= nextDrop)
            {
                nextDrop = Time.time + interval;
                SpawnSupply();
            }
        }

        private void SpawnSupply()
        {
            Vector2 c = Random.insideUnitCircle * mapRadius;
            Vector3 ground = new Vector3(c.x, 0f, c.y);
            if (Physics.Raycast(ground + Vector3.up * 40f, Vector3.down, out RaycastHit hit, 80f))
                ground = hit.point;

            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "SupplyCrate";
            crate.transform.position = ground + Vector3.up * 35f;
            crate.transform.localScale = new Vector3(1.6f, 1.2f, 1.6f);
            crate.GetComponent<Renderer>().material = BlockyCharacterBuilder.CreateLitMaterial(new Color(0.85f, 0.2f, 0.2f));

            var drop = crate.AddComponent<FallingSupply>();
            drop.Configure(ground + Vector3.up * 0.7f, rareLoot[Random.Range(0, rareLoot.Length)]);
        }
    }

    public class FallingSupply : MonoBehaviour
    {
        private Vector3 target;
        private ItemData loot;
        private bool landed;

        public void Configure(Vector3 landPos, ItemData item)
        {
            target = landPos;
            loot = item;
        }

        private void Update()
        {
            if (landed)
                return;

            transform.position = Vector3.MoveTowards(transform.position, target, 18f * Time.deltaTime);
            if (Vector3.Distance(transform.position, target) < 0.05f)
            {
                landed = true;
                transform.position = target;
                if (loot != null)
                    LootSpawner.SpawnPickup(loot, target + Vector3.forward * 1.2f);
            }
        }
    }
}
