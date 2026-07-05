using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class VRDeferredSceneLoader : MonoBehaviour
{
    private static bool isLoading;

    [SerializeField] private float fadeOutDuration = 0.55f;
    [SerializeField] private float fadeInDuration = 0.65f;

    public static bool TryLoad(string sceneReference)
    {
        if (isLoading)
        {
            return false;
        }

        isLoading = true;
        GameObject loaderObject = new GameObject("VR Deferred Scene Loader");
        DontDestroyOnLoad(loaderObject);

        VRDeferredSceneLoader loader = loaderObject.AddComponent<VRDeferredSceneLoader>();
        loader.StartCoroutine(loader.LoadAfterInputFrame(sceneReference));
        return true;
    }

    private IEnumerator LoadAfterInputFrame(string sceneReference)
    {
        // Let PointableCanvasModule finish the click that requested the load.
        yield return null;

        VRSceneFadeOverlay fadeOverlay = VRSceneFadeOverlay.Create();
        yield return fadeOverlay.FadeTo(1f, fadeOutDuration);

        VRSceneInputSanitizer.PrepareForSceneLoad();
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneReference);
        while (loadOperation != null && !loadOperation.isDone)
        {
            fadeOverlay.RefreshCamera();
            yield return null;
        }

        yield return null;
        VRSceneInputSanitizer.Sanitize();
        yield return fadeOverlay.FadeTo(0f, fadeInDuration);

        isLoading = false;
        Destroy(fadeOverlay.gameObject);
        Destroy(gameObject);
    }
}
