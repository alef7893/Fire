using System.Linq;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class VRThreeButtonPanelBuilder
{
    private const string ScenePath = "Assets/Scenes/Test.unity";
    private const string VrPrefabFolder = "Assets/Prefabs/VR";
    private const string CanvasPrefabPath =
        "Packages/com.meta.xr.sdk.interaction/Runtime/Sample/Objects/Props/FlatUnityCanvas.prefab";

    public static void Build()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var existingPanel = GameObject.Find("VRSelectionPanel");
        if (existingPanel != null)
        {
            Object.DestroyImmediate(existingPanel);
        }

        var canvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPrefabPath);
        if (canvasPrefab == null)
        {
            throw new System.InvalidOperationException($"Could not load {CanvasPrefabPath}");
        }

        var panel = (GameObject)PrefabUtility.InstantiatePrefab(canvasPrefab, scene);
        PrefabUtility.UnpackPrefabInstance(panel, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        panel.name = "VRSelectionPanel";
        panel.transform.SetPositionAndRotation(new Vector3(0f, 1.55f, 2.5f), Quaternion.Euler(0f, 180f, 0f));
        panel.transform.localScale = Vector3.one;

        var canvas = panel.GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            throw new System.InvalidOperationException("The official FlatUnityCanvas prefab has no Canvas.");
        }

        foreach (Transform child in canvas.transform.Cast<Transform>().ToArray())
        {
            Object.DestroyImmediate(child.gameObject);
        }

        var canvasRect = (RectTransform)canvas.transform;
        canvasRect.sizeDelta = new Vector2(320f, 270f);
        canvasRect.localScale = Vector3.one * 0.003f;

        var background = canvas.GetComponent<Image>();
        background.color = new Color(0.10f, 0.12f, 0.14f, 0.96f);

        var surface = panel.transform.Find("Surface");
        if (surface == null)
        {
            throw new System.InvalidOperationException("The official FlatUnityCanvas prefab has no Surface.");
        }
        surface.localScale = new Vector3(0.96f, 0.81f, 0.01f);

        var audio = panel.transform.Find("Audio");
        if (audio != null)
        {
            Object.DestroyImmediate(audio.gameObject);
        }

        CreateText(canvasRect, "Title", "Selecciona una opcion", new Vector2(0f, 105f),
            new Vector2(290f, 42f), 26, FontStyle.Bold);

        CreateButton(canvasRect, "OptionButton1", "Opcion 1", 42f);
        CreateButton(canvasRect, "OptionButton2", "Opcion 2", -30f);
        CreateButton(canvasRect, "OptionButton3", "Opcion 3", -102f);

        var pointableCanvas = panel.GetComponent<PointableCanvas>();
        var clippedSurface = panel.GetComponentInChildren<ClippedPlaneSurface>(true);
        var rayInteractable = panel.GetComponent<RayInteractable>() ?? panel.AddComponent<RayInteractable>();
        rayInteractable.InjectAllRayInteractable(clippedSurface);
        rayInteractable.InjectOptionalPointableElement(pointableCanvas);

        EnsurePointableCanvasModule();
        SetLayerRecursively(panel, LayerMask.NameToLayer("UI"));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Created VRSelectionPanel with three selectable VR buttons in Test scene.");
    }

    public static void AddOptionOneCubeToggle()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var panel = GameObject.Find("VRSelectionPanel");
        var buttonObject = GameObject.Find("OptionButton1");

        if (panel == null || buttonObject == null)
        {
            throw new System.InvalidOperationException(
                "VRSelectionPanel and OptionButton1 must exist before adding the cube toggle.");
        }

        var existingCube = GameObject.Find("OptionOneRedCube");
        if (existingCube != null)
        {
            Object.DestroyImmediate(existingCube);
        }

        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "OptionOneRedCube";
        cube.transform.SetPositionAndRotation(
            panel.transform.position + new Vector3(-0.9f, -0.1f, 0f),
            Quaternion.identity);
        cube.transform.localScale = Vector3.one * 0.45f;

        var renderer = cube.GetComponent<Renderer>();
        renderer.sharedMaterial = new Material(Shader.Find("Standard"))
        {
            color = new Color(0.85f, 0.04f, 0.02f, 1f)
        };

        var toggle = buttonObject.GetComponent<VRButtonCubeToggle>() ??
                     buttonObject.AddComponent<VRButtonCubeToggle>();
        toggle.SetTargetCube(cube);

        var button = buttonObject.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(button.onClick, toggle.ToggleCube);

        cube.SetActive(false);
        EditorUtility.SetDirty(button);
        EditorUtility.SetDirty(toggle);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Connected OptionButton1 to toggle OptionOneRedCube.");
    }

    public static void CreateReusableVrPrefabs()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EnsureFolder("Assets/Prefabs", "VR");

        CreateReusablePanelPrefab(scene);
        CreateReusablePlayerRigPrefab(scene);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created reusable VR panel and player rig prefabs.");
    }

    public static void ValidateReusableVrPrefabs()
    {
        var panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            $"{VrPrefabFolder}/VRInteractiveMenuPanel.prefab");
        var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            $"{VrPrefabFolder}/VRPlayerInteractionRig.prefab");

        ValidatePrefab(panelPrefab, "VRInteractiveMenuPanel");
        ValidatePrefab(rigPrefab, "VRPlayerInteractionRig");

        if (panelPrefab.GetComponentInChildren<PointableCanvas>(true) == null ||
            panelPrefab.GetComponentInChildren<RayInteractable>(true) == null ||
            panelPrefab.GetComponentInChildren<PokeInteractable>(true) == null ||
            panelPrefab.GetComponentInChildren<PointableCanvasModule>(true) == null)
        {
            throw new System.InvalidOperationException(
                "VRInteractiveMenuPanel is missing required VR interaction components.");
        }

        if (rigPrefab.GetComponentInChildren<OVRCameraRig>(true) == null)
        {
            throw new System.InvalidOperationException("VRPlayerInteractionRig is missing OVRCameraRig.");
        }

        Debug.Log("Validated reusable VR prefabs successfully.");
    }

    public static void CreateVrExtinguisherBackup()
    {
        EnsureFolder("Assets/Prefabs", "VR");

        const string sourcePath = "Assets/Prefabs/ExtintorP1.prefab";
        const string backupPath = "Assets/Prefabs/VR/VRFunctionalExtinguisher_Backup.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath) == null)
        {
            throw new System.InvalidOperationException($"Source prefab was not found: {sourcePath}");
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(backupPath) != null)
        {
            AssetDatabase.DeleteAsset(backupPath);
        }

        if (!AssetDatabase.CopyAsset(sourcePath, backupPath))
        {
            throw new System.InvalidOperationException("Unity could not duplicate ExtintorP1.");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var backup = AssetDatabase.LoadAssetAtPath<GameObject>(backupPath);
        ValidatePrefab(backup, "VRFunctionalExtinguisher_Backup");
        Debug.Log("Created VRFunctionalExtinguisher_Backup from ExtintorP1.");
    }

    public static void ConfigureVrExtinguisherFireSuppression()
    {
        const string extinguisherPath = "Assets/Prefabs/ExtintorP1.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(extinguisherPath);

        try
        {
            var lever = prefabRoot.GetComponentInChildren<LeverWithLocalRotation>(true);
            var sprayParticles = prefabRoot.GetComponentsInChildren<ParticleSystem>(true)
                .FirstOrDefault(system => system.name == "FireEx_PS");

            if (lever == null || sprayParticles == null)
            {
                throw new System.InvalidOperationException(
                    "ExtintorP1 requires LeverWithLocalRotation and FireEx_PS.");
            }

            Transform origin = sprayParticles.transform.Find("WaterDetectionOrigin");
            if (origin == null)
            {
                var originObject = new GameObject("WaterDetectionOrigin");
                origin = originObject.transform;
                origin.SetParent(sprayParticles.transform, false);
            }

            origin.localPosition = Vector3.zero;
            origin.localRotation = Quaternion.identity;
            origin.localScale = Vector3.one;

            var sprayer = prefabRoot.GetComponent<VRExtinguisherWaterSprayer>() ??
                          prefabRoot.AddComponent<VRExtinguisherWaterSprayer>();
            sprayer.Configure(lever, origin);

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, extinguisherPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Configured ExtintorP1 to suppress IFireWaterTarget objects while its VR lever is active.");
    }

    public static void ValidateVrExtinguisherFireSuppression()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var extinguisher = GameObject.Find("TestExtintorP1");
        if (extinguisher == null)
        {
            throw new System.InvalidOperationException("TestExtintorP1 was not found in Test.");
        }

        var sprayer = extinguisher.GetComponent<VRExtinguisherWaterSprayer>();
        if (sprayer == null || !sprayer.isActiveAndEnabled)
        {
            throw new System.InvalidOperationException(
                "TestExtintorP1 does not inherit an active VRExtinguisherWaterSprayer.");
        }

        int targetsWithColliders = Object.FindObjectsOfType<MonoBehaviour>(true)
            .Count(component => component is IFireWaterTarget &&
                                component.GetComponentInChildren<Collider>(true) != null);

        if (targetsWithColliders == 0)
        {
            throw new System.InvalidOperationException(
                "Test contains no IFireWaterTarget objects with colliders.");
        }

        var backup = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Prefabs/VR/VRFunctionalExtinguisher_Backup.prefab");
        if (backup != null && backup.GetComponentInChildren<VRExtinguisherWaterSprayer>(true) != null)
        {
            throw new System.InvalidOperationException(
                "VRFunctionalExtinguisher_Backup unexpectedly contains the new suppression component.");
        }

        Debug.Log($"Validated VR extinguisher suppression with {targetsWithColliders} detectable fire targets.");
    }

    private static void ValidatePrefab(GameObject prefab, string prefabName)
    {
        if (prefab == null)
        {
            throw new System.InvalidOperationException($"{prefabName} prefab was not found.");
        }

        foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) > 0)
            {
                throw new System.InvalidOperationException(
                    $"{prefabName} contains missing scripts on {transform.name}.");
            }
        }
    }

    private static void CreateReusablePanelPrefab(UnityEngine.SceneManagement.Scene scene)
    {
        var panel = GameObject.Find("VRSelectionPanel");
        if (panel == null)
        {
            throw new System.InvalidOperationException("VRSelectionPanel was not found in Test.");
        }

        var prefabRoot = new GameObject("VRInteractiveMenuPanel");
        var panelCopy = Object.Instantiate(panel, prefabRoot.transform);
        panelCopy.name = "InteractivePanel";
        panelCopy.transform.localPosition = Vector3.zero;
        panelCopy.transform.localRotation = Quaternion.identity;

        foreach (var button in panelCopy.GetComponentsInChildren<Button>(true))
        {
            while (button.onClick.GetPersistentEventCount() > 0)
            {
                UnityEventTools.RemovePersistentListener(button.onClick, 0);
            }
        }

        foreach (var toggle in panelCopy.GetComponentsInChildren<VRButtonCubeToggle>(true))
        {
            Object.DestroyImmediate(toggle);
        }

        var eventSystemObject = new GameObject("VRPanelEventSystem");
        eventSystemObject.transform.SetParent(prefabRoot.transform, false);
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<PointableCanvasModule>();

        PrefabUtility.SaveAsPrefabAsset(prefabRoot, $"{VrPrefabFolder}/VRInteractiveMenuPanel.prefab");
        Object.DestroyImmediate(prefabRoot);
    }

    private static void CreateReusablePlayerRigPrefab(UnityEngine.SceneManagement.Scene scene)
    {
        var cameraRig = Object.FindObjectOfType<OVRCameraRig>(true);
        if (cameraRig == null)
        {
            throw new System.InvalidOperationException("OVRCameraRig was not found in Test.");
        }

        var rigRoot = cameraRig.transform.root.gameObject;
        var rigCopy = Object.Instantiate(rigRoot);
        rigCopy.name = "VRPlayerInteractionRig";
        rigCopy.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        PrefabUtility.SaveAsPrefabAsset(rigCopy, $"{VrPrefabFolder}/VRPlayerInteractionRig.prefab");
        Object.DestroyImmediate(rigCopy);
    }

    private static void EnsureFolder(string parent, string child)
    {
        var path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void EnsurePointableCanvasModule()
    {
        var modules = Object.FindObjectsOfType<PointableCanvasModule>(true);
        if (modules.Length > 0)
        {
            foreach (var duplicate in modules.Skip(1))
            {
                duplicate.enabled = false;
            }
            return;
        }

        var eventSystem = Object.FindObjectsOfType<EventSystem>(true).FirstOrDefault();
        if (eventSystem == null)
        {
            var eventSystemObject = new GameObject("VRMenuEventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        eventSystem.gameObject.AddComponent<PointableCanvasModule>();
    }

    private static Button CreateButton(RectTransform parent, string name, string label, float y)
    {
        var buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(270f, 56f);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.95f, 0.32f, 0.06f, 1f);

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        button.colors = new ColorBlock
        {
            normalColor = new Color(0.95f, 0.32f, 0.06f, 1f),
            highlightedColor = new Color(1f, 0.78f, 0.18f, 1f),
            pressedColor = new Color(0.75f, 0.12f, 0.04f, 1f),
            selectedColor = new Color(1f, 0.48f, 0.08f, 1f),
            disabledColor = new Color(0.24f, 0.20f, 0.18f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };

        CreateText(rect, "Label", label, Vector2.zero, Vector2.zero, 24, FontStyle.Bold, true);
        return button;
    }

    private static Text CreateText(RectTransform parent, string name, string value, Vector2 position,
        Vector2 size, int fontSize, FontStyle style, bool stretch = false)
    {
        var textObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);

        var rect = (RectTransform)textObject.transform;
        if (stretch)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        var text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        root.layer = layer;
        foreach (Transform child in root.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
