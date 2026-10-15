using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using BlockyBattleRoyale.Networking;

namespace BlockyBattleRoyale.UI
{
    public class LobbyController : MonoBehaviour
    {
        [SerializeField] private Text playerListText;
        [SerializeField] private Text statusText;
        [SerializeField] private bool ready;

        private void Start()
        {
            Refresh();
        }

        public void ToggleReady()
        {
            ready = !ready;
            Refresh();
        }

        public void StartMatch()
        {
            if (Application.CanStreamedLevelBeLoaded("GameWorld"))
                SceneManager.LoadScene("GameWorld");
        }

        public void BackToMenu()
        {
            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
                SceneManager.LoadScene("MainMenu");
        }

        private void Refresh()
        {
            string name = "Player1";
            string mode = NetworkBootstrap.Instance != null ? NetworkBootstrap.Instance.Status : "Offline";
            if (playerListText != null)
                playerListText.text = $"{name}  {(ready ? "READY" : "NOT READY")}\nAI x7 will fill";
            if (statusText != null)
                statusText.text = mode;
        }

        public void Bind(Text list, Text status)
        {
            playerListText = list;
            statusText = status;
        }
    }
}
