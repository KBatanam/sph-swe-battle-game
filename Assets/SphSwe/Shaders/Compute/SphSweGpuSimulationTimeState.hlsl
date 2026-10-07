#ifndef SPH_SWE_GPU_SIMULATION_TIME_STATE_INCLUDED
#define SPH_SWE_GPU_SIMULATION_TIME_STATE_INCLUDED

struct SphSweGpuSimulationTimeState
{
    float AccumulatedSimulationTime;
    float CurrentSimulationTimeStep;
    uint CompletedSubstepCount;
    uint IsSimulationSubstepActive;
};

#endif