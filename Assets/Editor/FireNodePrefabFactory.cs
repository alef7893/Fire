using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FireNodePrefabFactory
{
    private const string PrefabFolder = "Assets/Prefabs/FireNodes";
    private const string OffMaterialPath = "Assets/Materials/FireNode_Off.mat";
    private const string OnMaterialPath = "Assets/Materials/FireNode_On.mat";
    private const string DestroyedMaterialPath = "Assets/Materials/FireNode_Destroyed.mat";
    private const string VegetationMaterialPath = "Assets/Materials/FireNode_Vegetation.mat";
    private const string FireEffectPrefabPath = "Assets/Vefects/Free Fire VFX URP/Particles/VFX_Fire_01_Small_Simple.prefab";

    [MenuItem("Tools/Fire Simulation/Create Fire Node Prefabs")]
    public static void CreateFireNodePrefabs()
    {
        EnsureFolder(PrefabFolder);

        Material offMaterial = AssetDatabase.LoadAssetAtPath<Material>(OffMaterialPath);
        Material onMaterial = AssetDatabase.LoadAssetAtPath<Material>(OnMaterialPath);
        Material destroyedMaterial = AssetDatabase.LoadAssetAtPath<Material>(DestroyedMaterialPath);
        Material vegetationMaterial = AssetDatabase.LoadAssetAtPath<Material>(VegetationMaterialPath);
        GameObject fireEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FireEffectPrefabPath);

        CreatePrefab(
            "FireNode_Spark",
            FireNodeType.Spark,
            FireNodeState.Burning,
            onMaterial,
            onMaterial,
            destroyedMaterial,
            ignitionResistance: 0.0f,
            firePower: 5.0f,
            exposureDecayRate: 0.0f,
            timeToDestroy: 5.0f,
            canBeDestroyed: true,
            isCritical: false,
            burningEffectPrefab: fireEffectPrefab);

        CreatePrefab(
            "FireNode_Vegetation",
            FireNodeType.Vegetation,
            FireNodeState.Off,
            vegetationMaterial,
            onMaterial,
            destroyedMaterial,
            ignitionResistance: 0.6f,
            firePower: 5.0f,
            exposureDecayRate: 0.1f,
            timeToDestroy: 5.0f,
            canBeDestroyed: true,
            isCritical: false,
            burningEffectPrefab: fireEffectPrefab);

        CreatePrefab(
            "FireNode_Sensitive",
            FireNodeType.Structure,
            FireNodeState.Off,
            offMaterial,
            onMaterial,
            destroyedMaterial,
            ignitionResistance: 1.0f,
            firePower: 5.0f,
            exposureDecayRate: 0.1f,
            timeToDestroy: 8.0f,
            canBeDestroyed: true,
            isCritical: false,
            burningEffectPrefab: fireEffectPrefab);

        CreatePrefab(
            "FireNode_NonFlammable",
            FireNodeType.NonFlammable,
            FireNodeState.Off,
            destroyedMaterial,
            destroyedMaterial,
            destroyedMaterial,
            ignitionResistance: 9999.0f,
            firePower: 0.0f,
            exposureDecayRate: 0.0f,
            timeToDestroy: 0.0f,
            canBeDestroyed: false,
            isCritical: false,
            burningEffectPrefab: null);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Fire node prefabs created in {PrefabFolder}.");
    }

    [MenuItem("Tools/Fire Simulation/Apply Fire Effects To Scene Nodes")]
    public static void ApplyFireEffectsToSceneNodes()
    {
        GameObject fireEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FireEffectPrefabPath);
        FireObject[] fireObjects = Object.FindObjectsOfType<FireObject>();
        int updatedCount = 0;

        foreach (FireObject fireObject in fireObjects)
        {
            if (fireObject.nodeType == FireNodeType.NonFlammable)
            {
                fireObject.burningEffectPrefab = null;
                continue;
            }

            fireObject.burningEffectPrefab = fireEffectPrefab;
            fireObject.burningEffectLocalOffset = new Vector3(0.0f, 0.5f, 0.0f);
            fireObject.burningEffectLocalScale = Vector3.one * 0.5f;
            fireObject.parentBurningEffectToNode = true;
            fireObject.burningEffectDestroyDelay = 2.0f;
            EditorUtility.SetDirty(fireObject);
            updatedCount++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Applied fire effects to {updatedCount} scene fire nodes.");
    }

    private static void CreatePrefab(
        string prefabName,
        FireNodeType nodeType,
        FireNodeState initialState,
        Material unlitMaterial,
        Material litMaterial,
        Material destroyedMaterial,
        float ignitionResistance,
        float firePower,
        float exposureDecayRate,
        float timeToDestroy,
        bool canBeDestroyed,
        bool isCritical,
        GameObject burningEffectPrefab)
    {
        string prefabPath = $"{PrefabFolder}/{prefabName}.prefab";
        GameObject node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        node.name = prefabName;

        FireObject fireObject = node.AddComponent<FireObject>();
        fireObject.nodeType = nodeType;
        fireObject.state = initialState;
        fireObject.unlitMaterial = unlitMaterial;
        fireObject.litMaterial = litMaterial;
        fireObject.destroyedMaterial = destroyedMaterial;
        fireObject.ignitionResistance = ignitionResistance;
        fireObject.firePower = firePower;
        fireObject.exposureDecayRate = exposureDecayRate;
        fireObject.timeToDestroy = timeToDestroy;
        fireObject.canBeDestroyed = canBeDestroyed;
        fireObject.isCritical = isCritical;
        fireObject.burningEffectPrefab = burningEffectPrefab;
        fireObject.burningEffectLocalOffset = new Vector3(0.0f, 0.5f, 0.0f);
        fireObject.burningEffectLocalScale = Vector3.one * 0.5f;
        fireObject.parentBurningEffectToNode = true;
        fireObject.burningEffectDestroyDelay = 2.0f;
        fireObject.blinkVegetationWhenBurning = true;
        fireObject.vegetationBlinkInterval = 0.25f;
        fireObject.fireIntensity = initialState == FireNodeState.Burning ? 1.0f : 0.0f;

        Renderer renderer = node.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = unlitMaterial;
        }

        PrefabUtility.SaveAsPrefabAsset(node, prefabPath);
        Object.DestroyImmediate(node);
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string currentPath = "Assets";
        string[] folders = folderPath.Substring("Assets/".Length).Split('/');
        foreach (string folder in folders)
        {
            string nextPath = Path.Combine(currentPath, folder).Replace("\\", "/");
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, folder);
            }

            currentPath = nextPath;
        }
    }
}
