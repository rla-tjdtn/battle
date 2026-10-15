using System;
using UnityEngine;

namespace BlockyBattleRoyale.Combat
{
    public static class GunshotBus
    {
        public static event Action<Vector3, float, bool> OnShot;

        public static void Report(Vector3 position, float hearRadius, bool suppressed)
        {
            OnShot?.Invoke(position, Mathf.Max(4f, hearRadius), suppressed);
        }
    }
}
