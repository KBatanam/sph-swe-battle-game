using Mirror;
using UnityEngine;

namespace SphSwe.Networking
{
    /// <summary>クライアントからサーバーへ自分のトークンを送る。</summary>
    public struct HandshakeTokenMessage : NetworkMessage
    {
        public string token;
    }

    /// <summary>サーバーからクライアントへ、サーバーとクライアントのトークンを返す。</summary>
    public struct HandshakeReplyMessage : NetworkMessage
    {
        public string serverToken;
        public string clientToken;
    }

    /// <summary>
    /// 動作確認用の最小の接続UI。
    /// ホスト開始、IP入力でのゲスト参加、退出を画面上のボタンで行う。
    /// 接続時にホストとクライアントが乱数トークンを交換して表示し、疎通を確認する。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkConnectionDebugUI : MonoBehaviour
    {
        [Header("References")]

        [SerializeField]
        private NetworkBattleSession session;

        [Header("Join Settings")]

        [SerializeField]
        private string joinAddress = "127.0.0.1";

        [SerializeField]
        private ushort port = NetworkBattleSession.DefaultPort;

        private string localToken;
        private string serverToken;
        private string receivedClientToken;
        private bool handlersRegistered;
        private bool tokenSent;

        private void Awake()
        {
            if (session == null)
            {
                session = FindFirstObjectByType<NetworkBattleSession>();
            }

            localToken = GenerateToken();
            RegisterHandlers();
        }

        private void OnDestroy()
        {
            UnregisterHandlers();
        }

        private void Update()
        {
            if (NetworkClient.isConnected && !tokenSent)
            {
                NetworkClient.Send(new HandshakeTokenMessage { token = localToken });
                tokenSent = true;
            }
            else if (!NetworkClient.isConnected && !NetworkServer.active)
            {
                tokenSent = false;
                serverToken = null;
                receivedClientToken = null;
            }
        }

        private void RegisterHandlers()
        {
            if (handlersRegistered)
            {
                return;
            }

            NetworkClient.RegisterHandler<HandshakeReplyMessage>(OnClientReply, false);
            NetworkServer.RegisterHandler<HandshakeTokenMessage>(OnServerToken, false);
            handlersRegistered = true;
        }

        private void UnregisterHandlers()
        {
            if (!handlersRegistered)
            {
                return;
            }

            NetworkClient.UnregisterHandler<HandshakeReplyMessage>();
            NetworkServer.UnregisterHandler<HandshakeTokenMessage>();
            handlersRegistered = false;
        }

        private void OnServerToken(NetworkConnectionToClient connection, HandshakeTokenMessage message)
        {
            receivedClientToken = message.token;
            connection.Send(new HandshakeReplyMessage
            {
                serverToken = localToken,
                clientToken = message.token,
            });
        }

        private void OnClientReply(HandshakeReplyMessage message)
        {
            serverToken = message.serverToken;
            receivedClientToken = message.clientToken;
        }

        private void OnGUI()
        {
            if (session == null)
            {
                return;
            }

            var area = new Rect(10f, 10f, 320f, 260f);
            GUILayout.BeginArea(area, GUI.skin.box);

            GUILayout.Label($"Network: {session.State}");
            GUILayout.Label($"My token:  {localToken}");
            GUILayout.Label($"Server:    {serverToken ?? "-"}");
            GUILayout.Label($"Client:    {receivedClientToken ?? "-"}");

            if (session.State == NetworkBattleSession.SessionState.Offline)
            {
                if (GUILayout.Button("Host"))
                {
                    session.StartHost();
                }

                GUILayout.Label("Join Address");
                joinAddress = GUILayout.TextField(joinAddress);

                if (GUILayout.Button("Join"))
                {
                    session.Join(joinAddress, port);
                }
            }
            else if (GUILayout.Button("Leave"))
            {
                session.Leave();
            }

            GUILayout.EndArea();
        }

        private static string GenerateToken()
        {
            return Random.Range(0, int.MaxValue).ToString("X8");
        }
    }
}
