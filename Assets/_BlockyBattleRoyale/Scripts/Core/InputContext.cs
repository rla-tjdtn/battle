namespace BlockyBattleRoyale.Core
{
    public static class InputContext
    {
        public static bool InventoryOpen;
        public static bool MapOpen;
        public static bool MenuOpen;
        public static bool BlockCombat => InventoryOpen || MapOpen || MenuOpen || !GameplayGate.InputEnabled;
    }

    public static class GameplayGate
    {
        public static bool InputEnabled = true;
    }
}
