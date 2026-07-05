using UnityEngine;

public enum VRMissionProgressMode
{
    FireCount,
    Timer
}

[DefaultExecutionOrder(-10000)]
public class VRMissionFlowController : MonoBehaviour
{
    [Header("Mission")]
    [SerializeField] private FireGraphRoot fireGraphRoot;
    [SerializeField] private FireGraphOutcomeController outcomeController;
    [SerializeField] private string missionDisplayName = "Mision";
    [SerializeField] private string retrySceneName;
    [SerializeField] private string continueSceneName;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string retryButtonLabel = "Reintentar";
    [SerializeField, Min(0.0f)] private float resultReturnDelay = 2.0f;

    [Header("Panels")]
    [SerializeField] private VRMissionPanelSet panelSet;
    [SerializeField] private VRMissionProgressDisplay progressDisplay;
    [SerializeField] private GameObject controlsPanel;

    [Header("Progress")]
    [SerializeField] private VRMissionProgressMode progressMode = VRMissionProgressMode.FireCount;
    [SerializeField, Min(1.0f)] private float missionDurationSeconds = 180.0f;
    [SerializeField] private bool useOutcomeTimeLimitForTimer = true;

    [Header("Start Area")]
    [SerializeField] private XRTrackedAreaBoundary startAreaBoundary;
    [SerializeField] private GameObject startAreaVisualRoot;
    [SerializeField] private StartAreaPanelFollower startAreaPanelFollower;
    [SerializeField, Min(0.1f)] private float startAreaRadius = 2.5f;
    [SerializeField, Min(0.1f)] private float startAreaVisualBaseRadius = 2.0f;
    [SerializeField] private float controlsPanelFocusOffsetX = 0.85f;
    [SerializeField] private bool useConfiguredStartAreaCenter;
    [SerializeField] private Vector3 startAreaWorldCenter;

    [Header("Mission Objects")]
    [SerializeField] private GameObject[] activateOnMissionStart;
    [SerializeField] private bool disableMissionObjectsOnAwake = true;
    [SerializeField] private bool resetOutcomeOnMissionStart = true;
    [SerializeField] private bool beginOutcomeMonitoringOnMissionStart = true;
    [SerializeField] private bool enablePlayerExposureDefeat = true;
    [SerializeField] private bool enableTimeLimitDefeat;

    private bool missionStarted;
    private bool returningAfterResult;
    private float resultReturnTimer;
    private float localRemainingTime;

    protected virtual void Awake()
    {
        ResolveReferences();
        ConfigureStartAreaRuntime();

        if (disableMissionObjectsOnAwake)
        {
            SetMissionObjectsActive(false);
        }

        SetPropagation(false);
        ShowStartPanel();
    }

    protected virtual void Update()
    {
        if (returningAfterResult)
        {
            TickResultReturn();
            return;
        }

        if (!missionStarted || outcomeController == null)
        {
            return;
        }

        if (outcomeController.currentOutcome == FireGraphOutcome.Victory ||
            outcomeController.currentOutcome == FireGraphOutcome.Defeat)
        {
            BeginResultReturn(outcomeController.currentOutcome);
            return;
        }

        RefreshProgress();
    }

    public virtual void BeginMission()
    {
        if (missionStarted)
        {
            return;
        }

        missionStarted = true;
        returningAfterResult = false;
        resultReturnTimer = 0.0f;
        localRemainingTime = Mathf.Max(1.0f, missionDurationSeconds);

        ReleaseStartArea();
        SetMissionObjectsActive(true);
        SetPropagation(true);

        if (outcomeController != null)
        {
            outcomeController.ConfigureOptionalDefeatConditions(
                enablePlayerExposureDefeat,
                enableTimeLimitDefeat);

            if (resetOutcomeOnMissionStart)
            {
                outcomeController.ResetOutcome();
            }

            if (beginOutcomeMonitoringOnMissionStart)
            {
                outcomeController.BeginMissionMonitoring();
            }
        }

        ShowGameplayProgressPanel();
        RefreshProgress();
    }

