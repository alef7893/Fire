using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigationButton : MonoBehaviour
{
    [SerializeField] private string targetSceneName;

    public void LoadTargetScene()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning($"{nameof(SceneNavigationButton)} has no target scene assigned.", this);
            return;
        }

        SceneManager.LoadScene(targetSceneName);
    }
}
