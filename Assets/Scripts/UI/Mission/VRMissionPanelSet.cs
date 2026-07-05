using UnityEngine;
using UnityEngine.UI;

public sealed class VRMissionPanelSet : MonoBehaviour
{
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private GameObject progressPanel;
    [SerializeField] private GameObject controlsPanel;

    public GameObject StartPanel => startPanel;
    public GameObject InstructionPanel => instructionPanel;
    public GameObject ProgressPanel => progressPanel;
    public GameObject ControlsPanel => controlsPanel;

    public void Configure(
        GameObject start,
        GameObject instructions,
        GameObject progress,
        GameObject controls)
    {
        startPanel = start;
        instructionPanel = instructions;
        progressPanel = progress;
        controlsPanel = controls;
    }

    public void ShowStart()
    {
        ShowOnly(startPanel);
        SetStartButtonsInteractable(true);
    }

    public void ShowGameplayProgress()
    {
        ShowOnly(progressPanel != null ? progressPanel : instructionPanel);
    }

    public void ShowControlsOverlay()
    {
        SetPanelActive(startPanel, true);
        SetStartButtonsInteractable(false);
        SetPanelActive(controlsPanel, true);
    }

    public void CloseControlsToStart()
    {
        SetPanelActive(controlsPanel, false);
        SetPanelActive(startPanel, true);
        SetStartButtonsInteractable(true);
    }

    public void ShowOnly(GameObject activePanel)
    {
        SetPanelActive(startPanel, activePanel == startPanel);
        SetPanelActive(instructionPanel, activePanel == instructionPanel);
        SetPanelActive(progressPanel, activePanel == progressPanel);
        SetPanelActive(controlsPanel, activePanel == controlsPanel);

        if (activePanel == startPanel)
        {
            SetStartButtonsInteractable(true);
        }
    }

    public void SetStartButtonsInteractable(bool interactable)
    {
        if (startPanel == null)
        {
            return;
        }

        foreach (Button button in startPanel.GetComponentsInChildren<Button>(true))
        {
            button.interactable = interactable;
        }
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }
}
