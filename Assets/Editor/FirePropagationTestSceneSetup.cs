using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FirePropagationTestSceneSetup
{
    private const string ScenePath = "Assets/Scenes/FirePropagationTest.unity";
    private const string PlayerCameraPrefabPath = "Assets/Prefabs/Player/PlayerCamera.prefab";
    private const string GroundName = "TestGround_20x20";
    private const string BoundaryRootName = "TestBoundaries";
    private const string PlayerName = "PlayerCamera";
    private const float NodeHeightAboveGround = 0.05f;
    private const float PlayerHeightAboveGround = 1.0f;

    [MenuItem("Tools/Fire Simulation/Setup Propagation Test Player And Colliders")]
    public static void SetupPropagationTestScene()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject ground = GameObject.Find(GroundName);
        if (ground == null)
        {
            Debug.LogError($"Could not find {GroundName} in {ScenePath}.");
            return;
        }

        Collider groundCollider = EnsureGroundCollider(ground);
        Bounds groundBounds = GetGroundBounds(ground, groundCollider);
        float groundY = groundBounds.max.y;

        AdjustFireNodeHeights(groundY);
        EnsurePlayer(groundY, groundBounds);
        EnsureBoundaryWalls(groundBounds);
        EnsureSingleAudioListener();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.Refresh();

        Debug.Log("Fire propagation test scene configured with ground collider, player camera, node heights, and invisible boundaries.");
    }

    private static Collider EnsureGroundCollider(GameObject ground)
    {
        Collider collider = ground.GetComponent<Collider>();
        if (collider != null)
        {
            EditorUtility.SetDirty(collider);
            return collider;
        }

        MeshFilter meshFilter = ground.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            MeshCollider meshCollider = ground.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
            EditorUtility.SetDirty(meshCollider);
            return meshCollider;
        }

        BoxCollider boxCollider = ground.AddComponent<BoxCollider>();
        EditorUtility.SetDirty(boxCollider);
        return boxCollider;
    }

    private static Bounds GetGroundBounds(GameObject ground, Collider collider)
    {
        if (collider != null)
        {
            return collider.bounds;
        }

        Renderer renderer = ground.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }

        return new Bounds(ground.transform.position, new Vector3(20.0f, 0.1f, 20.0f));
    }

    private static void AdjustFireNodeHeights(float groundY)
    {
        FireNode[] fireObjects = Object.FindObjectsOfType<FireNode>(true);
        foreach (FireNode fireNode in fireObjects)
        {
            if (fireNode == null)
            {
                continue;
            }

            Transform nodeTransform = fireNode.transform;
            Vector3 position = nodeTransform.position;
            nodeTransform.position = new Vector3(position.x, groundY + NodeHeightAboveGround, position.z);
            EditorUtility.SetDirty(nodeTransform);
            EditorUtility.SetDirty(fireNode);
        }
    }

    private static void EnsurePlayer(float groundY, Bounds groundBounds)
    {
        GameObject existingPlayer = GameObject.Find(PlayerName);
        if (existingPlayer == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerCameraPrefabPath);
            existingPlayer = prefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
                : new GameObject(PlayerName);

            existingPlayer.name = PlayerName;
        }

        DisableOtherCameras(existingPlayer);

        Camera camera = existingPlayer.GetComponent<Camera>();
        if (camera == null)
        {
            camera = existingPlayer.AddComponent<Camera>();
        }

        existingPlayer.tag = "MainCamera";
        existingPlayer.transform.position = new Vector3(groundBounds.center.x, groundY + PlayerHeightAboveGround, groundBounds.min.z + 2.5f);
        existingPlayer.transform.rotation = Quaternion.Euler(5.0f, 0.0f, 0.0f);

        CharacterController characterController = existingPlayer.GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = existingPlayer.AddComponent<CharacterController>();
        }

        characterController.height = 1.1f;
        characterController.radius = 0.28f;
        characterController.center = new Vector3(0.0f, -0.48f, 0.0f);
        characterController.stepOffset = 0.3f;
        characterController.slopeLimit = 45.0f;

        PlayerCameraController controller = existingPlayer.GetComponent<PlayerCameraController>();
        if (controller == null)
        {
            controller = existingPlayer.AddComponent<PlayerCameraController>();
        }

        controller.moveSpeed = 4.0f;
        controller.sprintMultiplier = 1.5f;
        controller.mouseSensitivity = 2.0f;
        controller.minPitch = -75.0f;
        controller.maxPitch = 75.0f;
        controller.gravity = -20.0f;

        EditorUtility.SetDirty(existingPlayer);
        EditorUtility.SetDirty(camera);
        EditorUtility.SetDirty(characterController);
        EditorUtility.SetDirty(controller);
    }

    private static void DisableOtherCameras(GameObject playerObject)
    {
        Camera[] cameras = Object.FindObjectsOfType<Camera>(true);
        foreach (Camera camera in cameras)
        {
            if (camera == null || camera.gameObject == playerObject)
            {
                continue;
            }

            camera.enabled = false;
            if (camera.CompareTag("MainCamera"))
            {
                camera.tag = "Untagged";
            }

            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(camera.gameObject);
        }
    }

    private static void EnsureBoundaryWalls(Bounds groundBounds)
    {
        GameObject root = GameObject.Find(BoundaryRootName);
        if (root == null)
        {
            root = new GameObject(BoundaryRootName);
        }

        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        }

        float wallHeight = 3.0f;
        float wallThickness = 0.5f;
        float centerY = groundBounds.min.y + wallHeight * 0.5f;

        CreateBoundaryWall(root.transform, "NorthWall",
            new Vector3(groundBounds.center.x, centerY, groundBounds.max.z + wallThickness * 0.5f),
            new Vector3(groundBounds.size.x + wallThickness * 2.0f, wallHeight, wallThickness));

        CreateBoundaryWall(root.transform, "SouthWall",
            new Vector3(groundBounds.center.x, centerY, groundBounds.min.z - wallThickness * 0.5f),
            new Vector3(groundBounds.size.x + wallThickness * 2.0f, wallHeight, wallThickness));

        CreateBoundaryWall(root.transform, "EastWall",
            new Vector3(groundBounds.max.x + wallThickness * 0.5f, centerY, groundBounds.center.z),
            new Vector3(wallThickness, wallHeight, groundBounds.size.z));

        CreateBoundaryWall(root.transform, "WestWall",
            new Vector3(groundBounds.min.x - wallThickness * 0.5f, centerY, groundBounds.center.z),
            new Vector3(wallThickness, wallHeight, groundBounds.size.z));

        EditorUtility.SetDirty(root);
    }

    private static void CreateBoundaryWall(Transform parent, string wallName, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(wallName);
        wall.transform.SetParent(parent, false);
        wall.transform.position = position;

        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;

        EditorUtility.SetDirty(wall);
        EditorUtility.SetDirty(collider);
    }

    private static void EnsureSingleAudioListener()
    {
        AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>(true);
        AudioListener activeListener = null;

        GameObject playerObject = GameObject.Find(PlayerName);
        if (playerObject != null)
        {
            activeListener = playerObject.GetComponent<AudioListener>();
            if (activeListener == null)
            {
                activeListener = playerObject.AddComponent<AudioListener>();
            }
        }

        foreach (AudioListener listener in listeners)
        {
            if (listener == null)
            {
                continue;
            }

            listener.enabled = listener == activeListener;
            EditorUtility.SetDirty(listener);
        }

        if (activeListener != null)
        {
            activeListener.enabled = true;
            EditorUtility.SetDirty(activeListener);
        }
    }
}
