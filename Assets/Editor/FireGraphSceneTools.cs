using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class FireGraphSceneTools
{
    private const string GraphRootName = "FireGraphRoot";
    private const string NodesRootName = "Nodes";

    [MenuItem("Tools/Fire Simulation/Organize Fire Nodes Under Graph Root")]
    public static void OrganizeFireNodesUnderGraphRoot()
    {
        FireGraphRoot graphRoot = Object.FindObjectOfType<FireGraphRoot>(true);
        GameObject graphObject = graphRoot != null
            ? graphRoot.gameObject
            : GameObject.Find(GraphRootName);

        if (graphObject == null)
        {
            graphObject = new GameObject(GraphRootName);
            graphRoot = graphObject.AddComponent<FireGraphRoot>();
            FireSimulationManager manager = graphObject.AddComponent<FireSimulationManager>();
            manager.graphRoot = graphRoot;
            EditorUtility.SetDirty(manager);
        }
        else if (graphRoot == null)
        {
            graphRoot = graphObject.AddComponent<FireGraphRoot>();
        }

        Transform nodesRoot = graphObject.transform.Find(NodesRootName);
        if (nodesRoot == null)
        {
            GameObject nodesObject = new GameObject(NodesRootName);
            nodesRoot = nodesObject.transform;
            nodesRoot.SetParent(graphObject.transform, false);
        }

        graphRoot.nodesRoot = nodesRoot;
        FireObject[] fireObjects = Object.FindObjectsOfType<FireObject>(true);
        int movedCount = 0;

        foreach (FireObject fireObject in fireObjects)
        {
            if (fireObject == null || fireObject.transform == nodesRoot || fireObject.transform.IsChildOf(nodesRoot))
            {
                continue;
            }

            fireObject.transform.SetParent(nodesRoot, true);
            EditorUtility.SetDirty(fireObject.gameObject);
            movedCount++;
        }

        EditorUtility.SetDirty(nodesRoot.gameObject);
        EditorUtility.SetDirty(graphRoot);
        EditorUtility.SetDirty(graphObject);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log($"Moved {movedCount} fire nodes under {graphObject.name}/{NodesRootName}.");
    }

    [MenuItem("Tools/Fire Simulation/Fix Duplicate Audio Listeners")]
    public static void FixDuplicateAudioListeners()
    {
        AudioListener[] listeners = Object.FindObjectsOfType<AudioListener>(true);
        if (listeners.Length <= 1)
        {
            Debug.Log($"Audio listener check complete: {listeners.Length} listener found.");
            return;
        }

        AudioListener listenerToKeep = FindPreferredListener(listeners);
        int disabledCount = 0;

        foreach (AudioListener listener in listeners)
        {
            if (listener == null || listener == listenerToKeep)
            {
                continue;
            }

            if (listener.enabled)
            {
                listener.enabled = false;
                disabledCount++;
            }

            EditorUtility.SetDirty(listener);
        }

        if (listenerToKeep != null && !listenerToKeep.enabled)
        {
            listenerToKeep.enabled = true;
            EditorUtility.SetDirty(listenerToKeep);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log($"Audio listener check complete: kept {listenerToKeep?.gameObject.name}, disabled {disabledCount} duplicate listeners.");
    }

    private static AudioListener FindPreferredListener(AudioListener[] listeners)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            AudioListener mainCameraListener = mainCamera.GetComponent<AudioListener>();
            if (mainCameraListener != null)
            {
                return mainCameraListener;
            }
        }

        foreach (AudioListener listener in listeners)
        {
            if (listener != null && listener.enabled && listener.gameObject.activeInHierarchy)
            {
                return listener;
            }
        }

        return listeners[0];
    }
}
