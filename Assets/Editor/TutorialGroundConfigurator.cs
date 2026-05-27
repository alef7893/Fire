using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TutorialGroundConfigurator
{
    private const string TutorialScenePath = "Assets/Scenes/Tutorial.unity";
    private const string TerrainName = "TutorialTerrain";
    private const float TerrainSize = 25.0f;
    private const float BoundaryOffset = 0.25f;
    private const float BoundaryHeight = 3.0f;
    private const float BoundaryThickness = 0.5f;

    [MenuItem("Tools/Tutorial/Configure Flat 25x25 Ground")]
    public static void ConfigureFlat25x25Ground()
    {
        Scene scene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
        Terrain terrain = FindTerrain(scene);

        if (terrain == null)
        {
            Debug.LogError($"Could not find terrain '{TerrainName}' in {TutorialScenePath}.");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        if (terrainData == null)
        {
            Debug.LogError($"Terrain '{TerrainName}' does not have TerrainData assigned.");
            return;
        }

        Vector3 previousSize = terrainData.size;
        int resolution = terrainData.heightmapResolution;
        float[,] flatHeights = new float[resolution, resolution];

        terrainData.SetHeights(0, 0, flatHeights);
        terrainData.size = new Vector3(TerrainSize, Mathf.Max(1.0f, previousSize.y), TerrainSize);
        EditorUtility.SetDirty(terrainData);

        terrain.transform.position = new Vector3(-TerrainSize * 0.5f, 0.0f, -TerrainSize * 0.5f);
        EditorUtility.SetDirty(terrain);

        ConfigureBoundary("NorthWall", new Vector3(0.0f, BoundaryHeight * 0.5f, TerrainSize * 0.5f + BoundaryOffset), new Vector3(TerrainSize + 1.0f, BoundaryHeight, BoundaryThickness));
        ConfigureBoundary("SouthWall", new Vector3(0.0f, BoundaryHeight * 0.5f, -TerrainSize * 0.5f - BoundaryOffset), new Vector3(TerrainSize + 1.0f, BoundaryHeight, BoundaryThickness));
        ConfigureBoundary("EastWall", new Vector3(TerrainSize * 0.5f + BoundaryOffset, BoundaryHeight * 0.5f, 0.0f), new Vector3(BoundaryThickness, BoundaryHeight, TerrainSize));
        ConfigureBoundary("WestWall", new Vector3(-TerrainSize * 0.5f - BoundaryOffset, BoundaryHeight * 0.5f, 0.0f), new Vector3(BoundaryThickness, BoundaryHeight, TerrainSize));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"Configured {TerrainName}: flattened heightmap, size {TerrainSize}x{TerrainSize}, centered at world origin. Previous TerrainData size was {previousSize}.");
    }

    private static Terrain FindTerrain(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Terrain terrain in root.GetComponentsInChildren<Terrain>(true))
            {
                if (terrain.name == TerrainName)
                {
                    return terrain;
                }
            }
        }

        return null;
    }

    private static void ConfigureBoundary(string objectName, Vector3 position, Vector3 colliderSize)
    {
        GameObject boundary = GameObject.Find(objectName);
        if (boundary == null)
        {
            Debug.LogWarning($"Boundary '{objectName}' was not found.");
            return;
        }

        boundary.transform.position = position;

        BoxCollider collider = boundary.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = boundary.AddComponent<BoxCollider>();
        }

        collider.size = colliderSize;
        collider.center = Vector3.zero;

        EditorUtility.SetDirty(boundary);
        EditorUtility.SetDirty(collider);
    }
}
