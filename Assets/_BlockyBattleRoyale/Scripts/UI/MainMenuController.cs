using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using BlockyBattleRoyale.Networking;

namespace BlockyBattleRoyale.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private InputField addressField;
        [SerializeField] private Text statusText;
        [SerializeField] private Button soloButton;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button quitButton;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (NetworkBootstrap.Instance == null)
            {
                var go = new GameObject("NetworkBootstrap");
                go.AddComponent<NetworkBootstrap>();
            }

            WireButtons();
            if (GetComponent<LobbyChrome>() == null)
                gameObject.AddComponent<LobbyChrome>();
            SetStatus(NetworkBootstrap.Instance != null ? NetworkBootstrap.Instance.Status : "Ready");
        }

        private void WireButtons()
        {
            if (soloButton == null) soloButton = FindButton("SoloBtn");
            if (hostButton == null) hostButton = FindButton("HostBtn");
            if (joinButton == null) joinButton = FindButton("JoinBtn");
            if (quitButton == null) quitButton = FindButton("QuitBtn");
            if (addressField == null)
            {
                var go = GameObject.Find("Address");
                if (go != null) addressField = go.GetComponent<InputField>();
            }
            if (statusText == null)
            {
                var go = GameObject.Find("Status");
                if (go != null) statusText = go.GetComponent<Text>();
            }

            if (soloButton != null)
            {
                soloButton.onClick.RemoveAllListeners();
                soloButton.onClick.AddListener(OnSolo);
            }
            if (hostButton != null)
            {
                hostButton.onClick.RemoveAllListeners();
                hostButton.onClick.AddListener(OnHost);
            }
            if (joinButton != null)
            {
                joinButton.onClick.RemoveAllListeners();
                joinButton.onClick.AddListener(OnJoin);
            }
            if (quitButton != null)
            {
                quitButton.onClick.RemoveAllListeners();
                quitButton.onClick.AddListener(OnQuit);
            }
        }

        private static Button FindButton(string name)
        {
            var go = GameObject.Find(name);
            return go != null ? go.GetComponent<Button>() : null;
        }

        public void OnSolo()
        {
            var lobby = GetComponent<LobbyChrome>();
            if (lobby != null)
            {
                SetStatus("Searching...");
                lobby.BeginSearch();
                return;
            }
            SetStatus("Starting...");
            if (NetworkBootstrap.Instance != null)
                NetworkBootstrap.Instance.StartOffline();
            else
                SceneManager.LoadScene("GameWorld");
        }

        public void OnHost()
        {
            SetStatus("Hosting...");
            NetworkBootstrap.Instance?.StartHost();
        }

        public void OnJoin()
        {
            string addr = addressField != null ? addressField.text : "127.0.0.1";
            if (string.IsNullOrWhiteSpace(addr))
                addr = "127.0.0.1";
            SetStatus("Joining " + addr + "...");
            NetworkBootstrap.Instance?.StartClient(addr);
        }

        public void OnQuit()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        public void Bind(InputField address, Text status)
        {
            addressField = address;
            statusText = status;
        }

        public void BindButtons(Button solo, Button host, Button join, Button quit)
        {
            soloButton = solo;
            hostButton = host;
            joinButton = join;
            quitButton = quit;
        }

        private void Update()
        {
            if (statusText != null && NetworkBootstrap.Instance != null)
                statusText.text = NetworkBootstrap.Instance.Status;
        }

        private void SetStatus(string msg)
        {
            if (statusText != null)
                statusText.text = msg;
        }
    }
}
