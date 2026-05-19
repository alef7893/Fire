using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TutorialTerrainBuilder
{
    private const string TerrainFolder = "Assets/Tutorial/Terrain";
    private const string TerrainDataPath = TerrainFolder + "/TutorialTerrainData.asset";
    private const string TerrainObjectName = "TutorialTerrain";
    private const string EnvironmentRootName = "TutorialEnvironment";

    private const float TerrainSize = 20.0f;
    private const float TerrainHeight = 5.0f;

    private const string MossLayerPath = "Assets/ImportedPackages/Supercyan Free Forest Sample/TerrainLayers/forestpack_moss_light_terrainlayer.terrainlayer";
    private const string RoadLayerPath = "Assets/ImportedPackages/Supercyan Free Forest Sample/TerrainLayers/forestpack_road_terrailayer.terrainlayer";
    private const string RockLayerPath = "Assets/ImportedPackages/Supercyan Free Forest Sample/TerrainLayers/forestpack_rock_terrainlayer.terrainlayer";

    private static readonly string[] TreePrefabPaths =
    {
        "Assets/ImportedPackages/Supercyan Free Forest Sample/Prefabs/Mobile/Tree/Fir/Mobile_forestpack_tree_fir_tall.prefab",
        "Assets/ImportedPackages/Supercyan Free Forest Sample/Prefabs/Mobile/Tree/Leaf/Normal/Mobile_forestpack_tree_1_leaf_1.prefab"
    };

    private static readonly string[] GroundDetailPrefabPaths =
    {
        "Assets/ImportedPackages/Supercyan Free Forest Sample/Prefabs/Mobile/Foliage/Grass/Mobile_forestpack_foliage_grassPatch_small_1.prefab",
        "Assets/ImportedPackages/Supercyan Free Forest Sample/Prefabs/Mobile/Foliage/Grass/Mobile_forestpack_foliage_grassPatch_small_2.prefab",
        "Assets/ImportedPackages/Supercyan Free Forest Sample/Prefabs/Mobile/Stone/Mobile_forestpack_stone_medium_1.prefab",
        "Assets/ImportedPackages/Supercyan Free Forest Sample/Prefabs/Mobile/Tree/Treestump/Mobile_forestpack_tree_stump_1.prefab"
    };

    [MenuItem("Tools/Tutorial/Create Forest Terrain")]
    public static void CreateForestTerrain()
    {
        EnsureFolder(TerrainFolder);

        TerrainLayer moss = AssetDatabase.LoadAssetAtPath<TerrainLayer>(MossLayerPath);
        TerrainLayer road = AssetDatabase.LoadAssetAtPath<TerrainLayer>(RoadLayerPath);
        TerrainLayer rock = AssetDatabase.LoadAssetAtPath<TerrainLayer>(RockLayerPath);
        if (moss == null || road == null || rock == null)
        {
            Debug.LogError("Missing one or more Supercyan terrain layers. Reimport the forest package and try again.");
            return;
        }

        Vector3 center = GetFireGraphCenter();
        TerrainData terrainData = GetOrCreateTerrainData();
        ConfigureTerrainData(terrainData, moss, road, rock);

        Terrain terrain = GetOrCreateTerrain(terrainData);
        terrain.transform.position = new Vector3(center.x - TerrainSize * 0.5f, 0.0f, center.z - TerrainSize * 0.5f);
        ConfigureTerrainRuntimeSettings(terrain);

        GameObject environmentRoot = GetOrCreateRoot(EnvironmentRootName);
        ClearGeneratedChildren(environmentRoot.transform);
        PlaceForestProps(environmentRoot.transform, terrain.transform.position);

        Selection.activeGameObject = terrain.gameObject;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Tutorial forest terrain created. It uses Supercyan terrain layers and mobile forest prefabs.");
    }

    private static TerrainData GetOrCreateTerrainData()
    {
        TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
        if (terrainData != null)
        {
            return terrainData;
        }

        terrainData = new TerrainData();
        AssetDatabase.CreateAsset(terrainData, TerrainDataPath);
        return terrainData;
    }

    private static void ConfigureTerrainData(TerrainData terrainData, TerrainLayer moss, TerrainLayer road, TerrainLayer rock)
    {
        terrainData.heightmapResolution = 65;
        terrainData.alphamapResolution = 128;
        terrainData.baseMapResolution = 256;
        terrainData.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
        terrainData.terrainLayers = new[] { moss, road, rock };

        int heightResolution = terrainData.heightmapResolution;
        float[,] heights = new float[heightResolution, heightResolution];
        terrainData.SetHeights(0, 0, heights);

        PaintTerrain(terrainData);
        EditorUtility.SetDirty(terrainData);
    }

    private static void PaintTerrain(TerrainData terrainData)
    {
        int resolution = terrainData.alphamapResolution;
        int layerCount = terrainData.alphamapLayers;
        float[,,] alphas = new float[resolution, resolution, layerCount];

        for (int y = 0; y < resolution; y++)
        {
            float normalizedY = y / (float)(resolution - 1);
            for (int x = 0; x < resolution; x++)
            {
                float normalizedX = x / (float)(resolution - 1);
                float roadDistance = Mathf.Abs(normalizedX - 0.5f);
                float rockDistance = Mathf.Min(normalizedX, 1.0f - normalizedX);

                float roadWeight = Mathf.Clamp01(1.0f - roadDistance / 0.08f);
                float rockWeight = Mathf.Clamp01(1.0f - rockDistance / 0.12f) * 0.35f;
                float mossWeight = Mathf.Max(0.0f, 1.0f - roadWeight - rockWeight);

                float total = mossWeight + roadWeight + rockWeight;
                alphas[y, x, 0] = mossWeight / total;
                alphas[y, x, 1] = roadWeight / total;
                alphas[y, x, 2] = rockWeight / total;

                if (normalizedY < 0.08f || normalizedY > 0.92f)
                {
                    alphas[y, x, 0] = 0.7f;
                    alphas[y, x, 1] = 0.0f;
                    alphas[y, x, 2] = 0.3f;
                }
            }
        }

        terrainData.SetAlphamaps(0, 0, alphas);
    }

    private static Terrain GetOrCreateTerrain(TerrainData terrainData)
    {
        GameObject terrainObject = GameObject.Find(TerrainObjectName);
        if (terrainObject == null)
        {
            terrainObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainObject.name = TerrainObjectName;
        }

        Terrain terrain = terrainObject.GetComponent<Terrain>();
        TerrainCollider collider = terrainObject.GetComponent<TerrainCollider>();
        if (terrain == null)
        {
            terrain = terrainObject.AddComponent<Terrain>();
        }

        if (collider == null)
        {
            collider = terrainObject.AddComponent<TerrainCollider>();
        }

        terrain.terrainData = terrainData;
        collider.terrainData = terrainData;
        return terrain;
    }

    private static void ConfigureTerrainRuntimeSettings(Terrain terrain)
    {
        terrain.drawInstanced = true;
        terrain.heightmapPixelError = 20.0f;
        terrain.basemapDistance = 250.0f;
        terrain.treeDistance = 80.0f;
        terrain.detailObjectDistance = 35.0f;
        terrain.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        terrain.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    private static GameObject GetOrCreateRoot(string objectName)
    {
        GameObject root = GameObject.Find(objectName);
        if (root != null)
        {
            return root;
        }

        root = new GameObject(objectName);
        return root;
    }

    private static void ClearGeneratedChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(root.GetChild(i).gameObject);
        }
    }

    private static void PlaceForestProps(Transform root, Vector3 terrainOrigin)
    {
        GameObject treesRoot = CreateChildRoot(root, "Trees");
        GameObject detailsRoot = CreateChildRoot(root, "GroundDetails");

        Vector3[] treePositions =
        {
            new Vector3(2.0f, 0.0f, 2.5f),
            new Vector3(4.0f, 0.0f, 16.5f),
            new Vector3(16.0f, 0.0f, 3.0f),
            new Vector3(17.5f, 0.0f, 17.0f),
            new Vector3(2.5f, 0.0f, 10.0f),
            new Vector3(17.0f, 0.0f, 10.5f)
        };

        for (int i = 0; i < treePositions.Length; i++)
        {
            string prefabPath = TreePrefabPaths[i % TreePrefabPaths.Length];
            InstantiatePrefab(prefabPath, treesRoot.transform, terrainOrigin + treePositions[i], 35.0f * i, Vector3.one * 1.15f);
        }

        Vector3[] detailPositions =
        {
            new Vector3(5.0f, 0.0f, 4.0f),
            new Vector3(7.0f, 0.0f, 6.0f),
            new Vector3(13.0f, 0.0f, 5.0f),
            new Vector3(15.5f, 0.0f, 8.0f),
            new Vector3(4.0f, 0.0f, 13.0f),
            new Vector3(7.5f, 0.0f, 16.0f),
            new Vector3(13.0f, 0.0f, 15.0f),
            new Vector3(16.0f, 0.0f, 13.0f)
        };

        for (int i = 0; i < detailPositions.Length; i++)
        {
            string prefabPath = GroundDetailPrefabPaths[i % GroundDetailPrefabPaths.Length];
            float scale = i % 3 == 0 ? 0.8f : 1.0f;
            InstantiatePrefab(prefabPath, detailsRoot.transform, terrainOrigin + detailPositions[i], 47.0f * i, Vector3.one * scale);
        }
    }

    private static GameObject CreateChildRoot(Transform parent, string objectName)
    {
        GameObject child = new GameObject(objectName);
        child.transform.SetParent(parent);
        child.transform.localPosition = Vector3.zero;
        return child;
    }

    private static void InstantiatePrefab(string prefabPath, Transform parent, Vector3 position, float yaw, Vector3 scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"Could not load forest prefab at {prefabPath}.");
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0.0f, yaw, 0.0f);
        instance.transform.localScale = scale;
    }

    private static Vector3 GetFireGraphCenter()
    {
        FireObject[] fireObjects = Object.FindObjectsOfType<FireObject>();
        if (fireObjects.Length == 0)
        {
            return Vector3.zero;
        }

        Vector3 center = Vector3.zero;
        foreach (FireObject fireObject in fireObjects)
        {
            center += fireObject.transform.position;
        }

        return center / fireObjects.Length;
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
