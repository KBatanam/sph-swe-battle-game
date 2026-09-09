namespace Simulation
{
    public readonly struct SphSweTimeStepDiagnostics
    {
        public readonly int FluidParticleCount;
        public readonly int LimitingParticleIndex;
        public readonly int LastCompletedSubstepCount;
        public readonly int EstimatedRequiredSubstepCount;
        public readonly int MaximumSubstepCount;
        public readonly float LastRequestedSimulationTime;
        public readonly float MaximumConfiguredTimeStep;
        public readonly float CflTimeStep;
        public readonly float SelectedTimeStep;
        public readonly float LimitingParticleVelocity;
        public readonly float LimitingParticleFluidDepth;
        public readonly float LimitingParticleWaveSpeed;
        public readonly float LimitingParticleSignalSpeed;
        public readonly float LastSimulatedTime;
        public readonly float RemainingAccumulatedTime;

        public bool IsCflLimiting => CflTimeStep < MaximumConfiguredTimeStep;
        public bool CanCompleteFrame => EstimatedRequiredSubstepCount <= MaximumSubstepCount;

        public SphSweTimeStepDiagnostics(
            int fluidParticleCount,
            int limitingParticleIndex,
            int lastCompletedSubstepCount,
            int estimatedRequiredSubstepCount,
            int maximumSubstepCount,
            float lastRequestedSimulationTime,
            float maximumConfiguredTimeStep,
            float cflTimeStep,
            float selectedTimeStep,
            float limitingParticleVelocity,
            float limitingParticleFluidDepth,
            float limitingParticleWaveSpeed,
            float limitingParticleSignalSpeed,
            float lastSimulatedTime,
            float remainingAccumulatedTime)
        {
            FluidParticleCount = fluidParticleCount;
            LimitingParticleIndex = limitingParticleIndex;
            LastCompletedSubstepCount = lastCompletedSubstepCount;
            EstimatedRequiredSubstepCount = estimatedRequiredSubstepCount;
            MaximumSubstepCount = maximumSubstepCount;
            LastRequestedSimulationTime = lastRequestedSimulationTime;
            MaximumConfiguredTimeStep = maximumConfiguredTimeStep;
            CflTimeStep = cflTimeStep;
            SelectedTimeStep = selectedTimeStep;
            LimitingParticleVelocity = limitingParticleVelocity;
            LimitingParticleFluidDepth = limitingParticleFluidDepth;
            LimitingParticleWaveSpeed = limitingParticleWaveSpeed;
            LimitingParticleSignalSpeed = limitingParticleSignalSpeed;
            LastSimulatedTime = lastSimulatedTime;
            RemainingAccumulatedTime = remainingAccumulatedTime;
        }
    }
}
