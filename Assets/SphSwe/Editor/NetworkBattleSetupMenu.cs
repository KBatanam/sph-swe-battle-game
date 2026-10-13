using System;
using kcp2k;
using Mirror;
using SphSwe.Gameplay;
using SphSwe.Networking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SphSwe.Editor
{
    /// <summary>
    /// ネットワーク対戦に必要なコンポーネントと参照をシーンおよびプレハブへ設定する。
    /// 手作業での配線ミスを減らすため、Editorメニューから一度に適用する。
    /// </summary>
    public static class NetworkBattleSetupMenu
    {
        private const string NetworkManagerObjectName = "Network Manager";
        private const string PlayerCubeObjectName = "PlayerCube";
        private const string EnemyCubeObjectName = "EnemyCube";
        private const string ObjectiveBallObjectName = "ObjectiveBall";
        private const string PlayerCubePrefabPath =
            "Assets/SphSwe/Model/Player/Prefab/PlayerCube.prefab";

        [MenuItem("Tools/Battle/Setup/Configure Network Battle")]
        private static void ConfigureNetworkBattle()
        {
            var playerCube = FindRequiredGameObject(PlayerCubeObjectName);
            var enemyCube = FindRequiredGameObject(EnemyCubeObjectName);
            var objectiveBall = FindRequiredGameObject(ObjectiveBallObjectName);

            var playerPrefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerCubePrefabPath);

            if (playerPrefabAsset == null)
            {
                throw new InvalidOperationException(
                    $"PlayerCube prefab was not found at {PlayerCubePrefabPath}."
                );
            }

            ConfigurePlayerPrefab(playerPrefabAsset);
            ConfigureNetworkManager(playerPrefabAsset, playerCube, enemyCube);
            ConfigureObjectiveBall(objectiveBall);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(playerCube.scene);

            Debug.Log("Network battle setup completed.");
        }

        private static void ConfigurePlayerPrefab(GameObject playerPrefabAsset)
        {
            var prefabPath = AssetDatabase.GetAssetPath(playerPrefabAsset);
            var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                EnsureComponent<NetworkIdentity>(prefabRoot);
                EnsureComponent<NetworkBattleCharacter>(prefabRoot);
                EnsureComponent<NetworkCharacterMovement>(prefabRoot);
                EnsureComponent<NetworkCharacterStatus>(prefabRoot);
                EnsureComponent<NetworkCharacterActions>(prefabRoot);

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.ImportAsset(prefabPath);
        }

        private static void ConfigureNetworkManager(
            GameObject playerPrefabAsset,
            GameObject playerCube,
            GameObject enemyCube)
        {
            var networkManagerObject = GameObject.Find(NetworkManagerObjectName);

            if (networkManagerObject == null)
            {
                networkManagerObject = new GameObject(NetworkManagerObjectName);
                Undo.RegisterCreatedObjectUndo(networkManagerObject, "Create Network Manager");
            }

            var networkManager = EnsureComponent<Mirror.NetworkManager>(networkManagerObject);
            var transport = EnsureComponent<KcpTransport>(networkManagerObject);

            var serializedManager = new SerializedObject(networkManager);
            SetObjectReference(serializedManager, "transport", transport);
            SetObjectReference(serializedManager, "playerPrefab", playerPrefabAsset);
            serializedManager.ApplyModifiedProperties();

            var session = EnsureComponent<NetworkBattleSession>(networkManagerObject);
            var serializedSession = new SerializedObject(session);
            SetObjectReference(serializedSession, "networkManager", networkManager);

            var offlineCharactersProperty = serializedSession.FindProperty("offlineCharacters");

            if (offlineCharactersProperty != null)
            {
                offlineCharactersProperty.arraySize = 2;
                offlineCharactersProperty.GetArrayElementAtIndex(0).objectReferenceValue = playerCube;
                offlineCharactersProperty.GetArrayElementAtIndex(1).objectReferenceValue = enemyCube;
            }

            serializedSession.ApplyModifiedProperties();

            EditorUtility.SetDirty(networkManagerObject);
        }

        private static void ConfigureObjectiveBall(GameObject objectiveBallObject)
        {
            EnsureComponent<NetworkIdentity>(objectiveBallObject);
            EnsureComponent<NetworkObjectiveBall>(objectiveBallObject);

            EditorUtility.SetDirty(objectiveBallObject);
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();

            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }

        private static GameObject FindRequiredGameObject(string gameObjectName)
        {
            var gameObject = GameObject.Find(gameObjectName);

            if (gameObject == null)
            {
                throw new InvalidOperationException(
                    $"{gameObjectName} was not found in the active scene."
                );
            }

            return gameObject;
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
