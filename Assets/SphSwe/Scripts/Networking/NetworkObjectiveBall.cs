using Mirror;
using SphSwe.Gameplay;
using UnityEngine;

namespace SphSwe.Networking
{
    /// <summary>
    /// 目的球の位置をホスト権威で同期する。
    /// サーバーは <see cref="ObjectiveBall"/> がローカル流体から計算した位置を送信し、
    /// クライアントは受信値を補間して表示する。
    /// クライアント側では球と得点判定のローカル計算を無効化する。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkObjectiveBall : NetworkBehaviour
    {
        [Header("References")]

        [SerializeField]
        private ObjectiveBall objectiveBall;

        [SerializeField]
        private BattleScoreController battleScoreController;

        [Header("Interpolation")]

        [SerializeField, Min(0f)]
        private float interpolationRate = 20f;

        [SyncVar]
        private Vector3 synchronizedPosition;

        private void Awake()
        {
            syncDirection = SyncDirection.ServerToClient;

            if (objectiveBall == null)
            {
                objectiveBall = GetComponent<ObjectiveBall>();
            }

            if (battleScoreController == null)
            {
                battleScoreController = FindFirstObjectByType<BattleScoreController>();
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (objectiveBall != null)
            {
                objectiveBall.enabled = true;
            }

            if (battleScoreController != null)
            {
                battleScoreController.enabled = true;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            if (isServer)
            {
                return;
            }

            if (objectiveBall != null)
            {
                objectiveBall.enabled = false;
            }

            if (battleScoreController != null)
            {
                battleScoreController.enabled = false;
            }
        }

        private void Update()
        {
            if (isServer)
            {
                synchronizedPosition = transform.position;
                return;
            }

            var interpolation = 1f - Mathf.Exp(-interpolationRate * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, synchronizedPosition, interpolation);
        }
    }
}
