using Mirror;
using SphSwe.Gameplay;
using SphSwe.Gpu;
using UnityEngine;

namespace SphSwe.Networking
{
    /// <summary>
    /// ローカルプレイヤーが発生させた波と雷砲の発射を、
    /// ホスト権威の流体シミュレーションへ転送する。
    /// 自分の画面では即座に作用し、サーバー側でも同じインパルスを適用する。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkCharacterActions : NetworkBehaviour
    {
        [Header("References")]

        [SerializeField]
        private WaveGenerator waveGenerator;

        [SerializeField]
        private LightningCannon lightningCannon;

        [SerializeField]
        private SphSweGpuSimulation gpuSimulation;

        private void Awake()
        {
            if (waveGenerator == null)
            {
                waveGenerator = GetComponent<WaveGenerator>();
            }

            if (lightningCannon == null)
            {
                lightningCannon = GetComponent<LightningCannon>();
            }

            if (gpuSimulation == null)
            {
                gpuSimulation = FindFirstObjectByType<SphSweGpuSimulation>();
            }
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            if (waveGenerator != null)
            {
                waveGenerator.WaveGenerated += OnLocalWaveGenerated;
            }

            if (lightningCannon != null)
            {
                lightningCannon.LightningFired += OnLocalLightningFired;
            }
        }

        public override void OnStopClient()
        {
            if (waveGenerator != null)
            {
                waveGenerator.WaveGenerated -= OnLocalWaveGenerated;
            }

            if (lightningCannon != null)
            {
                lightningCannon.LightningFired -= OnLocalLightningFired;
            }

            base.OnStopClient();
        }

        private void OnLocalWaveGenerated(Vector2 center, Vector2 direction, float radius, float strength)
        {
            if (isServer)
            {
                return;
            }

            CmdGenerateWave(center, direction, radius, strength);
        }

        private void OnLocalLightningFired()
        {
            if (isServer)
            {
                return;
            }

            CmdFireLightning();
        }

        [Command]
        private void CmdGenerateWave(Vector2 center, Vector2 direction, float radius, float strength)
        {
            if (gpuSimulation == null)
            {
                return;
            }

            gpuSimulation.TryApplyWaveImpulse(center, direction, radius, strength);
        }

        [Command]
        private void CmdFireLightning()
        {
            if (lightningCannon == null)
            {
                return;
            }

            lightningCannon.TryFireLightningCannon();
        }
    }
}
