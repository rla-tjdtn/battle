using UnityEngine;
using BlockyBattleRoyale.Combat;
using BlockyBattleRoyale.InventorySystem;

namespace BlockyBattleRoyale.Player
{
    /// <summary>Shows backpack, vest, and helmet meshes from equipped data. Hitboxes stay on the body.</summary>
    public class EquipmentVisual : MonoBehaviour
    {
        private PlayerInventory inventory;
        private ArmorSystem armor;
        private Transform backpack;
        private Transform vest;
        private Transform helmet;

        private void Start()
        {
            inventory = GetComponent<PlayerInventory>();
            armor = GetComponent<ArmorSystem>();
            var visual = transform.Find("Visual");
            if (visual == null) return;
            backpack = visual.Find("Pack");
            if (inventory != null) inventory.OnInventoryChanged += Refresh;
            if (armor != null) armor.OnArmorChanged += Refresh;
            ApplyShirt();
            Refresh();
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.OnInventoryChanged -= Refresh;
            if (armor != null) armor.OnArmorChanged -= Refresh;
        }

        private void ApplyShirt()
        {
            if (!CompareTag("Player")) return;
            Color[] colors =
            {
                new Color(0.22f, 0.34f, 0.55f), Color.white, new Color(0.45f, 0.45f, 0.46f),
                new Color(0.7f, 0.22f, 0.2f), new Color(0.2f, 0.4f, 0.75f),
                new Color(0.25f, 0.5f, 0.3f), new Color(0.72f, 0.62f, 0.45f)
            };
            Color c = colors[Mathf.Abs(Core.GameSettings.Cosmetic) % colors.Length];
            var torso = transform.Find("Visual/Torso");
            var rend = torso != null ? torso.GetComponent<Renderer>() : null;
            if (rend == null) return;
            if (rend.material.HasProperty("_BaseColor")) rend.material.SetColor("_BaseColor", c);
            rend.material.color = c;
        }

        private void Refresh()
        {
            int level = inventory != null ? inventory.BackpackLevel : 0;
            if (backpack != null)
            {
                backpack.gameObject.SetActive(level > 0);
                float s = level == 1 ? 0.75f : level == 2 ? 1.05f : 1.35f;
                backpack.localScale = new Vector3(0.42f, 0.36f, 0.16f) * (level == 0 ? 1f : s);
            }

            var visual = transform.Find("Visual");
            if (visual == null || armor == null) return;

            bool hasVest = armor.Vest != null;
            if (vest == null && hasVest)
                vest = Create("VestMesh", visual, new Vector3(0f, 0.98f, 0.12f), new Vector3(0.78f, 0.42f, 0.18f), new Color(0.25f, 0.32f, 0.28f));
            if (vest != null)
            {
                vest.gameObject.SetActive(hasVest);
                if (hasVest)
                {
                    float t = armor.Vest.armorLevel == 1 ? 0.7f : armor.Vest.armorLevel == 2 ? 1f : 1.25f;
                    vest.localScale = new Vector3(0.78f, 0.42f, 0.16f) * t;
                }
            }

            bool hasHelm = armor.Helmet != null;
            if (helmet == null && hasHelm)
                helmet = Create("HelmetMesh", visual, new Vector3(0f, 1.82f, 0f), new Vector3(0.78f, 0.28f, 0.72f), new Color(0.35f, 0.38f, 0.32f));
            if (helmet != null)
            {
                helmet.gameObject.SetActive(hasHelm);
                if (hasHelm)
                {
                    float t = 0.85f + armor.Helmet.armorLevel * 0.12f;
                    helmet.localScale = new Vector3(0.78f, 0.28f, 0.72f) * t;
                }
            }
        }

        private static Transform Create(string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = BlockyCharacterBuilder.CreateLitMaterial(color);
            return go.transform;
        }
    }
}
