using Simulation;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public sealed class SphSweTimeStepDiagnosticsWindow : EditorWindow
    {
        private const string WindowTitle = "SPH-SWE Time Step";

        private SphSweSimulation simulation;
        private SphSweTimeStepDiagnostics diagnostics;
        private bool diagnosticsAvailable;
        private bool automaticRefreshEnabled = true;

        [MenuItem("Tools/SPH-SWE/Diagnostics/Time Step Diagnostics")]
        private static void Open()
        {
            GetWindow<SphSweTimeStepDiagnosticsWindow>(WindowTitle);
        }

        [MenuItem("Tools/SPH-SWE/Diagnostics/Log Current Time Step Diagnostics")]
        private static void LogCurrentTimeStepDiagnostics()
        {
            var currentSimulation =
                Object.FindFirstObjectByType<SphSweSimulation>();

            if (currentSimulation == null)
            {
                Debug.LogWarning("SphSweSimulation was not found in the current scene.");
                return;
            }

            var currentDiagnostics = currentSimulation.CalculateTimeStepDiagnostics();
            Debug.Log(CreateDiagnosticMessage(currentDiagnostics), currentSimulation);
        }

        private void OnEnable()
        {
            RefreshDiagnostics();
        }

        private void OnInspectorUpdate()
        {
            if (!automaticRefreshEnabled)
            {
                return;
            }

            RefreshDiagnostics();
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("CFL Time Step Diagnostics", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            automaticRefreshEnabled = EditorGUILayout.Toggle(
                "Automatic Refresh Enabled",
                automaticRefreshEnabled
            );

            if (GUILayout.Button("Refresh"))
            {
                RefreshDiagnostics();
            }

            EditorGUILayout.Space();

            if (simulation == null)
            {
                EditorGUILayout.HelpBox(
                    "SphSweSimulationが現在のSceneに見つかりません。",
                    MessageType.Warning
                );
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Simulation", simulation, typeof(SphSweSimulation), true);
            }

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "現在の粒子状態を診断するにはPlay Modeに入ってください。",
                    MessageType.Info
                );
                return;
            }

            if (!diagnosticsAvailable)
            {
                return;
            }

            DrawTimeStepSection();
            DrawLimitingParticleSection();
            DrawExecutionSection();
        }

        private void DrawTimeStepSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Time Step", EditorStyles.boldLabel);
            EditorGUILayout.FloatField(
                "Last Requested Simulation Time",
                diagnostics.LastRequestedSimulationTime
            );
            EditorGUILayout.FloatField("Maximum Configured Time Step", diagnostics.MaximumConfiguredTimeStep);

            if (float.IsPositiveInfinity(diagnostics.CflTimeStep))
            {
                EditorGUILayout.TextField("CFL Time Step", "Not available");
            }
            else
            {
                EditorGUILayout.FloatField("CFL Time Step", diagnostics.CflTimeStep);
            }

            EditorGUILayout.FloatField("Selected Time Step", diagnostics.SelectedTimeStep);
            EditorGUILayout.TextField(
                "Limiting Condition",
                diagnostics.IsCflLimiting ? "CFL condition" : "Maximum configured time step"
            );
        }

        private void DrawLimitingParticleSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Limiting Particle", EditorStyles.boldLabel);
            EditorGUILayout.IntField("Fluid Particle Count", diagnostics.FluidParticleCount);
            EditorGUILayout.IntField("Particle Index", diagnostics.LimitingParticleIndex);
            EditorGUILayout.FloatField("Velocity", diagnostics.LimitingParticleVelocity);
            EditorGUILayout.FloatField("Fluid Depth", diagnostics.LimitingParticleFluidDepth);
            EditorGUILayout.FloatField("Wave Speed", diagnostics.LimitingParticleWaveSpeed);
            EditorGUILayout.FloatField("Signal Speed", diagnostics.LimitingParticleSignalSpeed);
        }

        private void DrawExecutionSection()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Substeps", EditorStyles.boldLabel);
            EditorGUILayout.IntField("Estimated Required Count", diagnostics.EstimatedRequiredSubstepCount);
            EditorGUILayout.IntField("Maximum Count", diagnostics.MaximumSubstepCount);
            EditorGUILayout.IntField("Last Completed Count", diagnostics.LastCompletedSubstepCount);
            EditorGUILayout.FloatField("Last Simulated Time", diagnostics.LastSimulatedTime);
            EditorGUILayout.FloatField("Remaining Accumulated Time", diagnostics.RemainingAccumulatedTime);

            if (!diagnostics.CanCompleteFrame)
            {
                EditorGUILayout.HelpBox(
                    "推定必要サブステップ数が上限を超えています。シミュレーション時間が遅れる可能性があります。",
                    MessageType.Warning
                );
            }
        }

        private void RefreshDiagnostics()
        {
            simulation = Object.FindFirstObjectByType<SphSweSimulation>();
            diagnosticsAvailable = simulation != null && EditorApplication.isPlaying;

            if (diagnosticsAvailable)
            {
                diagnostics = simulation.CalculateTimeStepDiagnostics();
            }
        }

        private static string CreateDiagnosticMessage(SphSweTimeStepDiagnostics currentDiagnostics)
        {
            var cflTimeStep = float.IsPositiveInfinity(currentDiagnostics.CflTimeStep)
                ? "Not available"
                : $"{currentDiagnostics.CflTimeStep:F6}";

            return $"SPH-SWE time-step diagnostics: "
                + $"selected={currentDiagnostics.SelectedTimeStep:F6}, "
                + $"CFL={cflTimeStep}, "
                + $"last substeps={currentDiagnostics.LastCompletedSubstepCount}, "
                + $"estimated substeps={currentDiagnostics.EstimatedRequiredSubstepCount}, "
                + $"limiting particle={currentDiagnostics.LimitingParticleIndex}.";
        }
    }
}
