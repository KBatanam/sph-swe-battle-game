using Mirror;
using SphSwe.Gameplay;
using UnityEngine;

namespace SphSwe.Networking
{
    /// <summary>
    /// キャラクターの硬直と雷砲クールダウンをホスト権威で同期する。
    /// サーバーは <see cref="StunStatus"/> と <see cref="LightningCannon"/> の
    /// 残り時間を書き込み、クライアントは受信値を適用する。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkCharacterStatus : NetworkBehaviour
    {
        [Header("References")]

        [SerializeField]
        private StunStatus stunStatus;

        [SerializeField]
        private LightningCannon lightningCannon;

        [SyncVar]
        private float synchronizedStunRemaining;

        [SyncVar]
        private float synchronizedCooldownRemaining;

        private void Awake()
        {
            syncDirection = SyncDirection.ServerToClient;

            if (stunStatus == null)
            {
                stunStatus = GetComponent<StunStatus>();
            }

            if (lightningCannon == null)
            {
                lightningCannon = GetComponent<LightningCannon>();
            }
        }

        private void Update()
        {
            if (isServer)
            {
                if (stunStatus != null)
                {
                    synchronizedStunRemaining = stunStatus.RemainingStunDuration;
                }

                if (lightningCannon != null)
                {
                    synchronizedCooldownRemaining = lightningCannon.RemainingCooldownDuration;
                }

                return;
            }

            if (stunStatus != null)
            {
                stunStatus.SetAuthoritativeRemainingStunDuration(synchronizedStunRemaining);
            }

            if (lightningCannon != null)
            {
                lightningCannon.SetAuthoritativeRemainingCooldownDuration(synchronizedCooldownRemaining);
            }
        }
    }
}
