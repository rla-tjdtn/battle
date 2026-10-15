using UnityEngine;

namespace BlockyBattleRoyale.BattleRoyale
{
    [CreateAssetMenu(fileName = "ZoneSettings", menuName = "Blocky BR/Zone Settings")]
    public class ZoneSettings : ScriptableObject
    {
        public float startRadius = 90f;
        public ZonePhaseData[] phases =
        {
            new ZonePhaseData { waitTime = 90f, shrinkTime = 60f, endRadius = 70f, damagePerSecond = 1.5f },
            new ZonePhaseData { waitTime = 60f, shrinkTime = 50f, endRadius = 45f, damagePerSecond = 3f },
            new ZonePhaseData { waitTime = 45f, shrinkTime = 40f, endRadius = 25f, damagePerSecond = 5f },
            new ZonePhaseData { waitTime = 30f, shrinkTime = 35f, endRadius = 12f, damagePerSecond = 8f },
            new ZonePhaseData { waitTime = 20f, shrinkTime = 30f, endRadius = 4f, damagePerSecond = 12f }
        };
    }
}
