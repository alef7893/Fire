using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainGameEnvironmentTerrainIsolator
{
    private const string TargetScenePath = "Assets/Scenes/MainGameEnvironment.unity";
    private const string SourceScenePath =
        "Assets/ImportedAssetPacks/Polytope Studio/Lowpoly_Demos/Environment_Free/Environment_Free.unity";
    private const string SourceTerrainPath =
        "Assets/ImportedAssetPacks/Polytope Studio/Lowpoly_Demos/Environment_Free/Helpers/Terrain/New Terrain.asset";
    private const string TargetFolderPath = "Assets/Scenes/MainGameEnvironmentAssets";
    private const string TargetTerrainPath = TargetFolderPath + "/MainGameEnvironmentTerrain.asset";

    public static void IsolateTerrainAndRestoreWater()
    {
        EnsureTargetFolder();

        if (AssetDatabase.LoadAssetAtPath<TerrainData>(TargetTerrainPath) == null &&
            !AssetDatabase.CopyAsset(SourceTerrainPath, TargetTerrainPath))
        {
            throw new System.InvalidOperationException("Could not duplicate the MainGameEnvironment TerrainData.");
        }

        AssetDatabase.ImportAsset(TargetTerrainPath, ImportAssetOptions.ForceUpdate);
        TerrainData isolatedTerrain = AssetDatabase.LoadAssetAtPath<TerrainData>(TargetTerrainPath);
        if (isolatedTerrain == null)
        {
            throw new System.InvalidOperationException("The duplicated TerrainData could not be loaded.");
        }

        Scene targetScene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        Terrain targetTerrain = targetScene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Terrain>(true))
            .Single();

        targetTerrain.terrainData = isolatedTerrain;
        TerrainCollider terrainCollider = targetTerrain.GetComponent<TerrainCollider>();
        if (terrainCollider != null)
        {
            terrainCollider.terrainData = isolatedTerrain;
        }

        if (!targetScene.GetRootGameObjects().Any(root => root.name == "Plane"))
        {
            Scene sourceScene = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Additive);
            GameObject sourceWater = sourceScene.GetRootGameObjects().Single(root => root.name == "Plane");
            GameObject restoredWater = Object.Instantiate(sourceWater);
            restoredWater.name = "Plane";
            SceneManager.MoveGameObjectToScene(restoredWater, targetScene);
            EditorSceneManager.CloseScene(sourceScene, true);
        }

        EditorSceneManager.MarkSceneDirty(targetScene);
        EditorSceneManager.SaveScene(targetScene);
        AssetDatabase.SaveAssets();
        Debug.Log("MainGameEnvironment now uses an isolated TerrainData and includes the restored water Plane.");
    }

    private static void EnsureTargetFolder()
    {
        if (!AssetDatabase.IsValidFolder(TargetFolderPath))
        {
            AssetDatabase.CreateFolder("Assets/Scenes", "MainGameEnvironmentAssets");
        }
    }
}
