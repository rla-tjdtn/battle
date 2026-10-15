using UnityEngine;
using UnityEngine.SceneManagement;

namespace BlockyBattleRoyale.Networking
{
    /// <summary>
    /// Host/Client bootstrap wrapper. Uses Netcode when available; otherwise
    /// falls back to offline solo practice so the project always runs.
    /// </summary>
    public class NetworkBootstrap : MonoBehaviour
    {
        public static NetworkBootstrap Instance { get; private set; }

        [SerializeField] private string gameSceneName = "GameWorld";
        [SerializeField] private ushort port = 7777;
        [SerializeField] private string joinAddress = "127.0.0.1";

        public bool IsOnline { get; private set; }
        public bool IsHost { get; private set; }
        public string Status { get; private set; } = "Offline";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartOffline()
        {
            IsOnline = false;
            IsHost = true;
            Status = "Offline Practice";
            LoadGame();
        }

        public void StartHost()
        {
            if (TryNetcodeHost())
            {
                IsOnline = true;
                IsHost = true;
                Status = $"Hosting :{port}";
                LoadGame();
                return;
            }

            Debug.LogWarning("[Network] Netcode not ready — starting offline host simulation.");
            StartOffline();
        }

        public void StartClient(string address = null)
        {
            if (!string.IsNullOrEmpty(address))
                joinAddress = address;

            if (TryNetcodeClient(joinAddress))
            {
                IsOnline = true;
                IsHost = false;
                Status = $"Client -> {joinAddress}:{port}";
                LoadGame();
                return;
            }

            Debug.LogWarning("[Network] Netcode client failed — offline fallback.");
            StartOffline();
        }

        private void LoadGame()
        {
            Core.GameplayGate.InputEnabled = false;
            Core.PlayerProfile.BeginMatch();
            string scene = gameSceneName;
            if (!Application.CanStreamedLevelBeLoaded(scene) && SceneManager.sceneCountInBuildSettings > 1)
                scene = SceneUtility.GetScenePathByBuildIndex(1);
            if (string.IsNullOrEmpty(scene) || !Application.CanStreamedLevelBeLoaded(System.IO.Path.GetFileNameWithoutExtension(scene)))
            {
                if (SceneManager.sceneCountInBuildSettings > 1)
                {
                    StartCoroutine(LoadAsync(1));
                    return;
                }
            }
            else
            {
                StartCoroutine(LoadAsync(scene));
                return;
            }

            Status = "ERROR: GameWorld not in build";
            Core.GameplayGate.InputEnabled = true;
            Debug.LogError("[Network] GameWorld scene missing from build settings.");
        }

        private System.Collections.IEnumerator LoadAsync(string scene)
        {
            var op = SceneManager.LoadSceneAsync(scene);
            while (op != null && !op.isDone)
                yield return null;
            Core.GameplayGate.InputEnabled = true;
        }

        private System.Collections.IEnumerator LoadAsync(int index)
        {
            var op = SceneManager.LoadSceneAsync(index);
            while (op != null && !op.isDone)
                yield return null;
            Core.GameplayGate.InputEnabled = true;
        }

        private bool TryNetcodeHost()
        {
            var nmType = System.Type.GetType("Unity.Netcode.NetworkManager, Unity.Netcode.Runtime");
            if (nmType == null)
                return false;

            var nm = FindFirstObjectByType(nmType) as MonoBehaviour;
            if (nm == null)
            {
                var go = new GameObject("NetworkManager");
                DontDestroyOnLoad(go);
                nm = go.AddComponent(nmType) as MonoBehaviour;
                // Transport
                var transportType = System.Type.GetType("Unity.Netcode.Transports.UTP.UnityTransport, Unity.Netcode.Runtime");
                if (transportType != null)
                {
                    var transport = go.AddComponent(transportType);
                    var transportProp = nmType.GetProperty("NetworkConfig");
                    // Best-effort; if reflection wiring fails we still report false
                }
            }

            var startHost = nmType.GetMethod("StartHost");
            if (startHost == null)
                return false;

            try
            {
                object result = startHost.Invoke(nm, null);
                return result is bool b ? b : true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Network] StartHost failed: " + e.Message);
                return false;
            }
        }

        private bool TryNetcodeClient(string address)
        {
            var nmType = System.Type.GetType("Unity.Netcode.NetworkManager, Unity.Netcode.Runtime");
            if (nmType == null)
                return false;

            var nm = FindFirstObjectByType(nmType) as MonoBehaviour;
            if (nm == null)
                return false;

            try
            {
                var transportType = System.Type.GetType("Unity.Netcode.Transports.UTP.UnityTransport, Unity.Netcode.Runtime");
                if (transportType != null)
                {
                    var transport = nm.GetComponent(transportType);
                    if (transport != null)
                    {
                        var setAddr = transportType.GetMethod("SetConnectionData", new[] { typeof(string), typeof(ushort), typeof(string) });
                        setAddr?.Invoke(transport, new object[] { address, port, "0.0.0.0" });
                    }
                }

                var startClient = nmType.GetMethod("StartClient");
                object result = startClient?.Invoke(nm, null);
                return result is bool b ? b : true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Network] StartClient failed: " + e.Message);
                return false;
            }
        }
    }
}
