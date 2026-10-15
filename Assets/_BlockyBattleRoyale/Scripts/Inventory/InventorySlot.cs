using BlockyBattleRoyale.Items;

namespace BlockyBattleRoyale.InventorySystem
{
    [System.Serializable]
    public class InventorySlot
    {
        public ItemData item;
        public int count;

        public bool IsEmpty => item == null || count <= 0;

        public void Clear()
        {
            item = null;
            count = 0;
        }
    }
}
