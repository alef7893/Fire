using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneLoadUtility
{
    public static bool LoadScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("Scene load requested with an empty scene name.");
            return false;
        }

        string sceneReference = null;
        if (Application.CanStreamedLevelBeLoaded(sceneName))
        {
            sceneReference = sceneName;
        }

        for (int index = 0; sceneReference == null && index < SceneManager.sceneCountInBuildSettings; index++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(index);
            if (string.Equals(
                    Path.GetFileNameWithoutExtension(scenePath),
                    sceneName,
                    StringComparison.OrdinalIgnoreCase))
            {
                sceneReference = scenePath;
            }
        }

        if (sceneReference != null)
        {
            return VRDeferredSceneLoader.TryLoad(sceneReference);
        }

        Debug.LogError(
            $"Scene '{sceneName}' could not be loaded. Add it to File > Build Settings or verify the scene name.");
        return false;
    }
}
