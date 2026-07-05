using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class Mission0FlowController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private GameObject progressOnlyPanel;
    [SerializeField] private GameObject controlsPanel;

    [Header("Training")]
    [SerializeField] private Text progressText;
    [SerializeField] private bool useCompactProgressText = true;
    [SerializeField] private VRMissionPanelSet panelSet;
    [SerializeField] private VRMissionProgressDisplay progressDisplay;
    [SerializeField] private string fireTrainingRootName = "FireTrainingRoot";
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string missionOneSceneName = "M01_BasicSuppression";
    [SerializeField, Min(0.0f)] private float resultReturnDelay = 2.0f;

    [Header("Start Area")]
    [SerializeField] private XRTrackedAreaBoundary startAreaBoundary;
    [SerializeField] private GameObject startAreaVisualRoot;
    [SerializeField] private StartAreaPanelFollower startAreaPanelFollower;
    [SerializeField, Min(0.1f)] private float startAreaRadius = 2.5f;
    [SerializeField, Min(0.1f)] private float startAreaVisualBaseRadius = 2.0f;
    [SerializeField] private float controlsPanelFocusOffsetX = 0.85f;
    [SerializeField] private bool useConfiguredStartAreaCenter;
    [SerializeField] private Vector3 startAreaWorldCenter;

    private FireNodeSpark[] sparks;
    private GameObject fireTrainingRoot;
    private FireGraphOutcomeController outcomeController;
    private bool trainingStarted;
    private bool trainingCompleted;
    private bool returningAfterResult;
    private float resultReturnTimer;

    private void Awake()
    {
        EnsureUIHelpers();

        sparks = FindObjectsOfType<FireNodeSpark>(true)
            .OrderBy(node => node.name)
            .ToArray();
        outcomeController = FindObjectOfType<FireGraphOutcomeController>(true);

        fireTrainingRoot = FindSceneObject(fireTrainingRootName);
        if (fireTrainingRoot != null)
        {
            fireTrainingRoot.SetActive(false);
        }

        ConfigureStartAreaRuntime();
        ShowStartPanel();
        UpdateProgress(0);
    }

    private void Update()
    {
        if (returningAfterResult)
        {
            resultReturnTimer -= Time.deltaTime;
            if (resultReturnTimer <= 0.0f)
            {
                SceneLoadUtility.LoadScene(mainMenuSceneName);
            }
            return;
        }

        if (trainingStarted && outcomeController != null &&
            outcomeController.currentOutcome == FireGraphOutcome.Defeat)
        {
            BeginResultReturn(FireGraphOutcome.Defeat);
            return;
        }

        if (!trainingStarted || trainingCompleted || sparks.Length == 0)
        {
            return;
        }

        int extinguishedCount = sparks.Count(node =>
            node != null && node.state == FireNodeState.Extinguished);
        UpdateProgress(extinguishedCount);

        if (extinguishedCount < sparks.Length)
        {
            return;
        }

        trainingCompleted = true;
        BeginResultReturn(FireGraphOutcome.Victory);
    }

    public void BeginTraining()
    {
        trainingStarted = true;
        trainingCompleted = false;

        if (fireTrainingRoot != null)
        {
            fireTrainingRoot.SetActive(true);
        }

        ReleaseStartArea();

        if (outcomeController != null)
        {
            outcomeController.ConfigureOptionalDefeatConditions(true, false);
            outcomeController.ResetOutcome();
            outcomeController.BeginMissionMonitoring();
        }

        ShowGameplayProgressPanel();
        UpdateProgress(0);
    }

    public void ShowControls()
    {
        if (trainingStarted)
        {
            return;
        }

        if (panelSet != null)
        {
            panelSet.ShowControlsOverlay();
        }
        else
        {
            SetPanelActive(startPanel, true);
            SetStartPanelButtonsInteractable(false);
            SetPanelActive(controlsPanel, true);
        }

        if (startAreaPanelFollower != null)
        {
            startAreaPanelFollower.SetFocusLocalOffsetX(Mathf.Abs(controlsPanelFocusOffsetX));
            startAreaPanelFollower.SnapToCurrentView();
            startAreaPanelFollower.enabled = false;
        }
    }

    public void CloseControls()
    {
        if (trainingStarted)
        {
            ShowGameplayProgressPanel();
        }
        else
        {
            ShowStartPanel();
        }
    }

    public void CloseControlsToStart()
    {
        if (panelSet != null)
        {
            panelSet.CloseControlsToStart();
        }
        else
        {
            SetPanelActive(controlsPanel, false);
            SetPanelActive(startPanel, true);
            SetStartPanelButtonsInteractable(true);
        }

        if (startAreaPanelFollower != null)
        {
            startAreaPanelFollower.ClearFocusOffset();
            startAreaPanelFollower.enabled = false;
        }
    }

    public void RepeatTraining()
    {
        SceneLoadUtility.LoadScene(gameObject.scene.name);
    }

    public void CancelTraining()
    {
        trainingStarted = false;
        trainingCompleted = false;
        returningAfterResult = false;
        resultReturnTimer = 0.0f;

        if (outcomeController != null)
        {
            outcomeController.StopMissionMonitoring();
        }

        if (fireTrainingRoot != null)
        {
            fireTrainingRoot.SetActive(false);
        }

        ConfigureStartAreaRuntime();
        ShowStartPanel();
        UpdateProgress(0);
    }

    public void ContinueToMissionOne()
    {
        SceneLoadUtility.LoadScene(missionOneSceneName);
    }

    public void ReturnToMainMenu()
    {
        SceneLoadUtility.LoadScene(mainMenuSceneName);
    }

    private void BeginResultReturn(FireGraphOutcome outcome)
    {
        returningAfterResult = true;
        resultReturnTimer = Mathf.Max(0.0f, resultReturnDelay);
        if (outcomeController != null)
        {
            outcomeController.StopMissionMonitoring();
        }

        MissionDefeatReason defeatReason = outcome == FireGraphOutcome.Defeat && outcomeController != null
            ? outcomeController.currentDefeatReason
            : MissionDefeatReason.None;
        MissionResultState.SetResult(
            outcome,
            defeatReason,
            "Entrenamiento",
            gameObject.scene.name,
            outcome == FireGraphOutcome.Victory ? missionOneSceneName : string.Empty,
            "Repetir Entrenamiento");
    }

    public void Configure(
        GameObject start,
        GameObject instructions,
        GameObject controls,
        Text progress)
    {
        startPanel = start;
        instructionPanel = instructions;
        controlsPanel = controls;
        progressText = progress;
        EnsureUIHelpers();
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

    public void ConfigureProgressPanel(GameObject panel, Text progress)
    {
        progressOnlyPanel = panel;
        progressText = progress;
        EnsureUIHelpers();
    }

    public void ConfigureUIHelpers(VRMissionPanelSet panels, VRMissionProgressDisplay progress)
    {
        panelSet = panels;
        progressDisplay = progress;
        EnsureUIHelpers();
    }

    private void UpdateProgress(int extinguishedCount)
    {
        if (progressDisplay != null)
        {
            progressDisplay.SetProgress(extinguishedCount, sparks.Length);
            return;
        }

        if (progressText != null)
        {
            progressText.text = useCompactProgressText
                ? $"{extinguishedCount}/{sparks.Length}"
                : $"Fuegos apagados: {extinguishedCount}/{sparks.Length}";
        }
    }

    private void ShowStartPanel()
    {
        if (panelSet != null)
        {
            panelSet.ShowStart();
            return;
        }

        ShowOnly(startPanel);
    }

    private void ShowGameplayProgressPanel()
    {
        if (panelSet != null)
        {
            panelSet.ShowGameplayProgress();
            return;
        }

        ShowOnly(GetGameplayProgressPanel());
    }

    private void ShowOnly(GameObject panel)
    {
        SetPanelActive(startPanel, panel == startPanel);
        SetPanelActive(instructionPanel, panel == instructionPanel);
        SetPanelActive(progressOnlyPanel, panel == progressOnlyPanel);
        SetPanelActive(controlsPanel, panel == controlsPanel);

        if (panel == startPanel)
        {
            SetStartPanelButtonsInteractable(true);
        }
    }

    private void EnsureUIHelpers()
    {
        if (panelSet == null)
        {
            panelSet = GetComponent<VRMissionPanelSet>();
        }

        if (panelSet != null)
        {
            panelSet.Configure(startPanel, instructionPanel, progressOnlyPanel, controlsPanel);
        }

        if (progressDisplay == null)
        {
            progressDisplay = GetComponent<VRMissionProgressDisplay>();
        }

        if (progressDisplay != null)
        {
            progressDisplay.Configure(progressText, useCompactProgressText);
        }
    }

    private GameObject GetGameplayProgressPanel()
    {
        return progressOnlyPanel != null ? progressOnlyPanel : instructionPanel;
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

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private void SetStartPanelButtonsInteractable(bool interactable)
    {
        if (panelSet != null)
        {
            panelSet.SetStartButtonsInteractable(interactable);
            return;
        }

        if (startPanel == null)
        {
            return;
        }

        foreach (Button button in startPanel.GetComponentsInChildren<Button>(true))
        {
            button.interactable = interactable;
        }
    }

    private static GameObject FindSceneObject(string objectName)
    {
        return FindObjectsOfType<Transform>(true)
            .FirstOrDefault(item => item.name == objectName)
            ?.gameObject;
    }
}
