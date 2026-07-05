using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FireExposureVignette : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private FireGraphOutcomeController outcomeController;

    [Header("Placement")]
    [SerializeField, Min(0.1f)] private float distanceFromCamera = 0.55f;
    [SerializeField] private Vector2 canvasSize = new Vector2(1.45f, 0.9f);
    [SerializeField, Range(0.03f, 0.45f)] private float edgeThickness = 0.08f;

    [Header("Response")]
    [SerializeField, Range(0.0f, 1.0f)] private float visibleThreshold = 0.2f;
    [SerializeField, Range(0.0f, 1.0f)] private float criticalThreshold = 0.68f;
    [SerializeField, Range(0.0f, 1.0f)] private float maximumAlpha = 0.45f;
    [SerializeField, Min(0.1f)] private float fadeSharpness = 7.0f;
    [SerializeField, Min(0.1f)] private float pulseSpeed = 2.8f;

    [Header("Colors")]
    [SerializeField] private Color cautionColor = new Color(1.0f, 0.42f, 0.02f, 1.0f);
    [SerializeField] private Color dangerColor = new Color(1.0f, 0.03f, 0.0f, 1.0f);

    private Camera playerCamera;
    private Canvas vignetteCanvas;
    private Image[] edgeImages;
    private float currentAlpha;
    private static bool sceneLoadHookRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeRuntimeVignette()
    {
        EnsureRuntimeInstance();

        if (!sceneLoadHookRegistered)
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            sceneLoadHookRegistered = true;
        }
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureRuntimeInstance();
    }

    private static void EnsureRuntimeInstance()
    {
        if (FindObjectOfType<FireExposureVignette>(true) != null)
        {
            return;
        }

        if (FindObjectOfType<FireGraphOutcomeController>(true) == null)
        {
            return;
        }

        GameObject vignetteRoot = new GameObject("FireExposureVignette");
        vignetteRoot.AddComponent<FireExposureVignette>();
    }

    private void Awake()
    {
        ResolveReferences();
        EnsureCanvas();
        ApplyAlpha(0.0f);
    }

    private void LateUpdate()
    {
        ResolveReferences();
        EnsureCanvas();

        float targetAlpha = CalculateTargetAlpha();
        currentAlpha = Mathf.Lerp(
            currentAlpha,
            targetAlpha,
            1.0f - Mathf.Exp(-fadeSharpness * Time.unscaledDeltaTime));

        UpdateCanvasPose();
        ApplyAlpha(currentAlpha);
    }

    private void ResolveReferences()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (outcomeController == null)
        {
            outcomeController = FindObjectOfType<FireGraphOutcomeController>(true);
        }
    }

    private float CalculateTargetAlpha()
    {
        if (outcomeController == null ||
            !outcomeController.IsMonitoringOptionalDefeatConditions ||
            !outcomeController.IsPlayerExposureEnabled ||
            outcomeController.currentOutcome != FireGraphOutcome.InProgress)
        {
            return 0.0f;
        }

        float exposure = outcomeController.NormalizedExposure;
        if (exposure <= visibleThreshold)
        {
            return 0.0f;
        }

        float normalized = Mathf.InverseLerp(visibleThreshold, 1.0f, exposure);
        float alpha = Mathf.SmoothStep(0.0f, maximumAlpha, normalized);
        if (exposure >= criticalThreshold)
        {
            float pulse = 0.75f + 0.25f * Mathf.PingPong(Time.unscaledTime * pulseSpeed, 1.0f);
            alpha *= pulse;
        }

        return alpha;
    }

    private void EnsureCanvas()
    {
        if (vignetteCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("FireExposureVignetteCanvas", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);

        vignetteCanvas = canvasObject.GetComponent<Canvas>();
        vignetteCanvas.renderMode = RenderMode.WorldSpace;
        vignetteCanvas.sortingOrder = 200;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.sizeDelta = canvasSize;

        edgeImages = new[]
        {
            CreateEdgeImage(canvasRect, "TopEdge", new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.0f, 1.0f), new Vector2(0.0f, edgeThickness)),
            CreateEdgeImage(canvasRect, "BottomEdge", new Vector2(0.0f, 0.0f), new Vector2(1.0f, 0.0f), new Vector2(0.0f, 0.0f), new Vector2(0.0f, edgeThickness)),
            CreateEdgeImage(canvasRect, "LeftEdge", new Vector2(0.0f, 0.0f), new Vector2(0.0f, 1.0f), new Vector2(0.0f, 0.0f), new Vector2(edgeThickness, 0.0f)),
            CreateEdgeImage(canvasRect, "RightEdge", new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 0.0f), new Vector2(edgeThickness, 0.0f)),
        };
    }

    private Image CreateEdgeImage(
        RectTransform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 sizeDelta)
    {
        GameObject edgeObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        edgeObject.transform.SetParent(parent, false);

        RectTransform rect = edgeObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sizeDelta;

        Image image = edgeObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = new Color(cautionColor.r, cautionColor.g, cautionColor.b, 0.0f);
        return image;
    }

    private void UpdateCanvasPose()
    {
        if (vignetteCanvas == null || playerCamera == null)
        {
            return;
        }

        Transform canvasTransform = vignetteCanvas.transform;
        canvasTransform.SetParent(playerCamera.transform, false);
        canvasTransform.localPosition = Vector3.forward * distanceFromCamera;
        canvasTransform.localRotation = Quaternion.identity;

        RectTransform rect = vignetteCanvas.GetComponent<RectTransform>();
        rect.sizeDelta = canvasSize;
    }

    private void ApplyAlpha(float alpha)
    {
        if (vignetteCanvas != null && vignetteCanvas.gameObject.activeSelf != alpha > 0.001f)
        {
            vignetteCanvas.gameObject.SetActive(alpha > 0.001f);
        }

        if (edgeImages == null)
        {
            return;
        }

        float exposure = outcomeController != null ? outcomeController.NormalizedExposure : 0.0f;
        Color color = Color.Lerp(cautionColor, dangerColor, Mathf.InverseLerp(0.35f, 1.0f, exposure));
        color.a = alpha;

        foreach (Image image in edgeImages)
        {
            if (image != null)
            {
                image.color = color;
            }
        }
    }
}
