using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MainGameEnvironmentSceneDuplicator
{
    private const string SourcePath =
        "Assets/ImportedAssetPacks/Polytope Studio/Lowpoly_Demos/Environment_Free/Environment_Free.unity";
    private const string DestinationPath = "Assets/Prefabs/MainGameEnvironment.unity";

    public static void DuplicateAndValidate()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SourcePath) == null)
        {
            throw new InvalidOperationException($"Source scene not found: {SourcePath}");
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DestinationPath) != null)
        {
            AssetDatabase.DeleteAsset(DestinationPath);
        }

        if (!AssetDatabase.CopyAsset(SourcePath, DestinationPath))
        {
            throw new InvalidOperationException($"Could not duplicate scene to: {DestinationPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.OpenScene(DestinationPath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            throw new InvalidOperationException("The duplicated scene could not be opened.");
        }

        Debug.Log($"Duplicated and validated main game environment scene: {DestinationPath}");
    }
}
