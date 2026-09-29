using System;
using SphSwe.Gpu;
using UnityEngine;
using SphSwe.Validation;

namespace SphSwe.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class ObjectiveBall : MonoBehaviour
    {
        [Header("References")]

        [SerializeField, Required]
        private SphSweGpuSimulation gpuSimulation;

        [Header("Surface Following")]

        [SerializeField]
        private float groundHeight;

        [SerializeField, Min(0.01f)]
        private float fluidDepthHeightScale = 1f;

        [SerializeField, Min(0f)]
        private float surfaceHeightOffset = 0.2f;

        [SerializeField, Min(0f)]
        private float surfaceHeightFollowingSpeed = 15f;
        
        [SerializeField, Min(0.001f)]
        private float calmFluidDepth = 1f;

        [SerializeField, Min(0f)]
        private float waveEnergyFluxAccelerationScale = 100f;

        [SerializeField, Min(0f)]
        private float horizontalVelocityDrag = 1f;

        [SerializeField, Min(0.01f)]
        private float maximumHorizontalVelocity = 2f;

        private Vector2 horizontalVelocity;
        
        private SphSweGpuSurfaceSample latestSurfaceSample;
        private bool validSurfaceSampleReceived;
        
        private void Awake()
        {
            if (gpuSimulation == null)
            {
                throw new InvalidOperationException("GPU Simulation is not assigned.");
            }
        }

        private void Update()
        {
            if (!validSurfaceSampleReceived)
            {
                return;
            }

            ApplySurfaceSample(Time.deltaTime);
        }

        private void LateUpdate()
        {
            RequestSurfaceSample();
        }

        private void RequestSurfaceSample()
        {
            var simulation = gpuSimulation.SourceSimulation;
            var localPosition = simulation.transform.InverseTransformPoint(transform.position);
            var simulationPosition = new Vector2(localPosition.x, localPosition.z);

            gpuSimulation.TryRequestFluidSurfaceSample(simulationPosition, ReceiveSurfaceSample);
        }

        private void ReceiveSurfaceSample(SphSweGpuSurfaceSample surfaceSample)
        {
            if (this == null)
            {
                return;
            }

            latestSurfaceSample = surfaceSample;
            validSurfaceSampleReceived = surfaceSample.IsValid == 1u;
        }

        private void ApplySurfaceSample(float deltaTime)
        {
            var simulation = gpuSimulation.SourceSimulation;
            var simulationTransform = simulation.transform;
            var localPosition = simulationTransform.InverseTransformPoint(transform.position);

            UpdateHorizontalVelocity(deltaTime);

            localPosition.x += horizontalVelocity.x * deltaTime;
            localPosition.z += horizontalVelocity.y * deltaTime;
            var targetSurfaceHeight =
                groundHeight
                + latestSurfaceSample.FluidDepth
                * fluidDepthHeightScale
                + surfaceHeightOffset;

            var heightInterpolationRate = 1f - Mathf.Exp(-surfaceHeightFollowingSpeed * deltaTime);
            localPosition.y = Mathf.Lerp(localPosition.y, targetSurfaceHeight, heightInterpolationRate);

            var halfSimulationAreaSize = simulation.SimulationAreaSize * 0.5f;
            var minimumPosition = simulation.SimulationCenter - halfSimulationAreaSize;
            var maximumPosition = simulation.SimulationCenter + halfSimulationAreaSize;

            localPosition.x = Mathf.Clamp(
                localPosition.x,
                minimumPosition.x,
                maximumPosition.x
            );

            localPosition.z = Mathf.Clamp(
                localPosition.z,
                minimumPosition.y,
                maximumPosition.y
            );

            transform.position = simulationTransform.TransformPoint(localPosition);
        }

        /// <summary>
        /// 周辺流体の速度から球体を加速し、
        /// 波が通過した後も移動が残るように球体自身の慣性を保持する。
        /// 水平方向の抵抗によって速度は徐々に減衰する。
        /// </summary>
        private void UpdateHorizontalVelocity(float deltaTime)
        {
            var fluidDepthDeviation = latestSurfaceSample.FluidDepth - calmFluidDepth;
            var waveEnergyFlux = latestSurfaceSample.FluidVelocity * fluidDepthDeviation;
            horizontalVelocity += waveEnergyFlux * (waveEnergyFluxAccelerationScale * deltaTime);
            
            var dragRate = Mathf.Exp(-horizontalVelocityDrag * deltaTime);
            horizontalVelocity *= dragRate;
            
            var squaredMaximumHorizontalVelocity = maximumHorizontalVelocity * maximumHorizontalVelocity;

            if (horizontalVelocity.sqrMagnitude > squaredMaximumHorizontalVelocity)
            {
                horizontalVelocity = horizontalVelocity.normalized * maximumHorizontalVelocity;
            }
        }
        
        private void OnValidate()
        {
            fluidDepthHeightScale = Mathf.Max(0.01f, fluidDepthHeightScale);
            surfaceHeightOffset = Mathf.Max(0f, surfaceHeightOffset);
            surfaceHeightFollowingSpeed = Mathf.Max(0f, surfaceHeightFollowingSpeed);
            calmFluidDepth = Mathf.Max(0.001f, calmFluidDepth);
            waveEnergyFluxAccelerationScale = Mathf.Max(0f, waveEnergyFluxAccelerationScale);
            horizontalVelocityDrag = Mathf.Max(0f, horizontalVelocityDrag);
            maximumHorizontalVelocity = Mathf.Max(0.01f, maximumHorizontalVelocity);
        }
    }
}
