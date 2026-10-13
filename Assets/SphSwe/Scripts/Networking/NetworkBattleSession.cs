using System;
using Mirror;
using UnityEngine;

namespace SphSwe.Networking
{
    /// <summary>
    /// ネットワーク対戦のセッション制御。
    /// ホスト開始、IP直結でのゲスト参加、退出を担当する。
    /// 接続状態を <see cref="State"/> と <see cref="StateChanged"/> で公開し、
    /// UIやゲームフロー側から参照できるようにする。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkBattleSession : MonoBehaviour
    {
        public const ushort DefaultPort = 7777;

        public enum SessionState
        {
            Offline = 0,
            Hosting = 1,
            Connected = 2,
        }

        [Header("References")]

        [SerializeField]
        private NetworkManager networkManager;

        [Header("Offline Scene Objects")]

        [Tooltip("オンライン対戦中は無効化する、シーン上のオフライン用キャラクター。")]
        [SerializeField]
        private GameObject[] offlineCharacters;

        public SessionState State { get; private set; } = SessionState.Offline;

        public event Action<SessionState> StateChanged;

        public NetworkManager Manager => networkManager;

        private void Awake()
        {
            if (networkManager == null)
            {
                networkManager = NetworkManager.singleton;
            }
        }

        private void Update()
        {
            var nextState = EvaluateState();

            if (nextState != State)
            {
                SetState(nextState);
            }
        }

        public void StartHost()
        {
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager is not assigned.", this);
                return;
            }

            SetOfflineCharactersActive(false);
            networkManager.StartHost();
        }

        public bool Join(string address, ushort port = DefaultPort)
        {
            if (networkManager == null)
            {
                Debug.LogError("NetworkManager is not assigned.", this);
                return false;
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                Debug.LogError("Join address is empty.", this);
                return false;
            }

            networkManager.networkAddress = address.Trim();

            if (Transport.active is PortTransport portTransport)
            {
                portTransport.Port = port;
            }

            SetOfflineCharactersActive(false);
            networkManager.StartClient();
            return true;
        }

        public void Leave()
        {
            if (networkManager == null)
            {
                return;
            }

            if (NetworkServer.active && NetworkClient.active)
            {
                networkManager.StopHost();
            }
            else if (NetworkServer.active)
            {
                networkManager.StopServer();
            }
            else if (NetworkClient.active)
            {
                networkManager.StopClient();
            }
        }

        private SessionState EvaluateState()
        {
            if (NetworkServer.active)
            {
                return SessionState.Hosting;
            }

            if (NetworkClient.isConnected)
            {
                return SessionState.Connected;
            }

            return SessionState.Offline;
        }

        private void SetState(SessionState nextState)
        {
            var wasOffline = State == SessionState.Offline;
            State = nextState;

            if (nextState == SessionState.Offline && !wasOffline)
            {
                SetOfflineCharactersActive(true);
            }

            StateChanged?.Invoke(nextState);
        }

        private void SetOfflineCharactersActive(bool active)
        {
            if (offlineCharacters == null)
            {
                return;
            }

            for (var index = 0; index < offlineCharacters.Length; index++)
            {
                var offlineCharacter = offlineCharacters[index];

                if (offlineCharacter != null)
                {
                    offlineCharacter.SetActive(active);
                }
            }
        }
    }
}
