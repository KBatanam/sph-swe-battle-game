using System;
using Mirror;
using SphSwe.Gameplay;
using SphSwe.Simulation;
using UnityEngine;

namespace SphSwe.Networking
{
    /// <summary>
    /// ネットワーク対戦のプレイヤーキャラクター。
    /// 陣営 (Near/Far) の決定、スポーン位置の適用、
    /// ローカルプレイヤー以外の入力無効化、
    /// Far 側のカメラ・操作反転を担当する。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkBattleCharacter : NetworkBehaviour
    {
        [Header("References")]

        [SerializeField]
        private SphSweSimulation simulation;

        [SerializeField]
        private PlayerController playerController;

        [Tooltip("オフライン用AI。ネットワークキャラクターでは使用しない。")]
        [SerializeField]
        private EnemyController enemyController;

        [Header("Placement")]

        [SerializeField, Min(0f)]
        private float sideBoundaryInset = 0.25f;

        public NetworkPlayerSide Side { get; private set; } = NetworkPlayerSide.Near;

        public event Action<NetworkPlayerSide> SideResolved;

        private bool viewConfigured;

        private void Awake()
        {
            ResolveReferences();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            Side = ResolveSide();
            ApplySidePosition();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            Side = ResolveSide();
            ApplySidePosition();
            ApplyInputAvailability();
            SideResolved?.Invoke(Side);
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            ApplyInputAvailability();
            ConfigureLocalView();
        }

        private void ResolveReferences()
        {
            if (simulation == null)
            {
                simulation = FindFirstObjectByType<SphSweSimulation>();
            }

            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }

            if (enemyController == null)
            {
                enemyController = GetComponent<EnemyController>();
            }
        }

        private NetworkPlayerSide ResolveSide()
        {
            if (isServer)
            {
                return connectionToClient == NetworkServer.localConnection
                    ? NetworkPlayerSide.Near
                    : NetworkPlayerSide.Far;
            }

            return isOwned
                ? NetworkPlayerSide.Far
                : NetworkPlayerSide.Near;
        }

        private void ApplySidePosition()
        {
            if (simulation == null)
            {
                return;
            }

            var simulationTransform = simulation.transform;
            var halfArea = simulation.SimulationAreaSize * 0.5f;
            var inset = Mathf.Min(sideBoundaryInset, halfArea.y);

            var center = simulation.SimulationCenter;
            var sideSign = Side == NetworkPlayerSide.Near ? -1f : 1f;
            var localZ = center.y + sideSign * (halfArea.y - inset);

            var localPosition = simulationTransform.InverseTransformPoint(transform.position);
            localPosition.x = center.x;
            localPosition.z = localZ;
            transform.position = simulationTransform.TransformPoint(localPosition);
        }

        private void ApplyInputAvailability()
        {
            var isLocallyControlled = isLocalPlayer;

            if (playerController != null)
            {
                playerController.enabled = isLocallyControlled;
                playerController.MovementInputSign = 1f;
            }

            if (enemyController != null)
            {
                // ネットワーク対戦では AI は使用しない。
                enemyController.enabled = false;
            }
        }

        private void ConfigureLocalView()
        {
            if (viewConfigured)
            {
                return;
            }

            viewConfigured = true;

            if (Side != NetworkPlayerSide.Far)
            {
                return;
            }

            if (playerController != null)
            {
                playerController.MovementInputSign = -1f;
            }

            var mainCamera = Camera.main;

            if (mainCamera == null || simulation == null)
            {
                return;
            }

            mainCamera.transform.RotateAround(
                simulation.transform.position,
                Vector3.up,
                180f
            );
        }
    }
}
