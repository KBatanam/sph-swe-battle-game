using System.Runtime.InteropServices;

namespace Gpu
{
    /// <summary>
    /// GPU上で保持するSPH-SWEシミュレーションの時間進行状態。
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SphSweGpuSimulationTimeState
    {
        /// <summary>
        /// 構造体一要素当たりのバイト数。
        /// floatが2個、uintが2個で合計16バイトとなる。
        /// </summary>
        public const int Stride = 16;

        public float AccumulatedSimulationTime;
        public float CurrentSimulationTimeStep;
        public uint CompletedSubstepCount;
        public uint IsSimulationSubstepActive;
    }
}