    public virtual void ShowControls()
    {
        if (missionStarted)
        {
            return;
        }

        if (panelSet != null)
        {
            panelSet.ShowControlsOverlay();
        }

        if (startAreaPanelFollower != null)
        {
            startAreaPanelFollower.SetFocusLocalOffsetX(Mathf.Abs(controlsPanelFocusOffsetX));
            startAreaPanelFollower.SnapToCurrentView();
            startAreaPanelFollower.enabled = false;
        }
    }

    public virtual void CloseControlsToStart()
    {
        if (panelSet != null)
        {
            panelSet.CloseControlsToStart();
        }

        if (startAreaPanelFollower != null)
        {
            startAreaPanelFollower.ClearFocusOffset();
            startAreaPanelFollower.enabled = false;
        }
    }

    public virtual void CancelMission()
    {
        missionStarted = false;
        returningAfterResult = false;
        resultReturnTimer = 0.0f;
        localRemainingTime = Mathf.Max(1.0f, missionDurationSeconds);

        if (outcomeController != null)
        {
            outcomeController.StopMissionMonitoring();
        }

        SetPropagation(false);
        SetMissionObjectsActive(false);
        ConfigureStartAreaRuntime();
        ShowStartPanel();
        RefreshProgress();
    }

    public void RetryMission()
    {
        SceneLoadUtility.LoadScene(GetRetrySceneName());
    }

    public void ContinueMissionFlow()
    {
        if (!string.IsNullOrWhiteSpace(continueSceneName))
        {
            SceneLoadUtility.LoadScene(continueSceneName);
        }
    }

    public void ReturnToMainMenu()
    {
        SceneLoadUtility.LoadScene(mainMenuSceneName);
    }

    public void ConfigureStartArea(
        XRTrackedAreaBoundary boundary,
        GameObject visualRoot,
        float radius,
        Vector3 center)
    {
        startAreaBoundary = boundary;
        startAreaVisualRoot = visualRoot;
        startAreaRadius = Mathf.Max(0.1f, radius);
        startAreaVisualBaseRadius = 2.0f;
        startAreaWorldCenter = center;
        useConfiguredStartAreaCenter = true;
    }

    public void ConfigureStartAreaPanelFollower(StartAreaPanelFollower panelFollower)
    {
        startAreaPanelFollower = panelFollower;
    }

    public void ConfigureControlsPanelFocusOffset(float offset)
    {
        controlsPanelFocusOffsetX = Mathf.Max(0.0f, offset);
    }

    public void ConfigureUIHelpers(
        VRMissionPanelSet panels,
        VRMissionProgressDisplay progress)
    {
        panelSet = panels;
        progressDisplay = progress;
        if (panelSet != null && controlsPanel == null)
        {
            controlsPanel = panelSet.ControlsPanel;
        }
    }

    protected virtual void RefreshProgress()
    {
        if (progressDisplay == null)
        {
            return;
        }

        if (progressMode == VRMissionProgressMode.Timer)
        {
            progressDisplay.SetTimeRemaining(GetRemainingMissionTime());
            return;
        }

        FireNodeBase[] nodes = FindObjectsOfType<FireNodeBase>(true);
        int total = 0;
        int completed = 0;
        foreach (FireNodeBase node in nodes)
        {
            if (node == null)
            {
                continue;
            }

            total++;
            if (node.state == FireNodeState.Extinguished ||
                node.state == FireNodeState.Off)
            {
                completed++;
            }
        }

        progressDisplay.SetProgress(completed, total);
    }

    public void ConfigureProgressMode(
        VRMissionProgressMode mode,
        float durationSeconds,
        bool useOutcomeTimeLimit)
    {
        progressMode = mode;
        missionDurationSeconds = Mathf.Max(1.0f, durationSeconds);
        localRemainingTime = missionDurationSeconds;
        useOutcomeTimeLimitForTimer = useOutcomeTimeLimit;
    }

