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
        edge.frontFireEffectPrefab = groundFireEffectPrefab;
        edge.groundFirePatchPrefab = groundFireEffectPrefab;
        edge.nodeArrivalEffectPrefab = nodeArrivalEffectPrefab;
        edge.fireEffectLocalOffset = new Vector3(0.0f, 0.25f, 0.0f);
        edge.fireEffectLocalScale = Vector3.one * 0.7f;
        edge.firePatchLocalScale = Vector3.one * 0.6f;
        edge.nodeArrivalEffectLocalScale = Vector3.one * 0.35f;
        edge.alignEffectToEdge = true;
        edge.muteFirePatchAudio = true;
        edge.firePatchSpacing = 0.85f;
        edge.firePatchLifetime = 18.0f;
        edge.nodeArrivalEffectLifetime = 2.0f;
        edge.effectDestroyDelay = 2.0f;
        edge.showGizmo = true;
        edge.edgeColor = new Color(1.0f, 0.55f, 0.05f, 0.9f);
        edge.midpointSize = 0.12f;

        PrefabUtility.SaveAsPrefabAsset(edgeObject, $"{GraphPrefabFolder}/FireEdge_Ground_BigSimple.prefab");
        Object.DestroyImmediate(edgeObject);
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
        fireObject.canBeDestroyed = true;
        fireObject.isCritical = false;
        fireObject.burningEffectPrefab = null;
        fireObject.burningEffectLocalOffset = new Vector3(0.0f, 0.35f, 0.0f);
        fireObject.burningEffectLocalScale = Vector3.one * 0.4f;
        fireObject.parentBurningEffectToNode = true;
        fireObject.burningEffectDestroyDelay = 2.0f;
        fireObject.blinkVegetationWhenBurning = false;
        fireObject.vegetationBlinkInterval = 0.25f;
        fireObject.fireIntensity = initialState == FireNodeState.Burning ? 1.0f : 0.0f;

        FireGraphIdentity identity = node.AddComponent<FireGraphIdentity>();
        identity.nodeId = prefabName;

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
