using System.Collections;
using UnityEngine;

public sealed class VRSceneFadeOverlay : MonoBehaviour
{
    private const float OverlayDistance = 0.35f;
    private const float OverlayScale = 0.95f;

    private Transform cameraTransform;
    private MeshRenderer overlayRenderer;
    private Material overlayMaterial;
    private Color overlayColor = Color.black;

    public static VRSceneFadeOverlay Create()
    {
        GameObject overlayObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        overlayObject.name = "VR Scene Transition Fade";
        DontDestroyOnLoad(overlayObject);

        Collider overlayCollider = overlayObject.GetComponent<Collider>();
        if (overlayCollider != null)
        {
            Destroy(overlayCollider);
        }

        VRSceneFadeOverlay overlay = overlayObject.AddComponent<VRSceneFadeOverlay>();
        overlay.Configure();
        return overlay;
    }

    public IEnumerator FadeTo(float targetAlpha, float duration)
    {
        float startAlpha = overlayColor.a;
        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = Mathf.SmoothStep(0f, 1f, t);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, easedT));
            RefreshCamera();
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    public void RefreshCamera()
    {
        if (cameraTransform == null || !cameraTransform.gameObject.activeInHierarchy)
        {
            Camera activeCamera = Camera.main;
            if (activeCamera == null)
            {
                activeCamera = FindObjectOfType<Camera>();
            }

            cameraTransform = activeCamera != null ? activeCamera.transform : null;
        }

        if (cameraTransform == null)
        {
            return;
        }

        Transform overlayTransform = transform;
        overlayTransform.position = cameraTransform.position + cameraTransform.forward * OverlayDistance;
        overlayTransform.rotation = cameraTransform.rotation;
        overlayTransform.localScale = Vector3.one * OverlayScale;
    }

    private void Configure()
    {
        overlayRenderer = GetComponent<MeshRenderer>();
        overlayMaterial = new Material(Shader.Find("Unlit/Color"));
        overlayMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        overlayMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        overlayMaterial.SetInt("_ZWrite", 0);
        overlayMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
        overlayMaterial.DisableKeyword("_ALPHATEST_ON");
        overlayMaterial.EnableKeyword("_ALPHABLEND_ON");
        overlayMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        overlayMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Overlay;

        overlayRenderer.sharedMaterial = overlayMaterial;
        overlayRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        overlayRenderer.receiveShadows = false;

        SetAlpha(0f);
        RefreshCamera();
    }

    private void SetAlpha(float alpha)
    {
        overlayColor.a = Mathf.Clamp01(alpha);
        overlayMaterial.color = overlayColor;
    }
}
