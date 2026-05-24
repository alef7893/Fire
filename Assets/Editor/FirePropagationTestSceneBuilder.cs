using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FirePropagationTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/FirePropagationTest.unity";
    private const string GraphRootName = "FirePropagationTestGraph";
    private const string NodesRootName = "Nodes";
    private const string EdgesRootName = "Edges";
    private const string SparkPrefabPath = "Assets/Prefabs/FireNodes/FireNode_Spark.prefab";
    private const string SensitivePrefabPath = "Assets/Prefabs/FireNodes/FireNode_Sensitive.prefab";
    private const string EdgePrefabPath = "Assets/Prefabs/FireGraph/FireEdge_Ground_BigSimple.prefab";

    [MenuItem("Tools/Fire Simulation/Create Propagation Test Scene")]
    public static void CreatePropagationTestScene()
    {
        EnsureFolder("Assets/Scenes");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "FirePropagationTest";

        CreateGround();
        CreateLighting();
        CreateCamera();
        CreateTestGraph();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"Created fire propagation test scene at {ScenePath}.");
    }

    private static void CreateTestGraph()
    {
        GameObject graphRoot = new GameObject(GraphRootName);
        FireGraphRoot root = graphRoot.AddComponent<FireGraphRoot>();
        FireSimulationManager manager = graphRoot.AddComponent<FireSimulationManager>();
        manager.graphRoot = root;
        manager.treatConnectionsAsBidirectional = true;
        manager.includeInactiveNodes = true;
        manager.spreadInterval = 1.25f;
        manager.minimumEdgeDistance = 0.1f;
        manager.propagationMultiplier = 0.5f;

        GameObject nodesRoot = new GameObject(NodesRootName);
        nodesRoot.transform.SetParent(graphRoot.transform, false);
        GameObject edgesRoot = new GameObject(EdgesRootName);
        edgesRoot.transform.SetParent(graphRoot.transform, false);
        root.nodesRoot = nodesRoot.transform;
        root.edgesRoot = edgesRoot.transform;

        FireObject spark = CreateNode(nodesRoot.transform, "Spark_Start", SparkPrefabPath, new Vector3(0.0f, 0.25f, -5.0f));
        FireObject nodeA = CreateNode(nodesRoot.transform, "Node_A_Left", SensitivePrefabPath, new Vector3(-3.5f, 0.25f, -2.0f));
        FireObject nodeB = CreateNode(nodesRoot.transform, "Node_B_Left", SensitivePrefabPath, new Vector3(-3.5f, 0.25f, 2.0f));
        FireObject nodeC = CreateNode(nodesRoot.transform, "Node_C_Right", SensitivePrefabPath, new Vector3(3.5f, 0.25f, 2.0f));
        FireObject nodeD = CreateNode(nodesRoot.transform, "Node_D_Right", SensitivePrefabPath, new Vector3(3.5f, 0.25f, -2.0f));

        manager.startingNode = spark;

        CreateEdge(edgesRoot.transform, "Edge_Spark_Left", spark, nodeA);
        CreateEdge(edgesRoot.transform, "Edge_Spark_Right", spark, nodeD);
        CreateEdge(edgesRoot.transform, "Edge_Left_01", nodeA, nodeB);
        CreateEdge(edgesRoot.transform, "Edge_Right_01", nodeD, nodeC);
        CreateEdge(edgesRoot.transform, "Edge_Goal", nodeB, nodeC);

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(graphRoot);
    }

    private static FireObject CreateNode(Transform parent, string name, string prefabPath, Vector3 position)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject node = prefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
            : new GameObject(name);

        node.name = name;
        node.transform.SetParent(parent, true);
        node.transform.position = position;
        node.transform.rotation = Quaternion.identity;

        FireObject fireObject = node.GetComponent<FireObject>();
        if (fireObject == null)
        {
            fireObject = node.AddComponent<FireObject>();
        }

        ConfigureFireObject(fireObject);

        FireGraphIdentity identity = node.GetComponent<FireGraphIdentity>();
        if (identity == null)
        {
            identity = node.AddComponent<FireGraphIdentity>();
        }

        identity.nodeId = name;

        if (node.GetComponent<FireNodeGizmo>() == null)
        {
            node.AddComponent<FireNodeGizmo>();
        }

        EditorUtility.SetDirty(node);
        return fireObject;
    }

    private static void CreateEdge(Transform parent, string name, FireObject source, FireObject target)
    {
        if (source == null || target == null)
        {
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EdgePrefabPath);
        GameObject edgeObject = prefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
            : new GameObject(name);

        edgeObject.name = name;
        edgeObject.transform.SetParent(parent, false);
        FireEdge edge = edgeObject.GetComponent<FireEdge>();
        if (edge == null)
        {
            edge = edgeObject.AddComponent<FireEdge>();
        }

        edge.source = source;
        edge.target = target;
        edge.enabledForPropagation = true;
        edge.propagationCostMultiplier = 2.0f;
        edge.propagationSpeed = 1.0f;
        EditorUtility.SetDirty(edgeObject);
    }

    private static void ConfigureFireObject(FireObject fireObject)
    {
        if (fireObject.nodeType == FireNodeType.Spark)
        {
            fireObject.firePower = 4.0f;
            fireObject.timeToDestroy = 10.0f;
            fireObject.exposureDecayRate = 0.0f;
            return;
        }

        fireObject.ignitionResistance = 1.2f;
        fireObject.firePower = 4.0f;
        fireObject.exposureDecayRate = 0.05f;
        fireObject.timeToDestroy = 10.0f;
    }

    private static void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "TestGround_20x20";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(2.0f, 1.0f, 2.0f);
    }

    private static void CreateLighting()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.0f;
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(50.0f, -30.0f, 0.0f);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0.0f, 8.0f, -11.0f);
        cameraObject.transform.rotation = Quaternion.Euler(55.0f, 0.0f, 0.0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        cameraObject.AddComponent<AudioListener>();
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
