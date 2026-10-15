using UnityEngine;
using BlockyBattleRoyale.InventorySystem;

namespace BlockyBattleRoyale.Items
{
    public class WorldPickup : MonoBehaviour
    {
        [SerializeField] private ItemData itemData;
        [SerializeField] private int amount = 1;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float bobHeight = 0.15f;

        private Vector3 basePos;
        private bool playerNear;
        private int looseRounds = -1;
        private static WorldPickup nearest;

        public ItemData Data => itemData;
        public bool PlayerNear => playerNear;
        public static WorldPickup Nearest => nearest;

        public void Configure(ItemData data, int count = 1)
        {
            itemData = data;
            amount = count;
            ApplyVisual();
        }

        private void Start()
        {
            basePos = transform.position;
            ApplyVisual();

            var col = GetComponent<Collider>();
            if (col == null)
            {
                var sphere = gameObject.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.radius = 1.4f;
            }
            else
            {
                col.isTrigger = true;
            }
        }

        private void Update()
        {
            transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
            transform.Rotate(Vector3.up, 45f * Time.deltaTime, Space.World);

            if (playerNear && nearest == this && Input.GetKeyDown(KeyCode.F))
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null && BlockyBattleRoyale.World.DoorInteractable.BlocksPickup(player.transform.position))
                    return;
                TryPickup(player);
            }
        }

        private void ApplyVisual()
        {
            var rend = GetComponentInChildren<Renderer>();
            if (rend != null && itemData != null)
            {
                var mat = new Material(rend.sharedMaterial != null ? rend.sharedMaterial : rend.material);
                Color c = itemData.tint;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                if (mat.HasProperty("_Color")) mat.color = c;
                rend.material = mat;
            }

            if (itemData != null)
                transform.localScale = itemData.worldScale;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;
            playerNear = true;
            nearest = this;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;
            playerNear = false;
            if (nearest == this)
                nearest = null;
        }

        public bool TryPickup()
        {
            return TryPickup(GameObject.FindGameObjectWithTag("Player"));
        }

        public bool TryPickup(GameObject collector)
        {
            if (collector == null || itemData == null)
                return false;

            var inv = collector.GetComponent<PlayerInventory>();
            if (inv == null)
                return false;

            if (itemData.category == ItemCategory.Ammo)
            {
                if (looseRounds < 0)
                    looseRounds = Mathf.Max(1, itemData.ammoAmount) * amount;
                int took = inv.AcceptRounds(itemData, looseRounds);
                looseRounds -= took;
                if (looseRounds <= 0)
                {
                    if (nearest == this) nearest = null;
                    Destroy(gameObject);
                }
                return took > 0;
            }

            int before = amount;
            inv.AcceptStacks(itemData, ref amount);
            if (amount <= 0)
            {
                if (nearest == this) nearest = null;
                Destroy(gameObject);
                return true;
            }
            return amount < before;
        }

        public static WorldPickup Spawn(ItemData data, int count, Vector3 position)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Drop_" + (data != null ? data.itemName : "Item");
            go.transform.position = position + Vector3.up * 0.4f;
            go.transform.localScale = data != null ? data.worldScale : Vector3.one * 0.4f;
            var col = go.GetComponent<Collider>();
            col.isTrigger = true;
            var pickup = go.AddComponent<WorldPickup>();
            pickup.Configure(data, Mathf.Max(1, count));
            return pickup;
        }
    }
}
