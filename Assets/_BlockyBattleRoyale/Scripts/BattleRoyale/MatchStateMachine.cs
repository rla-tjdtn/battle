using UnityEngine;
using UnityEngine.SceneManagement;
using BlockyBattleRoyale.Core;
using BlockyBattleRoyale.UI;
using BlockyBattleRoyale.World;

namespace BlockyBattleRoyale.BattleRoyale
{
    public enum MatchState
    {
        MainMenu,
        Lobby,
        Waiting,
        Countdown,
        Transport,
        Parachuting,
        Playing,
        FinalCircle,
        MatchFinished,
        Results
    }

    /// <summary>
    /// Explicit match lifecycle (V2). Coordinates drop, play, zone end, results.
    /// </summary>
    public class MatchStateMachine : MonoBehaviour
    {
        public static MatchStateMachine Instance { get; private set; }

        [SerializeField] private MatchState state = MatchState.Playing;
        [SerializeField] private float countdownSeconds = 3f;
        [SerializeField] private bool autoStartDrop;
        [SerializeField] private float matchTime;

        private float stateTimer;

        public MatchState State => state;
        public float MatchTime => matchTime;
        public System.Action<MatchState> OnStateChanged;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (state == MatchState.Playing || state == MatchState.Transport || state == MatchState.Parachuting)
            {
                GameManager.Instance?.RefreshAliveCount();
            }

            if (autoStartDrop)
                Enter(MatchState.Countdown);
        }

        private void Update()
        {
            if (state == MatchState.Playing || state == MatchState.Parachuting ||
                state == MatchState.Transport || state == MatchState.FinalCircle)
                matchTime += Time.deltaTime;

            switch (state)
            {
                case MatchState.Countdown:
                    stateTimer -= Time.deltaTime;
                    if (stateTimer <= 0f)
                        StartTransportOrPlay();
                    break;
                case MatchState.Playing:
                case MatchState.FinalCircle:
                    if (ZoneManager.Instance != null && ZoneManager.Instance.PhaseIndex >= 4)
                        Enter(MatchState.FinalCircle);
                    break;
            }

            if (Input.GetKeyDown(KeyCode.Escape) && !Application.isEditor)
                ReturnToMenu();
        }

        public void Enter(MatchState next)
        {
            if (state == next)
                return;

            state = next;
            stateTimer = 0f;

            switch (state)
            {
                case MatchState.Countdown:
                    stateTimer = countdownSeconds;
                    break;
                case MatchState.Transport:
                    BeginTransport();
                    break;
                case MatchState.Playing:
                    MatchManager.Instance?.BeginPlay();
                    break;
                case MatchState.MatchFinished:
                case MatchState.Results:
                    break;
            }

            OnStateChanged?.Invoke(state);
        }

        private void StartTransportOrPlay()
        {
            var drop = FindFirstObjectByType<DropSystem>();
            var player = GameObject.FindGameObjectWithTag("Player");
            if (drop != null && player != null && autoStartDrop)
            {
                Enter(MatchState.Transport);
            }
            else
            {
                Enter(MatchState.Playing);
            }
        }

        private void BeginTransport()
        {
            var drop = FindFirstObjectByType<DropSystem>();
            var player = GameObject.FindGameObjectWithTag("Player");
            if (drop != null && player != null)
            {
                drop.enabled = true;
                drop.Begin(player.transform, Vector3.zero);
                Enter(MatchState.Parachuting);
            }
            else
            {
                Enter(MatchState.Playing);
            }
        }

        public void NotifyLanded()
        {
            Enter(MatchState.Playing);
        }

        public void FinishMatch(bool victory)
        {
            if (state == MatchState.Results || state == MatchState.MatchFinished)
                return;

            Enter(MatchState.MatchFinished);
            Enter(MatchState.Results);
            var hud = FindFirstObjectByType<GameHUD>();
            int kills = GameManager.Instance != null ? GameManager.Instance.KillCount : 0;
            hud?.ShowResult(victory, kills);
            MatchManager.Instance?.EndMatch(victory);
        }

        public void ReturnToMenu()
        {
            Enter(MatchState.MainMenu);
            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
                SceneManager.LoadScene("MainMenu");
            else
                SceneManager.LoadScene(0);
        }
    }
}
