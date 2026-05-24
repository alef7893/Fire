using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TutorialForestTerrainBuilder
{
    private const string ScenePath = "Assets/Scenes/Tutorial.unity";
    private const string TerrainName = "TutorialTerrain";
    private const string TerrainDataPath = "Assets/Tutorial/Terrain/TutorialTerrainData.asset";
    private const string BoundaryRootName = "TutorialBoundaries";
    private const string VisualBoundaryRootName = "TutorialVisualBoundaries";
    private const string GroundDetailsRootName = "GroundDetails";

    private const string GrassLayerPath = "Assets/ImportedPackages/Polytope Studio/Lowpoly_Demos/Environment_Free/Helpers/Ground_Layer_02.terrainlayer";
    private const string DirtLayerPath = "Assets/ImportedPackages/Polytope Studio/Lowpoly_Demos/Environment_Free/Helpers/Ground_Layer_01.terrainlayer";
    private const string TerrainMaterialPath = "Assets/ImportedPackages/Polytope Studio/Lowpoly_Environments/Sources/Materials/PT_Terrain_mat.mat";

    private const string RockPrefabPath = "Assets/ImportedPackages/LowPolyRockPack/Prefabs/Rock Type1 01.prefab";
    private const string LogPrefabPath = "Assets/ImportedPackages/Polytope Studio/Lowpoly_Environments/Prefabs/Trees/PT_Pine_Tree_03_logs.prefab";
    private const string GrassPrefabPath = "Assets/ImportedPackages/Polytope Studio/Lowpoly_Environments/Prefabs/Plants/PT_Grass_02.prefab";

    private const float TerrainSize = 30.0f;
    private const float TerrainHeight = 3.0f;
    private static readonly Vector2 CampCenter = new Vector2(0.0f, -8.0f);
    private const float CampHalfSize = 4.0f;

    [MenuItem("Tools/Tutorial/Build Forest Terrain")]
    public static void BuildForestTerrain()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        Terrain terrain = EnsureTerrain();
        ConfigureTerrainData(terrain);
        ConfigureTerrainObject(terrain);
        EnsureInvisibleBoundaries();
        BuildVisualBoundaries();
        BuildGroundDetails();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Tutorial forest terrain rebuilt with a flat 8x8 camp clearing, grass terrain, and visual boundary props.");
    }

    private static Terrain EnsureTerrain()
    {
        GameObject terrainObject = GameObject.Find(TerrainName);
        if (terrainObject == null)
        {
            terrainObject = Terrain.CreateTerrainGameObject(null);
            terrainObject.name = TerrainName;
        }

        Terrain terrain = terrainObject.GetComponent<Terrain>();
        TerrainCollider terrainCollider = terrainObject.GetComponent<TerrainCollider>();
        if (terrainCollider == null)
        {
            terrainCollider = terrainObject.AddComponent<TerrainCollider>();
        }

        TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
        if (terrainData == null)
        {
            terrainData = new TerrainData();
            AssetDatabase.CreateAsset(terrainData, TerrainDataPath);
        }

        terrain.terrainData = terrainData;
        terrainCollider.terrainData = terrainData;
        return terrain;
    }

    private static void ConfigureTerrainData(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        data.heightmapResolution = 129;
        data.alphamapResolution = 128;
        data.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);

        TerrainLayer grassLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(GrassLayerPath);
        TerrainLayer dirtLayer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(DirtLayerPath);
        if (grassLayer != null && dirtLayer != null)
        {
            data.terrainLayers = new[] { grassLayer, dirtLayer };
        }

        ApplyHeights(data);
        ApplyTextures(data);
        EditorUtility.SetDirty(data);
    }

    private static void ApplyHeights(TerrainData data)
    {
        int resolution = data.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float worldX = Mathf.Lerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, x / (float)(resolution - 1));
                float worldZ = Mathf.Lerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, y / (float)(resolution - 1));
                float edgeDistance = Mathf.Min(
                    worldX + TerrainSize * 0.5f,
                    TerrainSize * 0.5f - worldX,
                    worldZ + TerrainSize * 0.5f,
                    TerrainSize * 0.5f - worldZ);

                float edgeRise = Mathf.SmoothStep(0.35f, 0.0f, Mathf.Clamp01(edgeDistance / 3.2f)) * 0.32f;
                float noise = Mathf.PerlinNoise((worldX + 41.0f) * 0.16f, (worldZ - 19.0f) * 0.16f) * 0.055f;
                float broadNoise = Mathf.PerlinNoise((worldX - 13.0f) * 0.055f, (worldZ + 7.0f) * 0.055f) * 0.07f;
                float height = edgeRise + noise + broadNoise;

                float campBlend = GetCampBlend(worldX, worldZ);
                height = Mathf.Lerp(height, 0.0f, campBlend);
                heights[y, x] = Mathf.Clamp01(height);
            }
        }

        data.SetHeights(0, 0, heights);
    }

    private static void ApplyTextures(TerrainData data)
    {
        if (data.terrainLayers == null || data.terrainLayers.Length < 2)
        {
            return;
        }

        int width = data.alphamapWidth;
        int height = data.alphamapHeight;
        float[,,] maps = new float[height, width, 2];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float worldX = Mathf.Lerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, x / (float)(width - 1));
                float worldZ = Mathf.Lerp(-TerrainSize * 0.5f, TerrainSize * 0.5f, y / (float)(height - 1));
                float campBlend = GetCampBlend(worldX, worldZ);
                float pathNoise = Mathf.PerlinNoise((worldX + 2.0f) * 0.35f, (worldZ - 3.0f) * 0.35f) * 0.15f;
                float dirt = Mathf.Clamp01(campBlend + pathNoise * (1.0f - campBlend) * 0.35f);

                maps[y, x, 0] = 1.0f - dirt;
                maps[y, x, 1] = dirt;
            }
        }

        data.SetAlphamaps(0, 0, maps);
    }

    private static float GetCampBlend(float worldX, float worldZ)
    {
        float dx = Mathf.Abs(worldX - CampCenter.x);
        float dz = Mathf.Abs(worldZ - CampCenter.y);
        float outside = Mathf.Max(dx - CampHalfSize, dz - CampHalfSize);
        return 1.0f - Mathf.Clamp01(outside / 2.0f);
    }

    private static void ConfigureTerrainObject(Terrain terrain)
    {
        terrain.transform.position = new Vector3(-TerrainSize * 0.5f, 0.0f, -TerrainSize * 0.5f);
        terrain.drawInstanced = true;
        terrain.heightmapPixelError = 5.0f;
        terrain.basemapDistance = 80.0f;

        Material terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(TerrainMaterialPath);
        if (terrainMaterial != null)
        {
            terrain.materialTemplate = terrainMaterial;
        }

        TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
        if (collider != null)
        {
            collider.terrainData = terrain.terrainData;
        }

        EditorUtility.SetDirty(terrain);
    }

    private static void EnsureInvisibleBoundaries()
    {
        GameObject root = GameObject.Find(BoundaryRootName);
        if (root == null)
        {
            root = new GameObject(BoundaryRootName);
        }

        ClearChildren(root.transform);

        const float wallHeight = 3.0f;
        const float wallThickness = 0.5f;
        float half = TerrainSize * 0.5f;
        float centerY = wallHeight * 0.5f;

        CreateColliderWall(root.transform, "NorthWall", new Vector3(0.0f, centerY, half + wallThickness * 0.5f), new Vector3(TerrainSize + wallThickness * 2.0f, wallHeight, wallThickness));
        CreateColliderWall(root.transform, "SouthWall", new Vector3(0.0f, centerY, -half - wallThickness * 0.5f), new Vector3(TerrainSize + wallThickness * 2.0f, wallHeight, wallThickness));
        CreateColliderWall(root.transform, "EastWall", new Vector3(half + wallThickness * 0.5f, centerY, 0.0f), new Vector3(wallThickness, wallHeight, TerrainSize));
        CreateColliderWall(root.transform, "WestWall", new Vector3(-half - wallThickness * 0.5f, centerY, 0.0f), new Vector3(wallThickness, wallHeight, TerrainSize));
    }

    private static void BuildVisualBoundaries()
    {
        GameObject root = GameObject.Find(VisualBoundaryRootName);
        if (root == null)
        {
            root = new GameObject(VisualBoundaryRootName);
        }

        ClearChildren(root.transform);

        GameObject rockPrefab = LoadFirstAvailable(RockPrefabPath);
        GameObject logPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LogPrefabPath);

        const float half = TerrainSize * 0.5f;
        const float inset = 0.95f;

        for (int i = 0; i < 12; i++)
        {
            float t = -13.2f + i * 2.4f;
            PlaceProp(root.transform, rockPrefab, new Vector3(t, 0.0f, half - inset), 0.8f + (i % 3) * 0.18f, 20.0f + i * 31.0f, true);
            PlaceProp(root.transform, rockPrefab, new Vector3(t, 0.0f, -half + inset), 0.72f + (i % 4) * 0.14f, -10.0f + i * 27.0f, true);
            PlaceProp(root.transform, rockPrefab, new Vector3(half - inset, 0.0f, t), 0.78f + (i % 3) * 0.16f, 55.0f + i * 21.0f, true);
            PlaceProp(root.transform, rockPrefab, new Vector3(-half + inset, 0.0f, t), 0.74f + (i % 4) * 0.13f, 100.0f + i * 23.0f, true);
        }

        if (logPrefab != null)
        {
            PlaceProp(root.transform, logPrefab, new Vector3(-8.0f, 0.0f, -13.6f), 0.9f, 80.0f, true);
            PlaceProp(root.transform, logPrefab, new Vector3(8.5f, 0.0f, -13.4f), 0.85f, 105.0f, true);
            PlaceProp(root.transform, logPrefab, new Vector3(-13.5f, 0.0f, 7.0f), 0.85f, 8.0f, true);
            PlaceProp(root.transform, logPrefab, new Vector3(13.4f, 0.0f, 6.5f), 0.9f, -12.0f, true);
        }
    }

    private static void BuildGroundDetails()
    {
        GameObject root = GameObject.Find(GroundDetailsRootName);
        if (root == null)
        {
            root = new GameObject(GroundDetailsRootName);
        }

        ClearChildren(root.transform);

        GameObject grassPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GrassPrefabPath);
        if (grassPrefab == null)
        {
            return;
        }

        for (int x = -12; x <= 12; x += 4)
        {
            for (int z = -12; z <= 12; z += 4)
            {
                if (GetCampBlend(x, z) > 0.2f)
                {
                    continue;
                }

                float jitterX = ((x * 37 + z * 11) % 17 - 8) * 0.08f;
                float jitterZ = ((x * 13 + z * 29) % 19 - 9) * 0.08f;
                PlaceProp(root.transform, grassPrefab, new Vector3(x + jitterX, 0.0f, z + jitterZ), 0.85f, x * 17.0f + z * 9.0f, false);
            }
        }
    }

    private static void PlaceProp(Transform parent, GameObject prefab, Vector3 position, float scale, float yaw, bool ensureCollider)
    {
        if (prefab == null)
        {
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0.0f, yaw, 0.0f);
        instance.transform.localScale = Vector3.one * scale;

        if (ensureCollider && instance.GetComponentInChildren<Collider>() == null)
        {
            BoxCollider collider = instance.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.4f, 1.4f, 1.4f);
            collider.center = new Vector3(0.0f, 0.7f, 0.0f);
        }
    }

    private static void CreateColliderWall(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.position = position;

        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
    }

    private static GameObject LoadFirstAvailable(params string[] paths)
    {
        foreach (string path in paths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                return prefab;
            }
        }

        return null;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
        }
    }
}
