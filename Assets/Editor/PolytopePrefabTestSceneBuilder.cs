using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PolytopePrefabTestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/PolytopePrefabTest.unity";
    private const string PlayerCameraPrefabPath = "Assets/Prefabs/Player/PlayerCamera.prefab";
    private const string GrassTexturePath = "Assets/ImportedPackages/Polytope Studio/Lowpoly_Environments/Sources/Textures/PT_Ground_Grass_Green_01.png";
    private const string GroundMaterialPath = "Assets/Materials/PolytopeGrassGround.mat";

    [MenuItem("Tools/Polytope Studio/Create Prefab Test Scene")]
    public static void CreatePrefabTestScene()
    {
        EnsureFolder("Assets/Materials");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "PolytopePrefabTest";

        Material grassMaterial = EnsureGrassGroundMaterial();
        CreateGround(grassMaterial);
        CreateBoundaries();
        CreatePlayer();
        CreateLighting();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"Created Polytope prefab test scene at {ScenePath}.");
    }

    private static Material EnsureGrassGroundMaterial()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        if (urpLit == null)
        {
            Debug.LogError("Could not find Universal Render Pipeline/Lit.");
            return null;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(GroundMaterialPath);
        if (material == null)
        {
            material = new Material(urpLit);
            AssetDatabase.CreateAsset(material, GroundMaterialPath);
        }

        Texture grassTexture = AssetDatabase.LoadAssetAtPath<Texture>(GrassTexturePath);
        material.shader = urpLit;
        material.name = "PolytopeGrassGround";
        material.SetTexture("_BaseMap", grassTexture);
        material.SetTexture("_MainTex", grassTexture);
        material.SetColor("_BaseColor", Color.white);
        material.SetColor("_Color", Color.white);
        material.SetFloat("_Metallic", 0.0f);
        material.SetFloat("_Smoothness", 0.15f);
        material.mainTextureScale = new Vector2(3.0f, 3.0f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssets();

        return material;
    }

    private static void CreateGround(Material grassMaterial)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "GrassGround_30x30";
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(3.0f, 1.0f, 3.0f);

        MeshRenderer renderer = ground.GetComponent<MeshRenderer>();
        if (renderer != null && grassMaterial != null)
        {
            renderer.sharedMaterial = grassMaterial;
        }
    }

    private static void CreateBoundaries()
    {
        GameObject root = new GameObject("InvisibleBoundaries");
        const float halfSize = 15.0f;
        const float wallHeight = 3.0f;
        const float wallThickness = 0.5f;
        float centerY = wallHeight * 0.5f;

        CreateBoundaryWall(root.transform, "NorthWall",
            new Vector3(0.0f, centerY, halfSize + wallThickness * 0.5f),
            new Vector3(30.0f + wallThickness * 2.0f, wallHeight, wallThickness));

        CreateBoundaryWall(root.transform, "SouthWall",
            new Vector3(0.0f, centerY, -halfSize - wallThickness * 0.5f),
            new Vector3(30.0f + wallThickness * 2.0f, wallHeight, wallThickness));

        CreateBoundaryWall(root.transform, "EastWall",
            new Vector3(halfSize + wallThickness * 0.5f, centerY, 0.0f),
            new Vector3(wallThickness, wallHeight, 30.0f));

        CreateBoundaryWall(root.transform, "WestWall",
            new Vector3(-halfSize - wallThickness * 0.5f, centerY, 0.0f),
            new Vector3(wallThickness, wallHeight, 30.0f));
    }

    private static void CreateBoundaryWall(Transform parent, string name, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.position = position;

        BoxCollider collider = wall.AddComponent<BoxCollider>();
        collider.size = size;
    }

    private static void CreatePlayer()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerCameraPrefabPath);
        GameObject player = prefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(prefab)
            : new GameObject("PlayerCamera");

        player.name = "PlayerCamera";
        player.tag = "MainCamera";
        player.transform.position = new Vector3(0.0f, 1.0f, -12.0f);
        player.transform.rotation = Quaternion.identity;

        Camera camera = player.GetComponent<Camera>();
        if (camera == null)
        {
            camera = player.AddComponent<Camera>();
        }

        if (player.GetComponent<AudioListener>() == null)
        {
            player.AddComponent<AudioListener>();
        }
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
