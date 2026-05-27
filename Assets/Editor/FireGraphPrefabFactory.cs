using System.IO;
using UnityEditor;
using UnityEngine;

public static class FireGraphPrefabFactory
{
    private const string GraphPrefabFolder = "Assets/Prefabs/FireGraph";
    private const string NodePrefabFolder = "Assets/Prefabs/FireNodes";
    private const string OffMaterialPath = "Assets/Materials/FireNode_Off.mat";
    private const string OnMaterialPath = "Assets/Materials/FireNode_On.mat";
    private const string DestroyedMaterialPath = "Assets/Materials/FireNode_Destroyed.mat";
    private const string GroundFireEffectPrefabPath = "Assets/ImportedPackages/Free Fire VFX URP/Particles/VFX_Fire_01_Big_Simple.prefab";
    private const string NodeArrivalEffectPrefabPath = "Assets/ImportedPackages/Free Fire VFX URP/Particles/VFX_Fire_01_Small_Simple.prefab";

    [MenuItem("Tools/Fire Simulation/Create Graph Architecture Prefabs")]
    public static void CreateGraphArchitecturePrefabs()
    {
        EnsureFolder(GraphPrefabFolder);
        EnsureFolder(NodePrefabFolder);

        Material offMaterial = AssetDatabase.LoadAssetAtPath<Material>(OffMaterialPath);
        Material onMaterial = AssetDatabase.LoadAssetAtPath<Material>(OnMaterialPath);
        Material destroyedMaterial = AssetDatabase.LoadAssetAtPath<Material>(DestroyedMaterialPath);
        GameObject groundFireEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GroundFireEffectPrefabPath);
        GameObject nodeArrivalEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NodeArrivalEffectPrefabPath);

        CreateGraphRootPrefab();
        CreateGroundEdgePrefab(groundFireEffectPrefab, nodeArrivalEffectPrefab);
        CreateNodePrefab(
            "FireNode_Spark",
            FireNodeType.Spark,
            FireNodeState.Burning,
            onMaterial,
            onMaterial,
            destroyedMaterial,
            ignitionResistance: 0.0f,
            firePower: 4.0f,
            exposureDecayRate: 0.0f,
            timeToDestroy: 10.0f);

        CreateNodePrefab(
            "FireNode_Sensitive",
            FireNodeType.Structure,
            FireNodeState.Off,
            offMaterial,
            onMaterial,
            destroyedMaterial,
            ignitionResistance: 1.2f,
            firePower: 4.0f,
            exposureDecayRate: 0.05f,
            timeToDestroy: 10.0f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created fire graph architecture prefabs: root, ground edge, spark node, and sensitive node.");
    }

    private static void CreateGraphRootPrefab()
    {
        GameObject root = new GameObject("FireGraphRoot");
        GameObject nodes = new GameObject("Nodes");
        GameObject edges = new GameObject("Edges");

        nodes.transform.SetParent(root.transform, false);
        edges.transform.SetParent(root.transform, false);

        FireGraphRoot graphRoot = root.AddComponent<FireGraphRoot>();
        graphRoot.nodesRoot = nodes.transform;
        graphRoot.edgesRoot = edges.transform;

        FireSimulationManager manager = root.AddComponent<FireSimulationManager>();
        manager.graphRoot = graphRoot;
        manager.treatConnectionsAsBidirectional = true;
        manager.includeInactiveNodes = true;
        manager.spreadInterval = 1.25f;
        manager.minimumEdgeDistance = 0.1f;
        manager.propagationMultiplier = 0.5f;

        PrefabUtility.SaveAsPrefabAsset(root, $"{GraphPrefabFolder}/FireGraphRoot.prefab");
        Object.DestroyImmediate(root);
    }

    private static void CreateGroundEdgePrefab(GameObject groundFireEffectPrefab, GameObject nodeArrivalEffectPrefab)
    {
        GameObject edgeObject = new GameObject("FireEdge_Ground_BigSimple");
        FireEdge edge = edgeObject.AddComponent<FireEdge>();
        edge.surfaceType = FireSurfaceType.Ground;
        edge.enabledForPropagation = true;
        edge.spreadDelay = 0.0f;
        edge.propagationCostMultiplier = 2.0f;
        edge.propagationSpeed = 1.0f;
        edge.movingFireBridgePrefab = groundFireEffectPrefab;
        edge.groundFirePatchPrefab = groundFireEffectPrefab;
        edge.nodeArrivalEffectPrefab = nodeArrivalEffectPrefab;
        edge.fireEffectLocalOffset = Vector3.zero;
        edge.firePatchLocalScale = Vector3.one * 0.6f;
        edge.nodeArrivalEffectLocalScale = Vector3.one * 0.35f;
        edge.alignEffectToEdge = true;
        edge.muteFirePatchAudio = true;
        edge.firePatchSpacing = 0.6f;
        edge.firePatchLateralJitter = 0.25f;
        edge.firePatchLifetime = 18.0f;
        edge.useMovingFireBridge = true;
        edge.movingFireMinimumScale = 0.2f;
        edge.movingFireMaximumScale = 0.5f;
        edge.movingFireScaleSpeed = 0.5f;
        edge.movingFireProgressOffset = 0.09f;
        edge.movingFireDestroyDelay = 1.0f;
        edge.muteMovingFireAudio = false;
        edge.movingFireAudioVolume = 0.45f;
        edge.movingFireAudioSpatialBlend = 1.0f;
        edge.movingFireScaleCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
        ApplyDynamicFireScaleDefaults(edge);
        edge.nodeArrivalEffectLifetime = 2.0f;
        edge.showGizmo = true;
        edge.edgeColor = new Color(1.0f, 0.55f, 0.05f, 0.9f);
        edge.midpointSize = 0.12f;

        PrefabUtility.SaveAsPrefabAsset(edgeObject, $"{GraphPrefabFolder}/FireEdge_Ground_BigSimple.prefab");
        Object.DestroyImmediate(edgeObject);
    }

    [MenuItem("Tools/Fire Simulation/Apply Dynamic Fire Scale Defaults")]
    public static void ApplyDynamicFireScaleDefaultsToPrefab()
    {
        string edgePrefabPath = $"{GraphPrefabFolder}/FireEdge_Ground_BigSimple.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(edgePrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"Could not find edge prefab at {edgePrefabPath}.");
            return;
        }

        FireEdge edge = prefab.GetComponent<FireEdge>();
        if (edge == null)
        {
            Debug.LogError($"Could not find FireEdge on {edgePrefabPath}.");
            return;
        }

        ApplyDynamicFireScaleDefaults(edge);
        EditorUtility.SetDirty(edge);
        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Applied dynamic fire scale defaults to FireEdge_Ground_BigSimple prefab.");
    }

    private static void ApplyDynamicFireScaleDefaults(FireEdge edge)
    {
        edge.useDynamicPatchScale = true;
        edge.firePatchEdgeScaleFactor = 0.65f;
        edge.firePatchMinimumScale = 0.2f;
        edge.firePatchMaximumScale = 1.5f;
        edge.firePatchResizeSpeed = 1.0f;
        edge.scaleGrowthByPropagationCost = true;
        edge.firePatchGrowDuration = 3.0f;
        edge.firePatchFadeDuration = 3.0f;
        edge.firePatchGrowthCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);
        edge.firePatchFadeCurve = AnimationCurve.EaseInOut(0.0f, 1.0f, 1.0f, 0.0f);
    }

    private static void CreateNodePrefab(
        string prefabName,
        FireNodeType nodeType,
        FireNodeState initialState,
        Material unlitMaterial,
        Material litMaterial,
        Material destroyedMaterial,
        float ignitionResistance,
        float firePower,
        float exposureDecayRate,
        float timeToDestroy)
    {
        GameObject node = new GameObject(prefabName);

        SphereCollider collider = node.AddComponent<SphereCollider>();
        collider.radius = 0.35f;
        collider.isTrigger = true;

        FireNode fireNode = node.AddComponent<FireNode>();
        fireNode.nodeType = nodeType;
        fireNode.state = initialState;
        fireNode.unlitMaterial = unlitMaterial;
        fireNode.litMaterial = litMaterial;
        fireNode.destroyedMaterial = destroyedMaterial;
        fireNode.ignitionResistance = ignitionResistance;
        fireNode.firePower = firePower;
        fireNode.exposureDecayRate = exposureDecayRate;
        fireNode.timeToDestroy = timeToDestroy;
        fireNode.canBeDestroyed = true;
        fireNode.isCritical = false;
        fireNode.burningEffectPrefab = null;
        fireNode.burningEffectLocalOffset = new Vector3(0.0f, 0.35f, 0.0f);
        fireNode.burningEffectLocalScale = Vector3.one * 0.4f;
        fireNode.parentBurningEffectToNode = true;
        fireNode.burningEffectDestroyDelay = 2.0f;
        fireNode.blinkVegetationWhenBurning = false;
        fireNode.vegetationBlinkInterval = 0.25f;
        fireNode.fireIntensity = initialState == FireNodeState.Burning ? 1.0f : 0.0f;

        FireNodeGizmo gizmo = node.AddComponent<FireNodeGizmo>();
        gizmo.radius = 0.8f;
        gizmo.centerSize = 0.08f;
        gizmo.showGizmo = true;
        gizmo.showLabel = true;

        PrefabUtility.SaveAsPrefabAsset(node, $"{NodePrefabFolder}/{prefabName}.prefab");
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
