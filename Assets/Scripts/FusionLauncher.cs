using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Com.MyCompany.MyGame
{
    public class FusionLauncher : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField nameInputField;
        [SerializeField] private TMP_Dropdown colorDropdown;
        [SerializeField] private Button startButton;
        [SerializeField] private GameObject controlPanel;
        [SerializeField] private GameObject progressLabel;

        [Header("Game Settings")]
        [SerializeField] private NetworkPrefabRef playerPrefab;

        private NetworkRunner _runner;

        private void Awake()
        {
            // Prevents the Runner from being destroyed when the game scene loads
            DontDestroyOnLoad(this.gameObject);
        }

        private void Start()
        {
            startButton.onClick.AddListener(StartGame);
            controlPanel.SetActive(true);
            progressLabel.SetActive(false);
        }

        async void StartGame()
        {
            
            if (_runner != null)
            {
                await _runner.Shutdown();
                Destroy(_runner);
            }

            controlPanel.SetActive(false);
            progressLabel.SetActive(true);

            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;

            
            var currentScene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

            var result = await _runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = "TestRoom",
                Scene = currentScene, 
                SceneManager = gameObject.GetComponent<NetworkSceneManagerDefault>() ?? gameObject.AddComponent<NetworkSceneManagerDefault>()
            });

            if (!result.Ok)
            {
                
                Debug.LogError($"Fusion Start Failed: {result.ShutdownReason}");

                controlPanel.SetActive(true);
                progressLabel.SetActive(false);

                if (_runner != null) Destroy(_runner);
            }
        }
        

        #region INetworkRunnerCallbacks

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            // Only the Server/Host should spawn the player
            if (runner.IsServer)
            {
                Vector3 spawnPosition = new Vector3(UnityEngine.Random.Range(-5, 5), 1, UnityEngine.Random.Range(-5, 5));
                runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
                Debug.Log($"Spawned player for: {player}");
            }
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var data = new NetworkInputData();
            Vector2 moveDir = Vector2.zero;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed) moveDir.y += 1;
                if (Keyboard.current.sKey.isPressed) moveDir.y -= 1;
                if (Keyboard.current.aKey.isPressed) moveDir.x -= 1;
                if (Keyboard.current.dKey.isPressed) moveDir.x += 1;
            }

            data.direction = moveDir;
            input.Set(data);
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.LogWarning($"Runner Shutdown: {shutdownReason}");
            // Return to UI if disconnected
            if (controlPanel != null) controlPanel.SetActive(true);
            if (progressLabel != null) progressLabel.SetActive(false);
        }

        // --- Keep these for the Interface implementation ---
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        #endregion

        public string GetLocalPlayerName() => nameInputField.text;
        public int GetLocalPlayerColorIndex() => colorDropdown.value;
    }
}