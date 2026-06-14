using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MainGameEnvironmentFenceBuilder
{
    private const string ScenePath = "Assets/Scenes/MainGameEnvironment.unity";
    private const string FenceFolder =
        "Assets/ImportedAssetPacks/Polytope Studio/Lowpoly_Village/Prefabs/Modular/Fence/";

    public static void Build()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (GameObject root in scene.GetRootGameObjects().Where(root => root.name == "Fence"))
        {
            root.SetActive(false);
        }

        GameObject generatorObject = scene.GetRootGameObjects()
            .FirstOrDefault(root => root.name == "GeneratedModularFenceBoundary");
        if (generatorObject == null)
        {
            generatorObject = new GameObject("GeneratedModularFenceBoundary");
            SceneManager.MoveGameObjectToScene(generatorObject, scene);
        }

        Terrain terrain = Object.FindObjectOfType<Terrain>();
        Vector3 center = terrain != null
            ? terrain.transform.position + new Vector3(50f, 0f, 50f)
            : new Vector3(50f, 0f, 77f);
        generatorObject.transform.position = center;

        FenceBoundaryGenerator generator = generatorObject.GetComponent<FenceBoundaryGenerator>();
        if (generator == null)
        {
            generator = generatorObject.AddComponent<FenceBoundaryGenerator>();
        }

        generator.fencePrefabs = new[]
        {
            AssetDatabase.LoadAssetAtPath<GameObject>(FenceFolder + "PT_Modular_Fence_Wood_01.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>(FenceFolder + "PT_Modular_Fence_Wood_02.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>(FenceFolder + "PT_Modular_Fence_Wood_03.prefab")
        };
        generator.gatePrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(FenceFolder + "PT_Modular_Gate_Wood_01.prefab");
        generator.modulesAlongX = 18;
        generator.modulesAlongZ = 18;
        generator.moduleSpacing = 5f;
        generator.cornerClosureOffset = 2.5f;
        generator.placeGate = true;
        generator.gateSide = FenceBoundarySide.South;
        generator.gateStartModule = generator.modulesAlongX / 2;
        generator.gateModuleSpan = 1;
        generator.targetTerrain = terrain;
        generator.verticalOffset = 0f;
        generator.disablePrefabColliders = true;
        generator.createInvisibleBoundary = true;
        generator.boundaryHeight = 4f;
        generator.boundaryThickness = 0.5f;

        FenceBoundaryGeneratorUtility.Generate(generator);
        EditorUtility.SetDirty(generator);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Generated a modular fence boundary in MainGameEnvironment.");
    }

    public static void RelocateGateToWestSide()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        FenceBoundaryGenerator generator = Object.FindObjectOfType<FenceBoundaryGenerator>();
        if (generator == null)
        {
            throw new System.InvalidOperationException("MainGameEnvironment does not contain a FenceBoundaryGenerator.");
        }

        generator.placeGate = true;
        generator.gateSide = FenceBoundarySide.West;
        generator.gateStartModule = 7;
        generator.gateModuleSpan = 2;

        FenceBoundaryGeneratorUtility.Generate(generator);
        EditorUtility.SetDirty(generator);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Relocated GeneratedGate to replace west-side modules 7 and 8.");
    }
}
