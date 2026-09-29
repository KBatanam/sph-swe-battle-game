using System;
using Gameplay;
using Simulation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Editor
{
    public static class SphSweEnemyAiSetupMenu
    {
        private const string EnemyGameObjectName = "EnemyCube";
        private const string PlayerGameObjectName = "PlayerCube";

        [MenuItem("SPH-SWE/Setup/Configure Enemy AI")]
        private static void ConfigureEnemyAi()
        {
            var enemyGameObject = FindRequiredGameObject(EnemyGameObjectName);
            var playerGameObject = FindRequiredGameObject(PlayerGameObjectName);
            var simulation = FindRequiredObjectByType<SphSweSimulation>();
            var objectiveBall = FindRequiredObjectByType<SphSweObjectiveBall>();
            var characterMotor = GetRequiredComponent<SphSweBattleCharacterMotor>(enemyGameObject);
            var waveGenerator = GetRequiredComponent<SphSweGpuWaveGenerator>(enemyGameObject);
            var lightningCannon = GetRequiredComponent<SphSweLightningCannon>(enemyGameObject);
            var enemyController = enemyGameObject.GetComponent<SphSweEnemyController>();

            if (enemyController == null)
            {
                enemyController = Undo.AddComponent<SphSweEnemyController>(enemyGameObject);
            }

            var serializedController = new SerializedObject(enemyController);
            SetObjectReference(serializedController, "simulation", simulation);
            SetObjectReference(serializedController, "characterMotor", characterMotor);
            SetObjectReference(serializedController, "waveGenerator", waveGenerator);
            SetObjectReference(serializedController, "lightningCannon", lightningCannon);
            SetObjectReference(serializedController, "playerTransform", playerGameObject.transform);
            SetObjectReference(serializedController, "objectiveBall", objectiveBall);
            serializedController.ApplyModifiedProperties();

            EditorUtility.SetDirty(enemyController);
            EditorSceneManager.MarkSceneDirty(enemyGameObject.scene);
            Selection.activeGameObject = enemyGameObject;

            Debug.Log("Enemy AI setup completed.", enemyController);
        }

        private static GameObject FindRequiredGameObject(string gameObjectName)
        {
            var gameObject = GameObject.Find(gameObjectName);

            if (gameObject == null)
            {
                throw new InvalidOperationException($"{gameObjectName} was not found in the active scene.");
            }

            return gameObject;
        }

        private static T FindRequiredObjectByType<T>() where T : UnityEngine.Object
        {
            var targetObject = UnityEngine.Object.FindFirstObjectByType<T>();

            if (targetObject == null)
            {
                throw new InvalidOperationException($"{typeof(T).Name} was not found in the active scene.");
            }

            return targetObject;
        }

        private static T GetRequiredComponent<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();

            if (component == null)
            {
                throw new InvalidOperationException(
                    $"{typeof(T).Name} is not attached to {gameObject.name}."
                );
            }

            return component;
        }

        private static void SetObjectReference(
            SerializedObject serializedObject,
            string propertyName,
            UnityEngine.Object reference)
        {
            var property = serializedObject.FindProperty(propertyName);

            if (property == null)
            {
                throw new InvalidOperationException(
                    $"Serialized property {propertyName} was not found."
                );
            }

            property.objectReferenceValue = reference;
        }
    }
}
