using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FireVfxPreviewSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/FireVfxPreview.unity";
    private const string FirePrefabPath = "Assets/ImportedPackages/Free Fire VFX URP/Particles/VFX_Fire_01_Big_Simple.prefab";

    [MenuItem("Tools/Fire Simulation/Create Fire VFX Preview Scene")]
    public static void CreateFireVfxPreviewScene()
    {
        EnsureFolder("Assets/Scenes");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "FireVfxPreview";

        CreateFirePreviewObject();
        CreateCamera();
        CreateLighting();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"Created fire VFX preview scene at {ScenePath}.");
    }

    private static void CreateFirePreviewObject()
    {
        GameObject root = new GameObject("FireVfxPreviewRoot");
        root.transform.position = Vector3.zero;
        FireVfxScaleController scaleController = root.AddComponent<FireVfxScaleController>();
        scaleController.minimumScale = Vector3.one * 0.2f;
        scaleController.maximumScale = Vector3.one;
        scaleController.resizeSpeed = 1.0f;
        scaleController.playOnStart = true;
        scaleController.loop = true;

        GameObject firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(FirePrefabPath);
        if (firePrefab == null)
        {
            Debug.LogError($"Could not find fire prefab at {FirePrefabPath}.");
            return;
        }

        GameObject fire = (GameObject)PrefabUtility.InstantiatePrefab(firePrefab);
        fire.name = "VFX_Fire_01_Big_Simple";
        fire.transform.SetParent(root.transform, false);
        fire.transform.localPosition = Vector3.zero;
        fire.transform.localRotation = Quaternion.identity;
        fire.transform.localScale = Vector3.one;

        ParticleSystem[] particleSystems = fire.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            ParticleSystem.MainModule main = particleSystem.main;
            main.playOnAwake = true;
            particleSystem.Play(true);
        }

        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(scaleController);
        EditorUtility.SetDirty(fire);
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0.0f, 1.2f, -4.0f);
        cameraObject.transform.rotation = Quaternion.Euler(8.0f, 0.0f, 0.0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Skybox;
        camera.fieldOfView = 45.0f;
        camera.nearClipPlane = 0.01f;
        camera.farClipPlane = 100.0f;

        cameraObject.AddComponent<AudioListener>();
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
