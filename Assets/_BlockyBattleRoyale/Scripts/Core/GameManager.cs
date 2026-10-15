using UnityEngine;
using BlockyBattleRoyale.Combat;
using BlockyBattleRoyale.UI;

namespace BlockyBattleRoyale.Core
{
    public enum GameState
    {
        Playing,
        Victory,
        Defeat
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Match")]
        [SerializeField] private int killCount;
        [SerializeField] private int aliveCount;
        [SerializeField] private GameState state = GameState.Playing;

        public int KillCount => killCount;
        public int AliveCount => aliveCount;
        public GameState State => state;

        public System.Action<int> OnKillCountChanged;
        public System.Action<int> OnAliveCountChanged;
        public System.Action<GameState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            RefreshAliveCount();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void RegisterKill(GameObject killer, GameObject victim)
        {
            if (state != GameState.Playing)
                return;

            if (killer != null && killer.CompareTag(GameTags.Player))
            {
                killCount++;
                OnKillCountChanged?.Invoke(killCount);

                var hud = FindFirstObjectByType<GameHUD>();
                hud?.ShowKillFeed(killer.name, victim != null ? victim.name : "Enemy");
            }

            RefreshAliveCount();

            if (aliveCount <= 1)
            {
                var player = GameObject.FindGameObjectWithTag(GameTags.Player);
                bool playerAlive = player != null && player.activeInHierarchy;
                SetState(playerAlive ? GameState.Victory : GameState.Defeat);
            }
        }

        public void NotifyDeath(GameObject dead)
        {
            RefreshAliveCount();

            if (dead != null && dead.CompareTag(GameTags.Player))
            {
                SetState(GameState.Defeat);
            }
            else if (aliveCount <= 1)
            {
                SetState(GameState.Victory);
            }
        }

        public void RefreshAliveCount()
        {
            int count = 0;
            var healths = FindObjectsByType<Health>(FindObjectsSortMode.None);
            foreach (var h in healths)
            {
                if (h != null && h.IsAlive)
                    count++;
            }

            aliveCount = count;
            OnAliveCountChanged?.Invoke(aliveCount);
        }

        private void SetState(GameState newState)
        {
            if (state == newState)
                return;

            state = newState;
            OnStateChanged?.Invoke(state);

            if (state == GameState.Victory || state == GameState.Defeat)
            {
                bool victory = state == GameState.Victory;
                PlayerProfile.RecordOnce(victory, killCount, Time.timeSinceLevelLoad, victory ? 1 : Mathf.Max(1, aliveCount));
                if (BattleRoyale.MatchStateMachine.Instance != null)
                    BattleRoyale.MatchStateMachine.Instance.FinishMatch(victory);
                else
                    FindFirstObjectByType<GameHUD>()?.ShowResult(victory, killCount);
            }
        }
    }
}
