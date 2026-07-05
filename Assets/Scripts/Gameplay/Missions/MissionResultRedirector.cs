using UnityEngine;

public sealed class MissionResultRedirector : MonoBehaviour
{
    [SerializeField] private FireGraphOutcomeController outcomeController;
    [SerializeField] private string missionDisplayName = "Mision";
    [SerializeField] private string retrySceneName;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField, Min(0.0f)] private float returnDelay = 2.0f;

    private bool returningToMenu;
    private float returnTimer;

    private void Awake()
    {
        if (outcomeController == null)
        {
            outcomeController = FindObjectOfType<FireGraphOutcomeController>(true);
        }
    }

    private void Update()
    {
        if (returningToMenu)
        {
            returnTimer -= Time.deltaTime;
            if (returnTimer <= 0.0f)
            {
                SceneLoadUtility.LoadScene(mainMenuSceneName);
            }

            return;
        }

        if (outcomeController == null ||
            (outcomeController.currentOutcome != FireGraphOutcome.Victory &&
             outcomeController.currentOutcome != FireGraphOutcome.Defeat))
        {
            return;
        }

        returningToMenu = true;
        returnTimer = Mathf.Max(0.0f, returnDelay);
        outcomeController.StopMissionMonitoring();

        string sceneToRetry = string.IsNullOrWhiteSpace(retrySceneName)
            ? gameObject.scene.name
            : retrySceneName;
        MissionResultState.SetResult(
            outcomeController.currentOutcome,
            outcomeController.currentDefeatReason,
            missionDisplayName,
            sceneToRetry);
    }

    public void Configure(
        FireGraphOutcomeController outcome,
        string missionName,
        string retryScene,
        string menuScene,
        float delay)
    {
        outcomeController = outcome;
        missionDisplayName = missionName;
        retrySceneName = retryScene;
        mainMenuSceneName = menuScene;
        returnDelay = Mathf.Max(0.0f, delay);
    }
}
