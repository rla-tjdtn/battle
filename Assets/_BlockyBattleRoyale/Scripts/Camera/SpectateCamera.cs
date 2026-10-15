using UnityEngine;
using BlockyBattleRoyale.Combat;

namespace BlockyBattleRoyale.CameraSystem
{
    public class SpectateCamera : MonoBehaviour
    {
        [SerializeField] private QuarterViewCamera quarterView;
        private int index;
        private Health[] targets;
        private bool spectating;

        private void Update()
        {
            if (!spectating)
                return;

            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.RightArrow))
                Cycle(1);
            if (Input.GetKeyDown(KeyCode.LeftArrow))
                Cycle(-1);
        }

        public void BeginSpectate()
        {
            spectating = true;
            RefreshTargets();
            Cycle(0);
        }

        private void RefreshTargets()
        {
            targets = FindObjectsByType<Health>(FindObjectsSortMode.None);
        }

        private void Cycle(int delta)
        {
            RefreshTargets();
            if (targets == null || targets.Length == 0)
                return;

            for (int attempt = 0; attempt < targets.Length; attempt++)
            {
                index = (index + delta + targets.Length) % targets.Length;
                if (delta == 0 && attempt == 0)
                {
                    // first alive
                }

                var t = targets[index];
                if (t != null && t.IsAlive && !t.CompareTag("Player"))
                {
                    if (quarterView == null)
                        quarterView = FindFirstObjectByType<QuarterViewCamera>();
                    quarterView?.SetTarget(t.transform);
                    return;
                }

                if (delta == 0)
                    delta = 1;
            }
        }
    }
}
