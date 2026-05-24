using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class TutorialPlayerSetup
{
    private const string TerrainName = "TutorialTerrain";
    private const string MainCameraName = "Main Camera";
    private const string PlayerCameraPrefabPath = "Assets/Prefabs/Player/PlayerCamera.prefab";
    private const string BoundaryRootName = "TutorialBoundaries";
    private const float CameraHeight = 1.0f;

    [MenuItem("Tools/Tutorial/Setup Player Camera")]
    public static void SetupPlayerCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            GameObject cameraObject = GameObject.Find(MainCameraName);
            if (cameraObject == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerCameraPrefabPath);
                cameraObject = prefab != null
                    ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
                    : new GameObject(MainCameraName);
                cameraObject.name = MainCameraName;
            }

            camera = cameraObject.GetComponent<Camera>();
            if (camera == null)
            {
                camera = cameraObject.AddComponent<Camera>();
            }

            cameraObject.tag = "MainCamera";
        }

        GameObject playerObject = camera.gameObject;
        CharacterController characterController = playerObject.GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = playerObject.AddComponent<CharacterController>();
        }

        characterController.height = 1.1f;
        characterController.radius = 0.28f;
        characterController.center = new Vector3(0.0f, -0.48f, 0.0f);
        characterController.stepOffset = 0.3f;
        characterController.slopeLimit = 45.0f;

        PlayerCameraController playerController = playerObject.GetComponent<PlayerCameraController>();
        if (playerController == null)
        {
            playerController = playerObject.AddComponent<PlayerCameraController>();
        }

        playerController.moveSpeed = 4.0f;
        playerController.sprintMultiplier = 1.5f;
        playerController.mouseSensitivity = 2.0f;
        playerController.minPitch = -75.0f;
        playerController.maxPitch = 75.0f;
        playerController.gravity = -20.0f;

        EnsureTerrainCollider();
        EnsureBoundaryWalls();
        EnsureSingleAudioListener(playerObject);
        PositionPlayerAtTerrainStart(playerObject.transform);

        EditorUtility.SetDirty(playerObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Tutorial player camera configured with WASD movement, mouse look, and terrain collision support.");
    }

    private static void EnsureTerrainCollider()
    {
        Terrain terrain = GameObject.Find(TerrainName)?.GetComponent<Terrain>();
        if (terrain == null)
        {
            Debug.LogWarning($"Could not find {TerrainName}. Create the tutorial terrain before testing player collision.");
            return;
        }

        TerrainCollider terrainCollider = terrain.GetComponent<TerrainCollider>();
        if (terrainCollider == null)
        {
            terrainCollider = terrain.gameObject.AddComponent<TerrainCollider>();
        }

        terrainCollider.terrainData = terrain.terrainData;
        EditorUtility.SetDirty(terrain);
        EditorUtility.SetDirty(terrainCollider);
    }

    private static void PositionPlayerAtTerrainStart(Transform playerTransform)
    {
        Terrain terrain = GameObject.Find(TerrainName)?.GetComponent<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            playerTransform.position = new Vector3(0.0f, CameraHeight, -8.0f);
            playerTransform.rotation = Quaternion.identity;
            return;
        }

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrain.terrainData.size;
        Vector3 startPosition = terrainPosition + new Vector3(terrainSize.x * 0.5f, 0.0f, terrainSize.z * 0.25f);
        float groundHeight = terrain.SampleHeight(startPosition) + terrainPosition.y;

        playerTransform.position = new Vector3(startPosition.x, groundHeight + CameraHeight, startPosition.z);
        playerTransform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
    }

    private static void EnsureBoundaryWalls()
    {
        Terrain terrain = GameObject.Find(TerrainName)?.GetComponent<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            Debug.LogWarning($"Could not create tutorial boundaries because {TerrainName} was not found.");
            return;
        }

        GameObject root = GameObject.Find(BoundaryRootName);
        if (root == null)
        {
            root = new GameObject(BoundaryRootName);
        }

        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        }

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float wallHeight = 3.0f;
        float wallThickness = 0.5f;
        float centerY = origin.y + wallHeight * 0.5f;

        CreateBoundaryWall(root.transform, "NorthWall",
            new Vector3(origin.x + size.x * 0.5f, centerY, origin.z + size.z + wallThickness * 0.5f),
            new Vector3(size.x + wallThickness * 2.0f, wallHeight, wallThickness));

        CreateBoundaryWall(root.transform, "SouthWall",
            new Vector3(origin.x + size.x * 0.5f, centerY, origin.z - wallThickness * 0.5f),
            new Vector3(size.x + wallThickness * 2.0f, wallHeight, wallThickness));

        CreateBoundaryWall(root.transform, "EastWall",
            new Vector3(origin.x + size.x + wallThickness * 0.5f, centerY, origin.z + size.z * 0.5f),
            new Vector3(wallThickness, wallHeight, size.z));

        CreateBoundaryWall(root.transform, "WestWall",
            new Vector3(origin.x - wallThickness * 0.5f, centerY, origin.z + size.z * 0.5f),
            new Vector3(wallThickness, wallHeight, size.z));

        EditorUtility.SetDirty(root);
    }

    private static void CreateBoundaryWall(Transform parent, string wallName, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(parent);
        wall.transform.position = position;

        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
    }

    private static void EnsureSingleAudioListener(GameObject playerObject)
    {
        AudioListener playerListener = playerObject.GetComponent<AudioListener>();
        if (playerListener == null)
        {
            playerListener = playerObject.AddComponent<AudioListener>();
        }

        playerListener.enabled = true;
        AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>(true);
        foreach (AudioListener listener in listeners)
        {
            if (listener == null || listener == playerListener)
            {
                continue;
            }

            listener.enabled = false;
            EditorUtility.SetDirty(listener);
        }

        EditorUtility.SetDirty(playerListener);
    }
}
