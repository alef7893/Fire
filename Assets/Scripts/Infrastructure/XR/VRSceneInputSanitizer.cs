using Oculus.Interaction;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class VRSceneInputSanitizer
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void SanitizeAfterSceneLoad()
    {
        Sanitize();
    }

    public static void Sanitize()
    {
        EventSystem[] eventSystems = Object.FindObjectsOfType<EventSystem>(true);
        EventSystem primaryEventSystem = ChoosePrimary(eventSystems);

        PointableCanvasModule[] modules = Object.FindObjectsOfType<PointableCanvasModule>(true);
        PointableCanvasModule primaryModule = null;
        if (primaryEventSystem != null)
        {
            primaryModule = primaryEventSystem.GetComponent<PointableCanvasModule>();
        }

        if (primaryModule == null && modules.Length > 0)
        {
            primaryModule = modules[0];
        }

        if (primaryEventSystem == null || primaryModule == null)
        {
            EnsurePointableEventSystem(ref primaryEventSystem, ref primaryModule);
        }

        foreach (PointableCanvasModule module in modules)
        {
            if (module != primaryModule)
            {
                module.enabled = false;
            }
        }

        foreach (EventSystem eventSystem in eventSystems)
        {
            if (eventSystem != primaryEventSystem)
            {
                eventSystem.enabled = false;
            }
        }

        if (primaryEventSystem != null)
        {
            primaryEventSystem.enabled = true;
        }

        if (primaryModule != null)
        {
            primaryModule.enabled = true;
        }
    }

    public static void PrepareForSceneLoad()
    {
        EventSystem[] eventSystems = Object.FindObjectsOfType<EventSystem>(true);
        foreach (EventSystem eventSystem in eventSystems)
        {
            if (eventSystem == null)
            {
                continue;
            }

            eventSystem.SetSelectedGameObject(null);
            eventSystem.enabled = false;
        }

        PointableCanvasModule[] modules = Object.FindObjectsOfType<PointableCanvasModule>(true);
        foreach (PointableCanvasModule module in modules)
        {
            if (module != null)
            {
                module.enabled = false;
            }
        }
    }

    private static EventSystem ChoosePrimary(EventSystem[] eventSystems)
    {
        foreach (EventSystem eventSystem in eventSystems)
        {
            if (eventSystem != null && eventSystem.gameObject.scene == SceneManager.GetActiveScene())
            {
                return eventSystem;
            }
        }

        return eventSystems.Length > 0 ? eventSystems[0] : null;
    }

    private static void EnsurePointableEventSystem(
        ref EventSystem primaryEventSystem,
        ref PointableCanvasModule primaryModule)
    {
        if (primaryEventSystem == null)
        {
            GameObject eventSystemObject = new GameObject("VRMenuEventSystem");
            primaryEventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        if (primaryModule == null && primaryEventSystem != null)
        {
            primaryModule = primaryEventSystem.GetComponent<PointableCanvasModule>();
            if (primaryModule == null)
            {
                primaryModule = primaryEventSystem.gameObject.AddComponent<PointableCanvasModule>();
            }
        }
    }
}
