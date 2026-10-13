using UnityEngine;

namespace SphSwe.Networking
{
    /// <summary>
    /// 動作確認用の最小の接続UI。
    /// ホスト開始、IP入力でのゲスト参加、退出を画面上のボタンで行う。
    /// 検証用であり、正式なUIは別途用意する。
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

        private void Awake()
        {
            if (session == null)
            {
                session = FindFirstObjectByType<NetworkBattleSession>();
            }
        }

        private void OnGUI()
        {
            if (session == null)
            {
                return;
            }

            var area = new Rect(10f, 10f, 280f, 190f);
            GUILayout.BeginArea(area, GUI.skin.box);

            GUILayout.Label($"Network: {session.State}");

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
    }
}