    protected virtual void BeginResultReturn(FireGraphOutcome outcome)
    {
        returningAfterResult = true;
        resultReturnTimer = Mathf.Max(0.0f, resultReturnDelay);

        if (outcomeController != null)
        {
            outcomeController.StopMissionMonitoring();
        }

        MissionDefeatReason defeatReason = outcome == FireGraphOutcome.Defeat &&
            outcomeController != null
                ? outcomeController.currentDefeatReason
                : MissionDefeatReason.None;

        MissionResultState.SetResult(
            outcome,
            defeatReason,
            missionDisplayName,
            GetRetrySceneName(),
            outcome == FireGraphOutcome.Victory ? continueSceneName : string.Empty,
            retryButtonLabel);
    }

    private void ResolveReferences()
    {
        if (fireGraphRoot == null)
        {
            fireGraphRoot = FindObjectOfType<FireGraphRoot>(true);
        }

        if (outcomeController == null)
        {
            outcomeController = FindObjectOfType<FireGraphOutcomeController>(true);
        }

        if (panelSet == null)
        {
            panelSet = GetComponent<VRMissionPanelSet>();
        }

        if (progressDisplay == null)
        {
            progressDisplay = GetComponent<VRMissionProgressDisplay>();
        }

        if (panelSet != null && controlsPanel == null)
        {
            controlsPanel = panelSet.ControlsPanel;
        }
    }

    private void ConfigureStartAreaRuntime()
    {
        if (startAreaBoundary != null)
        {
            Camera playerCamera = Camera.main;
            if (playerCamera != null)
            {
                Vector3 center = useConfiguredStartAreaCenter
                    ? startAreaWorldCenter
                    : playerCamera.transform.position;
                startAreaBoundary.Configure(playerCamera.transform, center, startAreaRadius);
            }

            startAreaBoundary.enabled = true;
        }

        if (startAreaVisualRoot != null)
        {
            startAreaVisualRoot.SetActive(true);
            float visualScale = startAreaRadius / Mathf.Max(0.1f, startAreaVisualBaseRadius);
            startAreaVisualRoot.transform.localScale = Vector3.one * visualScale;
        }

        if (startAreaPanelFollower != null)
        {
            startAreaPanelFollower.ClearFocusOffset();
            startAreaPanelFollower.enabled = false;
        }
    }

    private void ReleaseStartArea()
    {
        if (startAreaBoundary != null)
        {
            startAreaBoundary.enabled = false;
        }

        if (startAreaVisualRoot != null)
        {
            startAreaVisualRoot.SetActive(false);
        }

        if (startAreaPanelFollower != null)
        {
            startAreaPanelFollower.enabled = false;
        }
    }

    private void TickResultReturn()
    {
        resultReturnTimer -= Time.deltaTime;
        if (resultReturnTimer <= 0.0f)
        {
            SceneLoadUtility.LoadScene(mainMenuSceneName);
        }
    }

    private void ShowStartPanel()
    {
        if (panelSet != null)
        {
            panelSet.ShowStart();
        }
    }

    private void ShowGameplayProgressPanel()
    {
        if (panelSet != null)
        {
            panelSet.ShowGameplayProgress();
        }
    }

    private void SetPropagation(bool enabled)
    {
        if (fireGraphRoot != null)
        {
            fireGraphRoot.enablePropagation = enabled;
        }
    }

    private void SetMissionObjectsActive(bool active)
    {
        foreach (GameObject target in activateOnMissionStart)
        {
            if (target != null)
            {
                target.SetActive(active);
            }
        }
    }

    private float GetRemainingMissionTime()
    {
        if (useOutcomeTimeLimitForTimer &&
            outcomeController != null &&
            outcomeController.IsTimeLimitEnabled)
        {
            return outcomeController.RemainingTime;
        }

        if (missionStarted)
        {
            localRemainingTime = Mathf.Max(0.0f, localRemainingTime - Time.deltaTime);
        }

        return localRemainingTime;
    }

    private string GetRetrySceneName()
    {
        return string.IsNullOrWhiteSpace(retrySceneName)
            ? gameObject.scene.name
            : retrySceneName;
    }
}
