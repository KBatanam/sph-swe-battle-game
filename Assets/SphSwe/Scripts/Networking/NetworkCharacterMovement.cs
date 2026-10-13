using Mirror;
using SphSwe.Simulation;
using UnityEngine;

namespace SphSwe.Networking
{
    /// <summary>
    /// 自キャラクターの左右位置 (シミュレーション座標系 X) を
    /// クライアント権威で同期する。
    /// オーナーは自身の <see cref="BattleCharacterMotor"/> が動かした結果を書き込み、
    /// 非オーナーは受信値を補間して適用する。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkCharacterMovement : NetworkBehaviour
    {
        [Header("References")]

        [SerializeField]
        private SphSweSimulation simulation;

        [Header("Interpolation")]

        [SerializeField, Min(0f)]
        private float interpolationRate = 20f;

        [SyncVar]
        private float synchronizedPositionX;

        private void Awake()
        {
            syncDirection = SyncDirection.ClientToServer;

            if (simulation == null)
            {
                simulation = FindFirstObjectByType<SphSweSimulation>();
            }
        }

        private void Update()
        {
            if (simulation == null)
            {
                return;
            }

            if (isOwned)
            {
                var localPosition = simulation.transform.InverseTransformPoint(transform.position);
                synchronizedPositionX = localPosition.x;
                return;
            }

            ApplyInterpolatedPosition();
        }

        private void ApplyInterpolatedPosition()
        {
            var simulationTransform = simulation.transform;
            var localPosition = simulationTransform.InverseTransformPoint(transform.position);

            var interpolation = 1f - Mathf.Exp(-interpolationRate * Time.deltaTime);
            localPosition.x = Mathf.Lerp(localPosition.x, synchronizedPositionX, interpolation);

            transform.position = simulationTransform.TransformPoint(localPosition);
        }
    }
}
