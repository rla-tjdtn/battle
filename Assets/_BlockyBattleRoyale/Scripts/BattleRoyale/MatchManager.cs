using UnityEngine;
using UnityEngine.SceneManagement;
using BlockyBattleRoyale.Combat;
using BlockyBattleRoyale.Core;
using BlockyBattleRoyale.UI;

namespace BlockyBattleRoyale.BattleRoyale
{
    public enum MatchPhase
    {
        Lobby,
        Dropping,
        Playing,
        Ended
    }

    public class MatchManager : MonoBehaviour
    {
        public static MatchManager Instance { get; private set; }

        [SerializeField] private MatchPhase phase = MatchPhase.Playing;
        [SerializeField] private float matchTime;
        [SerializeField] private bool soloPractice = true;

        public MatchPhase Phase => phase;
        public float MatchTime => matchTime;
        public bool SoloPractice => soloPractice;

        public System.Action<MatchPhase> OnPhaseChanged;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (phase == MatchPhase.Playing || phase == MatchPhase.Dropping)
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.RefreshAliveCount();
            }
        }

        private void Update()
        {
            if (phase == MatchPhase.Playing || phase == MatchPhase.Dropping)
                matchTime += Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Application.isEditor)
                {
                    // leave play mode manually
                }
                else
                {
                    ReturnToMenu();
                }
            }
        }

        public void BeginPlay()
        {
            SetPhase(MatchPhase.Playing);
        }

        public void BeginDrop()
        {
            SetPhase(MatchPhase.Dropping);
        }

        public void EndMatch(bool victory)
        {
            SetPhase(MatchPhase.Ended);
            // HUD is owned by MatchStateMachine / GameManager callers.
        }

        private void SetPhase(MatchPhase p)
        {
            phase = p;
            OnPhaseChanged?.Invoke(phase);
        }

        public void ReturnToMenu()
        {
            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
                SceneManager.LoadScene("MainMenu");
            else
                SceneManager.LoadScene(0);
        }
    }
}
